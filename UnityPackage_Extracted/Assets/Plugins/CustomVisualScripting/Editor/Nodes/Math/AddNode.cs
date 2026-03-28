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

        [Input("inputA")]
        public float inputA;

        [Input("inputB")]
        public float inputB;

        [Output("output")]
        public float output;

        public override string name => "Add (+)";
        
        protected override void Process()
        {
            output = inputA + inputB;
        }
    }
}