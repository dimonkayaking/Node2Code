using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    /// <summary>
    /// Перетаскивание с подпиской на корень панели: поддержка и Pointer-, и Mouse-событий (GraphView).
    /// </summary>
    internal sealed class PointerRootDragSession
    {
        private readonly Action<Vector2> _onDelta;
        private readonly Action _onEnd;

        private VisualElement _root;
        private int _pointerId = -1;
        private bool _listening;
        private bool _useMousePipe;

        private Vector2 _lastPointerPos;

        private readonly EventCallback<PointerMoveEvent> _pointerMoveHandler;
        private readonly EventCallback<PointerUpEvent> _pointerUpHandler;
        private readonly EventCallback<MouseMoveEvent> _mouseMoveHandler;
        private readonly EventCallback<MouseUpEvent> _mouseUpHandler;

        public PointerRootDragSession(Action<Vector2> onDelta, Action onEnd)
        {
            _onDelta = onDelta;
            _onEnd = onEnd;
            _pointerMoveHandler = OnPointerMove;
            _pointerUpHandler = OnPointerUp;
            _mouseMoveHandler = OnMouseMoveRoot;
            _mouseUpHandler = OnMouseUpRoot;
        }

        public bool TryBegin(VisualElement handle, PointerDownEvent evt) =>
            TryBeginFromPointer(handle, evt);

        public bool TryBeginFromPointer(VisualElement handle, PointerDownEvent evt)
        {
            if (evt.button != 0 || _listening)
                return false;

            _root = handle.panel?.visualTree;
            if (_root == null)
                return false;

            _useMousePipe = false;
            _pointerId = evt.pointerId;
            _lastPointerPos = evt.position;
            _root.RegisterCallback(_pointerMoveHandler, TrickleDown.TrickleDown);
            _root.RegisterCallback(_pointerUpHandler, TrickleDown.TrickleDown);
            _listening = true;
            return true;
        }

        public bool TryBeginFromMouse(VisualElement handle, MouseDownEvent evt)
        {
            if (evt.button != 0 || _listening)
                return false;

            _root = handle.panel?.visualTree;
            if (_root == null)
                return false;

            _useMousePipe = true;
            _root.RegisterCallback(_mouseMoveHandler, TrickleDown.TrickleDown);
            _root.RegisterCallback(_mouseUpHandler, TrickleDown.TrickleDown);
            _listening = true;
            return true;
        }

        public void Teardown()
        {
            if (!_listening || _root == null)
                return;

            if (_useMousePipe)
            {
                _root.UnregisterCallback(_mouseMoveHandler, TrickleDown.TrickleDown);
                _root.UnregisterCallback(_mouseUpHandler, TrickleDown.TrickleDown);
            }
            else
            {
                _root.UnregisterCallback(_pointerMoveHandler, TrickleDown.TrickleDown);
                _root.UnregisterCallback(_pointerUpHandler, TrickleDown.TrickleDown);
            }

            _listening = false;
            _useMousePipe = false;
            _root = null;
            _pointerId = -1;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_listening || _useMousePipe || evt.pointerId != _pointerId)
                return;

            Vector2 delta = evt.deltaPosition;
            var pos = (Vector2)evt.position;
            if (delta.sqrMagnitude < 1e-8f)
                delta = pos - _lastPointerPos;

            _lastPointerPos = pos;
            _onDelta?.Invoke(delta);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_listening || _useMousePipe || evt.pointerId != _pointerId)
                return;

            Teardown();
            _onEnd?.Invoke();
        }

        private void OnMouseMoveRoot(MouseMoveEvent evt)
        {
            if (!_listening || !_useMousePipe)
                return;

            _onDelta?.Invoke(evt.mouseDelta);
        }

        private void OnMouseUpRoot(MouseUpEvent evt)
        {
            if (!_listening || !_useMousePipe || evt.button != 0)
                return;

            Teardown();
            _onEnd?.Invoke();
        }
    }
}
