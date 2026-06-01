using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/Else")]
    public class ElseNode : BaseFlowNode
    {
        public override NodeType NodeType => NodeType.FlowElse;

        [HideInInspector]
        public GraphData bodySubGraph = new GraphData();

        public override string name => "Else";

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.BodySubGraph = bodySubGraph;
            return data;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            bodySubGraph = data.BodySubGraph ?? new GraphData();
        }
    }
}
