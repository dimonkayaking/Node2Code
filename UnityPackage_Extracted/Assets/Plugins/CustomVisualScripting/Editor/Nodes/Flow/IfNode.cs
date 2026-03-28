using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/If")]
    public class IfNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.FlowIf;

        [Input("condition")]
        public bool condition;

        [Output("true")]
        public global::CustomVisualScripting.Editor.Nodes.Base.Flow trueBranch;

        [Output("false")]
        public global::CustomVisualScripting.Editor.Nodes.Base.Flow falseBranch;

        public override string name => "If";

        protected override void Process()
        {
        }
    }
}
