using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Literals;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(IntNode))]
    public class IntNodeView : CollapsibleBodyGraphNodeView
    {
        private IntNode _node;

        public override void Enable()
        {
            base.Enable();

            _node = nodeTarget as IntNode;
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
                    ? $"Int: {_node.intValue}"
                    : $"Int: {_node.variableName} = {_node.intValue}";
            });
            controlsContainer.Add(LiteralRowLayout.Row("name", nameField));

            var valueField = new IntegerField();
            valueField.value = _node.intValue;
            valueField.RegisterValueChangedCallback(evt =>
            {
                _node.intValue = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName)
                    ? $"Int: {_node.intValue}"
                    : $"Int: {_node.variableName} = {_node.intValue}";
            });
            controlsContainer.Add(LiteralRowLayout.Row("value", valueField));

            title = string.IsNullOrEmpty(_node.variableName)
                ? $"Int: {_node.intValue}"
                : $"Int: {_node.variableName} = {_node.intValue}";

            FinishLiteralBodySetup();
        }
    }
}
