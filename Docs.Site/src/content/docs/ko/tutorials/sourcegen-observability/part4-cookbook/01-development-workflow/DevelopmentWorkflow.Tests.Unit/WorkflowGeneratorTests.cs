using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DevelopmentWorkflow.Generator;

namespace DevelopmentWorkflow.Tests.Unit;

public class WorkflowGeneratorTests
{
    [Fact]
    public void Generator_Produces_TypeName_For_Attributed_Class()
    {
        var source = """
            using DevelopmentWorkflow.Generated;

            namespace TestApp;

            [AutoInfo]
            public partial class MyService
            {
                public string GetData() => "data";
            }
            """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBeGreaterThan(0);

        var generatedCode = result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("TypeName"));
        generatedCode.ShouldNotBeNull();
        generatedCode.ShouldContain("MyService");
    }

    [Fact]
    public void Generator_Lists_Only_Public_Methods()
    {
        var source = """
            using DevelopmentWorkflow.Generated;

            namespace TestApp;

            [AutoInfo]
            public partial class MyService
            {
                public string GetData() => "data";
                public void Process() { }
                private void Secret() { }
            }
            """;

        var result = RunGenerator(source);

        var generatedCode = result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("PublicMethods"));
        generatedCode.ShouldNotBeNull();
        generatedCode.ShouldContain("GetData");
        generatedCode.ShouldContain("Process");
        generatedCode.ShouldNotContain("Secret");
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create("TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new WorkflowGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, out _, out _);

        return driver.GetRunResult();
    }
}
