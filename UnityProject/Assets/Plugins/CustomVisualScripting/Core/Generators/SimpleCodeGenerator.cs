using VisualScripting.Core.Models;

namespace VisualScripting.Core.Generators
{
    public class SimpleCodeGenerator  // ← убрали static
    {
        public string Generate(GraphData graph)  // ← убрали static
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