using System;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/For")]
    public class ForNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.FlowFor;

        [Input("init", allowMultiple = false)]
        public object init;

        [Input("condition", allowMultiple = false)]
        public bool condition;

        [Input("increment", allowMultiple = false)]
        public object increment;

        [Output("body", allowMultiple = false)]
        public object body;

        public override string name => "For Loop";

        protected override void Process()
        {
        }
    }
}