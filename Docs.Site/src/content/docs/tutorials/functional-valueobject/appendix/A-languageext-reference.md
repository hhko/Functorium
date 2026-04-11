---
title: "LanguageExt Key Type Reference"
---
## Overview

A quick reference guide for the core types of the LanguageExt library.

---

## Fin<T> - Result Type

### Basic Usage

```csharp
// Create success
Fin<int> success = 42;
Fin<int> success2 = FinSucc(42);

// Create failure
Fin<int> failure = Error.New("An error occurred");
Fin<int> failure2 = FinFail<int>(Error.New("Error"));

// Check result
if (result.IsSucc) { /* success */ }
if (result.IsFail) { /* failure */ }
```

### Match - Pattern Matching

```csharp
var message = result.Match(
    Succ: value => $"Success: {value}",
    Fail: error => $"Failure: {error.Message}"
);
```

### Map - Value Transformation

```csharp
Fin<int> number = 10;
Fin<string> text = number.Map(n => n.ToString());
// Result: "10"
```

### Bind - Chaining

```csharp
Fin<int> Parse(string s) =>
    int.TryParse(s, out var n) ? n : Error.New("Parse failed");

Fin<int> result = Fin<string>.Succ("42")
    .Bind(Parse)
    .Map(n => n * 2);
// Result: 84
```

### IfFail - Default Value

```csharp
int value = result.IfFail(0);
int value2 = result.IfFail(error => -1);
```

---

## Validation<Error, T> - Validation Type

### Basic Usage

```csharp
// Create success
Validation<Error, string> valid = Success<Error, string>("value");

// Create failure
Validation<Error, string> invalid = Fail<Error, string>(Error.New("Error"));
```

### Apply - Parallel Validation

```csharp
var result = (
    ValidateName(name),
    ValidateAge(age),
    ValidateEmail(email)
).Apply((n, a, e) => new User(n, a, e));

// All validation errors are collected
```

### Single Field Validation

```csharp
Validation<Error, string> ValidateName(string name) =>
    string.IsNullOrEmpty(name)
        ? Fail<Error, string>(Error.New("Name is required"))
        : Success<Error, string>(name);
```

### Error Collection

```csharp
result.Match(
    Succ: value => Console.WriteLine($"Success: {value}"),
    Fail: errors =>
    {
        foreach (var error in errors)
            Console.WriteLine($"Error: {error.Message}");
    }
);
```

---

## Option<T> - Optional Value

### Basic Usage

```csharp
// When a value exists
Option<int> some = Some(42);
Option<int> some2 = 42; // Implicit conversion

// When no value exists
Option<int> none = None;
```

### Match

```csharp
string message = option.Match(
    Some: value => $"Value: {value}",
    None: () => "No value"
);
```

### Map and Bind

```csharp
Option<int> result = Some(10)
    .Map(n => n * 2)
    .Bind(n => n > 0 ? Some(n) : None);
```

### Default Value

```csharp
int value = option.IfNone(0);
int value2 = option.IfNone(() => GetDefaultValue());
```

---

## Either<L, R> - Binary Choice

### Basic Usage

```csharp
// Right (success value)
Either<string, int> right = Right<string, int>(42);

// Left (error value)
Either<string, int> left = Left<string, int>("Error");
```

### Match

```csharp
string result = either.Match(
    Right: value => $"Success: {value}",
    Left: error => $"Failure: {error}"
);
```

---

## Error Type

### Creation

```csharp
// Basic error
var error = Error.New("Error message");

// Code and message
var error2 = Error.New("ERR001", "Error message");

// From exception
var error3 = Error.New(exception);

// With inner error
var error4 = Error.New("Outer error", Error.New("Inner error"));
```

### Properties

```csharp
string code = error.Code;
string message = error.Message;
Option<Error> inner = error.Inner;
Option<Exception> exception = error.Exception;
```

---

## Unit Type

### Purpose

```csharp
// Result type for functions with no return value
Fin<Unit> SaveToDatabase(Data data)
{
    // Save logic
    return unit; // Success
}

// Validation function
Fin<Unit> ValidateNotEmpty(string value) =>
    string.IsNullOrEmpty(value)
        ? Error.New("Value is empty")
        : unit;
```

---

## Prelude Static Methods

### Required Import

```csharp
using static LanguageExt.Prelude;
```

### Frequently Used Methods

```csharp
// Option creation
Some(42)
None

// Either creation
Right<L, R>(value)
Left<L, R>(value)

// Validation creation
Success<Error, T>(value)
Fail<Error, T>(error)

// Fin creation
FinSucc<T>(value)
FinFail<T>(error)

// Unit value
unit
```

---

## LINQ Extensions

### SelectMany (Bind)

```csharp
var result =
    from x in Fin<int>.Succ(10)
    from y in Fin<int>.Succ(20)
    select x + y;
// Result: 30
```

