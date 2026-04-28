using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// --- 1. Reflection-based logging approach ---
Console.WriteLine("=== Reflection-based Logging ===");

var sampleType = typeof(SampleService);
foreach (var method in sampleType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
{
    var parameters = method.GetParameters();
    var paramNames = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Console.WriteLine($"  Method: {method.Name}({paramNames}) -> {method.ReturnType.Name}");
}

// --- 2. Compile-time approach with LoggerMessage.Define ---
Console.WriteLine();
Console.WriteLine("=== LoggerMessage.Define Pattern (Compile-time) ===");

var code = """
    using Microsoft.Extensions.Logging;

    public static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Processing order {OrderId} for {CustomerName}")]
        public static partial void ProcessingOrder(this ILogger logger, string orderId, string customerName);
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

Console.WriteLine("  Parsed LoggerMessage pattern:");
foreach (var attr in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>())
{
    Console.WriteLine($"  Attribute: [{attr.Name}]");
    if (attr.ArgumentList is not null)
    {
        foreach (var arg in attr.ArgumentList.Arguments)
        {
            Console.WriteLine($"    {arg}");
        }
    }
}

Console.WriteLine();
Console.WriteLine("=== Comparison ===");
Console.WriteLine("  Reflection: Runtime overhead, no compile-time validation");
Console.WriteLine("  Source Gen:  Zero runtime overhead, compile-time validated");

public class SampleService
{
    public string ProcessOrder(string orderId, string customerName) => $"Processed {orderId}";
    public void CancelOrder(string orderId, string reason) { }
    public int GetOrderCount() => 0;
}
