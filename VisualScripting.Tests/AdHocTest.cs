using VisualScripting.Core.Generators;
using VisualScripting.Core.Parsers;
using Xunit;

namespace VisualScripting.Tests
{
    public class AdHocTest
    {
        [Fact]
        public void UserInputTest()
        {
            var parser = new RoslynCodeParser();
            var generator = new SimpleCodeGenerator();
            var code = @"
int age = 18;

if (age < 18)
{
    System.Console.WriteLine(""Вы несовершеннолетний"");
}
else if (age >= 18 && age < 65)
{
    System.Console.WriteLine(""Вы взрослый"");
}
else
{
    System.Console.WriteLine(""Вы пенсионер"");
}";
            var result = parser.Parse(code);
            var output = generator.Generate(result.Graph);
            System.Console.WriteLine(output);
            Assert.Contains("else if", output);
            Assert.DoesNotContain("if ((age >= 18) && (age < 65))", output);
        }
    }
}