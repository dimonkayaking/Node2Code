using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [System.Serializable, NodeMenuItem("Literals/Int")]
    public class IntNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LiteralInt;

        [Output("Value")]
        public int value;

        public int intValue = 0;

        public override string name => $"Int: {intValue}";
        
        protected override void Process()
        {
            value = intValue;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            if (int.TryParse(data.Value, out int parsed))
            {
                intValue = parsed;
            }
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = intValue.ToString();
            data.ValueType = "int";
            return data;
        }
    }
}