using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = """
    namespace MyApp.Domain;

    public interface IEntity<TId>
    {
        TId Id { get; }
    }

    public abstract class AggregateRoot<TId> : IEntity<TId>
    {
        public abstract TId Id { get; }
    }

    public class Order : AggregateRoot<string>
    {
        public override string Id { get; } = "";
        public string CustomerName { get; set; } = "";
        public decimal TotalAmount { get; set; }

        public Order() { }
        public Order(string id, string customerName) { Id = id; CustomerName = customerName; }
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("Demo",
    [tree],
    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

var model = compilation.GetSemanticModel(tree);
var root = tree.GetRoot();

foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
{
    if (model.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol symbol) continue;

    Console.WriteLine($"=== {symbol.TypeKind}: {symbol.Name} ===");
    Console.WriteLine($"  ContainingNamespace: {symbol.ContainingNamespace.ToDisplayString()}");
    Console.WriteLine($"  IsAbstract: {symbol.IsAbstract}");
    Console.WriteLine($"  IsGenericType: {symbol.IsGenericType}");

    if (symbol.TypeParameters.Length > 0)
        Console.WriteLine($"  TypeParameters: [{string.Join(", ", symbol.TypeParameters.Select(tp => tp.Name))}]");

    if (symbol.BaseType is { } baseType && baseType.SpecialType == SpecialType.None)
        Console.WriteLine($"  BaseType: {baseType.ToDisplayString()}");

    if (symbol.Interfaces.Length > 0)
        Console.WriteLine($"  Interfaces: [{string.Join(", ", symbol.Interfaces.Select(i => i.ToDisplayString()))}]");

    Console.WriteLine($"  Constructors: {symbol.Constructors.Length}");
    foreach (var ctor in symbol.Constructors)
    {
        var parms = string.Join(", ", ctor.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        Console.WriteLine($"    ctor({parms})");
    }
    Console.WriteLine();
}
