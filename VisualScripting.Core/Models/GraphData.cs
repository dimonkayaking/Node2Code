using System.Collections.Generic;

namespace VisualScripting.Core.Models
{
    /// <summary>
    /// Типы узлов (нод), поддерживаемые в текущем MVP.
    /// </summary>
    public enum NodeType
    {
        VariableInt,
        VariableFloat,
        VariableString,
        VariableBool,
        MathAdd,
        MathSubtract,
        MathMultiply,
        MathDivide,
        DebugLog
    }

    /// <summary>
    /// Представляет один узел (ноду) в визуальном графе.
    /// </summary>
    public class NodeData
    {
        /// <summary>
        /// Уникальный идентификатор ноды (например, сгенерированный GUID или строковый ID).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Тип ноды (определяет ее логику и визуальное представление).
        /// </summary>
        public NodeType Type { get; set; }

        /// <summary>
        /// Строковое значение для нод-литералов (переменных).
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Словарь входящих соединений.
        /// Ключ - название порта (например, "left", "right", "message").
        /// Значение - Id другой ноды, выход которой подключен к этому порту.
        /// </summary>
        public Dictionary<string, string> InputConnections { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Промежуточное представление (IR) графа.
    /// Этот класс служит контрактом между парсером кода и системой визуальных нод.
    /// </summary>
    public class GraphData
    {
        /// <summary>
        /// Список всех нод, находящихся на графе.
        /// </summary>
        public List<NodeData> Nodes { get; set; } = new List<NodeData>();
    }
}
