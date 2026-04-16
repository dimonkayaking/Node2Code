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
using CustomVisualScripting.Runtime.Execution;
using VisualScripting.Core.Models;
using CustomToolbar = CustomVisualScripting.Windows.Views.ToolbarView;

namespace CustomVisualScripting.Editor.Windows
{
    public class VisualScriptingWindow : EditorWindow
    {
        public static VisualScriptingWindow ActiveWindow { get; private set; }
        private const float MinNodeWidth = 220f;
        private const float MinNodeHeight = 120f;
        private const float AutoLayoutSpacingX = 280f;
        private const float AutoLayoutSpacingY = 180f;
        private const float AutoLayoutColumnGap = 60f;
        private const float AutoLayoutRowGap = 40f;
        private const float OverlapResolveMargin = 24f;
        private const float BoundsSyncEpsilon = 0.5f;

        private CompleteGraphData _currentGraph;
        private BaseGraph _internalGraph;
        private BaseGraphView _graphView;
        private VisualElement _graphContainer;
        
        private CodeEditorView _codeEditor;
        private CustomToolbar _toolbar;
        private ErrorPanel _errorPanel;
        private ConsoleView _consoleView;
        
        private string _currentFilePath;
        private bool _hasUnsavedChanges = false;
        
        private CSharpProcessRunner _csharpRunner;
        private bool _forceAutoLayoutNextUpdate;
        
