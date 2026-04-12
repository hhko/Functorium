---
title: "Test Scenario"
---

## Overview

The reliability of a source generator is the reliability of the generated code. If ObservablePortGenerator produces incorrect code, the error occurs at compile time, not runtime, making it difficult for users to identify the cause. To prevent this, 31 test scenarios are systematically organized into 8 categories. Each category is designed based not only on "what to test" but also on "why this scenario is needed."

## Test Design Principles

Before examining the test scenarios, understanding the four design principles that ObservablePortGenerator tests follow makes it easier to grasp the intent of each test.

**Single scenario principle.** Each test verifies only one feature. Separating `Count` and `Length` into different tests rather than bundling them together allows immediately pinpointing the cause upon failure.

**Boundary value testing.** Both sides of thresholds where behavior diverges, like `LoggerMessage.Define`'s 6-parameter limit, are tested. 2 parameters (total 6, using Define) and 3 parameters (total 7, LogDebug fallback) are verified as separate scenarios.

**Exception case testing.** Cases where code "should not be generated," such as adapters without methods or collections inside tuples, are explicitly verified. `ShouldNotContain` assertions confirm that unintended code is not generated.

**Clear naming convention.** The `Should_{Action}_{Condition}` pattern is followed, so the verification target and conditions are apparent from the test name alone.

## Learning Objectives

### Core Learning Objectives
1. **Understanding the 8 test categories**
   - Verification scope of each category, from basic generation to diagnostics
2. **Test cases for each scenario**
   - 31 scenarios covering both normal and exception paths
3. **Practical application of test design principles**
   - Confirming how the above principles are reflected in each test

---

## Test Category Overview

ObservablePortGenerator consists of 31 test scenarios organized into 8 categories.

| Category | Test Count | Verification Content |
|----------|-----------|---------------------|
| 1. Basic generation | 1 | Attribute generation |
| 2. Basic adapter | 3 | Class generation |
| 3. Parameters | 8 | Input parameter handling |
| 4. Return types | 6 | Output type handling |
| 5. Constructors | 4 | Constructor parameters |
| 6. Interfaces | 3 | IObservablePort implementation |
| 7. Namespaces | 2 | Namespace handling |
| 8. Diagnostics | 4 | Diagnostic reporting |

---

## 1. Basic Generation Tests

The most fundamental test to confirm that the source generator is correctly registered. Since the `[GenerateObservablePort]` Attribute itself is automatically provided by the source generator, the Attribute code should be generated even with empty input.

### GenerateObservablePortAttribute Auto-Generation

```csharp
/// <summary>
/// Verifies that the source generator automatically generates the [GenerateObservablePort] Attribute.
/// </summary>
[Fact]
public Task ObservablePortGenerator_ShouldGenerate_GenerateObservablePortAttribute()
{
    // Attribute code is generated even with empty input
    string input = string.Empty;

    string? actual = _sut.Generate(input);

    return Verify(actual);
}
```

**Verification content**: Source generator automatically provides the marker Attribute

---

## 2. Basic Adapter Scenarios

This category verifies the core functionality of ObservablePortGenerator: "generating an Observable class from an adapter class." It tests single method, multiple methods, and no-method cases to confirm the basic operational scope of the generator.

### Single Method Adapter

