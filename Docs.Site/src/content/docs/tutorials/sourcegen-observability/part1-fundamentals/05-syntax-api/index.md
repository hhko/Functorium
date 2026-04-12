---
title: "Syntax API"
---

## Overview

In the previous chapter, we examined the full picture of Roslyn's architecture. Now we dive deep into its first layer: the Syntax API.

The Syntax API provides a **structural representation** of source code. The `predicate` stage that quickly filters "does a certain class have a specific attribute?" in source generators is exactly the domain of the Syntax API. Our project's `Selectors.IsClass` is a typical example. However, the Syntax API alone cannot determine the full name of a type or whether it implements an interface, and recognizing this limitation is the starting point for understanding the Semantic API in the next chapter.

## Learning Objectives

### Core Learning Objectives
1. **Understand the differences between SyntaxNode, SyntaxToken, and SyntaxTrivia**
   - The roles and relationships of the three elements that compose a Syntax Tree
2. **Learn Syntax Tree traversal methods**
   - Traversal APIs such as `DescendantNodes()`, `ChildNodes()`, `Ancestors()`
3. **Learn how to use Syntax API in source generators**
   - Implementing syntax-level filtering in the `predicate` of `ForAttributeWithMetadataName`

---

## Components of a Syntax Tree

A Syntax Tree consists of three elements:

```
Syntax Tree Components
====================

SyntaxNode (node)
├── Unit representing grammatical structure
├── Examples: class declaration, method declaration, if statement
└── Contains child nodes or tokens

SyntaxToken (token)
├── Smallest grammatical unit
├── Examples: keyword (class), identifier (User), operator (+)
└── Contains Leading/Trailing Trivia

SyntaxTrivia (trivia)
├── Insignificant text
├── Examples: whitespace, line breaks, comments
└── Attached to tokens
```

### Understanding Through Example

```csharp
// Original code
public class User { }
```

```
Syntax Tree Structure
=====================

ClassDeclarationSyntax (node)
├── Modifiers: [public] (token)
│   └── LeadingTrivia: [whitespace]
├── Keyword: [class] (token)
│   └── LeadingTrivia: [whitespace]
├── Identifier: [User] (token)
│   └── LeadingTrivia: [whitespace]
├── OpenBraceToken: [{] (token)
│   └── LeadingTrivia: [whitespace]
└── CloseBraceToken: [}] (token)
    └── LeadingTrivia: [whitespace]
```

---

## Key SyntaxNode Types

Each C# grammar element has a corresponding SyntaxNode. The most frequently used in source generator development are declaration-related nodes. Our project primarily works with `ClassDeclarationSyntax` and `InterfaceDeclarationSyntax`.

```
Declaration-related (most frequently used in source generators)
=========
CompilationUnitSyntax       Entire file
NamespaceDeclarationSyntax  Namespace
ClassDeclarationSyntax      Class         <- Used in Selectors.IsClass
InterfaceDeclarationSyntax  Interface     <- Used in Selectors.IsInterface
MethodDeclarationSyntax     Method
PropertyDeclarationSyntax   Property
FieldDeclarationSyntax      Field
ParameterSyntax             Parameter

Statement-related
=========
BlockSyntax                 { } block
IfStatementSyntax           if statement
ForStatementSyntax          for statement
ReturnStatementSyntax       return statement
ExpressionStatementSyntax   Expression statement

Expression-related
==========
InvocationExpressionSyntax  Method call
MemberAccessExpressionSyntax Member access (a.b)
LiteralExpressionSyntax     Literal (5, "hello")
IdentifierNameSyntax        Identifier (variable name)
```

---

## Syntax Tree Traversal

### DescendantNodes - All Descendant Nodes

```csharp
string code = """
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

// Find all property declarations
var properties = root
    .DescendantNodes()
    .OfType<PropertyDeclarationSyntax>();

foreach (var prop in properties)
{
    Console.WriteLine($"{prop.Type} {prop.Identifier}");
}
// Output:
// int Id
// string Name
```

### ChildNodes - Direct Children Only

```csharp
var classDecl = root
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

// Only direct children of the class (properties, methods, etc.)
var members = classDecl.ChildNodes();

foreach (var member in members)
{
    Console.WriteLine($"Member kind: {member.Kind()}");
}
// Output:
// Member kind: PropertyDeclaration
// Member kind: PropertyDeclaration
```

### Ancestors - Parent Nodes

```csharp
var property = root
    .DescendantNodes()
    .OfType<PropertyDeclarationSyntax>()
    .First();

// Parent nodes of the property
var ancestors = property.Ancestors();

foreach (var ancestor in ancestors)
{
    Console.WriteLine($"Parent: {ancestor.Kind()}");
}
// Output:
// Parent: ClassDeclaration
// Parent: CompilationUnit
```

---

## Usage in Source Generators

### predicate of ForAttributeWithMetadataName

```csharp
context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "MyNamespace.GenerateObservablePortAttribute",
        // predicate: Uses Syntax API
        predicate: (node, cancellationToken) =>
        {
            // Check if node is a class
            return node is ClassDeclarationSyntax classDecl
                // Check if it's a public class (determinable from Syntax alone)
                && classDecl.Modifiers.Any(SyntaxKind.PublicKeyword);
        },
        transform: (ctx, ct) => /* ... */
    );
```

### Actual Code: Selectors.cs

