using System.Linq;
using Xunit;
using VisualScripting.Core.Generators;
using VisualScripting.Core.Models;
using VisualScripting.Core.Parsers;

namespace VisualScripting.Tests
{
    public class ParserCodegenTests
    {
        // ─────────────────────────────────────────────
        // Базовый цикл: Код → Граф → Код
        // ─────────────────────────────────────────────

        [Fact]
        public void TestParseAndGenerate()
        {
            string inputCode = "UnityEngine.Debug.Log(1 + 2);";
            var parser = new RoslynCodeParser();
            var generator = new SimpleCodeGenerator();

            ParseResult parseResult = parser.Parse(inputCode);
            GraphData graph = parseResult.Graph;

            Assert.False(parseResult.HasErrors);
            Assert.Equal(4, graph.Nodes.Count);

            var literal1 = graph.Nodes.First(n => n.Type == NodeType.VariableInt && n.Value == "1");
            var literal2 = graph.Nodes.First(n => n.Type == NodeType.VariableInt && n.Value == "2");
            var addNode = graph.Nodes.First(n => n.Type == NodeType.MathAdd);
            var debugNode = graph.Nodes.First(n => n.Type == NodeType.DebugLog);

            Assert.Equal(literal1.Id, addNode.InputConnections["left"]);
            Assert.Equal(literal2.Id, addNode.InputConnections["right"]);
            Assert.Equal(addNode.Id, debugNode.InputConnections["message"]);

            string generatedCode = generator.GenerateCode(graph);
            Assert.Contains("UnityEngine.Debug.Log((1 + 2));", generatedCode.Trim());
        }

        // ─────────────────────────────────────────────
        // Переменные
        // ─────────────────────────────────────────────

        [Fact]
        public void TestVariableDeclarationAndAssignment()
        {
            string code = @"
                int a = 5;
                a = a + 10;
                UnityEngine.Debug.Log(a);
            ";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var graph = parseResult.Graph;
            var declNode = graph.Nodes.First(n => n.Type == NodeType.VariableDeclaration);
            var assignNode = graph.Nodes.First(n => n.Type == NodeType.VariableAssignment);
            var logNode = graph.Nodes.First(n => n.Type == NodeType.DebugLog);

            Assert.Equal(assignNode.Id, declNode.ExecutionFlow["next"]);
            Assert.Equal(logNode.Id, assignNode.ExecutionFlow["next"]);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(graph);

            Assert.Contains("int a = 5;", generatedCode);
            Assert.Contains("a = (a + 10);", generatedCode);
            Assert.Contains("UnityEngine.Debug.Log(a);", generatedCode);
        }

        [Fact]
        public void TestVariableDeclarationWithoutInitializer()
        {
            string code = @"
                int b;
                b = 10;
            ";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(parseResult.Graph);

            Assert.Contains("int b;", generatedCode);
            Assert.Contains("b = 10;", generatedCode);
        }

        // ─────────────────────────────────────────────
        // Литералы
        // ─────────────────────────────────────────────

        [Fact]
        public void TestDoubleLiteral()
        {
            string code = @"UnityEngine.Debug.Log(1.5);";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var graph = parseResult.Graph;
            var floatNode = graph.Nodes.First(n => n.Type == NodeType.VariableFloat);
            Assert.Equal("1.5", floatNode.Value);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(graph);
            Assert.Contains("1.5", generatedCode);
        }

        // ─────────────────────────────────────────────
        // Сравнение
        // ─────────────────────────────────────────────

        [Fact]
        public void TestComparisonGreaterThan()
        {
            string code = @"
                float speed = 5;
                if (speed > 3)
                {
                    UnityEngine.Debug.Log(speed);
                }
            ";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var graph = parseResult.Graph;
            var compareNode = graph.Nodes.First(n => n.Type == NodeType.CompareGreater);
            var ifNode = graph.Nodes.First(n => n.Type == NodeType.IfStatement);

            Assert.Equal(compareNode.Id, ifNode.InputConnections["condition"]);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(graph);

            Assert.Contains("float speed = 5;", generatedCode);
            Assert.Contains("if ((speed > 3))", generatedCode);
            Assert.Contains("UnityEngine.Debug.Log(speed);", generatedCode);
        }

