using System;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/While")]
    public class WhileNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.FlowWhile;

        [Input("condition", allowMultiple = false)]
        public bool condition;

        [Output("body", allowMultiple = false)]
        public object body;

        public override string name => "While Loop";

        protected override void Process()
        {
        }
    }
}