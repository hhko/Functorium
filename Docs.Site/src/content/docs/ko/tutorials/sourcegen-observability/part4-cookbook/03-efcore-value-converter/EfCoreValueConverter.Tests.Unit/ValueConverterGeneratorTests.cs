using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using EfCoreValueConverter.Generator;

namespace EfCoreValueConverter.Tests.Unit;

public class ValueConverterGeneratorTests
{
    [Fact]
    public void Generator_Creates_Converter_For_Class_With_Value_Property()
    {
        var source = """
            using EfCoreValueConverter.Generated;
            namespace TestApp;

            [GenerateConverter]
            public partial class Email
            {
                public string Value { get; }
            }
            """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
        var generated = result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("EmailConverter"));
        generated.ShouldNotBeNull();
        generated.ShouldContain("ConvertToProvider");
        generated.ShouldContain("ConvertFromProvider");
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ValueConverterGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult();
    }
}
