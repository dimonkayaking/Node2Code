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
        private const float MinNodeWidth = 420f;
        private const float MinNodeHeight = 140f;

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
        private GraphData _subGraph;

        private Label _toggleLabel;
        private VisualElement _content;
        private BaseGraph _internalGraph;
        private BaseGraphView _graphView;
        private IVisualElementScheduledItem _syncTicker;
        private bool _isExpanded = true;
        private bool _isSyncing;
        private VisualElement _resizeHandle;

        public SubGraphPanel(string title, GraphData subGraph, bool isConditionPanel, bool verticalResizeOnly = false)
        {
            _title = title;
            _subGraph = subGraph ?? new GraphData();
            _isConditionPanel = isConditionPanel;
            _verticalResizeOnly = verticalResizeOnly;

            BuildUI();
            Rebuild();
            RegisterCallback<DetachFromPanelEvent>(_ => DisposeGraph());
        }

        public GraphData SubGraph => _subGraph;

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

            _toggleLabel = new Label("\u25BC");
            _toggleLabel.style.width = 16;
            _toggleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _toggleLabel.style.fontSize = 10;
            _toggleLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            header.Add(_toggleLabel);

            var titleLabel = new Label(_title);
            titleLabel.style.flexGrow = 1;
            titleLabel.style.color = Color.white;
            titleLabel.style.fontSize = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);

            header.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 0)
                {
                    ToggleExpanded();
                    e.StopPropagation();
                }
            });

            Add(header);

            _content = new VisualElement();
            _content.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            _content.style.borderBottomLeftRadius = 4;
            _content.style.borderBottomRightRadius = 4;
            _content.style.minHeight = MinPanelHeight;
            _content.style.overflow = Overflow.Hidden;
            Add(_content);

            _resizeHandle = new VisualElement();
            _resizeHandle.style.position = Position.Absolute;
            _resizeHandle.style.right = 2;
            _resizeHandle.style.bottom = 2;
            _resizeHandle.style.width = 12;
            _resizeHandle.style.height = 12;
            _resizeHandle.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            _resizeHandle.style.borderTopLeftRadius = 2;
            _resizeHandle.style.borderTopRightRadius = 2;
            _resizeHandle.style.borderBottomLeftRadius = 2;
            _resizeHandle.style.borderBottomRightRadius = 2;
            _resizeHandle.tooltip = "Потяните, чтобы изменить размер";
            Add(_resizeHandle);
            MakePanelResizable();
        }

        private void ToggleExpanded()
        {
            _isExpanded = !_isExpanded;
            _content.style.display = _isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _toggleLabel.text = _isExpanded ? "\u25BC" : "\u25B6";
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
            _graphView = new BaseGraphView(ownerWindow);
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

        private static void ConfigureNodeViewSizing(IEnumerable<BaseNodeView> nodeViews)
        {
            foreach (var nodeView in nodeViews)
            {
                if (nodeView == null)
                    continue;

                GetNestedSubGraphFlowMinDimensions(nodeView, out var minW, out var minH);

                nodeView.capabilities |= Capabilities.Resizable;
                nodeView.style.minWidth = minW;
                nodeView.style.minHeight = minH;

                var rect = nodeView.GetPosition();
                var width = Mathf.Max(rect.width, minW);
                var height = Mathf.Max(rect.height, minH);

                // Force actual width/height, not only min constraints, to prevent compressed internals.
                nodeView.style.width = width;
                nodeView.style.height = height;
                nodeView.SetPosition(new Rect(
                    rect.x,
                    rect.y,
                    width,
                    height));
            }
        }

        private static void GetNestedSubGraphFlowMinDimensions(BaseNodeView nodeView, out float minW, out float minH)
        {
            minW = MinNodeWidth;
            minH = MinNodeHeight;

            switch (nodeView.nodeTarget)
            {
                case IfNode:
                case WhileNode:
                    minW = NestedFlowMinWidthIfWhile;
                    minH = NestedFlowMinHeightIfWhile;
                    break;
                case ElseNode:
                    minW = NestedFlowMinWidthElse;
                    minH = NestedFlowMinHeightElse;
                    break;
                case ForNode:
                    minW = NestedFlowMinWidthFor;
                    minH = NestedFlowMinHeightFor;
                    break;
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

        private static void RefreshNodeViewsLayout(IReadOnlyList<BaseNodeView> nodeViews)
        {
            if (nodeViews == null)
                return;

            foreach (var nodeView in nodeViews)
            {
                if (nodeView == null)
                    continue;

                nodeView.RefreshPorts();
                nodeView.RefreshExpandedState();
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
            Vector2 startMouse = Vector2.zero;
            Vector2 startSize = Vector2.zero;
            bool resizing = false;

            _resizeHandle.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                resizing = true;
                startMouse = evt.mousePosition;
                startSize = new Vector2(resolvedStyle.width, resolvedStyle.height);
                _resizeHandle.CaptureMouse();
                evt.StopPropagation();
            });

            _resizeHandle.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!resizing)
                    return;

                var delta = evt.mousePosition - startMouse;
                float height = Mathf.Max(MinPanelHeight, startSize.y + delta.y);
                if (!_verticalResizeOnly)
                {
                    float width = Mathf.Max(MinPanelWidth, startSize.x + delta.x);
                    style.width = width;
                }
                style.height = height;
                _content.style.height = height - 28f;
                OnPanelResized?.Invoke(this, new Vector2(resolvedStyle.width, height));
                evt.StopPropagation();
            });

            _resizeHandle.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (!resizing)
                    return;

                resizing = false;
                _resizeHandle.ReleaseMouse();
                evt.StopPropagation();
            });
        }
    }
}
