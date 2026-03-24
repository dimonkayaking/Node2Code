using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Debug
{
    [System.Serializable, NodeMenuItem("Debug/Log")]
    public class DebugLogNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.DebugLog;

        [Input("Message")]
        public object message;

        [Input("Execute")]
        public bool execute;

        [Output("Out")]
        public bool output;

        public override string name => "Debug Log";
        
        protected override void Process()
        {
            output = execute;
            if (execute)
            {
                UnityEngine.Debug.Log($"[VS] {message}");
            }
        }
        
        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            return data;
        }
    }
}