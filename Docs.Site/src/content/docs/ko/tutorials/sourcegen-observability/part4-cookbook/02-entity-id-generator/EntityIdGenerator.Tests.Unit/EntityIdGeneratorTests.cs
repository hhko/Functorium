using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using EntityIdGenerator.Generator;

namespace EntityIdGenerator.Tests.Unit;

public class EntityIdGeneratorTests
{
    [Fact]
    public void Generator_Creates_EntityId_For_Attributed_Struct()
    {
        var source = """
            using EntityIdGenerator.Generated;
            namespace TestApp;

            [EntityId]
            public readonly partial struct OrderId;
            """;

        var result = RunGenerator(source);

        result.Diagnostics.ShouldBeEmpty();
        var generated = result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("readonly partial struct OrderId"));
        generated.ShouldNotBeNull();
        generated.ShouldContain("IEquatable<OrderId>");
        generated.ShouldContain("static OrderId New()");
        generated.ShouldContain("static OrderId From(string value)");
    }

    [Fact]
    public void Generator_Creates_Equality_Operators()
    {
        var source = """
            using EntityIdGenerator.Generated;
            namespace TestApp;

            [EntityId]
            public readonly partial struct ProductId;
            """;

        var result = RunGenerator(source);

        var generated = result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("ProductId"));
        generated.ShouldNotBeNull();
        generated.ShouldContain("operator ==");
        generated.ShouldContain("operator !=");
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new EntityIdSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult();
    }
}
