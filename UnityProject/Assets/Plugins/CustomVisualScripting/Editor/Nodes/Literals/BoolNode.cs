using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [System.Serializable, NodeMenuItem("Literals/Bool")]
    public class BoolNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LiteralBool;

        [Output("Value")]
        public bool value;

        public bool boolValue = true;

        public override string name => $"Bool: {boolValue}";
        
        protected override void Process()
        {
            value = boolValue;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            if (bool.TryParse(data.Value, out bool parsed))
            {
                boolValue = parsed;
            }
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = boolValue.ToString();
            data.ValueType = "bool";
            return data;
        }
    }
}