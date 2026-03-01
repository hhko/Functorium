using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Demonstrates string-based type parameter extraction
var code = """
    namespace MyApp;

    public class DataService
    {
        public Task<List<string>> GetNames() => Task.FromResult(new List<string>());
        public Dictionary<string, List<int>> GetMapping() => [];
        public (string Name, int Age) GetPerson() => default;
        public int? GetCount() => null;
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("Demo",
    [tree],
    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

var model = compilation.GetSemanticModel(tree);
var root = tree.GetRoot();

foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    if (model.GetDeclaredSymbol(method) is not IMethodSymbol methodSymbol) continue;

    Console.WriteLine($"=== {methodSymbol.Name} ===");
    var returnType = methodSymbol.ReturnType;
    Console.WriteLine($"  Full type: {returnType.ToDisplayString()}");
    AnalyzeType(returnType, "  ");
}

static void AnalyzeType(ITypeSymbol type, string indent)
{
    // Named type with type arguments (generics)
    if (type is INamedTypeSymbol { IsGenericType: true } namedType)
    {
        Console.WriteLine($"{indent}Generic: {namedType.Name}<>");
        Console.WriteLine($"{indent}TypeArguments ({namedType.TypeArguments.Length}):");
        foreach (var arg in namedType.TypeArguments)
        {
            Console.WriteLine($"{indent}  - {arg.ToDisplayString()}");
            AnalyzeType(arg, indent + "    ");
        }
    }
    // Nullable value type
    else if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol nullable)
    {
        Console.WriteLine($"{indent}Nullable: {nullable.TypeArguments[0].ToDisplayString()}?");
    }
    // Tuple type
    else if (type is INamedTypeSymbol { IsTupleType: true } tupleType)
    {
        Console.WriteLine($"{indent}Tuple elements:");
        foreach (var elem in tupleType.TupleElements)
        {
            Console.WriteLine($"{indent}  {elem.Type.ToDisplayString()} {elem.Name}");
        }
    }
}
