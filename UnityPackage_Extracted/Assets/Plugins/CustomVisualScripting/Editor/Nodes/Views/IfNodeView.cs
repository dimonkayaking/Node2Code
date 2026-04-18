using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Flow;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(IfNode))]
    public class IfNodeView : BaseNodeView, IFlowSubGraphNodeMinBounds
    {
        private IfNode _node;
        private SubGraphPanel _conditionPanel;
        private SubGraphPanel _bodyPanel;
        private VisualElement _panelsContainer;
        private Label _collapseToggle;
        private bool _panelsExpanded = true;
        private IVisualElementScheduledItem _syncBoundsTask;

        public override void Enable()
        {
            base.Enable();

            _node = nodeTarget as IfNode;
            if (_node == null) return;
            CleanupUi();

            if (controlsContainer == null)
            {
                controlsContainer = new VisualElement();
                controlsContainer.name = "controls";
                mainContainer.Add(controlsContainer);
            }

            _collapseToggle = new Label("\u25BC");
            _collapseToggle.style.position = Position.Absolute;
            _collapseToggle.style.right = 8;
            _collapseToggle.style.top = 4;
            _collapseToggle.style.fontSize = 12;
            _collapseToggle.style.color = new UnityEngine.Color(0.7f, 0.7f, 0.7f);
            _collapseToggle.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            _collapseToggle.style.width = 20;
            _collapseToggle.style.height = 20;
            _collapseToggle.RegisterCallback<MouseDownEvent>(e =>
            {
                TogglePanels();
                e.StopPropagation();
            });
            titleContainer.Add(_collapseToggle);

            _panelsContainer = new VisualElement();
            _panelsContainer.style.minWidth = 350;

            _conditionPanel = new SubGraphPanel(
                "\u0423\u0441\u043B\u043E\u0432\u0438\u0435",
                _node.conditionSubGraph,
                true);
            _conditionPanel.OnChanged += OnSubGraphChanged;
            _conditionPanel.OnPanelResized += OnPanelResized;
            _panelsContainer.Add(_conditionPanel);

            _bodyPanel = new SubGraphPanel(
                "\u0422\u0435\u043B\u043E",
                _node.bodySubGraph,
                false);
            _bodyPanel.OnChanged += OnSubGraphChanged;
            _bodyPanel.OnPanelResized += OnPanelResized;
            _panelsContainer.Add(_bodyPanel);

            controlsContainer.Add(_panelsContainer);

            title = "If Statement";

            NodeViewBoundsUtils.DisableGraphViewPortCollapse(this);
            RequestBoundsSync();
        }

        private void TogglePanels()
        {
            _panelsExpanded = !_panelsExpanded;
            _panelsContainer.style.display = _panelsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _collapseToggle.text = _panelsExpanded ? "\u25BC" : "\u25B6";
            NodeViewBoundsUtils.SetFlowControlsMinHeightForCollapse(controlsContainer, _panelsExpanded);
            RequestBoundsSync();
        }

        private void OnSubGraphChanged()
        {
            _node.conditionSubGraph = _conditionPanel.SubGraph;
            _node.bodySubGraph = _bodyPanel.SubGraph;
            RequestBoundsSync();
        }

        private void OnPanelResized(SubGraphPanel _, UnityEngine.Vector2 __)
        {
            RequestBoundsSync();
        }

        public (float minW, float minH) GetResolvedMinBounds()
        {
            if (!_panelsExpanded)
                return (NodeViewBoundsUtils.FlowIfWhileCollapsedMinWidth, NodeViewBoundsUtils.FlowIfWhileCollapsedMinHeight);
            if (!_conditionPanel.IsGraphExpanded && !_bodyPanel.IsGraphExpanded)
                return (NodeViewBoundsUtils.FlowIfWhileMinWidth, NodeViewBoundsUtils.FlowIfWhileAllSubPanelsCollapsedMinHeight);
            return (NodeViewBoundsUtils.FlowIfWhileMinWidth, NodeViewBoundsUtils.FlowIfWhileMinHeight);
        }

        private void RequestBoundsSync()
        {
            _syncBoundsTask?.Pause();
            _syncBoundsTask = schedule.Execute(() =>
            {
                NodeViewBoundsUtils.RunFlowBoundsSyncTwice(this, GetResolvedMinBounds, () => !_panelsExpanded,
                    () => !_conditionPanel.IsGraphExpanded && !_bodyPanel.IsGraphExpanded);
            });
            _syncBoundsTask.ExecuteLater(10);
        }

        private void CleanupUi()
        {
            _syncBoundsTask?.Pause();
            _syncBoundsTask = null;

            if (_conditionPanel != null)
            {
                _conditionPanel.OnChanged -= OnSubGraphChanged;
                _conditionPanel.OnPanelResized -= OnPanelResized;
            }

            if (_bodyPanel != null)
            {
                _bodyPanel.OnChanged -= OnSubGraphChanged;
                _bodyPanel.OnPanelResized -= OnPanelResized;
            }

            if (_collapseToggle != null && _collapseToggle.parent == titleContainer)
                titleContainer.Remove(_collapseToggle);

            _panelsContainer?.RemoveFromHierarchy();

            _conditionPanel = null;
            _bodyPanel = null;
            _panelsContainer = null;
            _collapseToggle = null;
        }
    }
}
