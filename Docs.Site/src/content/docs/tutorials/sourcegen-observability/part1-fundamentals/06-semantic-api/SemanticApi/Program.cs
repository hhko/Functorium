using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var code = """
    namespace MyApp;

    public class Calculator
    {
        public int Add(int a, int b) => a + b;
        public double Divide(double a, double b) => a / b;
    }

    public class MathService
    {
        private readonly Calculator _calc = new();

        public int ComputeSum(int x, int y)
        {
            var result = _calc.Add(x, y);
            return result;
        }
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("Demo",
    [tree],
    [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

var model = compilation.GetSemanticModel(tree);
var root = tree.GetRoot();

// 1. GetDeclaredSymbol — class/method declarations
Console.WriteLine("=== GetDeclaredSymbol ===");
foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
{
    var symbol = model.GetDeclaredSymbol(classDecl);
    Console.WriteLine($"  Class: {symbol?.ToDisplayString()} Kind={symbol?.Kind}");
}

// 2. GetSymbolInfo — expressions and references
Console.WriteLine("\n=== GetSymbolInfo (Invocations) ===");
foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
{
    var symbolInfo = model.GetSymbolInfo(invocation);
    Console.WriteLine($"  Call: {invocation} -> {symbolInfo.Symbol?.ToDisplayString() ?? "unresolved"}");
}

// 3. GetTypeInfo — type of expressions
Console.WriteLine("\n=== GetTypeInfo ===");
foreach (var varDecl in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
{
    if (varDecl.Initializer?.Value is { } value)
    {
        var typeInfo = model.GetTypeInfo(value);
        Console.WriteLine($"  var {varDecl.Identifier} = ... (Type: {typeInfo.Type?.ToDisplayString()})");
    }
}

// 4. Diagnostics
Console.WriteLine("\n=== Compilation Diagnostics ===");
foreach (var diag in compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning))
{
    Console.WriteLine($"  [{diag.Severity}] {diag.Id}: {diag.GetMessage()}");
}
