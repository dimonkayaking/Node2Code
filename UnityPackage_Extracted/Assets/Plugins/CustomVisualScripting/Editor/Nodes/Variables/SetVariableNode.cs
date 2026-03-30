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
        public string variableNameInput;

        [Input("Value")]
        public object value;

        [Output("Out")]
        public object output;

        public new string variableName = "";

        public override string name => $"Set: {variableName}";
        
        protected override void Process()
        {
            variableNameInput = variableName;
            output = value;
            // TODO: Установить значение переменной
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            variableName = data.Value;
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = variableName;
            data.ValueType = "string";
            return data;
        }
    }
}