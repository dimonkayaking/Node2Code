using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [System.Serializable, NodeMenuItem("Literals/String")]
    public class StringNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LiteralString;

        [Output("Value")]
        public string value;

        public string stringValue = "";

        public override string name => $"String: {stringValue}";
        
        protected override void Process()
        {
            value = stringValue;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            stringValue = data.Value;
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = stringValue;
            data.ValueType = "string";
            return data;
        }
    }
}