using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/Else")]
    public class ElseNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.FlowElse;

        [Input("execIn")]
        public object execIn;

        [Output("execOut")]
        public object execOut;

        public override string name => "Else";

        protected override void Process()
        {
        }
    }
}