using System.Collections.Generic;
using VisualScripting.Core.Models;

namespace VisualScripting.Core.Parsers
{
    public class ParseResult
    {
        public GraphData Graph { get; set; } = new GraphData();
        public List<string> Errors { get; set; } = new List<string>();
        public bool HasErrors => Errors.Count > 0;
    }
}