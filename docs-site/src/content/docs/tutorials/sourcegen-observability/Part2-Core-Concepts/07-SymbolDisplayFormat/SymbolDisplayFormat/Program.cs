using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = """
    namespace MyApp.Domain.Entities;

    public class Order
    {
        public string Id { get; set; } = "";
        public List<OrderItem> Items { get; set; } = [];
        public decimal GetTotal() => Items.Sum(i => i.Price * i.Quantity);
    }

    public record OrderItem(string ProductName, decimal Price, int Quantity);
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("Demo",
    [tree],
    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

var model = compilation.GetSemanticModel(tree);
var root = tree.GetRoot();

var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
var typeSymbol = model.GetDeclaredSymbol(classDecl)!;

// 1. Default format
Console.WriteLine("=== SymbolDisplayFormat Examples ===");
Console.WriteLine($"  Default:                {typeSymbol.ToDisplayString()}");

// 2. FullyQualifiedFormat — global:: prefix
Console.WriteLine($"  FullyQualified:         {typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");

// 3. MinimallyQualifiedFormat — shortest unambiguous name
Console.WriteLine($"  MinimallyQualified:     {typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");

// 4. Custom format examples
var customFormat = new SymbolDisplayFormat(
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
Console.WriteLine($"  NameAndNamespaces:      {typeSymbol.ToDisplayString(customFormat)}");

// 5. Method display formats
var methodDecl = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
if (model.GetDeclaredSymbol(methodDecl) is IMethodSymbol methodSymbol)
{
    Console.WriteLine($"\n=== Method Display ===");
    Console.WriteLine($"  Default:            {methodSymbol.ToDisplayString()}");
    Console.WriteLine($"  FullyQualified:     {methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
    Console.WriteLine($"  MinimallyQualified: {methodSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
}

// 6. Practical usage in source generation
Console.WriteLine($"\n=== Source Generation Usage ===");
Console.WriteLine($"  For 'using' statements:   {typeSymbol.ContainingNamespace.ToDisplayString()}");
Console.WriteLine($"  For global type reference: {typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
