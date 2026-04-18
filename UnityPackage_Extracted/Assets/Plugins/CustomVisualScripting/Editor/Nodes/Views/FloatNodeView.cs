using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Literals;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(FloatNode))]
    public class FloatNodeView : CollapsibleBodyGraphNodeView
    {
        private FloatNode _node;

        public override void Enable()
        {
            base.Enable();

            _node = nodeTarget as FloatNode;
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
                    ? $"Float: {_node.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                    : $"Float: {_node.variableName} = {_node.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            });
            controlsContainer.Add(LiteralRowLayout.Row("name", nameField));

            var valueField = new FloatField();
            valueField.value = _node.floatValue;
            valueField.RegisterValueChangedCallback(evt =>
            {
                _node.floatValue = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName)
                    ? $"Float: {_node.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                    : $"Float: {_node.variableName} = {_node.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            });
            controlsContainer.Add(LiteralRowLayout.Row("value", valueField));

            title = string.IsNullOrEmpty(_node.variableName)
                ? $"Float: {_node.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                : $"Float: {_node.variableName} = {_node.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            FinishLiteralBodySetup();
        }
    }
}
