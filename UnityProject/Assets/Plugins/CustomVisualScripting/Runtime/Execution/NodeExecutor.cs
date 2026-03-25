using System;
using System.Collections.Generic;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Runtime.Execution
{
    public class NodeExecutor
    {
        private Dictionary<string, object> _variables = new Dictionary<string, object>();
        
        public object ExecuteNode(NodeData node, Dictionary<string, object> context)
        {
            if (node == null) return null;
            
            try
            {
                switch ((int)node.Type)
                {
                    case 1: // LiteralInt
                        return int.Parse(node.Value);
                        
                    case 2: // LiteralFloat
                        return float.Parse(node.Value);
                        
                    case 3: // LiteralBool
                        return bool.Parse(node.Value);
                        
                    case 4: // LiteralString
                        return node.Value;
                        
                    case 10: // MathAdd
                        var left = GetInputValue(node, "left", context);
                        var right = GetInputValue(node, "right", context);
                        if (left is int l && right is int r) return l + r;
                        if (left is float lf && right is float rf) return lf + rf;
                        return 0;
                        
                    case 11: // MathSubtract
                        left = GetInputValue(node, "left", context);
                        right = GetInputValue(node, "right", context);
                        if (left is int l2 && right is int r2) return l2 - r2;
                        if (left is float lf2 && right is float rf2) return lf2 - rf2;
                        return 0;
                        
                    case 12: // MathMultiply
                        left = GetInputValue(node, "left", context);
                        right = GetInputValue(node, "right", context);
                        if (left is int l3 && right is int r3) return l3 * r3;
                        if (left is float lf3 && right is float rf3) return lf3 * rf3;
                        return 0;
                        
                    case 13: // MathDivide
                        left = GetInputValue(node, "left", context);
                        right = GetInputValue(node, "right", context);
                        if (right is int r4 && r4 == 0) return 0;
                        if (right is float rf4 && rf4 == 0) return 0;
                        if (left is int l4 && right is int r4_2) return l4 / r4_2;
                        if (left is float lf4 && right is float rf4_2) return lf4 / rf4_2;
                        return 0;
                        
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NodeExecutor] Ошибка выполнения узла {node.Id}: {ex.Message}");
                return null;
            }
        }
        
        private object GetInputValue(NodeData node, string portName, Dictionary<string, object> context)
        {
            if (node.InputConnections != null && node.InputConnections.ContainsKey(portName))
            {
                var sourceNodeId = node.InputConnections[portName];
                if (context.ContainsKey(sourceNodeId))
                {
                    return context[sourceNodeId];
                }
            }
            return null;
        }
        
        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
        }
        
        public object GetVariable(string name)
        {
            return _variables.ContainsKey(name) ? _variables[name] : null;
        }
    }
}