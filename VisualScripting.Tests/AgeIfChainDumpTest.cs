using VisualScripting.Core.Generators;
using VisualScripting.Core.Models;
using VisualScripting.Core.Parsers;
using System.Linq;
using System.Collections.Generic;

namespace VisualScripting.Tests;

public class AgeIfChainDumpTest
{
    [Fact]
    public void DumpGraphStructure()
    {
        var parser = new RoslynCodeParser();
        var code = @"
int age = 18;

if (age < 18)
{
    Console.WriteLine(""Вы несовершеннолетний"");
}
else if (age >= 18 && age < 65)
{
    Console.WriteLine(""Вы взрослый"");
}
else
{
    Console.WriteLine(""Вы пенсионер"");
}";
        var result = parser.Parse(code);
        var g = result.Graph;
        
        var output = new System.Text.StringBuilder();
        output.AppendLine("NODES:");
        foreach (var n in g.Nodes)
            output.AppendLine($"- {n.Id} ({n.Type}) {n.VariableName}");
        
        output.AppendLine("EDGES:");
        foreach (var e in g.Edges)
            output.AppendLine($"- {e.FromNodeId}.{e.FromPort} -> {e.ToNodeId}.{e.ToPort}");
        
        var gen = new SimpleCodeGenerator();
        output.AppendLine("CODE:");
        output.AppendLine(gen.Generate(g));

        throw new System.Exception(output.ToString());
    }
}
