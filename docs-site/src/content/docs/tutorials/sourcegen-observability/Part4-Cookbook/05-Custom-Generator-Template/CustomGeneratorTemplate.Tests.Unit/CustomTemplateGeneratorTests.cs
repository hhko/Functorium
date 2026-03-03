using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CustomGeneratorTemplate.Generator;

namespace CustomGeneratorTemplate.Tests.Unit;

public class CustomTemplateGeneratorTests
{
    [Fact]
    public void Generator_Creates_MapTo_Method()
    {
        var source = """
            using CustomGeneratorTemplate.Generated;
            namespace TestApp;

            [AutoMapper(typeof(TargetDto))]
            public partial class SourceEntity
            {
                public string Name { get; set; } = "";
                public int Value { get; set; }
            }

            public class TargetDto
            {
                public string Name { get; set; } = "";
                public int Value { get; set; }
            }
            """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
        var generated = result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("MapToTargetDto"));
        generated.ShouldNotBeNull();
        generated.ShouldContain("Name = this.Name");
        generated.ShouldContain("Value = this.Value");
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new CustomTemplateGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult();
    }
}