        [Fact]
        public void TestComparisonLessThan()
        {
            string code = @"
                int a = 10;
                if (a < 20)
                {
                    UnityEngine.Debug.Log(a);
                }
            ";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var graph = parseResult.Graph;
            Assert.Contains(graph.Nodes, n => n.Type == NodeType.CompareLess);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(graph);
            Assert.Contains("if ((a < 20))", generatedCode);
        }

        // ─────────────────────────────────────────────
        // Unity-типы
        // ─────────────────────────────────────────────

        [Fact]
        public void TestUnityTransformAndVector3()
        {
            string code = @"transform.position = new Vector3(0, 1, 0);";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var graph = parseResult.Graph;
            var setNode = graph.Nodes.First(n => n.Type == NodeType.TransformPositionSet);
            var vecNode = graph.Nodes.First(n => n.Type == NodeType.Vector3Create);

            Assert.Equal(vecNode.Id, setNode.InputConnections["value"]);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(graph);

            Assert.Contains("transform.position = new Vector3(0, 1, 0);", generatedCode);
        }

        // ─────────────────────────────────────────────
        // Управление потоком (If/Else)
        // ─────────────────────────────────────────────

        [Fact]
        public void TestIfElse()
        {
            string code = @"
                if (1)
                {
                    UnityEngine.Debug.Log(1);
                }
                else
                {
                    UnityEngine.Debug.Log(0);
                }
            ";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(parseResult.Graph);

            Assert.Contains("if (1)", generatedCode);
            Assert.Contains("else", generatedCode);
        }

        [Fact]
        public void TestIfWithMultipleStatements()
        {
            string code = @"
                if (1)
                {
                    int x = 5;
                    UnityEngine.Debug.Log(x);
                }
            ";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var graph = parseResult.Graph;
            var ifNode = graph.Nodes.First(n => n.Type == NodeType.IfStatement);
            var declNode = graph.Nodes.First(n => n.Type == NodeType.VariableDeclaration);
            var logNode = graph.Nodes.First(n => n.Type == NodeType.DebugLog);

            Assert.Equal(declNode.Id, ifNode.ExecutionFlow["true"]);
            Assert.Equal(logNode.Id, declNode.ExecutionFlow["next"]);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(graph);

            Assert.Contains("int x = 5;", generatedCode);
            Assert.Contains("UnityEngine.Debug.Log(x);", generatedCode);
        }

        // ─────────────────────────────────────────────
        // Обработка ошибок и защита
        // ─────────────────────────────────────────────

        [Fact]
        public void TestSyntaxErrorReporting()
        {
            string code = @"int a = 5";
            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);

            Assert.True(parseResult.HasErrors);
            Assert.NotEmpty(parseResult.Errors);
            Assert.Contains(";", parseResult.Errors[0]);
        }

        [Fact]
        public void TestCyclicGraphDoesNotCrash()
        {
            var graph = new GraphData();
            var nodeA = new NodeData { Id = "a", Type = NodeType.DebugLog };
            var nodeB = new NodeData { Id = "b", Type = NodeType.DebugLog };
            nodeA.ExecutionFlow["next"] = "b";
            nodeB.ExecutionFlow["next"] = "a";
            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(graph);
            Assert.NotNull(generatedCode);
        }

        // ─────────────────────────────────────────────
        // Интеграционный тест (полный демо-скрипт)
        // ─────────────────────────────────────────────

        [Fact]
        public void TestFullDemoScript()
        {
            string code = @"
                float speed = 5;
                float offset = speed * 0.1;
                if (speed > 3)
                {
                    transform.position = new Vector3(offset, 0, 0);
                    UnityEngine.Debug.Log(speed);
                }
                else
                {
                    UnityEngine.Debug.Log(0);
                }
            ";

            var parser = new RoslynCodeParser();
            var parseResult = parser.Parse(code);
            Assert.False(parseResult.HasErrors);

            var generator = new SimpleCodeGenerator();
            string generatedCode = generator.GenerateCode(parseResult.Graph);

            Assert.Contains("float speed = 5;", generatedCode);
            Assert.Contains("float offset = (speed * 0.1);", generatedCode);
            Assert.Contains("if ((speed > 3))", generatedCode);
            Assert.Contains("transform.position = new Vector3(offset, 0, 0);", generatedCode);
            Assert.Contains("UnityEngine.Debug.Log(speed);", generatedCode);
            Assert.Contains("else", generatedCode);
            Assert.Contains("UnityEngine.Debug.Log(0);", generatedCode);
        }
    }
}
