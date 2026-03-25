namespace VisualScripting.Core.Models
{
    public enum NodeType
    {
        // Литералы
        LiteralInt = 1,
        LiteralFloat = 2,
        LiteralBool = 3,
        LiteralString = 4,
        
        // Математические операции
        MathAdd = 10,
        MathSubtract = 11,
        MathMultiply = 12,
        MathDivide = 13,
        
        // Сравнения
        CompareEqual = 20,
        CompareGreater = 21,
        CompareLess = 22,
        
        // Flow
        FlowIf = 30,
        
        // Debug
        DebugLog = 40,
        
        // Unity
        UnityGetPosition = 50,
        UnitySetPosition = 51,
        UnityVector3 = 52,
        
        // Переменные
        VariableGet = 60,
        VariableSet = 61,
        VariableDeclaration = 62
    }
}