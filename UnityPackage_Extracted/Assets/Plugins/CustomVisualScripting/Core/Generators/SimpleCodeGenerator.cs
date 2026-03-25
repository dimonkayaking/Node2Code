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
                switch ((int)node.Type)
                {
                    case 1: // LiteralInt
                        var varName = $"temp{tempCounter++}";
                        variables[node.Id] = varName;
                        sb.AppendLine($"        int {varName} = {node.Value};");
                        break;
                    case 10: // MathAdd
                        var resultVar = $"result{tempCounter++}";
                        variables[node.Id] = resultVar;
                        sb.AppendLine($"        int {resultVar} = temp0 + temp1;");
                        break;
                }
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
    }
}
