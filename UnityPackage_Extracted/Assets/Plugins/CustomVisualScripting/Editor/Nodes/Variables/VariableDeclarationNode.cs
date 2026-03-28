using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [System.Serializable, NodeMenuItem("Variables/Declaration")]
    public class VariableDeclarationNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.VariableDeclaration;

        [Input("Variable Name")]
        public string nameInput;

        [Input("Value")]
        public object value;

        [Output("Out")]
        public object output;

        public string variableNameValue = "";
        public string variableType = "object";

        public override string name => $"Declare: {variableNameValue} : {variableType}";

        protected override void Process()
        {
            nameInput = variableNameValue;
            output = value;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            variableNameValue = data.Value;
            variableType = data.ValueType;
        }

        public override NodeData ToNodeData()
        {
            variableName = variableNameValue;
            var data = base.ToNodeData();
            data.Value = variableNameValue;
            data.ValueType = variableType;
            return data;
        }
    }
}
