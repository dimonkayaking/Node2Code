using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [System.Serializable, NodeMenuItem("Variables/Get")]
    public class GetVariableNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.VariableGet;

        [Input("Variable Name")]
        public string variableName;

        [Output("Value")]
        public object value;

        public string variableNameValue = "";

        public override string name => $"Get: {variableNameValue}";
        
        protected override void Process()
        {
            variableName = variableNameValue;
            // TODO: Получить значение из переменной
            value = null;
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