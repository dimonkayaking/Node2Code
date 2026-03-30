using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [System.Serializable, NodeMenuItem("Math/Modulo")]
    public class ModuloNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathModulo;

        [Input("inputA")]
        public float a;

        [Input("inputB")]
        public float b;

        [Output("output")]
        public float result;

        public override string name => "Modulo (%)";

        protected override void Process()
        {
            result = a % b;
        }
    }
}