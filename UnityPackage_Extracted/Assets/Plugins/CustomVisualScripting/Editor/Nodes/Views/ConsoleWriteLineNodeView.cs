using System.Linq;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor.Nodes.Flow;
using CustomVisualScripting.Editor.Nodes.Literals;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(ConsoleWriteLineNode))]
    public class ConsoleWriteLineNodeView : BaseNodeView
    {
        private ConsoleWriteLineNode _node;
        private TextField _messageField;
        private Label _inputInfoLabel;

        public override void Enable()
        {
            base.Enable();

            _node = nodeTarget as ConsoleWriteLineNode;
            if (_node == null) return;

            if (controlsContainer == null)
            {
                controlsContainer = new VisualElement();
                controlsContainer.name = "controls";
                mainContainer.Add(controlsContainer);
            }

            _messageField = new TextField("Message Text");
            _messageField.value = _node.messageText ?? "";
            _messageField.RegisterValueChangedCallback(evt => { _node.messageText = evt.newValue ?? ""; });
            controlsContainer.Add(_messageField);

            _inputInfoLabel = new Label();
            _inputInfoLabel.style.display = DisplayStyle.None;
            controlsContainer.Add(_inputInfoLabel);

            HideExecInPort();
            schedule.Execute(HideExecInPort).ExecuteLater(100);
            schedule.Execute(RefreshUiMode).Every(200);
            RefreshUiMode();
        }

        private void HideExecInPort()
        {
            if (inputPortViews == null) return;

            foreach (var port in inputPortViews.Where(p =>
                         string.Equals(p.fieldName, "execIn", System.StringComparison.OrdinalIgnoreCase)))
            {
                port.style.display = DisplayStyle.None;
                port.pickingMode = PickingMode.Ignore;
            }
        }

        private void RefreshUiMode()
        {
            var hasInput = TryGetConnectedMessageExpression(out var inputExpression);

            _messageField.SetEnabled(!hasInput);
            if (!hasInput)
            {
                var expectedValue = _node.messageText ?? "";
                if (_messageField.value != expectedValue)
                {
                    _messageField.SetValueWithoutNotify(expectedValue);
                }
                _inputInfoLabel.style.display = DisplayStyle.None;
                return;
            }

            _inputInfoLabel.text = $"Input: {inputExpression}";
            _inputInfoLabel.style.display = DisplayStyle.Flex;
        }

        private bool TryGetConnectedMessageExpression(out string expression)
        {
            expression = "";
            var messagePort = inputPortViews?.FirstOrDefault(p =>
                string.Equals(p.fieldName, "message", System.StringComparison.OrdinalIgnoreCase));
            if (messagePort == null)
                return false;

            var edge = messagePort.connections?.FirstOrDefault();
            var sourcePort = edge?.output as PortView;
            var sourceNode = sourcePort?.owner?.nodeTarget;
            if (sourceNode == null)
                return false;

            expression = ResolveSourceExpression(sourceNode);
            return true;
        }

        private static string ResolveSourceExpression(BaseNode sourceNode)
        {
            if (sourceNode is CustomBaseNode customNode)
            {
                if (!string.IsNullOrWhiteSpace(customNode.variableName))
                    return customNode.variableName;

                return customNode.NodeType switch
                {
                    NodeType.LiteralString when customNode is StringNode s => $"\"{s.stringValue}\"",
                    NodeType.LiteralInt when customNode is IntNode i => i.intValue.ToString(),
                    NodeType.LiteralFloat when customNode is FloatNode f => f.floatValue.ToString(),
                    NodeType.LiteralBool when customNode is BoolNode b => b.boolValue.ToString().ToLowerInvariant(),
                    _ => sourceNode.name
                };
            }

            return sourceNode.name;
        }
    }
}
