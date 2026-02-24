using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = """
    namespace MyApp.Services;

    public class OrderService
    {
        private readonly ILogger _logger;

        public OrderService(ILogger logger)
        {
            _logger = logger;
        }

        public Order GetOrder(string orderId)
        {
            return new Order(orderId);
        }

        public void ProcessOrder(string orderId, int quantity)
        {
            // Process logic
        }
    }

    public record Order(string Id);

    public interface ILogger
    {
        void Log(string message);
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

// 1. DescendantNodes — find all nodes of a specific type
Console.WriteLine("=== DescendantNodes<MethodDeclarationSyntax> ===");
foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    Console.WriteLine($"  {method.Identifier.Text}({method.ParameterList})");
}

// 2. ChildNodes — direct children only
Console.WriteLine("\n=== ChildNodes of CompilationUnit ===");
foreach (var child in root.ChildNodes())
{
    Console.WriteLine($"  {child.Kind()}: {child.ToString()[..Math.Min(50, child.ToString().Length)]}...");
}

// 3. Ancestors — navigate upward
Console.WriteLine("\n=== Ancestors of first MethodDeclaration ===");
var firstMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
foreach (var ancestor in firstMethod.Ancestors())
{
    Console.WriteLine($"  {ancestor.Kind()}");
}

// 4. SyntaxTokens and Trivia
Console.WriteLine("\n=== Tokens in first method signature ===");
foreach (var token in firstMethod.ChildTokens())
{
    Console.WriteLine($"  Token: '{token.Text}' Kind={token.Kind()}");
    if (token.HasLeadingTrivia)
    {
        foreach (var trivia in token.LeadingTrivia)
            Console.WriteLine($"    Leading trivia: {trivia.Kind()}");
    }
}

// 5. Finding specific patterns
Console.WriteLine("\n=== All Parameter Types ===");
foreach (var param in root.DescendantNodes().OfType<ParameterSyntax>())
{
    Console.WriteLine($"  {param.Type} {param.Identifier}");
}
