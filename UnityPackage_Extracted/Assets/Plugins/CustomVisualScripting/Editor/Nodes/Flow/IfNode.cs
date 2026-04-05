using System;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/If")]
    public class IfNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.FlowIf;

        [Input("condition", allowMultiple = false)]
        public bool condition;

        [Output("true", allowMultiple = false)]
        public object trueBranch;

        [Output("false", allowMultiple = false)]
        public object falseBranch;

        public override string name => "If Statement";

        protected override void Process()
        {
        }
    }
}