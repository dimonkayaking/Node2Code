using System;
using UnityEngine;

namespace CustomVisualScripting.Integration.Models
{
    [Serializable]
    public class VisualNodeData
    {
        public string NodeId;
        public Vector2 Position;
        public bool IsCollapsed;
        
        public VisualNodeData()
        {
            NodeId = string.Empty;
            Position = Vector2.zero;
            IsCollapsed = false;
        }
        
        public VisualNodeData(string nodeId, Vector2 position)
        {
            NodeId = nodeId;
            Position = position;
            IsCollapsed = false;
        }
    }
}