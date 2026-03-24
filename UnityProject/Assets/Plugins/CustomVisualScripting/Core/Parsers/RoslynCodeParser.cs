using VisualScripting.Core.Models;
using VisualScripting.Core.Parsers;

namespace VisualScripting.Core.Parsers
{
    public static class RoslynCodeParser
    {
        public static ParseResult Parse(string code)
        {
            // Временная реализация
            return new ParseResult
            {
                Graph = new GraphData(),
                Errors = new System.Collections.Generic.List<string>()
            };
        }
    }
}