using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Integration;
using CustomVisualScripting.Integration.Models;
using CustomVisualScripting.Windows.Views;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor.Nodes.Literals;
using CustomVisualScripting.Editor.Nodes.Math;
using CustomVisualScripting.Editor.Nodes.Comparison;
using CustomVisualScripting.Editor.Nodes.Flow;
using CustomVisualScripting.Editor.Nodes.Debug;
using CustomVisualScripting.Editor.Nodes.Logic;
using CustomVisualScripting.Editor.Nodes.Conversion;
using CustomVisualScripting.Editor.Nodes.Unity;
using CustomVisualScripting.Editor.Nodes.Views;
using CustomVisualScripting.Runtime.Execution;
using VisualScripting.Core.Models;
using CustomToolbar = CustomVisualScripting.Windows.Views.ToolbarView;

namespace CustomVisualScripting.Editor.Windows
{
    public partial class VisualScriptingWindow : EditorWindow
    {
        public static VisualScriptingWindow ActiveWindow { get; private set; }
        private const float MinNodeWidth = 220f;
        private const float MinNodeHeight = 120f;
        private const float AutoLayoutSpacingX = 280f;
        private const float AutoLayoutSpacingY = 180f;
        private const float AutoLayoutColumnGap = 60f;
        private const float AutoLayoutRowGap = 40f;
        private const float OverlapResolveMargin = 24f;

        private CompleteGraphData _currentGraph;
        private BaseGraph _internalGraph;
        private FilteredCreateMenuBaseGraphView _graphView;
        private VisualElement _graphContainer;
        
        private CodeEditorView _codeEditor;
        private CustomToolbar _toolbar;
        private ErrorPanel _errorPanel;
        private ConsoleView _consoleView;
        
        private string _currentFilePath;
        private bool _hasUnsavedChanges = false;
        
        private CSharpProcessRunner _csharpRunner;
        private bool _forceAutoLayoutNextUpdate;
        private bool _collapseFlowSubspacesOnNextRebuild;
        
        [MenuItem("Tools/Node2Code")]
        public static void OpenWindow()
        {
            var window = GetWindow<VisualScriptingWindow>();
            window.titleContent = new GUIContent("Node2Code");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            ActiveWindow = this;
            ParserBridge.Initialize();
            GeneratorBridge.Initialize();
            Application.logMessageReceived += OnLogMessageReceived;
            _csharpRunner = new CSharpProcessRunner();
            _csharpRunner.OnOutput += OnCSharpRunnerOutput;
            
            _currentGraph = new CompleteGraphData();
            _hasUnsavedChanges = false;
        }
        
        private void OnDisable()
        {
            if (ReferenceEquals(ActiveWindow, this))
                ActiveWindow = null;
            Application.logMessageReceived -= OnLogMessageReceived;
            if (_csharpRunner != null)
            {
                _csharpRunner.OnOutput -= OnCSharpRunnerOutput;
                _csharpRunner.Dispose();
                _csharpRunner = null;
            }
            CleanupGraph();
        }

        private void OnCSharpRunnerOutput(string message, LogType type)
        {
            var cleaned = message;
            var unityRelayType = type;
            var shouldRelayToUnity = false;

            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith(CSharpProcessRunner.UnityDebugLogErrorMarker, StringComparison.Ordinal))
                {
                    cleaned = message.Substring(CSharpProcessRunner.UnityDebugLogErrorMarker.Length);
                    unityRelayType = LogType.Error;
                    shouldRelayToUnity = true;
                }
                else if (message.StartsWith(CSharpProcessRunner.UnityDebugLogWarningMarker, StringComparison.Ordinal))
                {
                    cleaned = message.Substring(CSharpProcessRunner.UnityDebugLogWarningMarker.Length);
                    unityRelayType = LogType.Warning;
                    shouldRelayToUnity = true;
                }
                else if (message.StartsWith(CSharpProcessRunner.UnityDebugLogMarker, StringComparison.Ordinal))
                {
                    cleaned = message.Substring(CSharpProcessRunner.UnityDebugLogMarker.Length);
                    unityRelayType = LogType.Log;
                    shouldRelayToUnity = true;
                }
            }

            if (shouldRelayToUnity)
            {
                switch (unityRelayType)
                {
                    case LogType.Error:
                        UnityEngine.Debug.LogError(cleaned);
                        break;
                    case LogType.Warning:
                        UnityEngine.Debug.LogWarning(cleaned);
                        break;
                    default:
                        UnityEngine.Debug.Log(cleaned);
                        break;
                }
            }

