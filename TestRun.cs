using VisualScripting.Core.Generators;
using VisualScripting.Core.Parsers;
using System;

class Program
{
    static void Main()
    {
        var parser = new RoslynCodeParser();
        var generator = new SimpleCodeGenerator();
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
        if (result.HasErrors)
        {
            Console.WriteLine("ERRORS:\n" + string.Join("\n", result.Errors));
            return;
        }
        var output = generator.Generate(result.Graph);
        Console.WriteLine("OUTPUT:");
        Console.WriteLine(output);
    }
}
