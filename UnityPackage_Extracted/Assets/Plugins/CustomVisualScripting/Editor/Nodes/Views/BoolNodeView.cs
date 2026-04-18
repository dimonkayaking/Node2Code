using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Literals;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(BoolNode))]
    public class BoolNodeView : CollapsibleBodyGraphNodeView
    {
        private BoolNode _node;

        public override void Enable()
        {
            base.Enable();

            _node = nodeTarget as BoolNode;
            if (_node == null) return;

            if (controlsContainer == null)
            {
                controlsContainer = new VisualElement();
                controlsContainer.name = "controls";
                mainContainer.Add(controlsContainer);
            }

            controlsContainer.style.flexDirection = FlexDirection.Column;
            controlsContainer.style.alignSelf = Align.Stretch;

            var nameField = new TextField();
            nameField.value = _node.variableName;
            nameField.RegisterValueChangedCallback(evt =>
            {
                _node.variableName = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName)
                    ? $"Bool: {_node.boolValue}"
                    : $"Bool: {_node.variableName} = {_node.boolValue}";
            });
            controlsContainer.Add(LiteralRowLayout.Row("name", nameField));

            var valueField = new Toggle();
            valueField.value = _node.boolValue;
            valueField.RegisterValueChangedCallback(evt =>
            {
                _node.boolValue = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName)
                    ? $"Bool: {_node.boolValue}"
                    : $"Bool: {_node.variableName} = {_node.boolValue}";
            });
            controlsContainer.Add(LiteralRowLayout.Row("value", valueField));

            title = string.IsNullOrEmpty(_node.variableName)
                ? $"Bool: {_node.boolValue}"
                : $"Bool: {_node.variableName} = {_node.boolValue}";

            FinishLiteralBodySetup();
        }
    }
}
