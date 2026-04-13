using UnityEngine;
using VisualScripting.Core.Models;
using VisualScripting.Core.Parsers;

namespace CustomVisualScripting.Integration
{
    public static class ParserBridge
    {
        private static RoslynCodeParser _parser = new RoslynCodeParser();
        
        public static void Initialize()
        {
            Debug.Log("[VS] ParserBridge инициализирован");
        }
        
        public static ParseResult Parse(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("[VS] Код пуст");
                return new ParseResult { Errors = new System.Collections.Generic.List<string> { "Код пуст" } };
            }

            var result = _parser.Parse(code);
            NormalizePorts(result.Graph);
            if (result.HasErrors)
                Debug.LogWarning($"[VS] Parse: ошибок={result.Errors.Count}");
            else
                Debug.Log($"[VS] Parse OK: Nodes={result.Graph.Nodes.Count}, Edges={result.Graph.Edges.Count}");
            return result;
        }

        private static void NormalizePorts(GraphData graph)
        {
            if (graph?.Edges == null)
                return;

            foreach (var edge in graph.Edges)
            {
                edge.FromPort = PortIds.Normalize(edge.FromPort);
                edge.ToPort = PortIds.Normalize(edge.ToPort);
            }
        }
    }
}