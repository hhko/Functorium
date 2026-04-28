using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ValidationGenerator.Generator;

namespace ValidationGenerator.Tests.Unit;

public class ValidationGeneratorTests
{
    [Fact]
    public void Generator_Creates_Validate_Method()
    {
        var source = """
            using ValidationGenerator.Generated;
            namespace TestApp;

            [AutoValidate]
            public partial class TestModel
            {
                [Required]
                public string Name { get; set; } = "";
            }
            """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
        var generated = result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("string[] Validate()"));
        generated.ShouldNotBeNull();
        generated.ShouldContain("IsNullOrWhiteSpace");
    }

    [Fact]
    public void Generator_Handles_Range_Attribute()
    {
        var source = """
            using ValidationGenerator.Generated;
            namespace TestApp;

            [AutoValidate]
            public partial class TestModel
            {
                [Range(0, 100)]
                public int Score { get; set; }
            }
            """;

        var result = RunGenerator(source);

        var generated = result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("string[] Validate()"));
        generated.ShouldNotBeNull();
        generated.ShouldContain("Score");
        generated.ShouldContain("0");
        generated.ShouldContain("100");
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToArray<MetadataReference>();
        var compilation = CSharpCompilation.Create("TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ValidationSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult();
    }
}
