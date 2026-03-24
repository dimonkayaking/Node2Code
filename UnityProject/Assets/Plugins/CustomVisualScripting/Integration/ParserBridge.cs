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
            
            return _parser.Parse(code);
        }
    }
}