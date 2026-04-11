---
title: "Glossary"
---
## A

### Apply
A functional pattern that runs multiple validations in parallel and collects all errors. Used with the `Validation<Error, T>` type.

```csharp
(ValidateA(), ValidateB(), ValidateC())
    .Apply((a, b, c) => new Result(a, b, c));
```

### Always Valid
A pattern where a value object always maintains a valid state after creation. Created only after validation in a factory method.

---

## B

### Bind
The core operation of a monad. Passes the success value to another operation and short-circuits on failure. Equivalent to `SelectMany`.

```csharp
result.Bind(value => NextOperation(value));
```

---

## C

### ComparableValueObject
A value object that implements the `IComparable<T>` interface. Supports sorting and comparison operations.

### CQRS (Command Query Responsibility Segregation)
An architectural pattern that separates the responsibility of commands (writes) and queries (reads).

---

## D

### DDD (Domain-Driven Design)
A methodology for designing software centered around the domain model.

### DomainError
A helper class that creates domain errors for value objects through the `DomainError.For<T>()` static method. Automatically generates error codes in the format `DomainErrors.{ValueObjectName}.{ErrorName}`.

```csharp
using static Functorium.Domains.Errors.DomainErrorType;
DomainError.For<Email>(new Empty(), value, "Email cannot be empty");
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password too short");
```

### DomainErrorType
The base record class for domain error types. Defines type-safe errors as a sealed record hierarchy. Built-in types: `Empty`, `Null`, `TooShort`, `TooLong`, `WrongLength`, `OutOfRange`, `Negative`, `NotPositive`, `InvalidFormat`, etc. Custom error types can be defined by deriving from the `Custom` record.

```csharp
// Define a custom error type
public sealed record Unsupported : DomainErrorType.Custom;
```

---

## E

### Entity
A domain object that has a unique identifier and can change during its lifecycle. The contrasting concept to a value object.

### Error Type
The `Error` type from LanguageExt. Structured error information containing a code and a message.

---

## F

### Factory Method
A static method that encapsulates object creation. In value objects, the `Create` method that includes validation.

### Fin<T>
A result type from LanguageExt. Represents Success (Succ) or Failure (Fail).

```csharp
Fin<User> result = User.Create(name, email);
```

### Functor
A type that supports the `Map` operation. Transforms the value inside a container.

---

## G

### GetEqualityComponents
A method that returns the components used for equality comparison in a ValueObject.

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return Property1;
    yield return Property2;
}
```

---

## I

### Immutability
The property that an object's state cannot change after creation. A core principle of value objects.

### IValueObject
A marker interface indicating that something is a value object.

---

## L

### LanguageExt
A functional programming library for C#. Provides `Fin<T>`, `Option<T>`, `Validation<E, T>`, and more.

### LINQ Expression
Query syntax using `from`, `select`, `where`, etc. Can be combined with monad operations.

```csharp
var result =
    from a in GetA()
    from b in GetB(a)
    select Combine(a, b);
```

---

## M

### Map
A transformation operation for functors/monads. Applies a function to the inner value and wraps the result in the same container.

```csharp
Fin<int> number = 10;
Fin<string> text = number.Map(n => n.ToString());
```

### Match
Pattern matching. Executes different logic depending on the case such as success/failure, Some/None.

```csharp
result.Match(
    Succ: value => HandleSuccess(value),
    Fail: error => HandleError(error)
);
```

### Monad
A type that supports the `Bind` operation. Supports both `Map` and `Bind`.

---

## O

### Option<T>
A type that may or may not have a value. Used instead of null.

```csharp
Option<User> user = Some(new User());
Option<User> noUser = None;
```

### Operator Overloading
Custom implementation of operators such as `+`, `-`, `==`, `implicit`.

---

## P

### Prelude
A collection of static helper methods from LanguageExt. Used with `using static LanguageExt.Prelude;`.

### Pure Function
A function that always returns the same output for the same input without side effects.

---

## R

### Railway Oriented Programming
A functional error handling pattern that uses the analogy of railway tracks for success/failure paths.

```
   Success path ─────────────────────────▶
                ↘        ↘
   Failure path ────▶────────▶───────────▶
```

---

## S

### Sealed Class
A class that prohibits inheritance. Value objects are recommended to be declared as sealed.

### Short-Circuit
The behavior in a Bind chain where subsequent operations are skipped when a failure occurs.

### SimpleValueObject<T>
The base value object class that wraps a single value.

### SmartEnum
An enumeration with behavior and properties. Based on the `Ardalis.SmartEnum` library.

### Success-Driven Development
A methodology that develops around the success path using explicit result types instead of exceptions.

---

## T

### TypedValidation<TValueObject, T>
The return type of `ValidationRules<T>`, a readonly struct that carries value object type information during validation chaining. Can be implicitly converted to `Validation<Error, T>`.

---

## U

### Unit
A type that represents no return value. Used instead of `void` to enable functional composition.

```csharp
Fin<Unit> SaveData(data) => unit;
```

---

## V

### Validation<Error, T>
A type that supports parallel validation and error collection. Used with the Apply pattern.

### ValidationRules<T>
A static class that starts a validation chain by specifying the type parameter once. Provides start methods such as `NotNull`, `NotEmpty`, `MinLength`, `MaxLength` and chaining methods such as `ThenNotEmpty`, `ThenMaxLength`. The value object type name is automatically included in error codes.

```csharp
ValidationRules<Email>.NotNull(value).ThenNotEmpty().ThenMaxLength(255)
```

### Value Equality
Determining equality by value rather than by reference. Requires `Equals` and `GetHashCode` implementation.

### Value Object
An immutable object defined solely by its values without an identifier. A core building block of DDD.

**Characteristics:**
- Immutability
- Value Equality
- Self-Validation
- Side-Effect Free

---

## Next Steps

Check the references.

→ [D. References](D-references.md)
