using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [System.Serializable, NodeMenuItem("Math/Add")]
    public class AddNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathAdd;

        [Input("A")]
        public float a;

        [Input("B")]
        public float b;

        [Output("Result")]
        public float result;

        public override string name => "Add (+)";
        
        protected override void Process()
        {
            result = a + b;
        }
    }
}