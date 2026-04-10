using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Runtime.Execution
{
    public class GraphRunner
    {
        private NodeExecutor _executor = new NodeExecutor();
        private Dictionary<string, object> _context = new Dictionary<string, object>();
        private Dictionary<string, object> _variables = new Dictionary<string, object>();

        public event Action<string, LogType> OnLogMessage;

        public void Run(GraphData graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                SendLog("[GraphRunner] Граф пуст", LogType.Warning);
                return;
            }

            try
            {
                SendLog($"[GraphRunner] Запуск графа: {graph.Nodes.Count} нод, {graph.Edges.Count} связей",
                    LogType.Log);

                var hasIncomingExec = new HashSet<string>();
                foreach (var edge in graph.Edges)
                {
                    if (edge.ToPort == "execIn")
                        hasIncomingExec.Add(edge.ToNodeId);
                }

                var startNodes = graph.Nodes
                    .Where(n => !hasIncomingExec.Contains(n.Id) && IsFlowNode(n.Type))
                    .ToList();

                if (startNodes.Count == 0)
                {
                    ExecuteDataOnly(graph);
                    return;
                }

                foreach (var startNode in startNodes)
                {
                    ExecuteFlow(startNode.Id, graph);
                }

                SendLog("[GraphRunner] Выполнение завершено", LogType.Log);
            }
            catch (Exception ex)
            {
                SendLog($"[GraphRunner] Ошибка: {ex.Message}", LogType.Error);
            }
        }

        private void ExecuteFlow(string nodeId, GraphData graph, string fromPort = null)
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return;

            SendLog($"[Flow] Выполнение: {node.Type}", LogType.Log);

            if (node.Type == NodeType.FlowIf && HasSubGraphs(node))
            {
                ExecuteNewIfNode(node, graph);
                return;
            }

            if (node.Type == NodeType.FlowElse && node.BodySubGraph != null && node.BodySubGraph.Nodes.Count > 0)
            {
                ExecuteSubGraph(node.BodySubGraph);
                var execOutEdge = graph.Edges.FirstOrDefault(
                    e => e.FromNodeId == nodeId && e.FromPort == "execOut");
                if (execOutEdge != null)
                    ExecuteFlow(execOutEdge.ToNodeId, graph);
                return;
            }

            var inputs = GetInputValues(node, graph);
            var result = _executor.ExecuteNode(node, inputs, _variables);
            if (result != null)
            {
                _context[nodeId] = result;
            }

            if (!string.IsNullOrEmpty(node.VariableName) && result != null)
            {
                SetVariable(node.VariableName, result);
            }

            string nextPort = GetNextPort(node, result);

            var nextEdge = graph.Edges.FirstOrDefault(e => e.FromNodeId == nodeId && e.FromPort == nextPort);
            if (nextEdge != null)
            {
                ExecuteFlow(nextEdge.ToNodeId, graph);
            }
        }

        private void ExecuteNewIfNode(NodeData node, GraphData graph)
        {
            bool condition = EvaluateConditionSubGraph(node.ConditionSubGraph);
            SendLog($"[Flow] If условие = {condition}", LogType.Log);

            if (condition)
            {
                if (node.BodySubGraph != null && node.BodySubGraph.Nodes.Count > 0)
                    ExecuteSubGraph(node.BodySubGraph);

                var execOutEdge = graph.Edges.FirstOrDefault(
                    e => e.FromNodeId == node.Id && e.FromPort == "execOut");
                if (execOutEdge != null)
                    ExecuteFlow(execOutEdge.ToNodeId, graph);
            }
            else
            {
                var falseEdge = graph.Edges.FirstOrDefault(
                    e => e.FromNodeId == node.Id &&
                         (e.FromPort == "false" || e.FromPort == "falseBranch"));

                if (falseEdge != null)
                {
                    ExecuteFlow(falseEdge.ToNodeId, graph);
                }

                var execOutEdge = graph.Edges.FirstOrDefault(
                    e => e.FromNodeId == node.Id && e.FromPort == "execOut");
                if (execOutEdge != null)
                    ExecuteFlow(execOutEdge.ToNodeId, graph);
            }
        }

        private bool EvaluateConditionSubGraph(GraphData condGraph)
        {
            if (condGraph == null || condGraph.Nodes.Count == 0) return false;

            var order = GetTopologicalOrder(condGraph);
            var context = new Dictionary<string, object>();
            object lastResult = false;

            foreach (var nodeId in order)
            {
                var node = condGraph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null) continue;

                if (IsLiteralNode(node.Type) && !string.IsNullOrEmpty(node.VariableName))
                {
                    if (_variables.TryGetValue(node.VariableName, out var varVal))
                    {
                        context[nodeId] = varVal;
                        lastResult = varVal;
                        continue;
                    }
                }

                var inputs = GetSubGraphInputValues(node, condGraph, context);
                var result = _executor.ExecuteNode(node, inputs, _variables);
                if (result != null)
                {
                    context[nodeId] = result;
                    lastResult = result;
                }
            }

            return lastResult is bool b && b;
        }

        private void ExecuteSubGraph(GraphData subGraph)
        {
            var hasIncomingExec = new HashSet<string>();
            foreach (var edge in subGraph.Edges)
            {
                if (edge.ToPort == "execIn")
                    hasIncomingExec.Add(edge.ToNodeId);
            }

            var startNodes = subGraph.Nodes
                .Where(n => !hasIncomingExec.Contains(n.Id) && IsFlowNode(n.Type))
                .ToList();

            if (startNodes.Count > 0)
            {
                foreach (var start in startNodes)
                    ExecuteFlow(start.Id, subGraph);
                return;
            }

            var order = GetTopologicalOrder(subGraph);
            foreach (var nodeId in order)
            {
                var node = subGraph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null) continue;

                if (IsLiteralNode(node.Type) && !string.IsNullOrEmpty(node.VariableName))
                {
                    if (_variables.TryGetValue(node.VariableName, out var varVal))
                    {
                        _context[nodeId] = varVal;
                        continue;
                    }
                }

                var inputs = GetSubGraphInputValues(node, subGraph, _context);
                var result = _executor.ExecuteNode(node, inputs, _variables);
                if (result != null)
                    _context[nodeId] = result;

                if (!string.IsNullOrEmpty(node.VariableName) && result != null)
                    SetVariable(node.VariableName, result);
            }
        }

        private Dictionary<string, object> GetSubGraphInputValues(
            NodeData node, GraphData graph, Dictionary<string, object> context)
        {
            var inputs = new Dictionary<string, object>();
            foreach (var edge in graph.Edges.Where(e => e.ToNodeId == node.Id))
            {
                if (context.TryGetValue(edge.FromNodeId, out var value))
                    inputs[edge.ToPort] = value;
            }

            return inputs;
        }

        private static bool HasSubGraphs(NodeData node)
        {
            return node.ConditionSubGraph != null && node.ConditionSubGraph.Nodes.Count > 0;
        }

        private static bool IsLiteralNode(NodeType type) =>
            type is NodeType.LiteralBool or NodeType.LiteralInt
                or NodeType.LiteralFloat or NodeType.LiteralString;

        private Dictionary<string, object> GetInputValues(NodeData node, GraphData graph)
        {
            var inputs = new Dictionary<string, object>();

            foreach (var edge in graph.Edges.Where(e => e.ToNodeId == node.Id))
            {
                if (_context.TryGetValue(edge.FromNodeId, out var value))
                {
                    inputs[edge.ToPort] = value;
                }
            }

            return inputs;
        }

        private string GetNextPort(NodeData node, object result)
        {
            return node.Type switch
            {
                NodeType.FlowIf => (result is bool b && b) ? "true" : "false",
                NodeType.FlowElse => "execOut",
                NodeType.FlowFor => "body",
                NodeType.FlowWhile => "body",
                NodeType.ConsoleWriteLine => "execOut",
                _ => "execOut"
            };
        }

        private bool IsFlowNode(NodeType type)
        {
            return type is NodeType.FlowIf or NodeType.FlowElse or NodeType.FlowFor
                or NodeType.FlowWhile or NodeType.ConsoleWriteLine;
        }

        private void ExecuteDataOnly(GraphData graph)
        {
            var order = GetTopologicalOrder(graph);

            foreach (var nodeId in order)
            {
                var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null) continue;

                if (IsFlowNode(node.Type)) continue;

                var inputs = GetInputValues(node, graph);
                var result = _executor.ExecuteNode(node, inputs, _variables);
                if (result != null)
                {
                    _context[nodeId] = result;
                }

                if (!string.IsNullOrEmpty(node.VariableName) && result != null)
                {
                    SetVariable(node.VariableName, result);
                }
            }
        }

        private List<string> GetTopologicalOrder(GraphData graph)
        {
            var order = new List<string>();
            var visited = new HashSet<string>();

            foreach (var node in graph.Nodes)
            {
                VisitNode(node.Id, graph, visited, order);
            }

            return order;
        }

        private void VisitNode(string nodeId, GraphData graph, HashSet<string> visited, List<string> order)
        {
            if (visited.Contains(nodeId)) return;
            visited.Add(nodeId);

            var inputs = graph.Edges.Where(e => e.ToNodeId == nodeId).Select(e => e.FromNodeId);
            foreach (var inputId in inputs)
            {
                VisitNode(inputId, graph, visited, order);
            }

            order.Add(nodeId);
        }

        private void SendLog(string message, LogType type)
        {
            Debug.Log($"[VS] {message}");
            OnLogMessage?.Invoke(message, type);
        }

        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
            _executor.SetVariable(name, value);
        }

        public object GetVariable(string name)
        {
            return _variables.TryGetValue(name, out var v) ? v : null;
        }

        public void Clear()
        {
            _context.Clear();
            _variables.Clear();
            _executor.Clear();
        }
    }
}
