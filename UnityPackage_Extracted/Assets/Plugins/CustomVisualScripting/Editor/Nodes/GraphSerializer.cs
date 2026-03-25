using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes
{
    public static class GraphSerializer
    {
        public static GraphData SerializeToGraphData(BaseGraph graph)
        {
            var graphData = new GraphData();
            
            if (graph.nodes != null)
            {
                foreach (var node in graph.nodes)
                {
                    if (node is CustomBaseNode customNode)
                    {
                        graphData.Nodes.Add(customNode.ToNodeData());
                    }
                }
            }
            
            if (graph.edges != null)
            {
                foreach (var edge in graph.edges)
                {
                    // TODO: Добавить обработку связей
                }
            }
            
            return graphData;
        }
        
        public static void DeserializeToGraph(BaseGraph graph, GraphData graphData)
        {
            if (graphData?.Nodes == null) return;
            
            // TODO: Создать узлы из данных
        }
        
        public static string GetNodeColor(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.LiteralBool or NodeType.LiteralInt or NodeType.LiteralFloat or NodeType.LiteralString => "#4CAF50",
                NodeType.MathAdd or NodeType.MathSubtract or NodeType.MathMultiply or NodeType.MathDivide => "#2196F3",
                NodeType.CompareEqual or NodeType.CompareGreater or NodeType.CompareLess => "#FF9800",
                NodeType.FlowIf => "#9C27B0",
                NodeType.DebugLog => "#F44336",
                NodeType.UnityGetPosition or NodeType.UnitySetPosition or NodeType.UnityVector3 => "#00BCD4",
                NodeType.VariableGet or NodeType.VariableSet or NodeType.VariableDeclaration => "#3F51B5",
                _ => "#757575"
            };
        }
        
        public static string GetNodeDisplayName(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.LiteralBool => "Bool",
                NodeType.LiteralInt => "Int",
                NodeType.LiteralFloat => "Float",
                NodeType.LiteralString => "String",
                NodeType.MathAdd => "Add",
                NodeType.MathSubtract => "Subtract",
                NodeType.MathMultiply => "Multiply",
                NodeType.MathDivide => "Divide",
                NodeType.CompareEqual => "Equal",
                NodeType.CompareGreater => "Greater",
                NodeType.CompareLess => "Less",
                NodeType.FlowIf => "If",
                NodeType.DebugLog => "Debug Log",
                NodeType.UnityGetPosition => "Get Position",
                NodeType.UnitySetPosition => "Set Position",
                NodeType.UnityVector3 => "Vector3",
                NodeType.VariableGet => "Get Variable",
                NodeType.VariableSet => "Set Variable",
                NodeType.VariableDeclaration => "Declare Variable",
                _ => "Unknown"
            };
        }
    }
}