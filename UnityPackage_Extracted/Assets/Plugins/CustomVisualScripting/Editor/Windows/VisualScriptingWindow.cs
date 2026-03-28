using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Integration;
using CustomVisualScripting.Integration.Models;
using CustomVisualScripting.Windows.Views;
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
        
        private string _currentFilePath;
        
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
        }
        
        private void OnDisable()
        {
            CleanupGraph();
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
            
            var root = rootVisualElement;
            
            _toolbar = new CustomToolbar();
            _toolbar.NewButton.clicked += OnNew;
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
            
            var placeholder = new Label("Здесь будет граф\n\nНажмите 'Парсить код' для отображения");
            placeholder.style.marginTop = 20;
            placeholder.style.marginLeft = 10;
            placeholder.style.color = Color.gray;
            placeholder.style.whiteSpace = WhiteSpace.Normal;
            _graphContainer.Add(placeholder);
            
            splitView.Add(_graphContainer);
            root.Add(splitView);
            
            _errorPanel = new ErrorPanel();
            root.Add(_errorPanel);
            
            _toolbar.SetStatusNormal("Готов к работе");
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
            _currentGraph = GraphConverter.LogicToComplete(result.Graph, _currentGraph);
            UpdateGraphView();
            _toolbar.SetStatusSuccess($"Создано нод: {result.Graph.Nodes.Count}");
        }
        
        private void OnGenerate()
        {
            if (_currentGraph?.LogicGraph?.Nodes == null || _currentGraph.LogicGraph.Nodes.Count == 0)
            {
                _toolbar.SetStatusError("Сначала распарси код");
                return;
            }
            
            _toolbar.SetStatusWarning("Генерация...");
            
            string code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;
            
            _toolbar.SetStatusSuccess("Код сгенерирован");
        }
        
        private void OnNew()
        {
            string path = EditorUtility.SaveFilePanel("Новый граф — выберите имя файла", Application.dataPath, "new_graph.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            _codeEditor.Clear();
            _currentGraph = new CompleteGraphData();
            _errorPanel.Clear();
            _currentFilePath = path;

            if (GraphSaver.SaveToJson(_currentGraph, path))
            {
                UpdateGraphView();
                _toolbar.SetStatusSuccess($"Создан: {Path.GetFileName(path)}");
            }
            else
            {
                _toolbar.SetStatusError("Ошибка создания файла");
            }
        }

        private void OnSave()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                OnSaveAs();
                return;
            }

            SaveVisualNodePositions();

            if (GraphSaver.SaveToJson(_currentGraph, _currentFilePath))
            {
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(_currentFilePath)}");
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

            SaveVisualNodePositions();
            _currentFilePath = path;

            if (GraphSaver.SaveToJson(_currentGraph, path))
            {
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(path)}");
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
                _codeEditor.Code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
                UpdateGraphView();
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
            UpdateGraphView();
            _toolbar.SetStatusNormal("Очищено");
        }

        private void SaveVisualNodePositions()
        {
            if (_currentGraph?.VisualNodes == null || _graphView == null)
                return;

            foreach (var vnd in _currentGraph.VisualNodes)
            {
                var nodeView = _graphView.nodeViews?.FirstOrDefault(
                    nv => (nv.nodeTarget as dynamic)?.NodeId == vnd.NodeId);
                if (nodeView != null)
                {
                    vnd.Position = nodeView.GetPosition().position;
                }
            }
        }
        
        private void UpdateGraphView()
        {
            _graphContainer.Clear();
            
            if (_currentGraph?.LogicGraph?.Nodes == null || _currentGraph.LogicGraph.Nodes.Count == 0)
            {
                var placeholder = new Label("Нет нод для отображения\n\nНажмите 'Парсить код' для создания графа");
                placeholder.style.marginTop = 20;
                placeholder.style.marginLeft = 10;
                placeholder.style.color = Color.gray;
                placeholder.style.whiteSpace = WhiteSpace.Normal;
                _graphContainer.Add(placeholder);
                return;
            }

            var posDict = new System.Collections.Generic.Dictionary<string, Vector2>();
            if (_currentGraph.VisualNodes != null)
            {
                foreach (var vn in _currentGraph.VisualNodes)
                    posDict[vn.NodeId] = vn.Position;
            }

            var info = new VisualElement();
            info.style.marginTop = 10;
            info.style.marginLeft = 10;
            info.style.flexGrow = 1;

            int edgeCount = _currentGraph.LogicGraph.Edges?.Count ?? 0;
            var label = new Label($"Граф: {_currentGraph.LogicGraph.Nodes.Count} нод, {edgeCount} связей");
            label.style.color = Color.white;
            label.style.fontSize = 14;
            label.style.marginBottom = 10;
            info.Add(label);
            
            foreach (var node in _currentGraph.LogicGraph.Nodes)
            {
                var color = GraphConverter.GetNodeColor(node.Type);
                var displayName = GraphConverter.GetNodeDisplayName(node.Type);
                var varPart = string.IsNullOrEmpty(node.VariableName) ? "" : $" [{node.VariableName}]";
                var valPart = string.IsNullOrEmpty(node.Value) ? "" : $" = {node.Value}";

                var text = $"  {node.Id}: {displayName}{varPart}{valPart}";
                var nodeLabel = new Label(text);
                nodeLabel.style.color = color;
                nodeLabel.style.marginLeft = 20;
                nodeLabel.style.marginBottom = 2;
                info.Add(nodeLabel);
            }
            
            _graphContainer.Add(info);
        }
        
        private void OnDestroy()
        {
            CleanupGraph();
        }
    }
}