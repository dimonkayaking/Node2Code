using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [System.Serializable, NodeMenuItem("Math/Multiply")]
    public class MultiplyNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathMultiply;

        [Input("A")]
        public float a;

        [Input("B")]
        public float b;

        [Output("Result")]
        public float result;

        public override string name => "Multiply (*)";
        
        protected override void Process()
        {
            result = a * b;
        }
    }
}