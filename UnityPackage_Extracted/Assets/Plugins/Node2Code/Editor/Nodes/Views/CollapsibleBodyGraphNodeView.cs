using GraphProcessor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    /// <summary>
    /// Ноды с полями в <see cref="BaseNodeView.controlsContainer"/>.
    /// Стандартный <see cref="Capabilities.Collapsible"/> у Unity Node завязан на <c>expanded</c> и скрывает порты;
    /// здесь Collapsible отключён, сворачивается только тело по отдельной кнопке в заголовке.
    /// </summary>
    public abstract class CollapsibleBodyGraphNodeView : BaseNodeView
    {
        private bool _bodyExpanded = true;
        private Label _bodyCollapseToggle;

        /// <summary>Состояние блока полей (не путать с <c>expanded</c> GraphView для портов).</summary>
        protected bool IsBodyExpanded => _bodyExpanded;

        /// <summary>Для линии под плашками портов: только когда тело развернуто.</summary>
        public bool IsLiteralBodyExpanded => _bodyExpanded;

        public virtual (float minW, float minH) GetLiteralBoundsMins() =>
            _bodyExpanded ? (200f, 88f) : (180f, 52f);

        protected void ApplyBodyVisibility()
        {
            if (controlsContainer == null)
                return;
            controlsContainer.style.display = _bodyExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected void ScheduleBodyBoundsSync()
        {
            schedule.Execute(() =>
            {
                var (mw, mh) = GetLiteralBoundsMins();
                NodeViewBoundsUtils.ApplyNodeMinStyle(this, mw, mh);
                
                if (!_bodyExpanded)
                    NodeViewBoundsUtils.ForceSnapNodeSize(this, mw, mh);
                else
                    NodeViewBoundsUtils.SyncNodeRectToLayout(this, mw, mh);

                NodeViewBoundsUtils.ShrinkNodeRectToMeasuredLayout(this, mw, mh);
                NodeViewBoundsUtils.EnsurePortSectionBottomDivider(this);
                NodeViewBoundsUtils.RefreshPortDividerVisualColor(this);
            }).ExecuteLater(10);
        }

        /// <summary>Вызывать в конце Enable() после заполнения controlsContainer.</summary>
        protected void FinishLiteralBodySetup()
        {
            if (_bodyCollapseToggle != null && _bodyCollapseToggle.parent != null)
                titleContainer.Remove(_bodyCollapseToggle);

            capabilities &= ~Capabilities.Collapsible;
            base.expanded = true;

            _bodyCollapseToggle = new Label("\u25BC");
            _bodyCollapseToggle.style.position = Position.Absolute;
            _bodyCollapseToggle.style.right = 8;
            _bodyCollapseToggle.style.top = 4;
            _bodyCollapseToggle.style.fontSize = 12;
            _bodyCollapseToggle.style.color = new Color(0.7f, 0.7f, 0.7f);
            _bodyCollapseToggle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _bodyCollapseToggle.style.width = 20;
            _bodyCollapseToggle.style.height = 20;
            _bodyCollapseToggle.RegisterCallback<MouseDownEvent>(e =>
            {
                _bodyExpanded = !_bodyExpanded;
                _bodyCollapseToggle.text = _bodyExpanded ? "\u25BC" : "\u25B6";
                ApplyBodyVisibility();
                ScheduleBodyBoundsSync();
                e.StopPropagation();
            });
            titleContainer.Add(_bodyCollapseToggle);

            _bodyExpanded = true;
            ApplyBodyVisibility();

            if (controlsContainer != null)
            {
                controlsContainer.style.paddingBottom = 0;
                controlsContainer.style.marginBottom = 0;
                controlsContainer.style.flexShrink = 1;
                controlsContainer.style.alignItems = Align.Stretch;
            }

            NodeViewBoundsUtils.DisableGraphViewPortCollapse(this);
            ScheduleBodyBoundsSync();
        }
    }
}
