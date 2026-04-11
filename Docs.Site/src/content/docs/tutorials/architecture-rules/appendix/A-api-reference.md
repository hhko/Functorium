---
title: "API Reference"
---

This appendix is a reference organized so you can quickly look up the entire Functorium ArchitectureRules API at a glance. Use it to quickly check method usage or parameters during tutorial learning.

## ClassValidator

Inherits `TypeValidator<Class, ClassValidator>` and verifies class-level architecture rules.

### Visibility Rules

| Method | Description |
|--------|-------------|
| `RequirePublic()` | Must be a public class |
| `RequireInternal()` | Must be an internal class |

### Modifier Rules

| Method | Description |
|--------|-------------|
| `RequireSealed()` | Must be a sealed class |
| `RequireNotSealed()` | Must not be sealed |
| `RequireStatic()` | Must be a static class |
| `RequireNotStatic()` | Must not be static |
| `RequireAbstract()` | Must be an abstract class |
| `RequireNotAbstract()` | Must not be abstract |

### Type Rules

| Method | Description |
|--------|-------------|
| `RequireRecord()` | Must be a record type |
| `RequireNotRecord()` | Must not be a record |

### Attribute Rules

| Method | Description |
|--------|-------------|
| `RequireAttribute(string attributeName)` | Must have the specified attribute |

### Inheritance/Interface Rules

| Method | Description |
|--------|-------------|
| `RequireInherits(Type baseType)` | Must inherit a specific base class |
| `RequireImplements(Type interfaceType)` | Must implement a specific interface |
| `RequireImplementsGenericInterface(string name)` | Must implement a generic interface |

### Constructor Rules

| Method | Description |
|--------|-------------|
| `RequirePrivateAnyParameterlessConstructor()` | Must have a private parameterless constructor |
| `RequireAllPrivateConstructors()` | All constructors must be private |

### Property/Field Rules

| Method | Description |
|--------|-------------|
| `RequireProperty(string propertyName)` | A specific property must exist |
| `RequireNoPublicSetters()` | Must have no public setters |
| `RequireNoInstanceFields()` | Must have no instance fields |
| `RequireOnlyPrimitiveProperties(params string[])` | Only primitive type properties allowed |

### Nested Class Rules

| Method | Description |
|--------|-------------|
| `RequireNestedClass(string name, Action<ClassValidator>?)` | Nested class must exist (optional validation) |
| `RequireNestedClassIfExists(string name, Action<ClassValidator>?)` | Validates only if present |

### Immutability Rules

| Method | Description |
|--------|-------------|
| `RequireImmutable()` | Applies ImmutabilityRule (6-dimension immutability verification) |

## InterfaceValidator

Inherits `TypeValidator<Interface, InterfaceValidator>` and verifies interface-level rules.

Uses methods inherited from TypeValidator (naming, method verification, dependencies, etc.).

## MethodValidator

Performs method-level signature verification.

### Visibility/Modifier Rules

| Method | Description |
|--------|-------------|
| `RequireVisibility(Visibility)` | Must have specified visibility |
| `RequireStatic()` | Must be a static method |
| `RequireNotStatic()` | Must not be static |
| `RequireVirtual()` | Must be a virtual method |
| `RequireNotVirtual()` | Must not be virtual |
| `RequireExtensionMethod()` | Must be an extension method |

### Return Type Rules

| Method | Description |
|--------|-------------|
| `RequireReturnType(Type)` | Must have a specific return type (open generic supported) |
| `RequireReturnTypeOfDeclaringClass()` | Must return the declaring class type |
| `RequireReturnTypeOfDeclaringTopLevelClass()` | Must return the top-level class type |
| `RequireReturnTypeContaining(string)` | Return type name must contain the string |

### Parameter Rules

| Method | Description |
|--------|-------------|
| `RequireParameterCount(int)` | Exact parameter count |
| `RequireParameterCountAtLeast(int)` | Minimum parameter count |
| `RequireFirstParameterTypeContaining(string)` | First parameter type contains the string |
| `RequireAnyParameterTypeContaining(string)` | Any parameter type contains the string |

