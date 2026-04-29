using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor.Nodes.Literals;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(IntNode))]
    public class IntNodeView : CollapsibleBodyGraphNodeView
    {
        private IntNode _node;
        private IntegerField _valueField;
        private Label _inputInfoLabel;

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

            _valueField = new IntegerField();
            _valueField.value = _node.intValue;
            _valueField.RegisterValueChangedCallback(evt =>
            {
                _node.intValue = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName)
                    ? $"Int: {_node.intValue}"
                    : $"Int: {_node.variableName} = {_node.intValue}";
            });
            controlsContainer.Add(LiteralRowLayout.Row("value", _valueField));

            _inputInfoLabel = new Label();
            _inputInfoLabel.style.display = DisplayStyle.None;
            controlsContainer.Add(_inputInfoLabel);

            title = string.IsNullOrEmpty(_node.variableName)
                ? $"Int: {_node.intValue}"
                : $"Int: {_node.variableName} = {_node.intValue}";

            schedule.Execute(RefreshUiMode).Every(200);
            RefreshUiMode();
            FinishLiteralBodySetup();
        }

        private void RefreshUiMode()
        {
            var hasInput = TryGetConnectedInputExpression(out var inputExpression);
            _valueField?.SetEnabled(!hasInput);
            if (_inputInfoLabel == null)
                return;
            _inputInfoLabel.text = hasInput ? $"Input: {inputExpression}" : string.Empty;
            _inputInfoLabel.style.display = hasInput ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private bool TryGetConnectedInputExpression(out string expression)
        {
            expression = "";
            var inputValuePort = inputPortViews?.FirstOrDefault(p =>
                string.Equals(p.fieldName, "inputValue", System.StringComparison.OrdinalIgnoreCase));
            if (inputValuePort == null)
                return false;

            var edge = inputValuePort.connections?.FirstOrDefault();
            var sourcePort = edge?.output as PortView;
            var sourceNode = sourcePort?.owner?.nodeTarget;
            if (sourceNode == null)
                return false;

            expression = ResolveSourceExpression(sourceNode);
            return true;
        }

        private static string ResolveSourceExpression(BaseNode sourceNode)
        {
            if (sourceNode is CustomBaseNode customNode && !string.IsNullOrWhiteSpace(customNode.variableName))
                return customNode.variableName;

            return sourceNode.name;
        }
    }
}
