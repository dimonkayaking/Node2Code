using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Comparison
{
    [System.Serializable, NodeMenuItem("Comparison/Greater")]
    public class GreaterNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.CompareGreater;

        [Input("left")]
        public float left;

        [Input("right")]
        public float right;

        [Output("result")]
        public bool result;

        public override string name => "Greater (>)";
        
        protected override void Process()
        {
            result = left > right;
        }
    }
}