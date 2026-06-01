using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    public enum EdgeResizerDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public class EdgeResizer : VisualElement
    {
        private readonly EdgeResizerDirection _direction;
        private readonly Action<Vector2> _onResize;
        private readonly PointerRootDragSession _dragSession;
        private bool _isResizing;

        public EdgeResizer(EdgeResizerDirection direction, Action<Vector2> onResize)
        {
            _direction = direction;
            _onResize = onResize;
            _dragSession = new PointerRootDragSession(
                delta => _onResize?.Invoke(delta),
                () =>
                {
                    _isResizing = false;
                    EditorUiPointerCursor.Clear(this);
                });

            style.position = Position.Absolute;

            float thickness = 6f;

            switch (direction)
            {
                case EdgeResizerDirection.Top:
                    style.top = -thickness / 2f;
                    style.left = 0;
                    style.right = 0;
                    style.height = thickness;
                    break;
                case EdgeResizerDirection.Bottom:
                    style.bottom = -thickness / 2f;
                    style.left = 0;
                    style.right = 0;
                    style.height = thickness;
                    break;
                case EdgeResizerDirection.Left:
                    style.left = -thickness / 2f;
                    style.top = 0;
                    style.bottom = 0;
                    style.width = thickness;
                    break;
                case EdgeResizerDirection.Right:
                    style.right = -thickness / 2f;
                    style.top = 0;
                    style.bottom = 0;
                    style.width = thickness;
                    break;
            }

            style.backgroundColor = new Color(0, 0, 0, 0);

            pickingMode = PickingMode.Position;

            RegisterCallback<PointerEnterEvent>(_ => ApplyHoverCursor());
            RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (!_isResizing)
                    EditorUiPointerCursor.Clear(this);
            });

            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            RegisterCallback<DetachFromPanelEvent>(_ => _dragSession.Teardown());
        }

        private void ApplyHoverCursor()
        {
            var mc = _direction is EdgeResizerDirection.Top or EdgeResizerDirection.Bottom
                ? MouseCursor.ResizeVertical
                : MouseCursor.ResizeHorizontal;
            EditorUiPointerCursor.TryApply(this, mc);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            if (!_dragSession.TryBeginFromPointer(this, evt))
                return;

            _isResizing = true;
            evt.StopPropagation();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0)
                return;

            if (!_dragSession.TryBeginFromMouse(this, evt))
                return;

            _isResizing = true;
            evt.StopPropagation();
            evt.StopImmediatePropagation();
        }
    }
}
