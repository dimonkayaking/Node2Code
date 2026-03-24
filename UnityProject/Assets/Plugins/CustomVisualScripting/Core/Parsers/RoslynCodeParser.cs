using VisualScripting.Core.Models;

namespace VisualScripting.Core.Parsers
{
    public class RoslynCodeParser  // ← убрали static
    {
        public ParseResult Parse(string code)  // ← убрали static
        {
            return new ParseResult
            {
                Graph = new GraphData(),
                Errors = new System.Collections.Generic.List<string>()
            };
        }
    }
}