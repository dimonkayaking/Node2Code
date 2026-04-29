using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor.Nodes.Literals;
using CustomVisualScripting.Editor.Nodes.Flow;
using CustomVisualScripting.Editor.Nodes.Views;
using CustomVisualScripting.Integration.Models;
using CustomVisualScripting.Windows.Views;
using VisualScripting.Core.Models;
using CustomToolbar = CustomVisualScripting.Windows.Views.ToolbarView;

namespace CustomVisualScripting.Editor.Windows
{
    public partial class VisualScriptingWindow
    {
        private NodeToolbarView _nodeToolbar; // ДОБАВЛЕНО

        private void CleanupGraph()
        {
            DisposeAllSubspaceRuntimes();
            if (_graphView != null)
            {
                _graphView.graphViewChanged -= OnGraphViewChanged;
                _graphView.NodeViewAdded -= OnNodeViewAdded;
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
            InitializeTabsState();

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

            BuildGraphAreaWithTabs(splitView);
            root.Add(splitView);

            // ДОБАВЛЕНО: создаём панель инструментов нод
            if (_graphView != null)
            {
                _nodeToolbar = new NodeToolbarView(_graphView);
                root.Add(_nodeToolbar);
            }

            _errorPanel = new ErrorPanel();
            root.Add(_errorPanel);

            _consoleView = new ConsoleView();
            _consoleView.style.marginTop = 5;
            root.Add(_consoleView);

            _toolbar.SetStatusNormal("Готов к работе");

            UpdateGraphView();
        }

        private void RecreateGraphView()
        {
            CleanupGraph();
            _graphHost?.Clear();
            UpdateGraphView();
        }

        private void UpdateGraphView()
        {
            _graphHost?.Clear();

            try
            {
                _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
                var nodeMap = new Dictionary<string, BaseNode>();

                if (_currentGraph?.LogicGraph?.Nodes != null)
                {
                    foreach (var nodeData in _currentGraph.LogicGraph.Nodes)
                    {
                        var node = CreateNodeFromData(nodeData);
                        if (node == null)
                            continue;

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
                            ifNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new GraphData();
                            ifNode.bodySubGraph = nodeData.BodySubGraph ?? new GraphData();
                        }
                        else if (node is ElseNode elseNode)
                        {
                            elseNode.bodySubGraph = nodeData.BodySubGraph ?? new GraphData();
                        }
                        else if (node is ForNode forNode)
                        {
                            forNode.initSubGraph = nodeData.InitSubGraph ?? new GraphData();
                            forNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new GraphData();
                            forNode.incrementSubGraph = nodeData.IncrementSubGraph ?? new GraphData();
                            forNode.bodySubGraph = nodeData.BodySubGraph ?? new GraphData();
                        }
                        else if (node is WhileNode whileNode)
                        {
                            whileNode.conditionSubGraph = nodeData.ConditionSubGraph ?? new GraphData();
                            whileNode.bodySubGraph = nodeData.BodySubGraph ?? new GraphData();
                        }

                        _internalGraph.AddNode(node);
                        nodeMap[nodeData.Id] = node;
                    }
                }

                _graphView = new FilteredCreateMenuBaseGraphView(this);
                _graphView.NodeViewAdded += OnNodeViewAdded;
                _graphView.Initialize(_internalGraph);
                _graphView.style.flexGrow = 1;
                _graphView.graphViewChanged += OnGraphViewChanged;

                if (_currentGraph?.LogicGraph?.Edges != null && nodeMap.Count > 0)
                {
                    foreach (var edgeData in _currentGraph.LogicGraph.Edges)
                    {
                        if (!nodeMap.TryGetValue(edgeData.FromNodeId, out var fromNode)) continue;
                        if (!nodeMap.TryGetValue(edgeData.ToNodeId, out var toNode)) continue;
                        if (!_graphView.nodeViewsPerNode.TryGetValue(fromNode, out var fromNodeView)) continue;
                        if (!_graphView.nodeViewsPerNode.TryGetValue(toNode, out var toNodeView)) continue;

                        Debug.Log($"[VS] Восстанавливаем связь: {edgeData.FromNodeId}.{edgeData.FromPort} → {edgeData.ToNodeId}.{edgeData.ToPort}");

                        var fromPort = fromNodeView.outputPortViews.FirstOrDefault(p => IsPortMatchForStorage(p, edgeData.FromPort));
                        var toPort = toNodeView.inputPortViews.FirstOrDefault(p => IsPortMatchForStorage(p, edgeData.ToPort));

                        if (fromPort == null || toPort == null)
                            continue;
                        if (fromPort.direction != Direction.Output || toPort.direction != Direction.Input)
                            continue;

                        bool alreadyConnected = _graphView.edgeViews.Any(existingEdge => existingEdge.output == fromPort && existingEdge.input == toPort);
                        if (!alreadyConnected)
                            _graphView.Connect(toPort, fromPort);
                    }
                }

                if (_currentGraph?.VisualNodes != null)
                {
                    foreach (var nodeView in _graphView.nodeViews)
                    {
                        if (nodeView.nodeTarget is not CustomBaseNode customNode)
                            continue;

                        var visualNode = _currentGraph.VisualNodes.FirstOrDefault(v => v.NodeId == customNode.NodeId);
                        if (visualNode != null)
                            nodeView.SetPosition(new Rect(visualNode.Position, Vector2.zero));
                    }
                }

                if (_collapseFlowSubspacesOnNextRebuild)
                {
                    CollapseFlowSubspaceNodes(_graphView.nodeViews);
                    _collapseFlowSubspacesOnNextRebuild = false;
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
                DisplayActiveTabContent();

                // ДОБАВЛЕНО: обновляем тулбар при пересоздании графа
                if (_nodeToolbar != null && _nodeToolbar.parent != null)
                    _nodeToolbar.RemoveFromHierarchy();
                
                _nodeToolbar = new NodeToolbarView(_graphView);
                rootVisualElement.Add(_nodeToolbar);

                _toolbar.SetStatusSuccess($"Граф готов — {_internalGraph.nodes.Count} нод");
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
                    OnSave();
            }

            CleanupGraph();
        }

        private void ConfigureNodeViewSizing(IEnumerable<BaseNodeView> nodeViews)
        {
            foreach (var nodeView in nodeViews)
            {
                if (nodeView == null)
                    continue;

                var mins = NodeViewBoundsUtils.ResolveSyncMinBounds(nodeView);
                NodeViewBoundsUtils.ApplyNodeMinStyle(nodeView, mins.minW, mins.minH);
                NodeViewBoundsUtils.DisableGraphViewPortCollapse(nodeView);
                NodeViewBoundsUtils.MakeNodeEdgesResizable(nodeView);

                var rect = nodeView.GetPosition();
                var xy = NodeViewBoundsUtils.GetAuthoritativeNodeTopLeft(nodeView);
                var width = Mathf.Max(rect.width, mins.minW);
                var height = Mathf.Max(rect.height, mins.minH);
                nodeView.SetPosition(new Rect(xy.x, xy.y, width, height));

                nodeView.UnregisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
                nodeView.RegisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
            }
        }

        private static void CollapseFlowSubspaceNodes(IEnumerable<BaseNodeView> nodeViews)
        {
            foreach (var nodeView in nodeViews)
            {
                switch (nodeView)
                {
                    case IfNodeView ifNodeView:
                        ifNodeView.SetPanelsExpanded(false);
                        break;
                    case ElseNodeView elseNodeView:
                        elseNodeView.SetPanelsExpanded(false);
                        break;
                    case ForNodeView forNodeView:
                        forNodeView.SetPanelsExpanded(false);
                        break;
                    case WhileNodeView whileNodeView:
                        whileNodeView.SetPanelsExpanded(false);
                        break;
                }
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_graphView?.nodeViews != null)
                ConfigureNodeViewSizing(_graphView.nodeViews);
            return change;
        }

        private void OnNodeViewAdded(BaseNodeView nodeView)
        {
            if (nodeView == null)
                return;

            ConfigureNodeViewSizing(new[] { nodeView });
            nodeView.schedule.Execute(() =>
            {
                if (nodeView.panel == null)
                    return;
                ConfigureNodeViewSizing(new[] { nodeView });
                SyncNodeBoundsToLayout(nodeView);
            }).ExecuteLater(1);
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
            var customViews = nodeViews.Where(v => v?.nodeTarget is CustomBaseNode).ToList();
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
            var rootIds = incomingCount.Where(kv => kv.Value == 0).Select(kv => kv.Key).OrderBy(id => id, StringComparer.Ordinal).ToList();
            foreach (var rootId in rootIds)
                depthById[rootId] = 0;
            var queue = new Queue<string>(rootIds);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int currentDepth = depthById.TryGetValue(current, out var d) ? d : 0;

                foreach (var next in outgoing[current].OrderBy(id => id, StringComparer.Ordinal))
                {
                    int nextDepth = currentDepth + 1;
                    if (!depthById.TryGetValue(next, out var existingDepth) || nextDepth > existingDepth)
                        depthById[next] = nextDepth;

                    incomingCount[next]--;
                    if (incomingCount[next] == 0)
                        queue.Enqueue(next);
                }
            }

            int maxDepth = depthById.Count == 0 ? 0 : depthById.Values.Max();
            var unresolvedIds = nodeById.Keys.Where(id => !depthById.ContainsKey(id)).OrderBy(id => id, StringComparer.Ordinal).ToList();
            for (int i = 0; i < unresolvedIds.Count; i++)
                depthById[unresolvedIds[i]] = maxDepth + 1 + i;

            var layers = depthById
                .GroupBy(kv => kv.Value)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

            var laneCache = new Dictionary<string, int>(StringComparer.Ordinal);
            int GetBranchLane(string nodeId, HashSet<string> visiting = null)
            {
                if (laneCache.TryGetValue(nodeId, out var cached))
                    return cached;

                if (nodeTypeById.TryGetValue(nodeId, out var type))
                {
                    if (type == NodeType.FlowIf) return laneCache[nodeId] = 1;
                    if (type == NodeType.FlowElse) return laneCache[nodeId] = 2;
                }

                visiting ??= new HashSet<string>(StringComparer.Ordinal);
                if (!visiting.Add(nodeId))
                    return 0;

                int lane = 0;
                foreach (var parent in incoming[nodeId])
                    lane = Mathf.Max(lane, GetBranchLane(parent, visiting));
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
                    view.SetPosition(new Rect(columnX, rowY, width, height));
                    rowY += height + rowGap;
                }
                columnX += layerMaxWidth + columnGap;
            }
        }

        private void ResolveOverlaps(IReadOnlyList<BaseNodeView> nodeViews)
        {
            var customViews = nodeViews.Where(v => v?.nodeTarget is CustomBaseNode).ToList();
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

            NodeViewBoundsUtils.PerformFullNodeAppearanceFix(nodeView);
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

        private static bool HasMeaningfulSavedPositions(IReadOnlyList<CustomVisualScripting.Integration.Models.VisualNodeData> visualNodes)
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