using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Data models used in source generators
Console.WriteLine("=== Source Generator Data Models ===");

// 1. readonly record struct — immutable value type
var methodInfo = new MethodInfo("GetUser", "User", ["string userId", "bool includeDetails"]);
Console.WriteLine($"MethodInfo: {methodInfo}");

// 2. class-based model for complex data
var classInfo = new ClassInfo("UserService", "MyApp.Services", ["GetUser", "CreateUser", "DeleteUser"]);
Console.WriteLine($"ClassInfo: {classInfo.Name} in {classInfo.Namespace} with {classInfo.MethodNames.Count} methods");

// 3. Demonstrating equatable behavior for caching
var info1 = new MethodInfo("GetUser", "User", ["string userId"]);
var info2 = new MethodInfo("GetUser", "User", ["string userId"]);
Console.WriteLine($"\nEquality check (record struct): {info1 == info2}");

// 4. Parse real code to extract these models
var code = """
    namespace MyApp.Services;

    public class UserService
    {
        public User GetUser(string userId) => default!;
        public void CreateUser(string name, string email) { }
    }

    public record User(string Id, string Name);
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

Console.WriteLine("\n=== Parsed Declarations ===");
foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
{
    Console.WriteLine($"  Class: {classDecl.Identifier}");
    foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
    {
        var parms = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
        Console.WriteLine($"    Method: {method.ReturnType} {method.Identifier}({parms})");
    }
}

// Models
public readonly record struct MethodInfo(string Name, string ReturnType, IReadOnlyList<string> Parameters);

public class ClassInfo(string name, string @namespace, IReadOnlyList<string> methodNames)
{
    public string Name { get; } = name;
    public string Namespace { get; } = @namespace;
    public IReadOnlyList<string> MethodNames { get; } = methodNames;
}