```csharp
/// <summary>
/// Verifies that a pipeline class is generated for an adapter that implements
/// IPort and has a single method.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithSingleMethod()
{
    string input = """
        [GenerateObservablePort]
        public class TestAdapter : ITestAdapter
        {
            public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

### Multiple Method Adapter

```csharp
/// <summary>
/// Verifies that all methods are overridden for an adapter with multiple methods.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithMultipleMethods()
{
    string input = """
        [GenerateObservablePort]
        public class MultiMethodAdapter : IMultiMethodAdapter
        {
            public virtual FinT<IO, int> GetValue() => ...;
            public virtual FinT<IO, string> GetName() => ...;
            public virtual FinT<IO, bool> IsValid() => ...;
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

### Adapter Without Methods

```csharp
/// <summary>
/// When only implementing IPort without methods, no pipeline should be generated.
/// </summary>
[Fact]
public Task Should_NotGenerate_PipelineClass_WhenNoMethods()
{
    string input = """
        [GenerateObservablePort]
        public class EmptyAdapter : IObservablePort
        {
            public string RequestCategory => "Test";
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

---

## 3. Parameter Scenarios

Method parameters directly affect the number of logging fields, so this category requires the most test cases as it interacts with `LoggerMessage.Define`'s 6-parameter limit. It verifies boundary values (2 vs 3 parameters), Count field addition for collection parameters, and exception handling for types like tuples that could be mistaken for collections.

### Parameter Count and LoggerMessage.Define

| Parameter Count | Total Fields | Usage |
|----------------|-------------|-------|
| 0 | 4 | LoggerMessage.Define |
| 2 | 6 | LoggerMessage.Define |
| 3 | 7 | logger.LogDebug() |

```csharp
// 0-parameter test
[Fact]
public Task Should_Generate_LoggerMessageDefine_WithZeroParameters()
{
    string input = """
        [GenerateObservablePort]
        public class ZeroParamAdapter : IObservablePort
        {
            public virtual FinT<IO, int> GetValue() => ...;
        }
        """;
    // ...
}

// 2-parameter test (boundary value)
[Fact]
public Task Should_Generate_LoggerMessageDefine_WithTwoParameters()
{
    string input = """
        [GenerateObservablePort]
        public class TwoParamAdapter : IObservablePort
        {
            public virtual FinT<IO, string> GetData(int id, string name) => ...;
        }
        """;
    // ...
}

// 3-parameter test (fallback)
[Fact]
public Task Should_Generate_LogDebugFallback_WithThreeParameters()
{
    string input = """
        [GenerateObservablePort]
        public class ThreeParamAdapter : IObservablePort
        {
            public virtual FinT<IO, string> GetData(int id, string name, bool flag) => ...;
        }
        """;
    // ...
}
```

### Collection Parameters

```csharp
/// <summary>
/// Verifies that Count fields are added for collection type parameters.
/// </summary>
[Fact]
public Task Should_Generate_CollectionCountFields()
{
    string input = """
        [GenerateObservablePort]
        public class CollectionParamAdapter : IObservablePort
        {
            public virtual FinT<IO, int> ProcessItems(List<string> items) => ...;
        }
        """;
    // ...
}
```

### Tuple Parameters (Not Recognized as Collection)

```csharp
/// <summary>
/// Tuples are not recognized as collections, so Count fields should not be generated.
/// </summary>
[Fact]
public Task Should_NotGenerate_Count_ForTupleParameter()
{
    string input = """
        [GenerateObservablePort]
        public class TupleAdapter : IObservablePort
        {
            // Even if a List exists inside a tuple, Count is not generated
            public virtual FinT<IO, int> Process((int Id, List<string> Tags) user) => ...;
        }
        """;
    // ...
}
```

---

## 4. Return Type Scenarios

Return types are the area where `TypeExtractor`'s generic parsing and `CollectionTypeHelper`'s collection detection are applied simultaneously. Various patterns from simple types to nested generics, arrays, and tuples are tested to verify that type extraction and Count/Length generation work correctly.

### Simple Return Types

```csharp
/// <summary>
/// Verifies simple type extraction for FinT<IO, int>, FinT<IO, string>, etc.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithSimpleReturnType()
{
    string input = """
        [GenerateObservablePort]
        public class SimpleAdapter : IObservablePort
        {
            public virtual FinT<IO, int> GetNumber() => ...;
            public virtual FinT<IO, string> GetText() => ...;
            public virtual FinT<IO, bool> GetFlag() => ...;
        }
        """;
    // ...
}
```

### Collection Return Types

```csharp
/// <summary>
/// Verifies that Count/Length fields are generated for List<T> and T[] return types.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithCollectionReturnType()
{
    string input = """
        [GenerateObservablePort]
        public class CollectionAdapter : IObservablePort
        {
            public virtual FinT<IO, List<User>> GetUsers() => ...;
            public virtual FinT<IO, string[]> GetNames() => ...;
        }
        """;
    // ...
}
```

### Complex Generics

```csharp
/// <summary>
/// Verifies nested generic extraction like Dictionary<K, List<V>>.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithComplexGenericReturnType()
{
    string input = """
        [GenerateObservablePort]
        public class ComplexAdapter : IObservablePort
        {
            public virtual FinT<IO, Dictionary<string, List<int>>> GetComplexData() => ...;
        }
        """;
    // ...
}
```

### Tuple Return Types

```csharp
/// <summary>
/// Verifies that Count is not generated for (int Id, string Name) tuple returns.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithTupleReturnType()
{
    string input = """
        [GenerateObservablePort]
        public class TupleAdapter : IObservablePort
        {
            public virtual FinT<IO, (int Id, string Name)> GetUserInfo() => ...;
            public virtual FinT<IO, (int Id, List<string> Tags)> GetUserWithTags() => ...;
        }
        """;
    // ...
}
```

---

## 5. Constructor Scenarios

Constructor handling is accomplished through the cooperation of `ConstructorParameterExtractor` and `ParameterNameResolver`. Primary Constructor, optimal selection among multiple constructors, and reserved name conflict resolution for names like `logger` are each tested independently.

### Primary Constructor

```csharp
/// <summary>
/// Verifies handling of classes with C# 12+ Primary Constructor.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithPrimaryConstructor()
{
    string input = """
        [GenerateObservablePort]
        public class PrimaryCtorAdapter(string connectionString) : IObservablePort
        {
            public virtual FinT<IO, string> GetConnectionString() => ...;
        }
        """;
    // ...
}
```

### Multiple Constructors

```csharp
/// <summary>
/// Verifies that the constructor with the most parameters is selected among multiple constructors.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithMultipleConstructors()
{
    string input = """
        [GenerateObservablePort]
        public class MultiCtorAdapter : IObservablePort
        {
            public MultiCtorAdapter() { }
            public MultiCtorAdapter(string connStr) { }
            public MultiCtorAdapter(string connStr, int timeout) { }  // Selected
        }
        """;
    // ...
}
```

### Parameter Name Conflict

```csharp
/// <summary>
/// Verifies that the logger parameter is renamed to baseLogger.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithParameterNameConflict()
{
    string input = """
        [GenerateObservablePort]
        public class ConflictAdapter(ILogger<ConflictAdapter> logger) : IObservablePort
        {
            // logger -> needs to be converted to baseLogger
        }
        """;
    // ...
}
```

---

## 6. Interface Scenarios

ObservablePortGenerator operates on classes that implement `IObservablePort`. Direct implementation, indirect implementation through inherited interfaces, and implementing multiple interfaces simultaneously must all be correctly detected.

### Direct IObservablePort Implementation

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithDirectIPortImplementation()
{
    string input = """
        [GenerateObservablePort]
        public class DirectAdapter : IObservablePort
        {
            public virtual FinT<IO, int> GetValue() => ...;
        }
        """;
    // ...
}
```

### IObservablePort Inherited Interface

```csharp
/// <summary>
/// Verifies inherited interfaces of the form IUserRepository : IObservablePort.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithInheritedIPortInterface()
{
    string input = """
        public interface IUserRepository : IObservablePort
        {
            FinT<IO, string> GetUserById(int id);
        }

        [GenerateObservablePort]
        public class UserRepository : IUserRepository { ... }
        """;
    // ...
}
```

### Multiple Interfaces

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithMultipleInterfaces()
{
    string input = """
        [GenerateObservablePort]
        public class MultiInterfaceAdapter : IObservablePort, IDisposable
        {
            public virtual FinT<IO, int> GetValue() => ...;
            public void Dispose() { }
        }
        """;
    // ...
}
```

---

## 7. Namespace Scenarios

Generated code must be placed in the same namespace as the original class. Tests confirm that both simple and deep namespaces produce correct filenames and namespace declarations.

### Simple Namespace

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithSimpleNamespace()
{
    string input = """
        namespace MyApp;

        [GenerateObservablePort]
        public class SimpleAdapter : IObservablePort { ... }
        """;
    // Generated file: MyApp.SimpleObservablePort.g.cs
}
```

### Deep Namespace

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithDeepNamespace()
{
    string input = """
        namespace Company.Domain.Adapters.Infrastructure.Repositories;

        [GenerateObservablePort]
        public class DeepAdapter : IObservablePort { ... }
        """;
    // Generated file: Company.Domain.Adapters.Infrastructure.Repositories.DeepObservablePort.g.cs
}
```

---

## 8. Diagnostic Scenarios

A source generator is not only responsible for generating code but also for detecting incorrect usage patterns and **reporting Diagnostic messages.** When constructor parameters contain observability infrastructure types like `ActivitySource` or `IMeterFactory` as duplicates, they conflict with the generated Observable class's constructor. These 4 scenarios verify warning at compile time.

### Duplicate Parameter Type Detection

```csharp
[Fact]
public void Should_ReportDiagnostic_WhenDuplicateParameterTypes()
{
    // Constructor that already has ActivitySource -> diagnostic warning
    string input = """
        [GenerateObservablePort]
        public class DuplicateAdapter(ActivitySource activitySource) : IObservablePort { ... }
        """;
    // Verify FUNCTORIUM001 diagnostic report
}
```

### Duplicate MeterFactory Detection

```csharp
[Fact]
public void Should_ReportDiagnostic_WhenDuplicateMeterFactoryParameter()
{
    // Constructor that already has IMeterFactory -> diagnostic warning
}
```

### Diagnostic Location Accuracy

```csharp
[Fact]
public void Should_ReportDiagnostic_WithCorrectLocation()
{
    // Verify that the diagnostic message's Location points to the class declaration
}
```

### Normal Case (No Diagnostics)

```csharp
[Fact]
public void Should_NotReportDiagnostic_WhenNoParameterDuplication()
{
    // Normal constructor without duplicates -> 0 diagnostics
}
```

---

## Test Coverage

| Category | Normal Cases | Exception Cases |
|----------|-------------|-----------------|
| Basic generation | 1 | - |
| Basic adapter | 2 | 1 |
| Parameters | 6 | 2 |
| Return types | 4 | 2 |
| Constructors | 3 | 1 |
| Interfaces | 3 | - |
| Namespaces | 2 | - |
| Diagnostics | 1 | 3 |

---

## Summary at a Glance

The 31 test scenarios systematically verify all code generation paths of ObservablePortGenerator. Each category addresses an independent concern, including not only normal paths but also exception paths like adapters without methods and collections inside tuples. The four design principles defined earlier (single scenario, boundary values, exception cases, clear naming) are consistently applied across all tests, enabling immediate understanding of how source generator changes affect existing behavior.

---

## FAQ

### Q1: What are the most easily overlooked cases among the 31 test scenarios?
**A**: The most easily overlooked are collections contained inside tuples (`FinT<IO, (int Id, List<string> Tags)>`) and the `LoggerMessage.Define` boundary value (2 vs 3 parameters). The former causes a compilation error if Count is generated, and the latter differs between high-performance and fallback paths by a single parameter, so both sides must be tested.

### Q2: When is the `ShouldNotContain` assertion used?
**A**: It is used to verify code that "should not be generated." For example, when a `response.result.count` field should not be generated for a tuple return type, or when method overrides should not be generated for an adapter without methods, it is explicitly verified with `actual.ShouldNotContain("response.result.count")`. Snapshot tests alone make it difficult to confirm that "what should be absent is indeed absent."

### Q3: Why is the `Should_{Action}_{Condition}` pattern used in test names?
**A**: Because when a test fails, you can immediately understand "what failed under what condition" from the name alone. A name like `Should_Generate_LogDebugFallback_WithThreeParameters` clearly communicates "LogDebug fallback should be generated with 3 parameters," enabling fast root cause tracking.

---

We have covered all the advanced topics in Part 3 (constructors, generics, collections, LoggerMessage limits, testing). In the next Part, we will learn Source Generator development procedures through various practical examples.

-> [Part 4. Cookbook](../../Part4-Cookbook/01-Development-Workflow/)
