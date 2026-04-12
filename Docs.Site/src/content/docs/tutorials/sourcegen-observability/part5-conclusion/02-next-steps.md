---
title: "Next Steps"
---

## Overview

If you have learned the basics of source generators and completed ObservablePortGenerator, you are now ready to explore the broader Roslyn ecosystem building on that experience. This section introduces advanced Roslyn APIs, practical project ideas worth attempting, and community resources for continued learning.

## Learning Objectives

### Core Learning Objectives
1. **Identify additional learning topics**
   - Expand your capabilities to Roslyn APIs beyond source generators (Analyzer, Code Fix Provider)
2. **Practical project ideas**
   - Build source generator implementation skills through projects organized by difficulty
3. **Leverage community resources**
   - Continuously grow by utilizing official documentation, open-source projects, and learning materials

---

## Additional Learning Topics

### 1. Advanced Roslyn API

If source generators are tools that "add new code," Roslyn provides rich APIs beyond that for analyzing and transforming existing code.

#### Syntax Rewriter

A pattern that navigates the Syntax Tree of existing code and transforms specific nodes. For example, you can batch-change method naming conventions or automatically refactor specific code patterns.

```csharp
public class MyRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Modify method declaration
        return base.VisitMethodDeclaration(node);
    }
}
```

#### Code Fix Provider

A tool that suggests automatic fixes to developers when an Analyzer detects a problem. The Quick Fix that appears as a "lightbulb icon" in the IDE is exactly this mechanism.

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MyCodeFixProvider : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // Register code fix suggestion
    }
}
```

#### Analyzer

Defines custom code analysis rules that apply across the entire project. Creating an Analyzer that enforces correct usage of the `[GenerateObservablePort]` attribute would create synergy with the source generator.

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MyAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }
}
```

### 2. Other Source Generator Patterns

The .NET ecosystem already utilizes various source generators. Analyzing their designs lets you see how the patterns learned from ObservablePortGenerator are modified and extended.

#### JSON Serialization Generator

```csharp
// System.Text.Json.SourceGeneration
[JsonSerializable(typeof(User))]
public partial class MyJsonContext : JsonSerializerContext { }
```

#### Regex Source Generator

```csharp
// .NET 7+
[GeneratedRegex(@"\d+")]
private static partial Regex NumberRegex();
```

#### AutoMapper Source Generator

```csharp
[AutoMap(typeof(UserDto))]
public class User { }
```

### 3. Performance Optimization

When source generators process hundreds of classes in large projects, incremental caching and parallel processing directly impact build times.

#### Deep Dive into Incremental Caching

```csharp
// Value comparison optimization
context.SyntaxProvider
    .CreateSyntaxProvider(...)
    .WithComparer(new MyEqualityComparer())
```

#### Parallel Processing

```csharp
// For large-scale symbol processing
Parallel.ForEach(symbols, symbol => {
    ProcessSymbol(symbol);
});
```

---

## Other Source Generators in Functorium

The `IncrementalGeneratorBase` pattern learned from ObservablePortGenerator is already being applied to other domains within the Functorium project. **UnionTypeGenerator** is a source generator that automatically generates `Match`/`Switch` methods for discriminated union types, inheriting `IncrementalGeneratorBase<UnionTypeInfo>` and following the same 2-stage pipeline (source provider registration → code generation). Since this is an actual case where the Template Method pattern learned in this tutorial is reused in a completely different domain beyond observability, examining the source code (`Src/Functorium.SourceGenerators/Generators/UnionTypeGenerator/`) will provide a sense of pattern extension.

---

## Practical Project Ideas

The following projects are organized by difficulty. Build foundational skills with the DTO Mapper and Builder pattern generators, experience complex scenarios with Enum extensions and API client generators, and try integrating with Analyzers in the validation rule generator.

### 1. DTO Mapper Generator

**Goal**: Auto-generate entity → DTO mapping code. The logic of comparing property names and types directly leverages the experience of analyzing symbols in ObservablePortGenerator, making it suitable as a first project.

```csharp
// Input
[GenerateMapper]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Generated
public static class UserMapper
{
    public static UserDto ToDto(this User user) =>
        new UserDto { Id = user.Id, Name = user.Name };
}
```

### 2. Builder Pattern Generator

**Goal**: Auto-generate Builder classes for immutable objects. The process of analyzing record constructor parameters to generate a Fluent API is an extension of the patterns covered in ConstructorParameterExtractor.

```csharp
// Input
[GenerateBuilder]
public record User(int Id, string Name, string Email);

// Generated
public class UserBuilder
{
    private int _id;
    private string _name;
    private string _email;

    public UserBuilder WithId(int id) { _id = id; return this; }
    public UserBuilder WithName(string name) { _name = name; return this; }
    public User Build() => new(_id, _name, _email);
}
```

### 3. Enum Extensions Generator

**Goal**: Generate utility methods for Enums. The process of iterating through Enum members and generating switch expressions involves designing type-specific branching logic similar to CollectionTypeHelper.

```csharp
// Input
[GenerateEnumExtensions]
public enum OrderStatus { Pending, Processing, Completed }

// Generated
public static class OrderStatusExtensions
{
    public static string ToDisplayString(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pending",
        OrderStatus.Processing => "Processing",
        OrderStatus.Completed => "Completed",
        _ => throw new ArgumentOutOfRangeException()
    };

    public static bool IsFinalState(this OrderStatus status) =>
        status == OrderStatus.Completed;
}
```

### 4. API Client Generator

**Goal**: Generate HTTP client implementations from interfaces. The process of extracting URL patterns from attributes and determining HTTP methods and parameter bindings from method signatures extends the method analysis logic of ObservablePortGenerator one step further in complexity.

