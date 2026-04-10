using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = """
    using System;

    namespace MyApp;

    /// <summary>Sample class for Roslyn exploration.</summary>
    public class Calculator
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
    }
    """;

// 1. Parse source text into a SyntaxTree
var tree = CSharpSyntaxTree.ParseText(code);
Console.WriteLine($"=== SyntaxTree ===");
Console.WriteLine($"  File path: {tree.FilePath}");
Console.WriteLine($"  Has errors: {tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)}");

// 2. Get CompilationUnitSyntax (root node)
var root = tree.GetCompilationUnitRoot();
Console.WriteLine($"\n=== CompilationUnit ===");
Console.WriteLine($"  Usings: {root.Usings.Count}");
Console.WriteLine($"  Members: {root.Members.Count}");

// 3. Explore the tree structure
Console.WriteLine($"\n=== Tree Structure ===");
PrintNode(root, 0);

static void PrintNode(SyntaxNode node, int indent)
{
    var prefix = new string(' ', indent * 2);
    Console.WriteLine($"{prefix}{node.Kind()} [{node.Span.Start}..{node.Span.End}]");

    foreach (var child in node.ChildNodes())
    {
        if (indent < 3)  // Limit depth for readability
            PrintNode(child, indent + 1);
    }
}

// 4. Find specific node types
Console.WriteLine($"\n=== Class Declarations ===");
foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
{
    Console.WriteLine($"  Class: {cls.Identifier.Text}");
    Console.WriteLine($"    Modifiers: {cls.Modifiers}");
    Console.WriteLine($"    Members: {cls.Members.Count}");
}

Console.WriteLine($"\n=== Method Declarations ===");
foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    Console.WriteLine($"  Method: {method.Identifier.Text}");
    Console.WriteLine($"    Return type: {method.ReturnType}");
    Console.WriteLine($"    Parameters: {method.ParameterList}");
}
