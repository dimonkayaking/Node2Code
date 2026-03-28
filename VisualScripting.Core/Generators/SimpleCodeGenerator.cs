using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualScripting.Core.Models;

namespace VisualScripting.Core.Generators
{
    public class SimpleCodeGenerator
    {
        public string GenerateCode(GraphData graph) => GenerateMvp(graph);

        public string Generate(GraphData graph) => GenerateMvp(graph);

        private static string GenerateMvp(GraphData graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
                return "// Нет узлов для генерации";

            var map = graph.Nodes.ToDictionary(n => n.Id);
            var sb = new StringBuilder();

            foreach (var node in graph.Nodes)
            {
                if (IsLiteral(node.Type) && !string.IsNullOrEmpty(node.VariableName))
                {
                    sb.AppendLine($"{KeywordFor(node.ValueType)} {node.VariableName} = {LiteralRhs(node)};");
                    continue;
                }

                if (IsMath(node.Type) && !string.IsNullOrEmpty(node.VariableName))
                {
                    sb.AppendLine(
                        $"{KeywordFor(InferResultType(graph, map, node))} {node.VariableName} = {EmitMathExpr(graph, map, node)};");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string InferResultType(GraphData graph, Dictionary<string, NodeData> map, NodeData node)
        {
            var leftE = graph.Edges.FirstOrDefault(e => e.ToNodeId == node.Id && e.ToPort == "inputA");
            if (leftE == null)
                return "int";
            var ln = map[leftE.FromNodeId];
            if (ln.Type == NodeType.LiteralFloat || ln.ValueType == "float")
                return "float";
            if (IsMath(ln.Type) && InferResultType(graph, map, ln) == "float")
                return "float";
            return "int";
        }

        private static string LiteralRhs(NodeData n) =>
            n.ValueType switch
            {
                "string" => $"\"{n.Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
                "float" => $"{n.Value}f",
                "bool" => n.Value.ToLowerInvariant(),
                _ => n.Value
            };

        private static string KeywordFor(string valueType) =>
            valueType switch
            {
                "float" => "float",
                "bool" => "bool",
                "string" => "string",
                _ => "int"
            };

        private static string EmitMathExpr(GraphData graph, Dictionary<string, NodeData> map, NodeData node)
        {
            var leftE = graph.Edges.First(e => e.ToNodeId == node.Id && e.ToPort == "inputA");
            var rightE = graph.Edges.First(e => e.ToNodeId == node.Id && e.ToPort == "inputB");
            var op = node.Type switch
            {
                NodeType.MathAdd => "+",
                NodeType.MathSubtract => "-",
                NodeType.MathMultiply => "*",
                NodeType.MathDivide => "/",
                NodeType.MathModulo => "%",
                _ => "+"
            };
            return $"{EmitOperand(graph, map, leftE.FromNodeId)} {op} {EmitOperand(graph, map, rightE.FromNodeId)}";
        }

        private static string EmitOperand(GraphData graph, Dictionary<string, NodeData> map, string nodeId)
        {
            var n = map[nodeId];
            if (IsLiteral(n.Type))
            {
                if (!string.IsNullOrEmpty(n.VariableName))
                    return n.VariableName;
                return LiteralRhs(n);
            }

            if (IsMath(n.Type))
                return $"({EmitMathExpr(graph, map, n)})";

            return n.VariableName;
        }

        private static bool IsLiteral(NodeType t) =>
            t is NodeType.LiteralBool or NodeType.LiteralInt or NodeType.LiteralFloat or NodeType.LiteralString;

        private static bool IsMath(NodeType t) =>
            t is NodeType.MathAdd or NodeType.MathSubtract or NodeType.MathMultiply or NodeType.MathDivide
                or NodeType.MathModulo;
    }
}
