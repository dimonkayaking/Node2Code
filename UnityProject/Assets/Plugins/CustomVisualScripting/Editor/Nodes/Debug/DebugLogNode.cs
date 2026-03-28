using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Debug
{
    [System.Serializable, NodeMenuItem("Debug/Debug Log")]
    public class DebugLogNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.DebugLog;

        [Input("message")]
        public object message;

        public override string name => "Debug Log";
    }
}