```csharp
// Selectors.cs from the Functorium project
namespace Functorium.SourceGenerators.Abstractions;

public static class Selectors
{
    /// <summary>
    /// Checks if the node is a class declaration.
    /// </summary>
    public static bool IsClass(SyntaxNode node, CancellationToken cancellationToken)
        => node is ClassDeclarationSyntax;

    /// <summary>
    /// Checks if the node is an interface declaration.
    /// </summary>
    public static bool IsInterface(SyntaxNode node, CancellationToken cancellationToken)
        => node is InterfaceDeclarationSyntax;
}
```

---

## SyntaxToken Usage

### Accessing Token Information

```csharp
var classDecl = root
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

// Class name token
SyntaxToken identifier = classDecl.Identifier;
Console.WriteLine($"Name: {identifier.Text}");        // User
Console.WriteLine($"Position: {identifier.SpanStart}"); // character position
Console.WriteLine($"Kind: {identifier.Kind()}");      // IdentifierToken

// Modifier tokens
var modifiers = classDecl.Modifiers;
foreach (var modifier in modifiers)
{
    Console.WriteLine($"Modifier: {modifier.Text}");  // public
}
```

### Checking Specific Modifiers

```csharp
// Check if public
bool isPublic = classDecl.Modifiers.Any(SyntaxKind.PublicKeyword);

// Check if partial
bool isPartial = classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

// Check if abstract
bool isAbstract = classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword);
```

---

## SyntaxTrivia Usage

Used when comment or whitespace information is needed:

```csharp
string code = """
    /// <summary>
    /// User information
    /// </summary>
    public class User { }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var classDecl = tree.GetRoot()
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

// Trivia before the public keyword (including comments)
var leadingTrivia = classDecl.GetLeadingTrivia();

foreach (var trivia in leadingTrivia)
{
    if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
    {
        Console.WriteLine($"Documentation comment found: {trivia}");
    }
}
```

---

## Pattern Matching and Syntax API

C# pattern matching makes it easy to analyze Syntax nodes:

```csharp
// Method analysis
void AnalyzeMethod(SyntaxNode node)
{
    if (node is MethodDeclarationSyntax method)
    {
        // Method name
        var name = method.Identifier.Text;

        // Return type (Syntax level)
        var returnType = method.ReturnType switch
        {
            PredefinedTypeSyntax predefined => predefined.Keyword.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => $"{generic.Identifier}<...>",
            _ => "unknown"
        };

        // Parameter list
        var parameters = method.ParameterList.Parameters
            .Select(p => $"{p.Type} {p.Identifier}")
            .ToList();

        Console.WriteLine($"{returnType} {name}({string.Join(", ", parameters)})");
    }
}
```

---

## Limitations of the Syntax API

Clearly recognizing these limitations is the key takeaway of learning the Syntax API. The Syntax API alone cannot determine **type information**. This is precisely why our project's ObservablePortGenerator first filters with `ClassDeclarationSyntax` in the `predicate`, then necessarily uses the Semantic API (`ctx.TargetSymbol`) in the `transform`:

```csharp
string code = """
    public class Example
    {
        public void Process(User user) { }
    }
    """;

var method = tree.GetRoot()
    .DescendantNodes()
    .OfType<MethodDeclarationSyntax>()
    .First();

var parameter = method.ParameterList.Parameters.First();

// What Syntax alone can tell
Console.WriteLine(parameter.Type!.ToString());  // "User" (string)
Console.WriteLine(parameter.Identifier.Text);   // "user"

// What Syntax alone cannot tell
// - Is User a class or interface?
// - What is User's namespace?
// - Which assembly is User defined in?
// -> Such information requires the Semantic API
```

---

## Summary at a Glance

The Syntax API is a tool for quickly traversing the structure of source code. In source generators, it is primarily used for first-pass filtering in the `predicate` stage, while detailed analysis requiring type resolution is delegated to the Semantic API.

| Component | Role | Example |
|-----------|------|---------|
| SyntaxNode | Grammatical structure | ClassDeclarationSyntax |
| SyntaxToken | Smallest grammatical unit | `public`, `User` |
| SyntaxTrivia | Whitespace, comments | spaces, `// comment` |

| Traversal Method | Description |
|------------------|-------------|
| `DescendantNodes()` | All descendant nodes |
| `ChildNodes()` | Direct children only |
| `Ancestors()` | All parent nodes |
| `GetLeadingTrivia()` | Leading trivia |
| `GetTrailingTrivia()` | Trailing trivia |

---

## FAQ

### Q1: Why does the source generator's `predicate` use only the Syntax API?
**A**: The `predicate` is called for every syntax node, so it must execute quickly. The Syntax API only traverses the parsed tree, making it low-cost, while the Semantic API performs type resolution, making it expensive. The performance optimization pattern is to perform fast first-pass filtering to reduce candidates, then use the Semantic API only in the `transform`.

### Q2: When is `SyntaxTrivia` used in actual source generators?
**A**: In most source generators, you rarely need to work with `SyntaxTrivia` directly. However, it is used when analyzing XML documentation comments to reflect them in generated code, or when building code formatting tools that need whitespace and line break information.

### Q3: What is the criterion for choosing between `DescendantNodes()` and `ChildNodes()`?
**A**: `ChildNodes()` returns only direct children, so its scope is narrow and fast. `DescendantNodes()` recursively traverses all descendant nodes. Use `DescendantNodes()` when finding specific nodes in nested structures, and `ChildNodes()` when looking only one level down.

---

As we saw in the limitations of the Syntax API, semantic information such as the full name of a type or whether it implements an interface cannot be obtained through syntax analysis alone. In the next chapter, we learn the Semantic API that goes beyond these limitations.

-> [03. Semantic API](../06-Semantic-Api/)
