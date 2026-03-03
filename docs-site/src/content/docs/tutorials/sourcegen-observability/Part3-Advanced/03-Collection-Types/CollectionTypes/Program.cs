using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = """
    using System.Collections.Generic;

    namespace MyApp;

    public class DataService
    {
        public List<string> GetNames() => [];
        public IReadOnlyList<int> GetIds() => [];
        public Dictionary<string, object> GetMap() => [];
        public string[] GetArray() => [];
        public IEnumerable<double> GetValues() => [];
        public HashSet<string> GetUnique() => [];
        public int GetCount() => 0;
        public string GetSingle() => "";
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("Demo",
    [tree],
    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

var model = compilation.GetSemanticModel(tree);
var root = tree.GetRoot();

Console.WriteLine("=== Collection Type Analysis ===");
foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    if (model.GetDeclaredSymbol(method) is not IMethodSymbol methodSymbol) continue;
    var returnType = methodSymbol.ReturnType;

    var isCollection = IsCollectionType(returnType);
    var countExpr = GetCountExpression(returnType, "items");

    Console.WriteLine($"  {methodSymbol.Name}() -> {returnType.ToDisplayString()}");
    Console.WriteLine($"    IsCollection: {isCollection}");
    if (isCollection)
        Console.WriteLine($"    CountExpression: {countExpr}");
    Console.WriteLine();
}

static bool IsCollectionType(ITypeSymbol type)
{
    if (type is IArrayTypeSymbol) return true;
    if (type is not INamedTypeSymbol named) return false;

    var name = named.OriginalDefinition.ToDisplayString();
    return name.StartsWith("System.Collections.Generic.")
        || type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");
}

static string GetCountExpression(ITypeSymbol type, string varName)
{
    if (type is IArrayTypeSymbol)
        return $"{varName}.Length";

    if (type is INamedTypeSymbol named)
    {
        var hasCount = named.GetMembers("Count").OfType<IPropertySymbol>().Any();
        if (hasCount) return $"{varName}.Count";

        // IEnumerable — use LINQ Count()
        if (named.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"))
            return $"{varName}.Count()";
    }

    return $"/* unknown */ 0";
}