## TypeValidator (Common Base)

The common base class inherited by ClassValidator and InterfaceValidator.

### Naming Rules

| Method | Description |
|--------|-------------|
| `RequireNameStartsWith(string prefix)` | Name must start with the prefix |
| `RequireNameEndsWith(string suffix)` | Name must end with the suffix |
| `RequireNameMatching(string regex)` | Name must match the regular expression |

### Dependency Rules

| Method | Description |
|--------|-------------|
| `RequireNoDependencyOn(string typeNameContains)` | Must not depend on the specified type |

### Method Verification

| Method | Description |
|--------|-------------|
| `RequireMethod(string name, Action<MethodValidator>)` | Verify a specific method |
| `RequireMethodIfExists(string name, Action<MethodValidator>)` | Verify only if present |
| `RequireAllMethods(Action<MethodValidator>)` | Verify all methods |
| `RequireAllMethods(Func<MethodMember, bool>, Action<MethodValidator>)` | Verify filtered methods |

### Rule Composition

| Method | Description |
|--------|-------------|
| `Apply(IArchRule<TType> rule)` | Apply a custom rule |

## Entry Points (ArchitectureValidationEntryPoint)

| Extension Method | Description |
|-----------------|-------------|
| `ValidateAllClasses(Architecture, Action<ClassValidator>, bool verbose)` | Extension on `IObjectProvider<Class>` |
| `ValidateAllInterfaces(Architecture, Action<InterfaceValidator>, bool verbose)` | Extension on `IObjectProvider<Interface>` |

Return type: `ValidationResultSummary`

| Method | Description |
|--------|-------------|
| `ThrowIfAnyFailures(string ruleName)` | Throws `ArchitectureViolationException` on violations |

## Custom Rules

### IArchRule&lt;TType&gt;

```csharp
public interface IArchRule<in TType> where TType : IType
{
    string Description { get; }
    IReadOnlyList<RuleViolation> Validate(TType target, Architecture architecture);
}
```

### DelegateArchRule&lt;TType&gt;

```csharp
// Lambda-based custom rule
var rule = new DelegateArchRule<Class>(
    "Rule description",
    (target, architecture) => {
        // Verification logic
        return violations; // IReadOnlyList<RuleViolation>
    });
```

### CompositeArchRule&lt;TType&gt;

```csharp
// Compose multiple rules with AND
var composite = new CompositeArchRule<Class>(rule1, rule2, rule3);
// Description: "rule1 AND rule2 AND rule3"
```

### RuleViolation

```csharp
public sealed record RuleViolation(
    string TargetName,    // Full name of the violating type
    string RuleName,      // Rule name
    string Description);  // Violation description
```

## ImmutabilityRule 6-Dimension Verification

| Dimension | Verification Content |
|-----------|---------------------|
| **Writability** | Non-static members must be immutable |
| **Constructors** | Must have no public constructors |
| **Properties** | Must have no public setters |
| **Fields** | Must have no public non-static fields |
| **Collections** | Mutable collection types prohibited (List, Dictionary, etc.) |
| **Methods** | Public non-static methods only from the allowed list |

Allowed methods: `Equals`, `GetHashCode`, `ToString`, `Create`, `Validate`, operators, Getter methods

## Architecture Test Suites

Pre-built test suites that instantly apply verified architecture rules through inheritance alone.

### DomainArchitectureTestSuite

Provides 21 tests that verify DDD tactical patterns in the domain layer.

#### Abstract Properties (Required Override)

| Property | Type | Description |
|----------|------|-------------|
| `Architecture` | `Architecture` | Assembly architecture loaded with ArchLoader |
| `DomainNamespace` | `string` | Root namespace where domain types reside |

#### Virtual Properties (Optional Override)