```csharp
// Input
[GenerateHttpClient("https://api.example.com")]
public interface IUserApi
{
    [Get("/users/{id}")]
    Task<User> GetUserAsync(int id);

    [Post("/users")]
    Task<User> CreateUserAsync(CreateUserRequest request);
}

// Generated
public class UserApiClient : IUserApi
{
    private readonly HttpClient _client;

    public async Task<User> GetUserAsync(int id)
    {
        var response = await _client.GetAsync($"/users/{id}");
        return await response.Content.ReadFromJsonAsync<User>();
    }
}
```

### 5. Validation Rule Generator

**Goal**: Auto-generate data validation code. The most challenging project, which can combine attribute-based rule interpretation with an Analyzer to warn about incorrect validation rules at compile time.

```csharp
// Input
[GenerateValidator]
public class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Range(0, 150)]
    public int Age { get; set; }
}

// Generated
public class CreateUserRequestValidator
{
    public ValidationResult Validate(CreateUserRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(request.Name))
            errors.Add("Name is required");

        if (request.Name?.Length > 100)
            errors.Add("Name must be at most 100 characters");

        // ...

        return new ValidationResult(errors);
    }
}
```

---

## Community Resources

To study source generators in depth, it is effective to combine official documentation with practical open-source projects.

### Official Documentation

| Resource | URL |
|----------|-----|
| Roslyn Official Docs | [docs.microsoft.com/dotnet/csharp/roslyn-sdk](https://docs.microsoft.com/dotnet/csharp/roslyn-sdk) |
| Source Generator Cookbook | [github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md) |
| .NET Blog | [devblogs.microsoft.com/dotnet](https://devblogs.microsoft.com/dotnet) |

### GitHub Examples

| Project | Description |
|---------|-------------|
| System.Text.Json | JSON serialization source generator |
| Refit | REST API client generator |
| MediatR | CQRS pattern support generator |
| AutoMapper | Object mapping source generator |

### Learning Materials

For books, "Roslyn Cookbook" systematically covers the Roslyn API as a whole and is a good reference. For blogs, Andrew Lock's .NET Blog consistently covers deep .NET topics including source generators, and on YouTube, the Nick Chapsas channel provides video coverage of practical source generator use cases.

---

## Debugging Tips Review

One of the most challenging parts of source generator development is debugging. Since generators run inside the compiler, they are difficult to approach through regular breakpoint methods. Keep the following two techniques in mind.

### Debugger.Launch()

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
#if DEBUG
    if (!System.Diagnostics.Debugger.IsAttached)
    {
        System.Diagnostics.Debugger.Launch();
    }
#endif
}
```

### Diagnostic Message Output

```csharp
context.ReportDiagnostic(Diagnostic.Create(
    new DiagnosticDescriptor(
        "SG001",
        "Debug Info",
        "Processing: {0}",
        "Debug",
        DiagnosticSeverity.Warning,
        true),
    Location.None,
    className));
```

---

## FAQ

### Q1: What synergy is there when using an Analyzer and source generator together?
**A**: If the source generator is the "producer" that generates code, the Analyzer is the "verifier" that enforces usage rules. For example, creating an Analyzer that displays a compile warning when the `[GenerateObservablePort]` attribute is applied to a class that does not implement `IObservablePort` can prevent incorrect usage at compile time.

### Q2: Which practical project idea would you recommend starting with first?
**A**: We recommend the DTO Mapper generator. The logic of comparing property names and types directly connects to the experience of analyzing symbols in ObservablePortGenerator, and the structure of the code to generate is simple enough for quick completion. After completion, moving to the Builder pattern generator allows you to extend the constructor parameter analysis experience.

### Q3: What should be considered when introducing a source generator into a team project?
**A**: Three things should be considered. First, the review burden for `.verified.txt` files increases, so team consensus on snapshot changes is needed. Second, source generator projects target `netstandard2.0`, so there are constraints on using the latest C# features. Third, since generator bugs manifest as compile errors that are hard to trace, sufficient test coverage and debugging strategies (`Debugger.Launch()`, diagnostic messages) should be established in advance.

---

## Conclusion

### What We Learned

This tutorial started from Roslyn's foundational concepts (Syntax Tree, Semantic Model, Symbol), went through implementing incremental source generation patterns with `IIncrementalGenerator`, applying attribute-based filtering with `ForAttributeWithMetadataName`, ensuring code generation reliability with StringBuilder and deterministic output principles, handling advanced scenarios like constructors, generics, and collections, and verifying all results with CSharpCompilation and Verify snapshots.

### Key Takeaway

> **Compile-time code generation is** a powerful tool for eliminating
> repetitive boilerplate without runtime overhead.

### Next Goals

It is time to extend the patterns learned in this tutorial into practice. Start with a small project like a DTO Mapper generator to build your skills, propose introducing it to a team project to gain practical application experience. Analyzing the code of open-source source generators will teach you production-level design decisions, and expanding your scope to include Analyzers and Code Fix Providers will help you grow into a Roslyn expert who covers the full spectrum of development tools.

---

## Appendix Reference

Details not covered in the main tutorial can be found in the appendix.

- [A. Development Environment](../Appendix/A-development-environment.md)
- [B. API Reference](../Appendix/B-api-reference.md)
- [C. Test Scenario Catalog](../Appendix/C-test-scenario-catalog.md)
- [D. Troubleshooting](../Appendix/D-troubleshooting.md)

---

You have completed learning about observability code automation using source generators. Now go ahead and implement source generators yourself to apply the power of compile time to your projects.
