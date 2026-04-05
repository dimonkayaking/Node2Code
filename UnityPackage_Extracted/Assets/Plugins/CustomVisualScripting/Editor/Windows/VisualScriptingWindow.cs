using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
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
using CustomVisualScripting.Editor.Nodes.Variables;
using CustomVisualScripting.Editor.Nodes.Unity;
using VisualScripting.Core.Models;
using CustomToolbar = CustomVisualScripting.Windows.Views.ToolbarView;

namespace CustomVisualScripting.Editor.Windows
{
    public class VisualScriptingWindow : EditorWindow
    {
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
            ParserBridge.Initialize();
            GeneratorBridge.Initialize();
            Application.logMessageReceived += OnLogMessageReceived;
            
            _currentGraph = new CompleteGraphData();
            _hasUnsavedChanges = false;
        }
        
        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            CleanupGraph();
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
            
            _toolbar = new CustomToolbar();
            _toolbar.ParseButton.clicked += OnParse;
            _toolbar.GenerateButton.clicked += OnGenerate;
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
        
        private void OnSave()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                OnSaveAs();
                return;
            }
            
            SyncFullGraphFromView();
            
            if (GraphSaver.SaveToJson(_currentGraph, _currentFilePath))
            {
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(_currentFilePath)}");
                _hasUnsavedChanges = false;
            }
            else
            {
                _toolbar.SetStatusError("Ошибка сохранения");
            }
        }
        
        private void OnSaveAs()
        {
            string defaultName = !string.IsNullOrEmpty(_currentFilePath)
                ? Path.GetFileName(_currentFilePath)
                : "graph.json";
            
            string path = EditorUtility.SaveFilePanel("Сохранить граф как", Application.dataPath, defaultName, "json");
            if (string.IsNullOrEmpty(path)) return;
            
            SyncFullGraphFromView();
            _currentFilePath = path;
            
            if (GraphSaver.SaveToJson(_currentGraph, path))
            {
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(path)}");
                _hasUnsavedChanges = false;
            }
            else
            {
                _toolbar.SetStatusError("Ошибка сохранения");
            }
        }
        
        private void OnLoad()
        {
            string path = EditorUtility.OpenFilePanel("Загрузить граф", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            
            var loaded = GraphSaver.LoadFromJson(path);
            if (loaded != null)
            {
                _currentGraph = loaded;
                _currentFilePath = path;
                _hasUnsavedChanges = false;
                _codeEditor.Code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
                RecreateGraphView();
                _toolbar.SetStatusSuccess($"Загружено: {Path.GetFileName(path)}");
            }
            else
            {
                _toolbar.SetStatusError("Ошибка загрузки");
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
            if (_graphView == null) return;
            
            _currentGraph.LogicGraph.Nodes.Clear();
            _currentGraph.LogicGraph.Edges.Clear();
            
            foreach (var nodeView in _graphView.nodeViews)
            {
                if (nodeView.nodeTarget is CustomBaseNode customNode)
                {
                    var nodeData = customNode.ToNodeData();
                    nodeData.Id = customNode.NodeId;
                    nodeData.VariableName = customNode.variableName;
                    
                    if (customNode is IntNode intNode)
                        nodeData.Value = intNode.intValue.ToString();
                    else if (customNode is FloatNode floatNode)
                        nodeData.Value = floatNode.floatValue.ToString();
                    else if (customNode is BoolNode boolNode)
                        nodeData.Value = boolNode.boolValue.ToString();
                    else if (customNode is StringNode stringNode)
                        nodeData.Value = stringNode.stringValue;
                    
                    _currentGraph.LogicGraph.Nodes.Add(nodeData);
                }
            }
            
            foreach (var edgeView in _graphView.edgeViews)
            {
                if (edgeView == null) continue;
                
                var fromPort = edgeView.output as PortView;
                var toPort = edgeView.input as PortView;
                
                if (fromPort == null || toPort == null) continue;
                
                var fromNode = fromPort.owner.nodeTarget as CustomBaseNode;
                var toNode = toPort.owner.nodeTarget as CustomBaseNode;
                
                if (fromNode == null || toNode == null) continue;
                
                _currentGraph.LogicGraph.Edges.Add(new EdgeData
                {
                    FromNodeId = fromNode.NodeId,
                    FromPort = fromPort.fieldName,
                    ToNodeId = toNode.NodeId,
                    ToPort = toPort.fieldName
                });
            }
            
            SaveVisualNodePositions();
            _hasUnsavedChanges = true;
        }
        
        private void SaveVisualNodePositions()
        {
            if (_currentGraph?.VisualNodes == null || _graphView == null)
                return;
            
            _currentGraph.VisualNodes.Clear();
            
            foreach (var nodeView in _graphView.nodeViews)
            {
                if (nodeView.nodeTarget is CustomBaseNode customNode)
                {
                    _currentGraph.VisualNodes.Add(new VisualNodeData
                    {
                        NodeId = customNode.NodeId,
                        Position = nodeView.GetPosition().position,
                        IsCollapsed = false
                    });
                }
            }
        }
        
        private void UpdateGraphView()
        {
            _graphContainer.Clear();
            
            try
            {
                _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
                
                // Создаём ноды
                if (_currentGraph?.LogicGraph?.Nodes != null)
                {
                    foreach (var nodeData in _currentGraph.LogicGraph.Nodes)
                    {
                        var node = CreateNodeFromData(nodeData);
                        if (node != null)
                        {
                            node.NodeId = nodeData.Id;
                            node.InitializeFromData(nodeData);
                            
                            if (node is IntNode intNode && int.TryParse(nodeData.Value, out int intVal))
                                intNode.intValue = intVal;
                            else if (node is FloatNode floatNode && float.TryParse(nodeData.Value, out float floatVal))
                                floatNode.floatValue = floatVal;
                            else if (node is BoolNode boolNode && bool.TryParse(nodeData.Value, out bool boolVal))
                                boolNode.boolValue = boolVal;
                            else if (node is StringNode stringNode)
                                stringNode.stringValue = nodeData.Value;
                            
                            _internalGraph.AddNode(node);
                        }
                    }
                }
                
                _graphView = new BaseGraphView(this);
                _graphView.Initialize(_internalGraph);
                _graphView.style.flexGrow = 1;
                
                // НЕ ВОССТАНАВЛИВАЕМ СВЯЗИ АВТОМАТИЧЕСКИ
                // Пользователь соединит порты вручную
                
                // Восстанавливаем позиции
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
                
                _graphView.UpdateViewTransform(Vector3.zero, Vector3.one);
                _graphView.FrameAll();
                
                _graphContainer.Add(_graphView);
                
                int nodeCount = _internalGraph.nodes.Count;
                _toolbar.SetStatusSuccess($"Граф готов — {nodeCount} нод. Соедините порты вручную.");
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
                case NodeType.VariableDeclaration: return new VariableDeclarationNode();
                case NodeType.VariableGet: return new GetVariableNode();
                case NodeType.VariableSet: return new SetVariableNode();
                case NodeType.UnityVector3: return new Vector3CreateNode();
                case NodeType.UnityGetPosition: return new GetPositionNode();
                case NodeType.UnitySetPosition: return new SetPositionNode();
                default: return null;
            }
        }
        
        private void OnDestroy()
        {
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
    }
}