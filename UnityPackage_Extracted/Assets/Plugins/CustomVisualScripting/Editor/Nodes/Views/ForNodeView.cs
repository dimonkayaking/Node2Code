using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Flow;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(ForNode))]
    public class ForNodeView : BaseNodeView
    {
        private ForNode _node;
        private SubGraphPanel _conditionPanel;
        private SubGraphPanel _bodyPanel;
        private VisualElement _panelsContainer;
        private Label _collapseToggle;
        private bool _panelsExpanded = true;

        public override void Enable()
        {
            base.Enable();

            _node = nodeTarget as ForNode;
            if (_node == null) return;

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
            _panelsContainer.Add(_conditionPanel);

            _bodyPanel = new SubGraphPanel(
                "\u0422\u0435\u043B\u043E",
                _node.bodySubGraph,
                false);
            _bodyPanel.OnChanged += OnSubGraphChanged;
            _panelsContainer.Add(_bodyPanel);

            controlsContainer.Add(_panelsContainer);

            title = "For Loop";
        }

        private void TogglePanels()
        {
            _panelsExpanded = !_panelsExpanded;
            _panelsContainer.style.display = _panelsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            _collapseToggle.text = _panelsExpanded ? "\u25BC" : "\u25B6";
        }

        private void OnSubGraphChanged()
        {
            _node.conditionSubGraph = _conditionPanel.SubGraph;
            _node.bodySubGraph = _bodyPanel.SubGraph;
        }
    }
}
