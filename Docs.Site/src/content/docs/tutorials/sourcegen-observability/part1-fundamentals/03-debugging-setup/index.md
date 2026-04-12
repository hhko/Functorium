---
title: "Debugging Setup"
---

## Overview

When there's a bug in source generator code, you can't simply press F5 to debug like a regular application. This is because source generators execute at **compile time**, not runtime. Since you need to attach a debugger to the compiler process, separate setup is required. Without knowing this, you'll resort to the inefficient debugging approach of using `Console.WriteLine` to inspect generated code.

This chapter introduces three debugging methods and explains why test-project-based debugging is the most practical.

## Learning Objectives

### Core Learning Objectives
1. **Understand the special nature of source generator debugging**
   - How the constraint of compile-time execution affects debugging approaches
2. **Learn to use `Debugger.Launch()`**
   - How to leverage the JIT debugger in urgent situations
3. **Learn debugging techniques using test projects**
   - Repeatable debugging in a reproducible, isolated environment

---

## The Special Nature of Source Generator Debugging

Source generators execute at **compile time**, requiring a different approach from typical application debugging.

```
Regular Application Debugging
=============================
Developer → F5 → Runtime execution → Breakpoint

Source Generator Debugging
==========================
Developer → Build → Compiler execution → Source generator execution → Breakpoint
                         ↑
                Must attach debugger here
```

---

## Debugging Methods Overview

| Method | Difficulty | Stability | Recommended Scenario |
|------|--------|--------|-----------|
| Debugger.Launch() | Easy | High | Quick debugging |
| Test project | Easy | **Very high** | **Recommended (default)** |
| Attach to Process | Hard | Low | Special situations |

---

## Method 1: Using Debugger.Launch()

The most intuitive method is to request debugger attachment directly from code. Our project's `IncrementalGeneratorBase` abstracts this with an `AttachDebugger` parameter.

### Using IncrementalGeneratorBase

The Functorium project supports debugging through the `AttachDebugger` parameter:

```csharp
// IncrementalGeneratorBase.cs
public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext,
         IncrementalValuesProvider<TValue>> registerSourceProvider,
    Action<SourceProductionContext, ImmutableArray<TValue>> generate,
    //Action<IncrementalGeneratorPostInitializationContext>? registerPostInitializationSourceOutput = null,
    bool AttachDebugger = false)  // ← Debugging flag
    : IIncrementalGenerator
{
    protected const string ClassEntityName = "class";

    private readonly bool _attachDebugger = AttachDebugger;
    private readonly Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> _registerSourceProvider = registerSourceProvider;
    private readonly Action<SourceProductionContext, ImmutableArray<TValue>> _generate = generate;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        // Debugger attachment supported only in DEBUG builds
        // When debugging is needed, set AttachDebugger: true in ObservablePortGenerator
        if (_attachDebugger && Debugger.IsAttached is false)
        {
            Debugger.Launch();  // ← Shows JIT debugger dialog
        }
#endif

        IncrementalValuesProvider<TValue> provider = _registerSourceProvider(context)
            .Where(static m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<TValue> displayValues)
    {
        _generate(context, displayValues);
    }
}
```

### Enabling Debugging

```csharp
// ObservablePortGenerator.cs
[Generator(LanguageNames.CSharp)]
public sealed class ObservablePortGenerator()
    : IncrementalGeneratorBase<ObservableClassInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: true)  // Change to true
```

### Debugging Flow

```
1. Set AttachDebugger: true
              ↓
2. Build solution (Ctrl+Shift+B)
              ↓
3. "Just-In-Time Debugger" dialog appears
              ↓
4. Select Visual Studio instance
              ↓
5. Execution stops at breakpoint
              ↓
6. Restore AttachDebugger: false after debugging
```

### Cautions

```
Important: Always restore to false after debugging

If AttachDebugger: true is committed:
- Debugger dialog appears for all team members' builds
- CI/CD pipeline fails (timeout due to dialog waiting)
```

---

## Method 2: Debugging from Test Project (Recommended)

`Debugger.Launch()` is quick but one-time. In actual development, you need an environment where you can repeatedly debug with the same input. Test-project-based debugging solves this problem.

### Advantages

- Stable: No compiler process timing issues
- Repeatable: Test multiple times with the same input
- Isolated environment: No impact on other projects
- Fast feedback: No full build needed

### Using SourceGeneratorTestRunner

```csharp
// SourceGeneratorTestRunner.cs
public static class SourceGeneratorTestRunner
{
    public static string? Generate<TGenerator>(
        this TGenerator generator,
        string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        // 1. Parse source code
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // 2. Create compilation
        var compilation = CSharpCompilation.Create(
            "SourceGeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 3. Run source generator
        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

        // 4. Return generated code
        return outputCompilation.SyntaxTrees
            .Skip(1)  // Exclude original
            .LastOrDefault()?
            .ToString();
    }
}
```

### Debugging from Test Code

```csharp
[Fact]
public Task Should_Generate_Observable_For_Simple_Adapter()
{
    // Arrange
    string input = """
        using Functorium.Adapters.SourceGenerators;
        using LanguageExt;

        namespace MyApp.Adapters;

        public interface IUserRepository : IObservablePort
        {
            FinT<IO, User> GetUserAsync(int id);
        }

        [GenerateObservablePort]
        public class UserRepository : IUserRepository
        {
            public FinT<IO, User> GetUserAsync(int id) => throw new NotImplementedException();
        }
        """;

    // Act - Set breakpoint here!
    string? actual = _sut.Generate(input);  // ← F11 to step into source generator

    // Assert
    return Verify(actual);
}
```

