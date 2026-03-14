using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualScripting.Core.Models;

namespace VisualScripting.Core.Generators
{
    /// <summary>
    /// Интерфейс для генераторов кода, превращающих граф в строку исходного кода.
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Генерирует C# код на основе переданного графа.
        /// </summary>
        string GenerateCode(GraphData graph);
    }

    /// <summary>
    /// Простой генератор кода для MVP.
    /// Ищет исполняемые узлы (например, DebugLog) и рекурсивно собирает их аргументы.
    /// </summary>
    public class SimpleCodeGenerator : ICodeGenerator
    {
        public string GenerateCode(GraphData graph)
        {
            var sb = new StringBuilder();
            var nodes = graph.Nodes.ToDictionary(n => n.Id);

            // В полноценной системе здесь должна быть топологическая сортировка и обработка 
            // потока выполнения (execution flow - белые провода в Unreal Blueprints).
            // Для MVP мы просто находим все ноды вывода в консоль и вычисляем их входы.
            var executeNodes = graph.Nodes.Where(n => n.Type == NodeType.DebugLog).ToList();

            foreach (var node in executeNodes)
            {
                string statement = GenerateStatementForNode(node, nodes);
                sb.AppendLine(statement + ";");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Рекурсивно генерирует строку кода для конкретной ноды.
        /// </summary>
        private string GenerateStatementForNode(NodeData node, Dictionary<string, NodeData> allNodes)
        {
            switch (node.Type)
            {
                case NodeType.VariableInt:
                case NodeType.VariableFloat:
                case NodeType.VariableBool:
                    return node.Value;
                case NodeType.VariableString:
                    return $"\"{node.Value}\"";

                case NodeType.MathAdd:
                case NodeType.MathSubtract:
                case NodeType.MathMultiply:
                case NodeType.MathDivide:
                    string left = GetInputValue(node, "left", allNodes);
                    string right = GetInputValue(node, "right", allNodes);
                    string op = node.Type switch
                    {
                        NodeType.MathAdd => "+",
                        NodeType.MathSubtract => "-",
                        NodeType.MathMultiply => "*",
                        NodeType.MathDivide => "/",
                        _ => "+"
                    };
                    return $"({left} {op} {right})";

                case NodeType.DebugLog:
                    string message = GetInputValue(node, "message", allNodes);
                    return $"UnityEngine.Debug.Log({message})";

                default:
                    throw new NotImplementedException($"Тип ноды {node.Type} еще не поддерживается.");
            }
        }

        /// <summary>
        /// Получает строковое представление значения, подключенного к указанному порту ноды.
        /// </summary>
        private string GetInputValue(NodeData node, string portId, Dictionary<string, NodeData> allNodes)
        {
            if (node.InputConnections.TryGetValue(portId, out string? connectedNodeId) && allNodes.TryGetValue(connectedNodeId, out NodeData? connectedNode))
            {
                return GenerateStatementForNode(connectedNode, allNodes);
            }
            return "null"; // Фолбэк, если к порту ничего не подключено
        }
    }
}
