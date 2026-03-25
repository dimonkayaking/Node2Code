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
            
            return _generator.Generate(graph);
        }
    }
}