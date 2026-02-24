using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = """
    namespace MyApp;

    public interface IRepository<T> where T : class
    {
        T? GetById(string id);
        IReadOnlyList<T> GetAll();
    }

    public class UserRepository : IRepository<User>
    {
        public User? GetById(string id) => default;
        public IReadOnlyList<User> GetAll() => [];
        internal void ClearCache() { }
    }

    public record User(string Id, string Name, string Email)
    {
        public string DisplayName => $"{Name} <{Email}>";
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("Demo",
    [tree],
    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

var model = compilation.GetSemanticModel(tree);
var root = tree.GetRoot();

// 1. INamedTypeSymbol
Console.WriteLine("=== INamedTypeSymbol ===");
foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
{
    if (model.GetDeclaredSymbol(typeDecl) is INamedTypeSymbol typeSymbol)
    {
        Console.WriteLine($"  {typeSymbol.TypeKind}: {typeSymbol.Name}");
        Console.WriteLine($"    Namespace: {typeSymbol.ContainingNamespace}");
        Console.WriteLine($"    IsGeneric: {typeSymbol.IsGenericType}");
        Console.WriteLine($"    Interfaces: [{string.Join(", ", typeSymbol.Interfaces.Select(i => i.ToDisplayString()))}]");
        Console.WriteLine($"    Constructors: {typeSymbol.Constructors.Length}");
    }
}

// 2. IMethodSymbol
Console.WriteLine("\n=== IMethodSymbol ===");
foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    if (model.GetDeclaredSymbol(methodDecl) is IMethodSymbol methodSymbol)
    {
        Console.WriteLine($"  {methodSymbol.DeclaredAccessibility} {methodSymbol.ReturnType.ToDisplayString()} {methodSymbol.Name}");
        Console.WriteLine($"    Parameters: [{string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"))}]");
        Console.WriteLine($"    IsStatic: {methodSymbol.IsStatic}");
    }
}

// 3. IPropertySymbol
Console.WriteLine("\n=== IPropertySymbol ===");
foreach (var propDecl in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
{
    if (model.GetDeclaredSymbol(propDecl) is IPropertySymbol propSymbol)
    {
        Console.WriteLine($"  {propSymbol.Type.ToDisplayString()} {propSymbol.Name}");
        Console.WriteLine($"    GetMethod: {propSymbol.GetMethod is not null}");
        Console.WriteLine($"    SetMethod: {propSymbol.SetMethod is not null}");
        Console.WriteLine($"    IsReadOnly: {propSymbol.IsReadOnly}");
    }
}