        [MenuItem("Tools/Visual Scripting")]
        public static void OpenWindow()
        {
            var window = GetWindow<VisualScriptingWindow>();
            window.titleContent = new GUIContent("Visual Scripting");
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
            EditorApplication.delayCall += () =>
            {
                if (_consoleView != null)
                {
                    _consoleView.AddMessage(message, type);
                }

                if (_toolbar != null && type == LogType.Error)
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
        
        private void CleanupGraph()
        {
            if (_graphView != null)
            {
                _graphView.graphViewChanged -= OnGraphViewChanged;
                _graphView.Dispose();
                _graphView = null;
            }
            
            if (_internalGraph != null)
            {
                DestroyImmediate(_internalGraph);
                _internalGraph = null;
            }
        }
        
        private void CreateGUI()
        {
            _currentGraph = new CompleteGraphData();
            _hasUnsavedChanges = false;
            
            var root = rootVisualElement;
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Plugins/CustomVisualScripting/Windows/Styles/WindowStyles.uss");
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }
            
            _toolbar = new CustomToolbar();
            _toolbar.ParseButton.clicked += OnParse;
            _toolbar.GenerateButton.clicked += OnGenerate;
            _toolbar.RunButton.clicked += OnRun;
            _toolbar.StopButton.clicked += OnStop;
            _toolbar.SaveButton.clicked += OnSave;
            _toolbar.SaveAsButton.clicked += OnSaveAs;
            _toolbar.LoadButton.clicked += OnLoad;
            _toolbar.ClearButton.clicked += OnClear;
            root.Add(_toolbar);
            
            var splitView = new TwoPaneSplitView(0, 350, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            
            _codeEditor = new CodeEditorView();
            splitView.Add(_codeEditor);
            
            _graphContainer = new VisualElement();
            _graphContainer.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            _graphContainer.style.flexGrow = 1;
            
            splitView.Add(_graphContainer);
            root.Add(splitView);
            
            _errorPanel = new ErrorPanel();
            root.Add(_errorPanel);
            
            _consoleView = new ConsoleView();
            _consoleView.style.marginTop = 5;
            root.Add(_consoleView);
            
            _toolbar.SetStatusNormal("Готов к работе");
            
            UpdateGraphView();
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
            
            RecreateGraphView();
            
            _toolbar.SetStatusSuccess($"Создано нод: {result.Graph.Nodes.Count}, связей: {result.Graph.Edges.Count}");
        }
        
        private void RecreateGraphView()
        {
            CleanupGraph();
            _graphContainer.Clear();
            UpdateGraphView();
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
        
        private void UpdateGraphView()
        {
            _graphContainer.Clear();
            
            try
            {
                _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
                var nodeMap = new Dictionary<string, BaseNode>();
                
                if (_currentGraph?.LogicGraph?.Nodes != null)
                {
                    foreach (var nodeData in _currentGraph.LogicGraph.Nodes)
                    {
                        var node = CreateNodeFromData(nodeData);
                        if (node != null)
                        {
                            node.NodeId = nodeData.Id;
                            node.InitializeFromData(nodeData);
                            if (node.GUID != node.NodeId)
                                node.SetGUID(node.NodeId);
                            
                            if (node is IntNode intNode && int.TryParse(nodeData.Value, out int intVal))
                                intNode.intValue = intVal;
                            else if (node is FloatNode floatNode && float.TryParse(nodeData.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                                floatNode.floatValue = floatVal;
                            else if (node is BoolNode boolNode && bool.TryParse(nodeData.Value, out bool boolVal))
                                boolNode.boolValue = boolVal;
                            else if (node is StringNode stringNode)
                                stringNode.stringValue = nodeData.Value;

                            if (node is IfNode ifNode)
                            {
                                ifNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new VisualScripting.Core.Models.GraphData();
                                ifNode.bodySubGraph = nodeData.BodySubGraph ?? new VisualScripting.Core.Models.GraphData();
                            }
                            else if (node is ElseNode elseNode)
                            {
                                elseNode.bodySubGraph = nodeData.BodySubGraph ?? new VisualScripting.Core.Models.GraphData();
                            }
                            else if (node is ForNode forNode)
                            {
                                forNode.initSubGraph = nodeData.InitSubGraph ?? new VisualScripting.Core.Models.GraphData();
                                forNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new VisualScripting.Core.Models.GraphData();
                                forNode.incrementSubGraph = nodeData.IncrementSubGraph ?? new VisualScripting.Core.Models.GraphData();
                                forNode.bodySubGraph = nodeData.BodySubGraph ?? new VisualScripting.Core.Models.GraphData();
                            }
                            else if (node is WhileNode whileNode)
                            {
                                whileNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new VisualScripting.Core.Models.GraphData();
                                whileNode.bodySubGraph = nodeData.BodySubGraph ?? new VisualScripting.Core.Models.GraphData();
                            }
                            
                            _internalGraph.AddNode(node);
                            nodeMap[nodeData.Id] = node;
                        }
                    }
                }
                
                _graphView = new BaseGraphView(this);
                _graphView.Initialize(_internalGraph);
                _graphView.style.flexGrow = 1;
                _graphView.graphViewChanged += OnGraphViewChanged;
                
                if (_currentGraph?.LogicGraph?.Edges != null && nodeMap.Count > 0)
                {
                    foreach (var edgeData in _currentGraph.LogicGraph.Edges)
                    {
                        // Removing execution port filter
                        
                        if (!nodeMap.TryGetValue(edgeData.FromNodeId, out var fromNode)) continue;
                        if (!nodeMap.TryGetValue(edgeData.ToNodeId, out var toNode)) continue;
                        
                        if (!_graphView.nodeViewsPerNode.TryGetValue(fromNode, out var fromNodeView)) continue;
                        if (!_graphView.nodeViewsPerNode.TryGetValue(toNode, out var toNodeView)) continue;
                        
                        Debug.Log($"[VS] Восстанавливаем связь: {edgeData.FromNodeId}.{edgeData.FromPort} → {edgeData.ToNodeId}.{edgeData.ToPort}");
                        
                        var fromPort = fromNodeView.outputPortViews.FirstOrDefault(p => IsPortMatchForStorage(p, edgeData.FromPort));
                        var toPort = toNodeView.inputPortViews.FirstOrDefault(p => IsPortMatchForStorage(p, edgeData.ToPort));

                        if (fromPort == null)
                        {
                            Debug.LogWarning($"[VS] Не найден выходной порт: '{edgeData.FromPort}' в ноде {fromNode.GetType().Name}. Доступные: {string.Join(", ", fromNodeView.outputPortViews.Select(p => p.fieldName))}");
                            continue;
                        }
                        if (toPort == null)
                        {
                            Debug.LogWarning($"[VS] Не найден входной порт: '{edgeData.ToPort}' в ноде {toNode.GetType().Name}. Доступные: {string.Join(", ", toNodeView.inputPortViews.Select(p => p.fieldName))}");
                            continue;
                        }

                        if (fromPort.direction != Direction.Output || toPort.direction != Direction.Input)
                        {
                            Debug.LogWarning($"[VS] Пропуск связи из-за направления портов: {fromNode.GetType().Name}.{fromPort.fieldName}({fromPort.direction}) -> {toNode.GetType().Name}.{toPort.fieldName}({toPort.direction})");
                            continue;
                        }
                        
                        bool alreadyConnected = false;
                        foreach (var existingEdge in _graphView.edgeViews)
                        {
                            if (existingEdge.output == fromPort && existingEdge.input == toPort)
                            {
                                alreadyConnected = true;
                                break;
                            }
                        }
                        
                        if (!alreadyConnected)
                        {
                            _graphView.Connect(toPort, fromPort);
                            Debug.Log($"[VS] Связь создана: {edgeData.FromNodeId}.{edgeData.FromPort} → {edgeData.ToNodeId}.{edgeData.ToPort}");
                        }
                    }
                }
                
                if (_currentGraph?.VisualNodes != null)
                {
                    foreach (var nodeView in _graphView.nodeViews)
                    {
                        if (nodeView.nodeTarget is CustomBaseNode customNode)
                        {
                            var visualNode = _currentGraph.VisualNodes.FirstOrDefault(v => v.NodeId == customNode.NodeId);
                            if (visualNode != null)
                            {
                                nodeView.SetPosition(new Rect(visualNode.Position, Vector2.zero));
                            }
                        }
                    }
                }

                ConfigureNodeViewSizing(_graphView.nodeViews);
                if (_forceAutoLayoutNextUpdate)
                {
                    ApplyDagAutoLayout(_graphView.nodeViews);
                    ResolveOverlaps(_graphView.nodeViews);
                    _forceAutoLayoutNextUpdate = false;
                }
                else
                {
                    AutoLayoutIfNeeded();
                }

                SyncNodeBoundsToLayout(_graphView.nodeViews);
                _graphView.schedule.Execute(() =>
                {
                    if (_graphView?.nodeViews == null)
                        return;
                    SyncNodeBoundsToLayout(_graphView.nodeViews);
                }).ExecuteLater(0);
                
                _graphView.UpdateViewTransform(Vector3.zero, Vector3.one);
                _graphView.FrameAll();
                
                _graphContainer.Add(_graphView);
                
                int nodeCount = _internalGraph.nodes.Count;
                _toolbar.SetStatusSuccess($"Граф готов — {nodeCount} нод");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VS] Ошибка создания графа: {e.Message}\n{e.StackTrace}");
                ShowTextualGraph();
            }
        }
        
        private void ShowTextualGraph()
        {
            var info = new VisualElement();
            info.style.marginTop = 10;
            info.style.marginLeft = 10;
            info.style.flexGrow = 1;
            
            var label = new Label($"Граф: {_currentGraph.LogicGraph.Nodes.Count} нод, {_currentGraph.LogicGraph.Edges.Count} связей");
            label.style.color = Color.white;
            label.style.fontSize = 14;
            info.Add(label);
            
            _graphContainer.Add(info);
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
        
        private void OnDestroy()
        {
            if (ReferenceEquals(ActiveWindow, this))
                ActiveWindow = null;
            if (_hasUnsavedChanges && _currentGraph != null && _currentGraph.LogicGraph.Nodes.Count > 0)
            {
                bool save = EditorUtility.DisplayDialog(
                    "Несохранённые изменения",
                    "Хотите сохранить граф перед закрытием?",
                    "Сохранить",
                    "Не сохранять"
                );
                
                if (save)
                {
                    OnSave();
                }
            }
            
            CleanupGraph();
        }

        private void ConfigureNodeViewSizing(IEnumerable<BaseNodeView> nodeViews)
        {
            foreach (var nodeView in nodeViews)
            {
                if (nodeView == null)
                    continue;

                nodeView.capabilities |= Capabilities.Resizable;
                nodeView.style.minWidth = MinNodeWidth;
                nodeView.style.minHeight = MinNodeHeight;

                var rect = nodeView.GetPosition();
                var width = Mathf.Max(rect.width, MinNodeWidth);
                var height = Mathf.Max(rect.height, MinNodeHeight);
                nodeView.SetPosition(new Rect(rect.x, rect.y, width, height));
                nodeView.UnregisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
                nodeView.RegisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_graphView?.nodeViews != null)
                ConfigureNodeViewSizing(_graphView.nodeViews);
            return change;
        }

        private void AutoLayoutIfNeeded()
        {
            if (_graphView == null || _graphView.nodeViews == null || _graphView.nodeViews.Count == 0)
                return;

            bool hasSavedPositions = _currentGraph?.VisualNodes != null &&
                                     _currentGraph.VisualNodes.Count >= _graphView.nodeViews.Count;
            bool hasMeaningfulSaved = hasSavedPositions && HasMeaningfulSavedPositions(_currentGraph.VisualNodes);

            bool needLayout = !hasMeaningfulSaved || HasHeavyOverlap(_graphView.nodeViews);
            if (!needLayout)
                return;

            ApplyDagAutoLayout(_graphView.nodeViews);
            ResolveOverlaps(_graphView.nodeViews);
        }

        private void ApplyDagAutoLayout(IReadOnlyList<BaseNodeView> nodeViews, float spacingX = AutoLayoutSpacingX, float spacingY = AutoLayoutSpacingY)
        {
            var customViews = nodeViews
                .Where(v => v?.nodeTarget is CustomBaseNode)
                .ToList();
            if (customViews.Count == 0)
                return;

            var nodeById = new Dictionary<string, BaseNodeView>(StringComparer.Ordinal);
            foreach (var view in customViews)
            {
                var node = view.nodeTarget as CustomBaseNode;
                if (node != null && !string.IsNullOrEmpty(node.NodeId))
                    nodeById[node.NodeId] = view;
            }

            var outgoing = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var incoming = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var incomingCount = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var nodeId in nodeById.Keys)
            {
                outgoing[nodeId] = new HashSet<string>(StringComparer.Ordinal);
                incoming[nodeId] = new HashSet<string>(StringComparer.Ordinal);
                incomingCount[nodeId] = 0;
            }

            if (_currentGraph?.LogicGraph?.Edges != null)
            {
                foreach (var edge in _currentGraph.LogicGraph.Edges)
                {
                    if (edge == null || string.IsNullOrEmpty(edge.FromNodeId) || string.IsNullOrEmpty(edge.ToNodeId))
                        continue;
                    if (!nodeById.ContainsKey(edge.FromNodeId) || !nodeById.ContainsKey(edge.ToNodeId))
                        continue;
                    if (edge.FromNodeId == edge.ToNodeId)
                        continue;

                    if (outgoing[edge.FromNodeId].Add(edge.ToNodeId))
                    {
                        incoming[edge.ToNodeId].Add(edge.FromNodeId);
                        incomingCount[edge.ToNodeId]++;
                    }
                }
            }

            var nodeTypeById = new Dictionary<string, NodeType>(StringComparer.Ordinal);
            if (_currentGraph?.LogicGraph?.Nodes != null)
            {
                foreach (var n in _currentGraph.LogicGraph.Nodes)
                {
                    if (n != null && !string.IsNullOrEmpty(n.Id))
                        nodeTypeById[n.Id] = n.Type;
                }
            }

            var inDegreeOriginal = incomingCount.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
            var depthById = new Dictionary<string, int>(StringComparer.Ordinal);
            var rootIds = incomingCount
                .Where(kv => kv.Value == 0)
                .Select(kv => kv.Key)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            foreach (var rootId in rootIds)
                depthById[rootId] = 0;
            var queue = new Queue<string>(rootIds);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int currentDepth = depthById.TryGetValue(current, out var d) ? d : 0;

                var sortedNext = outgoing[current].OrderBy(id => id, StringComparer.Ordinal);
                foreach (var next in sortedNext)
                {
                    int nextDepth = currentDepth + 1;
                    if (!depthById.TryGetValue(next, out var existingDepth) || nextDepth > existingDepth)
                        depthById[next] = nextDepth;

                    incomingCount[next]--;
                    if (incomingCount[next] == 0)
                        queue.Enqueue(next);
                }
            }

            // Fallback for cycles/unresolved nodes: place them into deterministic extra layers.
            int maxDepth = depthById.Count == 0 ? 0 : depthById.Values.Max();
            var unresolvedIds = nodeById.Keys
                .Where(id => !depthById.ContainsKey(id))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            for (int i = 0; i < unresolvedIds.Count; i++)
                depthById[unresolvedIds[i]] = maxDepth + 1 + i;

            var layers = depthById
                .GroupBy(kv => kv.Value)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(kv => kv.Key).ToList());

            var laneCache = new Dictionary<string, int>(StringComparer.Ordinal);
            int GetBranchLane(string nodeId, HashSet<string> visiting = null)
            {
                if (laneCache.TryGetValue(nodeId, out var cached))
                    return cached;

                if (nodeTypeById.TryGetValue(nodeId, out var type))
                {
                    if (type == NodeType.FlowIf)
                    {
                        laneCache[nodeId] = 1;
                        return 1;
                    }
                    if (type == NodeType.FlowElse)
                    {
                        laneCache[nodeId] = 2;
                        return 2;
                    }
                }

                visiting ??= new HashSet<string>(StringComparer.Ordinal);
                if (!visiting.Add(nodeId))
                    return 0;

                int lane = 0;
                foreach (var parent in incoming[nodeId])
                {
                    int parentLane = GetBranchLane(parent, visiting);
                    if (parentLane > lane)
                        lane = parentLane;
                }
                visiting.Remove(nodeId);
                laneCache[nodeId] = lane;
                return lane;
            }

            int TypePriority(string nodeId)
            {
                if (!nodeTypeById.TryGetValue(nodeId, out var type))
                    return 50;

                switch (type)
                {
                    case NodeType.LiteralBool:
                    case NodeType.LiteralInt:
                    case NodeType.LiteralFloat:
                    case NodeType.LiteralString:
                        return 10;

                    case NodeType.MathAdd:
                    case NodeType.MathSubtract:
                    case NodeType.MathMultiply:
                    case NodeType.MathDivide:
                    case NodeType.MathModulo:
                    case NodeType.CompareEqual:
                    case NodeType.CompareGreater:
                    case NodeType.CompareLess:
                    case NodeType.CompareNotEqual:
                    case NodeType.CompareGreaterOrEqual:
                    case NodeType.CompareLessOrEqual:
                    case NodeType.LogicalAnd:
                    case NodeType.LogicalOr:
                    case NodeType.LogicalNot:
                    case NodeType.MathfAbs:
                    case NodeType.MathfMax:
                    case NodeType.MathfMin:
                    case NodeType.IntParse:
                    case NodeType.FloatParse:
                    case NodeType.ToStringConvert:
                    case NodeType.UnityVector3:
                    case NodeType.UnityGetPosition:
                        return 20;

                    case NodeType.FlowIf:
                    case NodeType.FlowElse:
                    case NodeType.FlowFor:
                    case NodeType.FlowWhile:
                        return 30;

                    case NodeType.ConsoleWriteLine:
                    case NodeType.DebugLog:
                    case NodeType.UnitySetPosition:
                        return 40;

                    default:
                        return 50;
                }
            }

            foreach (var layer in layers.Values)
            {
                // Stable ordering inside a layer:
                // 1) branch lanes (if/else in parallel rows),
                // 2) semantic node type priority (source -> op -> result),
                // 3) graph degree hints,
                // 4) NodeId as deterministic fallback.
                layer.Sort((a, b) =>
                {
                    int laneCmp = GetBranchLane(a).CompareTo(GetBranchLane(b));
                    if (laneCmp != 0) return laneCmp;

                    int typeCmp = TypePriority(a).CompareTo(TypePriority(b));
                    if (typeCmp != 0) return typeCmp;

                    int inCmp = inDegreeOriginal[a].CompareTo(inDegreeOriginal[b]);
                    if (inCmp != 0) return inCmp;

                    int outCmp = outgoing[b].Count.CompareTo(outgoing[a].Count);
                    if (outCmp != 0) return outCmp;

                    return StringComparer.Ordinal.Compare(a, b);
                });
            }

            float columnGap = Mathf.Max(AutoLayoutColumnGap, spacingX * 0.2f);
            float rowGap = Mathf.Max(AutoLayoutRowGap, spacingY * 0.2f);
            const float startX = 40f;
            const float startY = 40f;
            float columnX = startX;
            foreach (var layerEntry in layers)
            {
                var ids = layerEntry.Value;
                float layerMaxWidth = MinNodeWidth;
                float rowY = startY;
                for (int row = 0; row < ids.Count; row++)
                {
                    var view = nodeById[ids[row]];
                    var rect = view.GetPosition();
                    float width = Mathf.Max(rect.width, MinNodeWidth);
                    float height = Mathf.Max(rect.height, MinNodeHeight);
                    layerMaxWidth = Mathf.Max(layerMaxWidth, width);
                    view.SetPosition(new Rect(
                        columnX,
                        rowY,
                        width,
                        height));
                    rowY += height + rowGap;
                }
                columnX += layerMaxWidth + columnGap;
            }
        }

        private void ResolveOverlaps(IReadOnlyList<BaseNodeView> nodeViews)
        {
            var customViews = nodeViews
                .Where(v => v?.nodeTarget is CustomBaseNode)
                .ToList();
            if (customViews.Count <= 1)
                return;

            const int maxPasses = 4;
            for (int pass = 0; pass < maxPasses; pass++)
            {
                bool movedAny = false;
                for (int i = 0; i < customViews.Count; i++)
                {
                    var aView = customViews[i];
                    var a = aView.GetPosition();
                    for (int j = i + 1; j < customViews.Count; j++)
                    {
                        var bView = customViews[j];
                        var b = bView.GetPosition();
                        if (!a.Overlaps(b))
                            continue;

                        float moveX = Mathf.Max(0f, a.xMax - b.xMin) + OverlapResolveMargin;
                        float moveY = Mathf.Max(0f, a.yMax - b.yMin) + OverlapResolveMargin;
                        if (moveX <= 0f && moveY <= 0f)
                            continue;

                        if (moveX <= moveY)
                            b.x += moveX;
                        else
                            b.y += moveY;

                        bView.SetPosition(b);
                        movedAny = true;
                    }
                }

                if (!movedAny)
                    return;
            }
        }

        private void OnNodeGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt?.currentTarget is not BaseNodeView nodeView)
                return;

            nodeView.schedule.Execute(() => SyncNodeBoundsToLayout(nodeView)).ExecuteLater(0);
        }

