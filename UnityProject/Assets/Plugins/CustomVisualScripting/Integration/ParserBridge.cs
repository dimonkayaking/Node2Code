using System;
using UnityEngine;
using VisualScripting.Core.Models;
using VisualScripting.Core.Parsers;

namespace CustomVisualScripting.Integration
{
    public static class ParserBridge
    {
        private static RoslynCodeParser _parser;
        private static bool _initialized = false;
        
        public static void Initialize()
        {
            if (_initialized) return;
            
            try
            {
                _parser = new RoslynCodeParser();
                _initialized = true;
                Debug.Log("[VS] Парсер инициализирован");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VS] Ошибка инициализации парсера: {e.Message}");
            }
        }
        
        public static ParseResult Parse(string code)
        {
            Initialize();
            
            var result = new ParseResult();
            
            try
            {
                if (_parser == null)
                {
                    result.Errors.Add("Парсер не инициализирован");
                    return result;
                }
                
                Debug.Log($"[VS] Парсинг кода ({code.Length} символов)");
                result = _parser.Parse(code);
                
                if (result.HasErrors)
                {
                    Debug.LogWarning($"[VS] Найдено ошибок: {result.Errors.Count}");
                }
                else
                {
                    Debug.Log($"[VS] Создано нод: {result.Graph.Nodes.Count}");
                }
            }
            catch (Exception e)
            {
                result.Errors.Add($"Критическая ошибка: {e.Message}");
                Debug.LogError(e);
            }
            
            return result;
        }
    }
}