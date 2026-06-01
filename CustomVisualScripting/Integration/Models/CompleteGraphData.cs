using System;
using System.Collections.Generic;
using UnityEngine;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Integration.Models
{
    [Serializable]
    public class CompleteGraphData
    {
        public GraphData LogicGraph;
        public List<VisualNodeData> VisualNodes;
        public Vector2 GraphOffset;
        public float GraphZoom;
        public string Version;
        
        public CompleteGraphData()
        {
            LogicGraph = new GraphData();
            VisualNodes = new List<VisualNodeData>();
            GraphOffset = Vector2.zero;
            GraphZoom = 1f;
            Version = "1.0";
        }
        
        public CompleteGraphData(GraphData logic, List<VisualNodeData> visual)
        {
            LogicGraph = logic;
            VisualNodes = visual ?? new List<VisualNodeData>();
            GraphOffset = Vector2.zero;
            GraphZoom = 1f;
            Version = "1.0";
        }
    }
}