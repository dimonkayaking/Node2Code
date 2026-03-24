using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [System.Serializable, NodeMenuItem("Flow/If")]
    public class IfNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.FlowIf;

        [Input("Condition")]
        public bool condition;

        [Input("True")]
        public object trueValue;

        [Input("False")]
        public object falseValue;

        [Output("Result")]
        public object result;

        public override string name => "If Condition";
        
        protected override void Process()
        {
            result = condition ? trueValue : falseValue;
        }
    }
}