using UnityEngine;
using VisualScripting.Core.Models;
using VisualScripting.Core.Generators;

namespace CustomVisualScripting.Integration
{
    public static class GeneratorBridge
    {
        private static SimpleCodeGenerator _generator = new SimpleCodeGenerator();
        
        public static void Initialize()
        {
            Debug.Log("[VS] GeneratorBridge инициализирован");
        }
        
        public static string Generate(GraphData graph)
        {
            if (graph == null)
            {
                Debug.LogError("[VS] GraphData пуст");
                return "";
            }

            NormalizePorts(graph);
            return _generator.Generate(graph);
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