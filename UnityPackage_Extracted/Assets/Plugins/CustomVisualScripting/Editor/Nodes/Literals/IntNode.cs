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

        [Output("output")]
        public int output;

        public int intValue = 0;

        public override string name => string.IsNullOrEmpty(variableName) ? $"Int: {intValue}" : $"{variableName} = {intValue}";

        protected override void Process()
        {
            output = intValue;
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