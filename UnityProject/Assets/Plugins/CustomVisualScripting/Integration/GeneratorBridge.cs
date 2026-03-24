using System;
using UnityEngine;
using VisualScripting.Core.Models;
using VisualScripting.Core.Generators;

namespace CustomVisualScripting.Integration
{
    public static class GeneratorBridge
    {
        private static SimpleCodeGenerator _generator;
        private static bool _initialized = false;
        
        public static void Initialize()
        {
            if (_initialized) return;
            
            try
            {
                _generator = new SimpleCodeGenerator();
                _initialized = true;
                Debug.Log("[VS] Генератор инициализирован");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VS] Ошибка инициализации генератора: {e.Message}");
            }
        }
        
        public static string Generate(GraphData graph)
        {
            Initialize();
            
            try
            {
                if (_generator == null)
                {
                    return "// Ошибка: генератор не инициализирован";
                }
                
                Debug.Log($"[VS] Генерация кода из {graph.Nodes.Count} нод");
                string code = _generator.GenerateCode(graph);
                Debug.Log($"[VS] Сгенерировано {code.Length} символов");
                
                return code;
            }
            catch (Exception e)
            {
                Debug.LogError($"[VS] Ошибка генерации: {e.Message}");
                return $"// Ошибка генерации: {e.Message}";
            }
        }
    }
}