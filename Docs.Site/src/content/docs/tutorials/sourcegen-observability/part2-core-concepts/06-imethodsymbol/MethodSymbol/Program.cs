using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = """
    namespace MyApp;

    public class OrderService
    {
        public Order? GetOrder(string orderId) => default;
        public IReadOnlyList<Order> GetOrders(int page, int pageSize = 10) => [];
        protected internal void ProcessOrder(string orderId, CancellationToken cancellationToken = default) { }
        private static decimal CalculateDiscount(decimal amount, double rate) => amount * (decimal)rate;
    }

    public record Order(string Id, decimal Amount);
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var refs = new[]
{
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
};
var compilation = CSharpCompilation.Create("Demo", [tree], refs);
var model = compilation.GetSemanticModel(tree);
var root = tree.GetRoot();

foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    if (model.GetDeclaredSymbol(methodDecl) is not IMethodSymbol method) continue;

    Console.WriteLine($"=== {method.Name} ===");
    Console.WriteLine($"  DeclaredAccessibility: {method.DeclaredAccessibility}");
    Console.WriteLine($"  ReturnType: {method.ReturnType.ToDisplayString()}");
    Console.WriteLine($"  IsStatic: {method.IsStatic}");
    Console.WriteLine($"  ReturnsVoid: {method.ReturnsVoid}");

    Console.WriteLine($"  Parameters ({method.Parameters.Length}):");
    foreach (var param in method.Parameters)
    {
        var defaultValue = param.HasExplicitDefaultValue ? $" = {param.ExplicitDefaultValue ?? "default"}" : "";
        Console.WriteLine($"    {param.Type.ToDisplayString()} {param.Name}{defaultValue}");
        Console.WriteLine($"      RefKind: {param.RefKind}, IsOptional: {param.IsOptional}");
    }
    Console.WriteLine();
}
