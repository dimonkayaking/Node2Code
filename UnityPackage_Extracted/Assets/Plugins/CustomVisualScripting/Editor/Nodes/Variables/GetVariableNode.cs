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
        public string variableNameInput;

        [Output("Value")]
        public object value;

        public new string variableName = "";

        public override string name => $"Get: {variableName}";
        
        protected override void Process()
        {
            variableNameInput = variableName;
            // TODO: Получить значение из переменной
            value = null;
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