using VisualScripting.Core.Models;

namespace VisualScripting.Core.Generators
{
    public static class SimpleCodeGenerator
    {
        public static string Generate(GraphData graph)
        {
            return "// Generated code\n" +
                   "public class GeneratedClass\n" +
                   "{\n" +
                   "    public void Execute()\n" +
                   "    {\n" +
                   "        // TODO: Implement generation from graph\n" +
                   "    }\n" +
                   "}";
        }
    }
}