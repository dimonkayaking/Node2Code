using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [System.Serializable, NodeMenuItem("Variables/Set")]
    public class SetVariableNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.VariableSet;

        [Input("Variable Name")]
        public string variableName;

        [Input("Value")]
        public object value;

        [Output("Out")]
        public object output;

        public string variableNameValue = "";

        public override string name => $"Set: {variableNameValue}";
        
        protected override void Process()
        {
            variableName = variableNameValue;
            output = value;
            // TODO: Установить значение переменной
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            variableNameValue = data.Value;
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = variableNameValue;
            data.ValueType = "string";
            return data;
        }
    }
}