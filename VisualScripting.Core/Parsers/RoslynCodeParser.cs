using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VisualScripting.Core.Models;

namespace VisualScripting.Core.Parsers
{
    /// <summary>
    /// Интерфейс для парсеров, превращающих исходный код в граф нод.
    /// </summary>
    public interface ICodeParser
    {
        /// <summary>
        /// Парсит C# код и возвращает промежуточное представление в виде графа.
        /// </summary>
        GraphData Parse(string code);
    }

    /// <summary>
    /// Парсер кода на базе Microsoft Roslyn.
    /// Строит синтаксическое дерево (AST) и обходит его для формирования нод графа.
    /// </summary>
    public class RoslynCodeParser : ICodeParser
    {
        public GraphData Parse(string code)
        {
            // Создаем синтаксическое дерево из переданной строки кода
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            var graph = new GraphData();
            
            // Запускаем "обходчика" по узлам дерева
            var walker = new GraphBuilderWalker(graph);
            walker.Visit(root);
            
            return graph;
        }
    }

    /// <summary>
    /// Вспомогательный класс-визитор (Walker) для обхода синтаксического дерева Roslyn.
    /// При обходе узлов дерева создает соответствующие ноды для нашего визуального графа.
    /// </summary>
    public class GraphBuilderWalker : CSharpSyntaxWalker
    {
        private readonly GraphData _graph;
        private int _nodeCounter = 1;
        
        // Хранит ID последней распарсенной ноды для того, чтобы 
        // родительские узлы (например, математическая операция) могли подключить
        // к себе дочерние (например, литералы).
        private string? _lastNodeId;

        public GraphBuilderWalker(GraphData graph)
        {
            _graph = graph;
        }

        /// <summary>
        /// Генерирует уникальный ID для новой ноды.
        /// </summary>
        private string NextId() => $"node_{_nodeCounter++}";

        /// <summary>
        /// Вызывается при встрече выражения-утверждения (например: UnityEngine.Debug.Log(1 + 2);)
        /// </summary>
        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            base.VisitExpressionStatement(node);
        }

        /// <summary>
        /// Вызывается при встрече вызова метода.
        /// </summary>
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // Проверка на Debug.Log (для MVP)
            if (node.Expression.ToString() == "UnityEngine.Debug.Log" || node.Expression.ToString() == "Debug.Log")
            {
                var debugNode = new NodeData
                {
                    Id = NextId(),
                    Type = NodeType.DebugLog
                };
                
                // Обрабатываем аргументы вызова метода
                if (node.ArgumentList.Arguments.Count > 0)
                {
                    var arg = node.ArgumentList.Arguments[0];
                    Visit(arg.Expression); // Рекурсивный обход аргумента установит _lastNodeId
                    
                    if (!string.IsNullOrEmpty(_lastNodeId))
                    {
                        // Подключаем результат аргумента во входной порт "message"
                        debugNode.InputConnections["message"] = _lastNodeId;
                    }
                }
                
                _graph.Nodes.Add(debugNode);
                _lastNodeId = debugNode.Id;
            }
            else
            {
                base.VisitInvocationExpression(node);
            }
        }

        /// <summary>
        /// Вызывается при встрече бинарного выражения (сложение, вычитание и т.д.).
        /// </summary>
        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            NodeType type;
            switch (node.Kind())
            {
                case SyntaxKind.AddExpression: type = NodeType.MathAdd; break;
                case SyntaxKind.SubtractExpression: type = NodeType.MathSubtract; break;
                case SyntaxKind.MultiplyExpression: type = NodeType.MathMultiply; break;
                case SyntaxKind.DivideExpression: type = NodeType.MathDivide; break;
                default: 
                    base.VisitBinaryExpression(node);
                    return;
            }

            var binaryNode = new NodeData { Id = NextId(), Type = type };
            
            // Обходим левую часть выражения (например, "1" в "1 + 2")
            Visit(node.Left);
            string? leftId = _lastNodeId;
            
            // Обходим правую часть выражения (например, "2" в "1 + 2")
            Visit(node.Right);
            string? rightId = _lastNodeId;

            // Подключаем левую и правую части в порты
            if (leftId != null) binaryNode.InputConnections["left"] = leftId;
            if (rightId != null) binaryNode.InputConnections["right"] = rightId;

            _graph.Nodes.Add(binaryNode);
            _lastNodeId = binaryNode.Id;
        }

        /// <summary>
        /// Вызывается при встрече литерала (числа, строки, булевого значения).
        /// </summary>
        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            NodeType type;
            string value = node.Token.ValueText;

            if (node.Token.Value is int) type = NodeType.VariableInt;
            else if (node.Token.Value is float) type = NodeType.VariableFloat;
            else if (node.Token.Value is string) type = NodeType.VariableString;
            else if (node.Token.Value is bool) type = NodeType.VariableBool;
            else
            {
                base.VisitLiteralExpression(node);
                return;
            }

            var literalNode = new NodeData
            {
                Id = NextId(),
                Type = type,
                Value = value
            };

            _graph.Nodes.Add(literalNode);
            _lastNodeId = literalNode.Id;
        }
    }
}
