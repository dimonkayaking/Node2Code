using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor.Nodes.Comparison;
using CustomVisualScripting.Editor.Nodes.Conversion;
using CustomVisualScripting.Editor.Nodes.Debug;
using CustomVisualScripting.Editor.Nodes.Flow;
using CustomVisualScripting.Editor.Nodes.Literals;
using CustomVisualScripting.Editor.Nodes.Logic;
using CustomVisualScripting.Editor.Nodes.Math;
using CustomVisualScripting.Editor.Nodes.Unity;
using CustomVisualScripting.Editor.Windows;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    public class SubGraphPanel : VisualElement
    {
        public event Action OnChanged;
        public event Action<SubGraphPanel, Vector2> OnPanelResized;
        private const float MinPanelWidth = 520f;
        private const float MinPanelHeight = 180f;

        // Nested graphs force node rect to min size; flow nodes need room for SubGraphPanel chrome
        // (MinPanelHeight per panel + title/ports) or headers/bodies overlap visually.
        private const float NestedFlowMinWidthIfWhile = 540f;
        private const float NestedFlowMinHeightIfWhile = 520f;
        private const float NestedFlowMinWidthElse = 540f;
        private const float NestedFlowMinHeightElse = 320f;
        private const float NestedFlowMinWidthFor = 640f;
        private const float NestedFlowMinHeightFor = 600f;

        private readonly string _title;
        private readonly bool _isConditionPanel;
        private readonly bool _verticalResizeOnly;
        /// <summary>Если false — только подпись секции без стрелки и без сворачивания строки заголовка.</summary>
        private readonly bool _showHeaderCollapseToggle;
        private GraphData _subGraph;

        private Label _toggleLabel;
        private VisualElement _content;
        private BaseGraph _internalGraph;
        private BaseGraphView _graphView;
        private IVisualElementScheduledItem _syncTicker;
        private bool _isExpanded = true;
        private bool _isSyncing;

        /// <summary>Совпадает с вычитанием «шапки» в MakePanelResizable: контент = высота панели − это значение.</summary>
        private const float HeaderChromePixels = 28f;
        private float _storedFlexGrow;
        private float _storedOuterHeight;
        public SubGraphPanel(
            string title,
            GraphData subGraph,
            bool isConditionPanel,
            bool verticalResizeOnly = false,
            bool showHeaderCollapseToggle = true)
        {
            _title = title;
            _subGraph = subGraph ?? new GraphData();
            _isConditionPanel = isConditionPanel;
            _verticalResizeOnly = verticalResizeOnly;
            _showHeaderCollapseToggle = showHeaderCollapseToggle;

            BuildUI();
            Rebuild();
            RegisterCallback<DetachFromPanelEvent>(_ => DisposeGraph());
        }

        public GraphData SubGraph => _subGraph;

        /// <summary>True, если область графа развёрнута (не только строка заголовка).</summary>
        public bool IsGraphExpanded => _isExpanded;

        public void SetSubGraph(GraphData subGraph)
        {
            _subGraph = subGraph ?? new GraphData();
            Rebuild();
        }

        public void Rebuild()
        {
            DisposeGraph();
            CreateGraphViewFromSubGraph();
        }

        private void BuildUI()
        {
            style.marginTop = 4;
            style.marginBottom = 4;
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor =
                new Color(0.3f, 0.3f, 0.3f);
            style.borderTopLeftRadius = style.borderTopRightRadius =
                style.borderBottomLeftRadius = style.borderBottomRightRadius = 4;
            style.minWidth = MinPanelWidth;
            style.minHeight = MinPanelHeight;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            header.style.paddingLeft = 8;
            header.style.paddingRight = 8;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.borderTopLeftRadius = 4;
            header.style.borderTopRightRadius = 4;

            if (_showHeaderCollapseToggle)
            {
                _toggleLabel = new Label("\u25BC");
                _toggleLabel.style.width = 16;
                _toggleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _toggleLabel.style.fontSize = 10;
                _toggleLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                header.Add(_toggleLabel);

                header.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 0)
                    {
                        ToggleExpanded();
                        e.StopPropagation();
                    }
                });
            }

            var titleLabel = new Label(_title);
            titleLabel.style.flexGrow = 1;
            titleLabel.style.color = Color.white;
            titleLabel.style.fontSize = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);

            Add(header);

            _content = new VisualElement();
            _content.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            _content.style.borderBottomLeftRadius = 4;
            _content.style.borderBottomRightRadius = 4;
            _content.style.minHeight = MinPanelHeight;
            _content.style.overflow = Overflow.Hidden;
            Add(_content);

            MakePanelResizable();
        }

        private void ToggleExpanded()
        {
            if (_isExpanded)
            {
                _storedFlexGrow = resolvedStyle.flexGrow;
                _storedOuterHeight = resolvedStyle.height;
            }

            _isExpanded = !_isExpanded;

            if (_isExpanded)
                ApplyExpandedPanelLayout();
            else
                ApplyCollapsedPanelLayout();

            if (_toggleLabel != null)
                _toggleLabel.text = _isExpanded ? "\u25BC" : "\u25B6";
            OnPanelResized?.Invoke(this, new Vector2(resolvedStyle.width, resolvedStyle.height));
        }

        private void ApplyExpandedPanelLayout()
        {
            _content.style.display = DisplayStyle.Flex;
            _content.style.minHeight = MinPanelHeight;
            style.minHeight = MinPanelHeight;
            style.flexGrow = _storedFlexGrow;

            if (_storedOuterHeight > HeaderChromePixels + 2f)
            {
                style.height = _storedOuterHeight;
                _content.style.height =
                    Mathf.Max(MinPanelHeight - 10f, _storedOuterHeight - HeaderChromePixels);
            }
            else
            {
                style.height = StyleKeyword.Auto;
                _content.style.height = StyleKeyword.Auto;
            }
        }

        private void ApplyCollapsedPanelLayout()
        {
            _content.style.display = DisplayStyle.None;
            _content.style.minHeight = 0;
            _content.style.height = StyleKeyword.Auto;

            style.flexGrow = 0;
            style.minHeight = HeaderChromePixels;
            style.height = HeaderChromePixels;
        }

        /// <summary>После Rebuild граф заново создан — повторно скрыть тело, если панель была свёрнута.</summary>
        private void ApplyCollapsedVisualStateIfNeeded()
        {
            if (_isExpanded)
                return;
            ApplyCollapsedPanelLayout();
        }

        private void CreateGraphViewFromSubGraph()
        {
            _content.Clear();
            _internalGraph = ScriptableObject.CreateInstance<BaseGraph>();
            var nodeMap = new Dictionary<string, CustomBaseNode>();

            foreach (var nodeData in _subGraph.Nodes)
            {
                var node = CreateNodeFromData(nodeData);
                if (node == null)
                    continue;

                node.NodeId = nodeData.Id;
                node.InitializeFromData(nodeData);
                if (node.GUID != node.NodeId)
                    node.SetGUID(node.NodeId);

                ApplyNodeLiteralValues(node, nodeData);
                _internalGraph.AddNode(node);
                nodeMap[nodeData.Id] = node;
            }

            var ownerWindow = (EditorWindow)VisualScriptingWindow.ActiveWindow
                              ?? EditorWindow.focusedWindow
                              ?? Resources.FindObjectsOfTypeAll<VisualScriptingWindow>().FirstOrDefault();
            _graphView = new FilteredCreateMenuBaseGraphView(ownerWindow);
            _graphView.Initialize(_internalGraph);
            _graphView.style.flexGrow = 1;
            _graphView.style.minHeight = MinPanelHeight - 10f;
            _graphView.graphViewChanged += OnGraphViewChanged;
            _syncTicker = _graphView.schedule.Execute(SyncBackFromGraphView).Every(300);

            RestoreEdges(nodeMap);
            _content.Add(_graphView);
            ConfigureNodeViewSizing(_graphView.nodeViews);
            RefreshNodeViewsLayout(_graphView.nodeViews);
            AutoLayoutIfNeeded(_graphView.nodeViews);
            RefreshNodeViewsLayout(_graphView.nodeViews);

            // One extra deferred pass after mount: GraphView/Ports geometry settles asynchronously.
            _graphView.schedule.Execute(() =>
            {
                if (_graphView == null)
                    return;
                ConfigureNodeViewSizing(_graphView.nodeViews);
                AutoLayoutIfNeeded(_graphView.nodeViews);
                RefreshNodeViewsLayout(_graphView.nodeViews);
            }).ExecuteLater(50);

            ApplyCollapsedVisualStateIfNeeded();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isSyncing)
                return change;

            SyncBackFromGraphView();
            return change;
        }

        private void SyncBackFromGraphView()
        {
            if (_graphView == null || _internalGraph == null)
                return;

            _isSyncing = true;
            try
            {
                _subGraph.Nodes.Clear();
                _subGraph.Edges.Clear();

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

                    _subGraph.Nodes.Add(nodeData);
                    validNodeIds.Add(customNode.NodeId);
                }

                foreach (var edgeView in _graphView.edgeViews)
                {
                    if (edgeView == null)
                        continue;

                    var fromPort = edgeView.output as PortView;
                    var toPort = edgeView.input as PortView;
                    if (fromPort == null || toPort == null)
                        continue;
                    if (fromPort.direction != Direction.Output || toPort.direction != Direction.Input)
                        continue;

                    var fromNode = fromPort.owner.nodeTarget as CustomBaseNode;
                    var toNode = toPort.owner.nodeTarget as CustomBaseNode;
                    if (fromNode == null || toNode == null)
                        continue;
                    if (!validNodeIds.Contains(fromNode.NodeId) || !validNodeIds.Contains(toNode.NodeId))
                        continue;

                    _subGraph.Edges.Add(new EdgeData
                    {
                        FromNodeId = fromNode.NodeId,
                        FromPort = CanonicalPortIdForStorage(fromPort),
                        ToNodeId = toNode.NodeId,
                        ToPort = CanonicalPortIdForStorage(toPort)
                    });
                }
            }
            finally
            {
                _isSyncing = false;
            }

            OnChanged?.Invoke();
        }

        private void RestoreEdges(Dictionary<string, CustomBaseNode> nodeMap)
        {
            if (_subGraph.Edges == null || _subGraph.Edges.Count == 0)
                return;

            foreach (var edgeData in _subGraph.Edges)
            {
                if (!nodeMap.TryGetValue(edgeData.FromNodeId, out var fromNode))
                    continue;
                if (!nodeMap.TryGetValue(edgeData.ToNodeId, out var toNode))
                    continue;
                if (!_graphView.nodeViewsPerNode.TryGetValue(fromNode, out var fromNodeView))
                    continue;
                if (!_graphView.nodeViewsPerNode.TryGetValue(toNode, out var toNodeView))
                    continue;

                var fromPort = fromNodeView.outputPortViews.FirstOrDefault(p => IsPortMatchForStorage(p, edgeData.FromPort));
                var toPort = toNodeView.inputPortViews.FirstOrDefault(p => IsPortMatchForStorage(p, edgeData.ToPort));
                if (fromPort == null || toPort == null)
                    continue;

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
                    _graphView.Connect(toPort, fromPort);
            }
        }

        private static string CanonicalPortIdForStorage(PortView port)
        {
            var fn = PortIds.Normalize(port.fieldName);
            if (!string.IsNullOrEmpty(fn))
                return fn;

            var pn = PortIds.Normalize(port.portName);
            if (!string.IsNullOrEmpty(pn))
                return pn;

            if (port.direction == Direction.Input)
                return PortIds.ExecIn;
            if (port.direction == Direction.Output)
                return PortIds.ExecOut;
            return "";
        }

        private static bool IsPortMatchForStorage(PortView port, string savedPortId)
        {
            if (port == null || string.IsNullOrWhiteSpace(savedPortId))
                return false;

            var expected = PortIds.Normalize(savedPortId);
            if (string.IsNullOrEmpty(expected))
                return false;

            var field = PortIds.Normalize(port.fieldName);
            if (!string.IsNullOrEmpty(field) &&
                string.Equals(field, expected, StringComparison.OrdinalIgnoreCase))
                return true;

            var name = PortIds.Normalize(port.portName);
            return !string.IsNullOrEmpty(name) &&
                   string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyNodeLiteralValues(CustomBaseNode node, NodeData nodeData)
        {
            if (node is IntNode intNode && int.TryParse(nodeData.Value, out int intVal))
                intNode.intValue = intVal;
            else if (node is FloatNode floatNode &&
                     float.TryParse(nodeData.Value, System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                floatNode.floatValue = floatVal;
            else if (node is BoolNode boolNode && bool.TryParse(nodeData.Value, out bool boolVal))
                boolNode.boolValue = boolVal;
            else if (node is StringNode stringNode)
                stringNode.stringValue = nodeData.Value;
        }

        private CustomBaseNode CreateNodeFromData(NodeData data)
        {
            if (data == null)
                return null;

            if (_isConditionPanel && data.Type is NodeType.FlowIf or NodeType.FlowElse or NodeType.FlowFor or NodeType.FlowWhile)
                return null;

            return data.Type switch
            {
                NodeType.LiteralInt => new IntNode(),
                NodeType.LiteralFloat => new FloatNode(),
                NodeType.LiteralBool => new BoolNode(),
                NodeType.LiteralString => new StringNode(),
                NodeType.MathAdd => new AddNode(),
                NodeType.MathSubtract => new SubtractNode(),
                NodeType.MathMultiply => new MultiplyNode(),
                NodeType.MathDivide => new DivideNode(),
                NodeType.MathModulo => new ModuloNode(),
                NodeType.CompareEqual => new EqualNode(),
                NodeType.CompareNotEqual => new NotEqualNode(),
                NodeType.CompareGreater => new GreaterNode(),
                NodeType.CompareGreaterOrEqual => new GreaterOrEqualNode(),
                NodeType.CompareLess => new LessNode(),
                NodeType.CompareLessOrEqual => new LessOrEqualNode(),
                NodeType.LogicalAnd => new AndNode(),
                NodeType.LogicalOr => new OrNode(),
                NodeType.LogicalNot => new NotNode(),
                NodeType.FlowIf => new IfNode(),
                NodeType.FlowElse => new ElseNode(),
                NodeType.FlowFor => new ForNode(),
                NodeType.FlowWhile => new WhileNode(),
                NodeType.ConsoleWriteLine => new ConsoleWriteLineNode(),
                NodeType.DebugLog => new DebugLogNode(),
                NodeType.IntParse => new IntParseNode(),
                NodeType.FloatParse => new FloatParseNode(),
                NodeType.ToStringConvert => new ToStringNode(),
                NodeType.MathfAbs => new MathfAbsNode(),
                NodeType.MathfMax => new MathfMaxNode(),
                NodeType.MathfMin => new MathfMinNode(),
                NodeType.UnityVector3 => new Vector3CreateNode(),
                NodeType.UnityGetPosition => new GetPositionNode(),
                NodeType.UnitySetPosition => new SetPositionNode(),
                _ => null
            };
        }

        private void DisposeGraph()
        {
            if (_graphView != null)
            {
                _syncTicker?.Pause();
                _syncTicker = null;
                _graphView.graphViewChanged -= OnGraphViewChanged;
                _graphView.Dispose();
                _graphView = null;
            }

            if (_internalGraph != null)
            {
                ScriptableObject.DestroyImmediate(_internalGraph);
                _internalGraph = null;
            }
        }

        private static void ResolveSubGraphNodeMinSizes(BaseNodeView nodeView, out float minW, out float minH)
        {
            var resolved = NodeViewBoundsUtils.ResolveSyncMinBounds(nodeView);
            minW = resolved.minW;
            minH = resolved.minH;
            if (TryGetNestedSubGraphFlowMinDimensions(nodeView, out var nestedW, out var nestedH))
            {
                minW = Mathf.Max(minW, nestedW);
                minH = Mathf.Max(minH, nestedH);
            }
        }

        private static void ConfigureNodeViewSizing(IEnumerable<BaseNodeView> nodeViews)
        {
            foreach (var nodeView in nodeViews)
            {
                if (nodeView == null)
                    continue;

                ResolveSubGraphNodeMinSizes(nodeView, out var minW, out var minH);

                NodeViewBoundsUtils.ApplyNodeMinStyle(nodeView, minW, minH);
                NodeViewBoundsUtils.DisableGraphViewPortCollapse(nodeView);
                NodeViewBoundsUtils.MakeNodeEdgesResizable(nodeView);

                var rect = nodeView.GetPosition();
                var xy = NodeViewBoundsUtils.GetAuthoritativeNodeTopLeft(nodeView);
                var width = Mathf.Max(rect.width, minW);
                var height = Mathf.Max(rect.height, minH);
                nodeView.SetPosition(new Rect(xy.x, xy.y, width, height));
            }
        }

        /// <summary>
        /// Крупные минимумы только для flow-нод с вложенными панелями; остальные берут <see cref="NodeViewBoundsUtils.ResolveSyncMinBounds"/>.
        /// </summary>
        private static bool TryGetNestedSubGraphFlowMinDimensions(BaseNodeView nodeView, out float minW, out float minH)
        {
            minW = 0f;
            minH = 0f;

            switch (nodeView.nodeTarget)
            {
                case IfNode:
                case WhileNode:
                    minW = NestedFlowMinWidthIfWhile;
                    minH = NestedFlowMinHeightIfWhile;
                    return true;
                case ElseNode:
                    minW = NestedFlowMinWidthElse;
                    minH = NestedFlowMinHeightElse;
                    return true;
                case ForNode:
                    minW = NestedFlowMinWidthFor;
                    minH = NestedFlowMinHeightFor;
                    return true;
                default:
                    return false;
            }
        }

        private void AutoLayoutIfNeeded(IReadOnlyList<BaseNodeView> nodeViews)
        {
            if (nodeViews == null || nodeViews.Count == 0)
                return;
            if (!HasHeavyOverlap(nodeViews))
                return;

            ApplyDagAutoLayout(nodeViews);
            if (HasHeavyOverlap(nodeViews))
                ApplyDagAutoLayout(nodeViews, 420f, 280f);
        }

        private void ApplyDagAutoLayout(IReadOnlyList<BaseNodeView> nodeViews, float spacingX = 280f, float spacingY = 180f)
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
            if (nodeById.Count == 0)
                return;

            var outgoing = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var incoming = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var incomingCount = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var nodeId in nodeById.Keys)
            {
                outgoing[nodeId] = new HashSet<string>(StringComparer.Ordinal);
                incoming[nodeId] = new HashSet<string>(StringComparer.Ordinal);
                incomingCount[nodeId] = 0;
            }

            if (_subGraph?.Edges != null)
            {
                foreach (var edge in _subGraph.Edges)
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
            if (_subGraph?.Nodes != null)
            {
                foreach (var n in _subGraph.Nodes)
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

            float columnGap = Mathf.Max(40f, spacingX * 0.2f);
            float rowGap = Mathf.Max(24f, spacingY * 0.2f);
            const float startX = 30f;
            const float startY = 30f;
            float columnX = startX;
            foreach (var layerEntry in layers)
            {
                var ids = layerEntry.Value;
                float layerMaxWidth = NodeViewBoundsUtils.DefaultGraphNodeMinWidth;
                float rowY = startY;
                for (int row = 0; row < ids.Count; row++)
                {
                    var view = nodeById[ids[row]];
                    var rect = view.GetPosition();
                    ResolveSubGraphNodeMinSizes(view, out var cellMinW, out var cellMinH);
                    float width = Mathf.Max(rect.width, cellMinW);
                    float height = Mathf.Max(rect.height, cellMinH);
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

        private static void RefreshNodeViewsLayout(IReadOnlyList<BaseNodeView> nodeViews)
        {
            if (nodeViews == null)
                return;

            foreach (var nodeView in nodeViews)
            {
                if (nodeView == null)
                    continue;

                NodeViewBoundsUtils.DisableGraphViewPortCollapse(nodeView);
                ResolveSubGraphNodeMinSizes(nodeView, out var minW, out var minH);
                NodeViewBoundsUtils.ApplyNodeMinStyle(nodeView, minW, minH);
                NodeViewBoundsUtils.SyncNodeRectToLayout(nodeView, minW, minH);
            }
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

            return overlaps >= System.Math.Max(1, nodeViews.Count / 3);
        }

        private void MakePanelResizable()
        {
            bool resizingBottom = false;
            bool resizingRight = false;

            var bottomResizer = new VisualElement();
            bottomResizer.style.position = Position.Absolute;
            bottomResizer.style.bottom = -4;
            bottomResizer.style.left = 0;
            bottomResizer.style.right = 0;
            bottomResizer.style.height = 8;
            bottomResizer.pickingMode = PickingMode.Position;
            bottomResizer.tooltip = "Потяните, чтобы изменить высоту панели";
            bottomResizer.RegisterCallback<PointerEnterEvent>(_ =>
                EditorUiPointerCursor.TryApply(bottomResizer, MouseCursor.ResizeVertical));
            bottomResizer.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (!resizingBottom)
                    EditorUiPointerCursor.Clear(bottomResizer);
            });
            Add(bottomResizer);

            var rightResizer = new VisualElement();
            rightResizer.style.position = Position.Absolute;
            rightResizer.style.right = -4;
            rightResizer.style.top = 0;
            rightResizer.style.bottom = 0;
            rightResizer.style.width = 8;
            rightResizer.pickingMode = PickingMode.Position;
            rightResizer.tooltip = "Потяните, чтобы изменить ширину панели";
            rightResizer.RegisterCallback<PointerEnterEvent>(_ =>
                EditorUiPointerCursor.TryApply(rightResizer, MouseCursor.ResizeHorizontal));
            rightResizer.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (!resizingRight)
                    EditorUiPointerCursor.Clear(rightResizer);
            });
            if (!_verticalResizeOnly)
                Add(rightResizer);

            var bottomDrag = new PointerRootDragSession(
                delta =>
                {
                    if (!resizingBottom)
                        return;
                    float rh = resolvedStyle.minHeight.value;
                    float minH = rh > 2f ? rh : MinPanelHeight;
                    float height = Mathf.Max(minH, resolvedStyle.height + delta.y);
                    style.height = height;
                    _content.style.height = height - 28f;
                    OnPanelResized?.Invoke(this, new Vector2(resolvedStyle.width, height));
                },
                () =>
                {
                    resizingBottom = false;
                    EditorUiPointerCursor.Clear(bottomResizer);
                });

            var rightDrag = new PointerRootDragSession(
                delta =>
                {
                    if (!resizingRight)
                        return;
                    float rw = resolvedStyle.minWidth.value;
                    float minW = rw > 2f ? rw : MinPanelWidth;
                    float width = Mathf.Max(minW, resolvedStyle.width + delta.x);
                    style.width = width;
                    OnPanelResized?.Invoke(this, new Vector2(width, resolvedStyle.height));
                },
                () =>
                {
                    resizingRight = false;
                    EditorUiPointerCursor.Clear(rightResizer);
                });

            bottomResizer.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                resizingBottom = true;
                if (!bottomDrag.TryBeginFromPointer(bottomResizer, evt))
                {
                    resizingBottom = false;
                    return;
                }

                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            bottomResizer.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                if (!bottomDrag.TryBeginFromMouse(bottomResizer, evt))
                    return;
                resizingBottom = true;
                evt.StopPropagation();
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            bottomResizer.RegisterCallback<DetachFromPanelEvent>(_ => bottomDrag.Teardown());

            rightResizer.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                resizingRight = true;
                if (!rightDrag.TryBeginFromPointer(rightResizer, evt))
                {
                    resizingRight = false;
                    return;
                }

                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            rightResizer.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                if (!rightDrag.TryBeginFromMouse(rightResizer, evt))
                    return;
                resizingRight = true;
                evt.StopPropagation();
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            rightResizer.RegisterCallback<DetachFromPanelEvent>(_ => rightDrag.Teardown());
        }
    }
}