### Select (Map)

```csharp
var result =
    from x in Some(10)
    select x * 2;
// Result: Some(20)
```

### Where (Filter)

```csharp
var result =
    from x in Some(10)
    where x > 5
    select x;
// Result: Some(10)
```

---

## Collection Extensions

### Seq<T>

```csharp
// Immutable sequence
var seq = Seq(1, 2, 3, 4, 5);
var head = seq.Head; // Some(1)
var tail = seq.Tail; // Seq(2, 3, 4, 5)
```

### Arr<T>

```csharp
// Immutable array
var arr = Array(1, 2, 3);
var added = arr.Add(4); // Returns a new array
```

### Map<K, V>

```csharp
// Immutable dictionary
var map = Map(("a", 1), ("b", 2));
var value = map.Find("a"); // Some(1)
var updated = map.Add("c", 3);
```

---

## Commonly Used Patterns

### Chaining Pattern

```csharp
var result = GetUser(id)
    .Bind(ValidateUser)
    .Bind(UpdateUser)
    .Map(ToResponse);
```

### Parallel Validation Pattern

**Method 1: Tuple-based Apply (Recommended)**

```csharp
var result = (
    ValidateField1(input.Field1),
    ValidateField2(input.Field2),
    ValidateField3(input.Field3)
).Apply((f1, f2, f3) => new Output(f1, f2, f3));
```

**Method 2: fun-based Individual Apply**

The `fun` function is a helper for lambda type inference that applies Apply step by step through currying.

```csharp
// Wrap the constructor/factory with fun and call Apply individually
var result = fun((string f1, string f2, string f3) => new Output(f1, f2, f3))
    .Map(f => Success<Error, Func<string, string, string, Output>>(f))
    .Apply(ValidateField1(input.Field1))
    .Apply(ValidateField2(input.Field2))
    .Apply(ValidateField3(input.Field3));
```

Or more concisely using Pure:

```csharp
var result = Pure<Validation<Error>, Output>(
    fun((string f1, string f2, string f3) => new Output(f1, f2, f3)))
    .Apply(ValidateField1(input.Field1))
    .Apply(ValidateField2(input.Field2))
    .Apply(ValidateField3(input.Field3));
```

A comparison of the two Apply approaches:

| Method | Characteristics | When to Use |
|--------|----------------|-------------|
| Tuple Apply | Concise and intuitive | Recommended for most cases |
| fun Individual Apply | Currying-based, step-by-step application | Dynamic parameter count, advanced composition |

### Option Chaining

```csharp
var result = user
    .Map(u => u.Address)
    .Bind(a => a.City)
    .Map(c => c.Name)
    .IfNone("Unknown");
```

---

## Functorium Validation Helpers

### ValidationRules<T> - Type-Safe Validation Entry Point

`ValidationRules<T>` is a static class that starts a validation chain by specifying the value object type parameter once. The value object type name is automatically included in error codes.

```csharp
using Functorium.Domains.ValueObjects.Validations.Typed;

// Chaining validation: NotNull → ThenNotEmpty → ThenMaxLength
ValidationRules<Email>.NotNull(value)
    .ThenNotEmpty()
    .ThenMaxLength(255);

// Start methods: NotNull, NotEmpty, MinLength, MaxLength, ExactLength, etc.
// Chaining methods: ThenNotEmpty, ThenMinLength, ThenMaxLength, ThenExactLength, ThenNormalize, etc.
```

### TypedValidation<TValueObject, T> - Type Information Wrapper

The return type of `ValidationRules<T>`, which carries value object type information throughout the chain. It implicitly converts to `Validation<Error, T>`.

```csharp
// TypedValidation implicitly converts to Validation<Error, T>
TypedValidation<Email, string> typed = ValidationRules<Email>.NotNull(value);
Validation<Error, string> validation = typed; // Implicit conversion

// Can be passed directly to CreateFromValidation
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(
        ValidationRules<Email>.NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(255),
        v => new Email(v));
```

### ValidationApplyExtensions - Tuple Apply Extensions

`ValidationApplyExtensions` provides Apply overloads for `Validation<Error, T>` tuples, internally converting the `K<Validation<Error>, T>` returned by LanguageExt's generic Apply with `.As()`. The caller does not need to call `.As()` directly.

```csharp
using Functorium.Domains.ValueObjects.Validations;

// Returns concrete Validation<Error, R> without .As()
var result = (
    ValidateAmount(amount),
    ValidateCurrency(currency)
).Apply((a, c) => new Money(a, c));
// result type: Validation<Error, Money> (not K<>)

// Supports 2 to 5 tuples
```

---

## Next Steps

Check the framework type selection guide.

→ [B. Framework Type Selection Guide](B-type-selection-guide.md)
