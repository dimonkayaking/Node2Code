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

        public string variableType = "object";

        public override string name => string.IsNullOrEmpty(variableName) ? $"Declare: {variableType}" : $"Declare: {variableName} : {variableType}";
        
        protected override void Process()
        {
            nameInput = variableName;
            output = value;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            variableType = data.ValueType;
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.ValueType = variableType;
            return data;
        }
    }
}