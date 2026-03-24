using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [System.Serializable, NodeMenuItem("Math/Divide")]
    public class DivideNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathDivide;

        [Input("A")]
        public float a;

        [Input("B")]
        public float b;

        [Output("Result")]
        public float result;

        public override string name => "Divide (/)";
        
        protected override void Process()
        {
            if (Mathf.Approximately(b, 0))
            {
                result = 0;
                UnityEngine.Debug.LogWarning("Деление на ноль в DivideNode");
            }
            else
            {
                result = a / b;
            }
        }
    }
}