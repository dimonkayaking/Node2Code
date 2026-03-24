using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Comparison
{
    [System.Serializable, NodeMenuItem("Comparison/Less")]
    public class LessNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.CompareLess;

        [Input("left")]
        public float left;

        [Input("right")]
        public float right;

        [Output("result")]
        public bool result;

        public override string name => "Less (<)";
        
        protected override void Process()
        {
            result = left < right;
        }
    }
}