### Debugging Tests in Visual Studio

```
1. Set breakpoint in test method

2. Set breakpoints in source generator code
   - ObservablePortGenerator.cs: MapToObservableClassInfo()
   - ObservablePortGenerator.cs: Generate()

3. Open Test Explorer (Ctrl+E, T)

4. Right-click test → Select "Debug"

5. Execution stops at breakpoint

6. F11 (Step Into) to enter source generator internals
```

---

## Method 3: Attach to Process

### Usage Scenarios

- Problem occurs during actual project build
- Environment where Debugger.Launch() doesn't work

### Procedure

```
1. Start build from command line (with --no-incremental option)
   dotnet build MyProject.csproj --no-incremental

2. Attach to Process in Visual Studio (Ctrl+Alt+P)

3. Search for process: "csc" or "VBCSCompiler"

4. Select process and Attach

5. Execution stops at breakpoint
```

### Disadvantages

```
Not recommended because:

- Compiler process terminates quickly
- Very difficult to match timing
- Repeatable debugging is difficult
```

---

## Useful Debugging Tips

### 1. Viewing Generated Code

You can directly view generated code in Visual Studio:

```
Solution Explorer
→ Dependencies
→ Analyzers
→ Functorium.SourceGenerators
→ Functorium.SourceGenerators.ObservablePortGenerator
   → GenerateObservablePortAttribute.g.cs
   → Repositories.UserRepositoryObservable.g.cs
   → ...
```

### 2. Using the Watch Window

Useful expressions during debugging:

```csharp
// Full class name
classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
// → "global::MyApp.Adapters.UserRepository"

// All interfaces
classSymbol.AllInterfaces.Select(i => i.Name).ToArray()
// → ["IUserRepository", "IObservablePort"]

// Method signature
method.ToDisplayString()
// → "GetUserAsync(int)"

// Parameter types
method.Parameters.Select(p => p.Type.ToDisplayString()).ToArray()
// → ["int"]

// Return type
method.ReturnType.ToDisplayString()
// → "LanguageExt.FinT<LanguageExt.IO, User>"
```

### 3. Conditional Breakpoints

Stop only under specific conditions:

```
Right-click breakpoint → Conditions

Condition examples:
- className == "UserRepository"
- method.Name == "GetUserAsync"
- method.Parameters.Length > 2
```

### 4. Build Log Inspection

```bash
# Generate detailed log
dotnet build MyProject.csproj -v:diag > build.log

# Search for source generator related logs
grep -i "sourcegenerator" build.log
```

---

## Troubleshooting

### Problem 1: Breakpoint Not Working

**Symptom**: Breakpoint appears as an empty circle

**Solution**:

```bash
# 1. Delete build cache
rm -rf bin obj

# 2. Clean and rebuild solution
dotnet clean
dotnet build
```

### Problem 2: Code Changes Not Reflected

**Symptom**: Previous code is still generated after modifying the source generator

**Solution**:

```
1. Close Visual Studio completely (important!)

2. Delete all bin, obj folders:
   Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force

3. Restart Visual Studio

4. Clean → Rebuild
```

### Problem 3: Cannot Step Into Source Generator from Test

**Solution**: Check source generator reference in the test project

```xml
<ProjectReference
    Include="..\MySourceGenerator\MySourceGenerator.csproj"
    ReferenceOutputAssembly="true" />  ← Verify true
```

---

## Recommended Workflow

```
Regular Development
===================
1. Debug from test project (Method 2) ← Default
2. Write new test cases
3. Resolve issues through repeated debugging

Urgent Situations
=================
1. Use Debugger.Launch() (Method 1)
2. Immediately restore to false after identifying the issue

Verification Tasks
==================
1. Check generated code in Solution Explorer → Analyzers
2. Analyze build logs
```

---

## Summary at a Glance

Among the three debugging methods, test-project-based debugging is the most practical in terms of stability and repeatability. `Debugger.Launch()` should only be used in urgent situations, and the `ToDisplayString()` expression in the Watch window is a key tool for understanding symbol state.

| Item | Recommended Method |
|------|-----------|
| Default debugging | Use test project |
| Quick check | Debugger.Launch() (temporary) |
| View generated code | Solution Explorer → Analyzers |
| Debugging expressions | classSymbol.ToDisplayString(), etc. |

---

## FAQ

### Q1: What happens if `Debugger.Launch()` is left in production code?
**A**: It is wrapped in `#if DEBUG` preprocessor directives, so it is not included in Release builds. However, the debugger dialog may unintentionally appear in Debug builds, so after resolving the issue, you must restore it to `false` or deactivate that code.

### Q2: Why is test-project-based debugging recommended over `Debugger.Launch()`?
**A**: In the test project, you create an isolated compilation environment with `CSharpCompilation` to run the generator. You can set breakpoints and repeatedly execute like regular unit tests, making it stable and not affecting the actual build process.

### Q3: How do you resolve the case where source generator code was modified but previous results keep appearing?
**A**: This occurs due to Roslyn's caching mechanism. Delete all `bin`/`obj` folders, close Visual Studio completely, then reopen and perform a Clean Build to resolve the issue.

---

With the debugging environment in place, it's time to understand the architecture of the Roslyn compiler platform that source generators utilize. We'll examine what Syntax Tree, Semantic Model, and Symbol each are and how they connect.

→ [4. Roslyn Architecture](../04-Roslyn-Architecture/)
