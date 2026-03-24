using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Integration.Models;

namespace CustomVisualScripting.Integration
{
    public static class GraphConverter
    {
        public static CompleteGraphData LogicToComplete(GraphData logic, CompleteGraphData existing = null)
        {
            var complete = new CompleteGraphData
            {
                LogicGraph = logic,
                GraphOffset = existing?.GraphOffset ?? Vector2.zero,
                GraphZoom = existing?.GraphZoom ?? 1f,
                Version = "1.0"
            };
            
            var existingVisual = existing?.VisualNodes ?? new List<VisualNodeData>();
            var existingDict = existingVisual.ToDictionary(v => v.NodeId, v => v);
            
            foreach (var node in logic.Nodes)
            {
                if (existingDict.TryGetValue(node.Id, out var existingNode))
                {
                    complete.VisualNodes.Add(new VisualNodeData
                    {
                        NodeId = node.Id,
                        Position = existingNode.Position,
                        IsCollapsed = existingNode.IsCollapsed
                    });
                }
                else
                {
                    complete.VisualNodes.Add(new VisualNodeData
                    {
                        NodeId = node.Id,
                        Position = GetDefaultPosition(complete.VisualNodes.Count)
                    });
                }
            }
            
            return complete;
        }
        
        public static GraphData CompleteToLogic(CompleteGraphData complete)
        {
            return complete?.LogicGraph ?? new GraphData();
        }
        
        private static Vector2 GetDefaultPosition(int index)
        {
            float x = 100 + (index % 5) * 200;
            float y = 100 + (index / 5) * 150;
            return new Vector2(x, y);
        }
        
        public static Color GetNodeColor(NodeType type)
        {
            switch (type)
            {
                case NodeType.VariableInt:
                case NodeType.VariableFloat:
                case NodeType.VariableString:
                case NodeType.VariableBool:
                    return new Color(0.3f, 0.5f, 0.9f);
                    
                case NodeType.MathAdd:
                case NodeType.MathSubtract:
                case NodeType.MathMultiply:
                case NodeType.MathDivide:
                    return new Color(0.2f, 0.8f, 0.4f);
                    
                case NodeType.CompareGreater:
                case NodeType.CompareLess:
                case NodeType.CompareEqual:
                    return new Color(1f, 0.6f, 0.1f);
                    
                case NodeType.VariableDeclaration:
                case NodeType.VariableRead:
                case NodeType.VariableAssignment:
                    return new Color(0.7f, 0.3f, 0.8f);
                    
                case NodeType.IfStatement:
                    return new Color(0.9f, 0.2f, 0.2f);
                    
                case NodeType.Vector3Create:
                case NodeType.TransformPositionRead:
                case NodeType.TransformPositionSet:
                    return new Color(0.2f, 0.7f, 0.9f);
                    
                case NodeType.DebugLog:
                    return new Color(0.6f, 0.6f, 0.6f);
                    
                default:
                    return Color.gray;
            }
        }
        
        public static string GetNodeDisplayName(NodeType type)
        {
            switch (type)
            {
                case NodeType.VariableInt: return "Int";
                case NodeType.VariableFloat: return "Float";
                case NodeType.VariableString: return "String";
                case NodeType.VariableBool: return "Bool";
                case NodeType.MathAdd: return "Add";
                case NodeType.MathSubtract: return "Subtract";
                case NodeType.MathMultiply: return "Multiply";
                case NodeType.MathDivide: return "Divide";
                case NodeType.CompareGreater: return "Greater";
                case NodeType.CompareLess: return "Less";
                case NodeType.CompareEqual: return "Equal";
                case NodeType.VariableDeclaration: return "Declare";
                case NodeType.VariableRead: return "Get";
                case NodeType.VariableAssignment: return "Set";
                case NodeType.IfStatement: return "If";
                case NodeType.Vector3Create: return "Vector3";
                case NodeType.TransformPositionRead: return "Get Position";
                case NodeType.TransformPositionSet: return "Set Position";
                case NodeType.DebugLog: return "Debug Log";
                default: return type.ToString();
            }
        }
    }
}