| Property | Default | Description |
|----------|---------|-------------|
| `ValueObjectExcludeFromFactoryMethods` | `[]` | ValueObject types to exclude from Create/Validate factory method verification |
| `DomainServiceAllowedFieldTypes` | `[]` | Field types to allow in DomainService's `RequireNoInstanceFields` |

#### Test List (21)

| Category | Test | Verification Content |
|----------|------|---------------------|
| **Entity** | `AggregateRoot_ShouldBe_PublicSealedClass` | public sealed, not static |
| **Entity** | `AggregateRoot_ShouldHave_CreateAndCreateFromValidated` | Create/CreateFromValidated static factory methods |
| **Entity** | `AggregateRoot_ShouldHave_GenerateEntityIdAttribute` | `[GenerateEntityId]` attribute |
| **Entity** | `AggregateRoot_ShouldHave_AllPrivateConstructors` | All constructors private |
| **Entity** | `Entity_ShouldBe_PublicSealedClass` | public sealed, not static (excluding AggregateRoot) |
| **Entity** | `Entity_ShouldHave_CreateAndCreateFromValidated` | Create/CreateFromValidated static factory methods |
| **Entity** | `Entity_ShouldHave_AllPrivateConstructors` | All constructors private |
| **ValueObject** | `ValueObject_ShouldBe_PublicSealedWithPrivateConstructors` | public sealed + private constructors |
| **ValueObject** | `ValueObject_ShouldBe_Immutable` | ImmutabilityRule 6-dimension immutability |
| **ValueObject** | `ValueObject_ShouldHave_CreateFactoryMethod` | Create -> `Fin<T>` return |
| **ValueObject** | `ValueObject_ShouldHave_ValidateMethod` | Validate -> `Validation<Error, T>` return |
| **DomainEvent** | `DomainEvent_ShouldBe_SealedRecord` | sealed record |
| **DomainEvent** | `DomainEvent_ShouldHave_EventSuffix` | "Event" suffix |
| **Specification** | `Specification_ShouldBe_PublicSealed` | public sealed |
| **Specification** | `Specification_ShouldInherit_SpecificationBase` | `Specification<T>` inheritance |
| **Specification** | `Specification_ShouldResideIn_DomainLayer` | Located within domain namespace |
| **DomainService** | `DomainService_ShouldBe_PublicSealed` | public sealed |
| **DomainService** | `DomainService_ShouldBe_Stateless` | No instance fields (excluding allowed types) |
| **DomainService** | `DomainService_ShouldNotDependOn_IObservablePort` | IObservablePort dependency prohibited |
| **DomainService** | `DomainService_PublicMethods_ShouldReturn_Fin` | Public instance methods return `Fin<T>` |
| **DomainService** | `DomainService_ShouldNotBe_Record` | Not a record type |

### ApplicationArchitectureTestSuite

Provides 4 tests that verify Command/Query structure in the application layer.

#### Abstract Properties (Required Override)

| Property | Type | Description |
|----------|------|-------------|
| `Architecture` | `Architecture` | Assembly architecture loaded with ArchLoader |
| `ApplicationNamespace` | `string` | Root namespace where application types reside |

#### Test List (4)

| Test | Verification Content |
|------|---------------------|
| `Command_ShouldHave_ValidatorNestedClass` | If a Command has a Validator, it must be sealed + implement `AbstractValidator` |
| `Command_ShouldHave_UsecaseNestedClass` | Command must have a Usecase, sealed + implement `ICommandUsecase` |
| `Query_ShouldHave_ValidatorNestedClass` | If a Query has a Validator, it must be sealed + implement `AbstractValidator` |
| `Query_ShouldHave_UsecaseNestedClass` | Query must have a Usecase, sealed + implement `IQueryUsecase` |

---

The next appendix provides a cheat sheet for quick reference of ArchUnitNET core APIs.

-> [Appendix B: ArchUnitNET Cheat Sheet](B-archunitnet-cheatsheet.md)
