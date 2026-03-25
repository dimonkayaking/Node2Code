using VisualScripting.Core.Models;
using System.Text;

namespace VisualScripting.Core.Generators
{
    public class SimpleCodeGenerator
    {
        public string Generate(GraphData graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                return "// Нет узлов для генерации";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine("// Generated code from Visual Scripting");
            sb.AppendLine("public class GeneratedClass");
            sb.AppendLine("{");
            sb.AppendLine("    public void Execute()");
            sb.AppendLine("    {");
            
            var variables = new System.Collections.Generic.Dictionary<string, string>();
            int tempCounter = 0;
            
            foreach (var node in graph.Nodes)
            {
                switch (node.Type)
                {
                    case NodeType.LiteralInt:
                        var varName = $"temp{tempCounter++}";
                        variables[node.Id] = varName;
                        sb.AppendLine($"        int {varName} = {node.Value};");
                        break;
                        
                    case NodeType.LiteralFloat:
                        varName = $"temp{tempCounter++}";
                        variables[node.Id] = varName;
                        sb.AppendLine($"        float {varName} = {node.Value}f;");
                        break;
                        
                    case NodeType.LiteralBool:
                        varName = $"temp{tempCounter++}";
                        variables[node.Id] = varName;
                        sb.AppendLine($"        bool {varName} = {node.Value.ToLower()};");
                        break;
                        
                    case NodeType.LiteralString:
                        varName = $"temp{tempCounter++}";
                        variables[node.Id] = varName;
                        sb.AppendLine($"        string {varName} = \"{node.Value}\";");
                        break;
                        
                    case NodeType.MathAdd:
                        var resultVar = $"result_{tempCounter++}";
                        variables[node.Id] = resultVar;
                        sb.AppendLine($"        var {resultVar} = temp0 + temp1; // TODO: Connect inputs");
                        break;
                        
                    case NodeType.MathSubtract:
                        resultVar = $"result_{tempCounter++}";
                        variables[node.Id] = resultVar;
                        sb.AppendLine($"        var {resultVar} = temp0 - temp1; // TODO: Connect inputs");
                        break;
                        
                    case NodeType.MathMultiply:
                        resultVar = $"result_{tempCounter++}";
                        variables[node.Id] = resultVar;
                        sb.AppendLine($"        var {resultVar} = temp0 * temp1; // TODO: Connect inputs");
                        break;
                        
                    case NodeType.MathDivide:
                        resultVar = $"result_{tempCounter++}";
                        variables[node.Id] = resultVar;
                        sb.AppendLine($"        var {resultVar} = temp0 / temp1; // TODO: Connect inputs");
                        break;
                        
                    default:
                        sb.AppendLine($"        // Неподдерживаемый тип узла: {node.Type}");
                        break;
                }
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
    }
}