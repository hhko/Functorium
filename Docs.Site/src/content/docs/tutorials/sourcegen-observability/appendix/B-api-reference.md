---
title: "API Reference"
---

This is a quick reference for the core types and classes related to ObservablePortGenerator. For detailed descriptions of each type, refer to the corresponding Part in the main tutorial.

---

## IncrementalGeneratorBase&lt;TValue&gt;

An abstract class based on the Template Method pattern for incremental source generators.

**Namespace:** `Functorium.SourceGenerators.Generators`

| Constructor Parameter | Type | Description |
|----------------------|------|-------------|
| `registerSourceProvider` | `Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>>` | Source provider registration (Step 1) |
| `generate` | `Action<SourceProductionContext, ImmutableArray<TValue>>` | Code generation (Step 2) |
| `AttachDebugger` | `bool` (default: `false`) | Whether to attach debugger in DEBUG builds |

---

## ObservableClassInfo

A record struct that holds target class information extracted by the source generator.

**Namespace:** `Functorium.SourceGenerators.Generators.ObservablePortGenerator`

| Property | Type | Description |
|----------|------|-------------|
| `Namespace` | `string` | Namespace of the class |
| `ClassName` | `string` | Class name |
| `Methods` | `List<MethodInfo>` | List of methods |
| `BaseConstructorParameters` | `List<ParameterInfo>` | Base class constructor parameters |
| `Location` | `Location?` | Source code location (for diagnostics) |

The static field `ObservableClassInfo.None` represents an empty instance.

---

## MethodInfo

A class that holds method signature information.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Method name |
| `Parameters` | `List<ParameterInfo>` | List of parameters |
| `ReturnType` | `string` | Return type string |

---

## ParameterInfo

A class that holds parameter information.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Parameter name |
| `Type` | `string` | Type string |
| `RefKind` | `RefKind` | Reference kind (in, out, ref, etc.) |
| `IsCollection` | `bool` | Whether it is a collection type |

---

## TypeExtractor

A utility that extracts the second type parameter from `FinT<IO, T>`.

| Method | Description |
|--------|-------------|
| `ExtractSecondTypeParameter(string returnType)` | Extracts the second parameter from a generic type |

**Input/Output examples:**

| Input | Output |
|-------|--------|
| `FinT<IO, User>` | `User` |
| `FinT<IO, List<User>>` | `List<User>` |
| `FinT<IO, Dictionary<string, int>>` | `Dictionary<string, int>` |
| `FinT<IO, (int Id, string Name)>` | `(int Id, string Name)` |
| `FinT<IO, string[]>` | `string[]` |

---

## CollectionTypeHelper

A utility for determining collection types and generating Count/Length expressions.

| Method | Description |
|--------|-------------|
| `IsCollectionType(string typeFullName)` | Whether the type is a collection (excluding tuples) |
| `IsTupleType(string typeFullName)` | Whether the type is a tuple |
| `GetCountExpression(string variableName, string typeFullName)` | Generates a `.Count` or `.Length` expression |
| `GetRequestFieldName(string parameterName)` | Generates a `request.params.{name}` field name |
| `GetResponseFieldName()` | Returns the `response.result` field name |
| `GetResponseCountFieldName()` | Returns the `response.result.count` field name |

**Recognized collection types:** `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `HashSet<T>`, `Dictionary<K,V>`, `IDictionary<K,V>`, `IReadOnlyDictionary<K,V>`, `Queue<T>`, `Stack<T>`, arrays (`T[]`)

---

## ConstructorParameterExtractor

A utility that extracts constructor parameters from the target class.

| Method | Description |
|--------|-------------|
| `ExtractParameters(INamedTypeSymbol classSymbol)` | Extracts constructor parameters |

**Priority rules:**

1. If a Primary Constructor exists, its parameters are used
2. If there are multiple constructors, the one with the most parameters is selected
3. If there are no parameters, an empty list is returned

---

## ParameterNameResolver

A utility that resolves parameter name conflicts in the generated Observable class constructor.

| Method | Description |
|--------|-------------|
| `ResolveName(string parameterName)` | Converts a single parameter name |
| `ResolveNames(List<ParameterInfo> parameters)` | Batch parameter name conversion |

**Conversion examples:**

| Original | Converted Result | Reason |
|----------|-----------------|--------|
| `logger` | `baseLogger` | Conflicts with the Observable class's `logger` |
| `_logger` | `baseLogger` | Underscore prefix removed, then `base` prefix added |
| `activitySource` | `baseActivitySource` | Conflicts with reserved parameters of the Observable class |
| `connectionString` | `connectionString` | No conflict — no conversion |

---

## SymbolDisplayFormats

Display formats for deterministic type string generation.

| Field | Description |
|-------|-------------|
| `GlobalQualifiedFormat` | A format that always includes the `global::` prefix |

`FullyQualifiedFormat` output may vary depending on using statements, but `GlobalQualifiedFormat` always outputs in the form `global::System.Collections.Generic.List<T>`, ensuring deterministic code generation.

---

## IObservablePort

A marker interface that identifies target classes for the source generator.

**Namespace:** `Functorium.Abstractions.Observabilities`

```csharp
public interface IObservablePort
{
    string RequestCategory { get; }
}
```

`RequestCategory` is used to distinguish the request category in observability tags.

---

## SourceGeneratorTestRunner

A utility class for testing source generators.

**Namespace:** `Functorium.Testing.Actions.SourceGenerators`

| Method | Description |
|--------|-------------|
| `Generate<TGenerator>(this TGenerator, string sourceCode)` | Runs the generator and returns generated code (fails on diagnostic errors) |
| `GenerateWithDiagnostics<TGenerator>(this TGenerator, string sourceCode)` | Returns generated code along with Diagnostics |

Both methods are extension methods on `IIncrementalGenerator` and internally use `CSharpCompilation` and `CSharpGeneratorDriver` to run the generator in an isolated compilation environment.
