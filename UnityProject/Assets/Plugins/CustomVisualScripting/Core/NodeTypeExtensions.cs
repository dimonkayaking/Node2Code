using VisualScripting.Core.Models;

namespace CustomVisualScripting.Core
{
    public static class NodeTypeExtensions
    {
        // Расширение для NodeType с дополнительными значениями
        public enum CustomNodeType
        {
            // Стандартные значения из VisualScripting.Core
            LiteralBool = 0,
            LiteralInt = 1,
            LiteralFloat = 2,
            LiteralString = 3,
            MathAdd = 10,
            MathSubtract = 11,
            MathMultiply = 12,
            MathDivide = 13,
            CompareEqual = 20,
            CompareGreater = 21,
            CompareLess = 22,
            FlowIf = 30,
            DebugLog = 40,
            UnityGetPosition = 50,
            UnitySetPosition = 51,
            UnityVector3 = 52,
            VariableGet = 60,
            VariableSet = 61,
            VariableDeclaration = 62
        }
        
        public static CustomNodeType ToCustomNodeType(this NodeType nodeType)
        {
            // Преобразование между типами
            return (CustomNodeType)(int)nodeType;
        }
        
        public static NodeType ToNodeType(this CustomNodeType customType)
        {
            return (NodeType)(int)customType;
        }
    }
}