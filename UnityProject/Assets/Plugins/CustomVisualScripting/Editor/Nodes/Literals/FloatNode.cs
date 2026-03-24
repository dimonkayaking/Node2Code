using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [System.Serializable, NodeMenuItem("Literals/Float")]
    public class FloatNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LiteralFloat;

        [Output("Value")]
        public float value;

        public float floatValue = 0f;

        public override string name => $"Float: {floatValue}";
        
        protected override void Process()
        {
            value = floatValue;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            if (float.TryParse(data.Value, out float parsed))
            {
                floatValue = parsed;
            }
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = floatValue.ToString();
            data.ValueType = "float";
            return data;
        }
    }
}