        private static void SyncNodeBoundsToLayout(IReadOnlyList<BaseNodeView> nodeViews)
        {
            if (nodeViews == null)
                return;

            foreach (var nodeView in nodeViews)
                SyncNodeBoundsToLayout(nodeView);
        }

        private static void SyncNodeBoundsToLayout(BaseNodeView nodeView)
        {
            if (nodeView == null)
                return;

            var rect = nodeView.GetPosition();
            float resolvedWidth = nodeView.resolvedStyle.width;
            float resolvedHeight = nodeView.resolvedStyle.height;
            float layoutWidth = nodeView.layout.width;
            float layoutHeight = nodeView.layout.height;
            float width = Mathf.Max(rect.width, MinNodeWidth, resolvedWidth, layoutWidth);
            float height = Mathf.Max(rect.height, MinNodeHeight, resolvedHeight, layoutHeight);
            if (float.IsNaN(width) || float.IsInfinity(width) ||
                float.IsNaN(height) || float.IsInfinity(height))
                return;

            if (Mathf.Abs(width - rect.width) <= BoundsSyncEpsilon &&
                Mathf.Abs(height - rect.height) <= BoundsSyncEpsilon)
                return;

            nodeView.SetPosition(new Rect(rect.x, rect.y, width, height));
            nodeView.RefreshPorts();
            nodeView.RefreshExpandedState();
        }

        private static bool HasHeavyOverlap(IReadOnlyList<BaseNodeView> nodeViews)
        {
            if (nodeViews.Count <= 1)
                return false;

            int overlaps = 0;
            for (int i = 0; i < nodeViews.Count; i++)
            {
                var a = nodeViews[i].GetPosition();
                for (int j = i + 1; j < nodeViews.Count; j++)
                {
                    var b = nodeViews[j].GetPosition();
                    if (a.Overlaps(b))
                        overlaps++;
                }
            }

            return overlaps >= Math.Max(1, nodeViews.Count / 3);
        }

        private static bool HasMeaningfulSavedPositions(IReadOnlyList<VisualNodeData> visualNodes)
        {
            if (visualNodes == null || visualNodes.Count == 0)
                return false;

            var unique = new HashSet<string>();
            foreach (var vn in visualNodes)
            {
                var x = Mathf.RoundToInt(vn.Position.x);
                var y = Mathf.RoundToInt(vn.Position.y);
                unique.Add($"{x}:{y}");
            }

            return unique.Count > Math.Max(1, visualNodes.Count / 3);
        }
    }
}