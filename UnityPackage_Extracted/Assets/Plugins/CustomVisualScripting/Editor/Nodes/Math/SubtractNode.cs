using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [System.Serializable, NodeMenuItem("Math/Subtract")]
    public class SubtractNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathSubtract;

        [Input("A")]
        public float a;

        [Input("B")]
        public float b;

        [Output("Result")]
        public float result;

        public override string name => "Subtract (-)";
        
        protected override void Process()
        {
            result = a - b;
        }
    }
}