            EditorApplication.delayCall += () =>
            {
                if (_consoleView != null)
                {
                    _consoleView.AddMessage(cleaned, unityRelayType);
                }

                if (_toolbar != null && type == LogType.Error && !shouldRelayToUnity)
                {
                    _toolbar.SetStatusError("Ошибка выполнения C#");
                }
            };
        }
        
        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (_consoleView != null)
            {
                _consoleView.AddMessage(condition, type);
            }
        }
        
        private void OnParse()
        {
            _toolbar.SetStatusWarning("Парсинг...");
            
            var result = ParserBridge.Parse(_codeEditor.Code);
            
            if (result.HasErrors)
            {
                _errorPanel.ShowErrors(result.Errors);
                _toolbar.SetStatusError($"Ошибок: {result.Errors.Count}");
                return;
            }
            
            _errorPanel.Clear();
            
            _currentGraph = new CompleteGraphData();
            _currentGraph = GraphConverter.LogicToComplete(result.Graph, _currentGraph);
            _hasUnsavedChanges = true;
            _forceAutoLayoutNextUpdate = true;
            _collapseFlowSubspacesOnNextRebuild = true;
            
            RecreateGraphView();
            
            _toolbar.SetStatusSuccess($"Создано нод: {result.Graph.Nodes.Count}, связей: {result.Graph.Edges.Count}");
        }
        
        private void OnGenerate()
        {
            _toolbar.SetStatusWarning("Генерация...");
            
            SyncFullGraphFromView();
            
            string code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;
            
            _toolbar.SetStatusSuccess("Код сгенерирован");
        }
        
        private async void OnRun()
        {
            if (_currentGraph?.LogicGraph == null || _currentGraph.LogicGraph.Nodes.Count == 0)
            {
                _toolbar.SetStatusError("Нет графа для выполнения");
                return;
            }

            if (_csharpRunner == null)
            {
                _toolbar.SetStatusError("Runner не инициализирован");
                return;
            }

            if (_csharpRunner.IsRunning)
            {
                _toolbar.SetStatusWarning("Выполнение уже запущено");
                return;
            }
            
            _toolbar.SetRunMode(true);
            _toolbar.SetStatusWarning("Выполнение...");
            SyncFullGraphFromView();
            var code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;
            
            try
            {
                var exitCode = await _csharpRunner.RunAsync(code);
                EditorApplication.delayCall += () =>
                {
                    _toolbar.SetRunMode(false);
                    _toolbar.SetStatusSuccess(exitCode == 0
                        ? "Выполнение завершено"
                        : $"Выполнение завершено с ошибкой ({exitCode})");
                };
            }
            catch (Exception e)
            {
                EditorApplication.delayCall += () =>
                {
                    _toolbar.SetRunMode(false);
                    _toolbar.SetStatusError($"Ошибка: {e.Message}");
                    Debug.LogError($"[VS] Ошибка выполнения: {e.Message}");
                };
            }
        }
        
        private void OnStop()
        {
            _csharpRunner?.Stop();
            _toolbar.SetRunMode(false);
            _toolbar.SetStatusNormal("Выполнение остановлено");
        }
        
        private void OnSave()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                OnSaveAs();
                return;
            }

            SyncFullGraphFromView();
            string code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;

            try
            {
                File.WriteAllText(_currentFilePath, code);
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(_currentFilePath)}");
                _hasUnsavedChanges = false;
            }
            catch (Exception e)
            {
                _toolbar.SetStatusError($"Ошибка сохранения: {e.Message}");
            }
        }
        
        private void OnSaveAs()
        {
            string defaultName = !string.IsNullOrEmpty(_currentFilePath)
                ? Path.GetFileName(_currentFilePath)
                : "Script.cs";
            
            string path = EditorUtility.SaveFilePanel("Сохранить код как", Application.dataPath, defaultName, "cs");
            if (string.IsNullOrEmpty(path)) return;
            
            SyncFullGraphFromView();
            string code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;
            _currentFilePath = path;

            try
            {
                File.WriteAllText(path, code);
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(path)}");
                _hasUnsavedChanges = false;
            }
            catch (Exception e)
            {
                _toolbar.SetStatusError($"Ошибка сохранения: {e.Message}");
            }
        }
        
        private void OnLoad()
        {
            string path = EditorUtility.OpenFilePanel("Загрузить C# код", Application.dataPath, "cs");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var code = File.ReadAllText(path);
                _currentFilePath = path;
                _codeEditor.Code = code;

                var result = ParserBridge.Parse(code);
                if (result.HasErrors)
                {
                    _errorPanel.ShowErrors(result.Errors);
                    _toolbar.SetStatusError($"Ошибок: {result.Errors.Count}");
                    return;
                }

                _errorPanel.Clear();
                _currentGraph = new CompleteGraphData();
                _currentGraph = GraphConverter.LogicToComplete(result.Graph, _currentGraph);
                _hasUnsavedChanges = false;
                _collapseFlowSubspacesOnNextRebuild = true;
                RecreateGraphView();
                _toolbar.SetStatusSuccess($"Загружено и распарсено: {Path.GetFileName(path)}");
            }
            catch (Exception e)
            {
                _toolbar.SetStatusError($"Ошибка загрузки: {e.Message}");
            }
        }
        
        private void OnClear()
        {
            _codeEditor.Clear();
            _currentGraph = new CompleteGraphData();
            _errorPanel.Clear();
            _currentFilePath = null;
            _hasUnsavedChanges = false;
            _collapseFlowSubspacesOnNextRebuild = false;
            RecreateGraphView();
            _toolbar.SetStatusNormal("Очищено");
        }
        
        private void SyncFullGraphFromView()
        {
            if (_graphView == null || _internalGraph == null) return;

            _currentGraph.LogicGraph.Nodes.Clear();
            _currentGraph.LogicGraph.Edges.Clear();

            var graphNodes = _internalGraph.nodes.OfType<CustomBaseNode>().ToList();
            var validNodeIds = new HashSet<string>();

            foreach (var customNode in graphNodes)
            {
                var nodeData = customNode.ToNodeData();
                nodeData.Id = customNode.NodeId;
                nodeData.VariableName = customNode.variableName;

                if (customNode is IntNode intNode)
                {
                    nodeData.Value = intNode.intValue.ToString();
                    nodeData.ExpressionOverride = intNode.expressionOverride;
                }
                else if (customNode is FloatNode floatNode)
                {
                    nodeData.Value = floatNode.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    nodeData.ExpressionOverride = floatNode.expressionOverride;
                }
                else if (customNode is BoolNode boolNode)
                {
                    nodeData.Value = boolNode.boolValue.ToString();
                    nodeData.ExpressionOverride = boolNode.expressionOverride;
                }
                else if (customNode is StringNode stringNode)
                {
                    nodeData.Value = stringNode.stringValue;
                    nodeData.ExpressionOverride = stringNode.expressionOverride;
                }
                else if (customNode is ConsoleWriteLineNode cwlNode)
                {
                    nodeData.Value = cwlNode.messageText;
                    nodeData.ValueType = cwlNode.messageValueType;
                }

                if (customNode is IfNode ifNode)
                {
                    nodeData.ConditionSubGraph = ifNode.conditionSubGraph;
                    nodeData.BodySubGraph = ifNode.bodySubGraph;
                }
                else if (customNode is ElseNode elseNode)
                {
                    nodeData.BodySubGraph = elseNode.bodySubGraph;
                }
        else if (customNode is ForNode forNode)
        {
            nodeData.InitSubGraph = forNode.initSubGraph;
            nodeData.ConditionSubGraph = forNode.conditionSubGraph;
            nodeData.IncrementSubGraph = forNode.incrementSubGraph;
            nodeData.BodySubGraph = forNode.bodySubGraph;
        }
                else if (customNode is WhileNode whileNode)
                {
                    nodeData.ConditionSubGraph = whileNode.conditionSubGraph;
                    nodeData.BodySubGraph = whileNode.bodySubGraph;
                }

                _currentGraph.LogicGraph.Nodes.Add(nodeData);
                validNodeIds.Add(customNode.NodeId);
            }

            foreach (var edgeView in _graphView.edgeViews)
            {
                if (edgeView == null) continue;

                var fromPort = edgeView.output as PortView;
                var toPort = edgeView.input as PortView;

                if (fromPort == null || toPort == null) continue;
                if (fromPort.direction != Direction.Output || toPort.direction != Direction.Input) continue;

                var fromNode = fromPort.owner.nodeTarget as CustomBaseNode;
                var toNode = toPort.owner.nodeTarget as CustomBaseNode;

                if (fromNode == null || toNode == null) continue;
                if (!validNodeIds.Contains(fromNode.NodeId) || !validNodeIds.Contains(toNode.NodeId)) continue;

                Debug.Log($"[VS] Сохраняем связь: {fromNode.NodeId}.{fromPort.fieldName} → {toNode.NodeId}.{toPort.fieldName}");

                var canonicalFrom = CanonicalFromPortIdForStorage(fromPort, fromNode, toNode, CanonicalPortIdForStorage(toPort));
                var canonicalTo = CanonicalPortIdForStorage(toPort);
                if (string.IsNullOrEmpty(canonicalFrom) || string.IsNullOrEmpty(canonicalTo))
                    continue;

                _currentGraph.LogicGraph.Edges.Add(new EdgeData
                {
                    FromNodeId = fromNode.NodeId,
                    FromPort = canonicalFrom,
                    ToNodeId = toNode.NodeId,
                    ToPort = canonicalTo
                });
            }

            SaveVisualNodePositions();
            _hasUnsavedChanges = true;
        }
        
        private void SaveVisualNodePositions()
        {
            if (_currentGraph?.VisualNodes == null || _graphView == null || _internalGraph == null)
                return;
            
            _currentGraph.VisualNodes.Clear();

            foreach (var customNode in _internalGraph.nodes.OfType<CustomBaseNode>())
            {
                if (!_graphView.nodeViewsPerNode.TryGetValue(customNode, out var nodeView))
                    continue;

                _currentGraph.VisualNodes.Add(new VisualNodeData
                {
                    NodeId = customNode.NodeId,
                    Position = nodeView.GetPosition().position,
                    IsCollapsed = false
                });
            }
        }
        
        private CustomBaseNode CreateNodeFromData(NodeData data)
        {
            if (data == null) return null;
            
            switch (data.Type)
            {
                case NodeType.LiteralInt: return new IntNode();
                case NodeType.LiteralFloat: return new FloatNode();
                case NodeType.LiteralBool: return new BoolNode();
                case NodeType.LiteralString: return new StringNode();
                case NodeType.MathAdd: return new AddNode();
                case NodeType.MathSubtract: return new SubtractNode();
                case NodeType.MathMultiply: return new MultiplyNode();
                case NodeType.MathDivide: return new DivideNode();
                case NodeType.MathModulo: return new ModuloNode();
                case NodeType.CompareEqual: return new EqualNode();
                case NodeType.CompareNotEqual: return new NotEqualNode();
                case NodeType.CompareGreater: return new GreaterNode();
                case NodeType.CompareGreaterOrEqual: return new GreaterOrEqualNode();
                case NodeType.CompareLess: return new LessNode();
                case NodeType.CompareLessOrEqual: return new LessOrEqualNode();
                case NodeType.LogicalAnd: return new AndNode();
                case NodeType.LogicalOr: return new OrNode();
                case NodeType.LogicalNot: return new NotNode();
                case NodeType.FlowIf: return new IfNode();
                case NodeType.FlowElse: return new ElseNode();
                case NodeType.FlowFor: return new ForNode();
                case NodeType.FlowWhile: return new WhileNode();
                case NodeType.ConsoleWriteLine: return new ConsoleWriteLineNode();
                case NodeType.DebugLog: return new DebugLogNode();
                case NodeType.IntParse: return new IntParseNode();
                case NodeType.FloatParse: return new FloatParseNode();
                case NodeType.ToStringConvert: return new ToStringNode();
                case NodeType.MathfAbs: return new MathfAbsNode();
                case NodeType.MathfMax: return new MathfMaxNode();
                case NodeType.MathfMin: return new MathfMinNode();
                case NodeType.UnityVector3: return new Vector3CreateNode();
                case NodeType.UnityGetPosition: return new GetPositionNode();
                case NodeType.UnitySetPosition: return new SetPositionNode();
                default: return null;
            }
        }

        /// <summary>
        /// Единые имена портов в LogicGraph (GraphProcessor может отличаться регистром fieldName / portName).
        /// </summary>
        private static string CanonicalFromPortIdForStorage(PortView port, CustomBaseNode fromNode, CustomBaseNode toNode, string canonicalToPort)
        {
            return CanonicalPortIdForStorage(port);
        }

        private static bool IsPortMatchForStorage(PortView port, string savedPortId)
        {
            if (port == null || string.IsNullOrWhiteSpace(savedPortId))
                return false;

            var expected = NormalizePortId(savedPortId);
            if (string.IsNullOrEmpty(expected))
                return false;

            var field = NormalizePortId(port.fieldName);
            if (!string.IsNullOrEmpty(field) &&
                string.Equals(field, expected, StringComparison.OrdinalIgnoreCase))
                return true;

            var name = NormalizePortId(port.portName);
            return !string.IsNullOrEmpty(name) &&
                   string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Единые имена портов в LogicGraph (GraphProcessor может отличаться регистром fieldName / portName).
        /// </summary>
        private static string CanonicalPortIdForStorage(PortView port)
        {
            var fn = NormalizePortId(port.fieldName);
            if (!string.IsNullOrEmpty(fn))
                return fn;

            var pn = NormalizePortId(port.portName);
            if (!string.IsNullOrEmpty(pn))
                return pn;

            // Fallback for legacy/unnamed execution ports.
            if (port.direction == Direction.Input)
                return PortIds.ExecIn;
            if (port.direction == Direction.Output)
                return PortIds.ExecOut;

            return "";
        }

        private static string NormalizePortId(string rawPortId)
        {
            return PortIds.Normalize(rawPortId);
        }
        
    }
}