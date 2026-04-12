---
title: "value object Testing Strategies"
---
## Overview

How can you be sure that a value object's `Create()` method correctly rejects all invalid inputs? Could there be subtle hash code bugs hiding in equality comparisons?

Since value objects are the foundation of the domain model, thorough testing is essential. In this chapter, we cover strategies for implementing and utilizing helper methods to test creation validation, equality comparison, comparability, and `Fin<T>` results.

## Learning Objectives

- Write value object creation tests for both valid and invalid inputs.
- Systematically verify value-based equality (`Equals`, `GetHashCode`, `==`).
- Test `IComparable<T>` implementations and sorting behavior.
- Implement and utilize `Fin<T>` test helpers such as `ShouldBeSuccess()` and `ShouldBeFail()`.

## Why Is This Needed?

Value objects encapsulate domain invariants. Business rules like "email must contain the @ symbol" and "age must be between 0 and 150" must be guaranteed through tests.

Tests also serve as a safety net for refactoring. Even if the value object implementation changes, passing tests provide confidence that existing behavior is preserved -- especially since equality and hash codes are areas prone to subtle bugs. Additionally, test code serves as living documentation showing the usage and constraints of value objects. A new team member can understand what inputs `Email.Create()` accepts just by looking at the tests.

## Core Concepts

### Creation Test Patterns

Verifies success and failure of value object creation. Uses the `IsSucc` and `IsFail` properties of `Fin<T>`.

```csharp
// Valid input test
[Fact]
public void Create_WithValidEmail_ReturnsSuccess()
{
    var result = Email.Create("user@example.com");

    result.IsSucc.Should().BeTrue();
    result.GetSuccessValue().Value.Should().Be("user@example.com");
}

// Invalid input test
[Fact]
public void Create_WithInvalidEmail_ReturnsFailure()
{
    var result = Email.Create("invalid-email");

    result.IsFail.Should().BeTrue();
    result.GetFailError().Message.Should().Contain("Email.InvalidFormat");
}

// Boundary value test
[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("   ")]
public void Create_WithEmptyOrNull_ReturnsFailure(string? input)
{
    var result = Email.Create(input!);

    result.IsFail.Should().BeTrue();
}
```

Write success and failure cases for every validation path in the value object's `Create()` method.

### Equality Test Patterns

Thoroughly verifies the value object's equality implementation. `Equals()`, `GetHashCode()`, `==`, and `!=` must all be tested.

```csharp
[Fact]
public void Equals_SameValue_ReturnsTrue()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");

    email1.Equals(email2).Should().BeTrue();
    (email1 == email2).Should().BeTrue();
    (email1 != email2).Should().BeFalse();
}

[Fact]
public void Equals_DifferentValue_ReturnsFalse()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("other@example.com");

    email1.Equals(email2).Should().BeFalse();
    (email1 == email2).Should().BeFalse();
    (email1 != email2).Should().BeTrue();
}

[Fact]
public void GetHashCode_SameValue_ReturnsSameHash()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");

    email1.GetHashCode().Should().Be(email2.GetHashCode());
}
```

Equal objects must have the same hash code. If this rule is broken, unexpected behavior occurs in `Dictionary` and `HashSet`.

### Comparability Test Patterns

Tests the sorting behavior of value objects that implement `IComparable<T>`.

```csharp
[Fact]
public void CompareTo_ReturnsCorrectOrder()
{
    var age20 = Age.CreateFromValidated(20);
    var age25 = Age.CreateFromValidated(25);
    var age30 = Age.CreateFromValidated(30);

    age20.CompareTo(age25).Should().BeNegative();
    age30.CompareTo(age25).Should().BePositive();
    age25.CompareTo(age25).Should().Be(0);
}

[Fact]
public void ComparisonOperators_WorkCorrectly()
{
    var age20 = Age.CreateFromValidated(20);
    var age25 = Age.CreateFromValidated(25);

    (age20 < age25).Should().BeTrue();
    (age25 > age20).Should().BeTrue();
    (age20 <= age20).Should().BeTrue();
    (age25 >= age25).Should().BeTrue();
}

[Fact]
public void Sort_OrdersCorrectly()
{
    var ages = new[] {
        Age.CreateFromValidated(30),
        Age.CreateFromValidated(20),
        Age.CreateFromValidated(25)
    };

    Array.Sort(ages);

    ages[0].Value.Should().Be(20);
    ages[1].Value.Should().Be(25);
    ages[2].Value.Should().Be(30);
}
```

Verifies that `CompareTo()` results and comparison operators behave consistently.

### Fin\<T\> Test Helpers

Extension methods for testing `Fin<T>` results. `result.ShouldBeSuccess()` expresses intent more clearly than `result.IsSucc.Should().BeTrue()`.

```csharp
public static class FinTestExtensions
{
    public static void ShouldBeSuccess<T>(this Fin<T> fin)
    {
        if (fin.IsFail)
        {
            var message = fin.Match(_ => "", e => e.Message);
            throw new Exception($"Expected Succ but got Fail: {message}");
        }
    }

    public static void ShouldBeFail<T>(this Fin<T> fin)
    {
        if (fin.IsSucc)
        {
            throw new Exception("Expected Fail but got Succ");
        }
    }

    public static T GetSuccessValue<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: value => value,
            Fail: error => throw new Exception($"Expected Succ but got Fail: {error.Message}")
        );
    }

    public static Error GetFailError<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: _ => throw new Exception("Expected Fail but got Succ"),
            Fail: error => error
        );
    }
}
```

## Practical Guidelines

### Expected Output
```
=== value object Testing Strategies ===

1. Creation Test Patterns
────────────────────────────────────────
   [Valid input test] user@example.com -> PASS
   [Invalid input test] invalid-email -> PASS
   [Error code verification] Contains 'Email.InvalidFormat' -> PASS
   [Boundary value test] Empty string/null -> PASS

2. Equality Test Patterns
────────────────────────────────────────
   [Same value equality] email1 == email2 -> PASS
   [Different value inequality] email1 != email3 -> PASS
   [Hash code consistency] hash(email1) == hash(email2) -> PASS
   [Operator test] == and != -> PASS

3. Comparability Test Patterns
────────────────────────────────────────
   [CompareTo test] 20 < 25 < 30 -> PASS
   [Comparison operator test] < operator -> PASS
   [Sort test] Order after sorting -> PASS

4. Test Helper Usage
────────────────────────────────────────
   [ShouldBeSuccess helper] -> PASS
   [ShouldBeFail helper] -> PASS
   [GetSuccessValue helper] -> PASS
   [GetFailError helper] -> PASS
```

### Test Class Structure Example

Grouping related tests with nested classes improves readability.

```csharp
public class EmailTests
{
    public class CreateMethod
    {
        [Fact]
        public void WithValidEmail_ReturnsSuccess() { ... }

        [Theory]
        [InlineData("invalid")]
        [InlineData("no-at-sign")]
        public void WithInvalidFormat_ReturnsFailure(string input) { ... }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void WithEmptyOrNull_ReturnsEmptyError(string? input) { ... }
    }

    public class Equality
    {
        [Fact]
        public void SameValue_AreEqual() { ... }

        [Fact]
        public void DifferentValue_AreNotEqual() { ... }

        [Fact]
        public void HashCode_ConsistentWithEquals() { ... }
    }
}
```

## Project Description

### Project Structure
```
04-Testing-Strategies/
├── TestingStrategies/
│   ├── Program.cs                    # Main executable (test demo)
│   └── TestingStrategies.csproj      # Project file
└── README.md                         # Project documentation
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### Core Code

**value objects Under Test**
```csharp
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Fin<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");
        if (!value.Contains('@'))
            return DomainErrors.InvalidFormat(value);
        return new Email(value.ToLowerInvariant());
    }

    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Email? left, Email? right) { ... }
    public static bool operator !=(Email? left, Email? right) => !(left == right);
}
```

**Test Helper Extension Methods**
```csharp
public static class FinTestExtensions
{
    public static void ShouldBeSuccess<T>(this Fin<T> fin)
    {
        if (fin.IsFail)
        {
            var message = fin.Match(_ => "", e => e.Message);
            throw new Exception($"Expected Succ but got Fail: {message}");
        }
    }

    public static void ShouldBeFail<T>(this Fin<T> fin)
    {
        if (fin.IsSucc)
            throw new Exception("Expected Fail but got Succ");
    }

    public static T GetSuccessValue<T>(this Fin<T> fin) { ... }
    public static Error GetFailError<T>(this Fin<T> fin) { ... }
}
```

## Summary at a Glance

### Test Type Checklist

Items to verify for each test type when testing value objects.

| Test Type | Verification Items |
|-----------|-------------------|
| **Creation tests** | Valid input -> success, invalid input -> failure |
| **Boundary value tests** | null, empty string, maximum/minimum values |
| **Error verification** | error code, error message content |
| **Equality tests** | `Equals()`, `==`, `!=`, `GetHashCode()` |
| **Comparison tests** | `CompareTo()`, `<`, `>`, `<=`, `>=`, sorting |

### Fin\<T\> Test Helper Summary

Summarizes the purpose of each helper method.

| Helper Method | Purpose |
|--------------|---------|
| `ShouldBeSuccess()` | Verify success state (throws on failure) |
| `ShouldBeFail()` | Verify failure state (throws on success) |
| `GetSuccessValue()` | Extract success value (throws on failure) |
| `GetFailError()` | Extract error information (throws on success) |

### Equality Contract Rules

Mathematical rules that value object equality implementations must follow.

| Rule | Description |
|------|-------------|
| Reflexivity | `x.Equals(x)` -> true |
| Symmetry | `x.Equals(y)` <-> `y.Equals(x)` |
| Transitivity | `x.Equals(y)` && `y.Equals(z)` -> `x.Equals(z)` |
| Consistency | Same input always yields the same result |
| Hash code | `x.Equals(y)` -> `x.GetHashCode() == y.GetHashCode()` |

## FAQ

### Q1: What tests should be written for every value object?
**A**: At minimum, write creation tests (valid/invalid input), boundary value tests (null, empty values, max/min), equality tests (same value, different value, null), and hash code consistency tests. For comparable value objects, add `CompareTo()`, comparison operator, and sorting tests.

### Q2: When should I use Theory vs Fact?
**A**: Use `[Fact]` for single scenarios, and `[Theory]` with `[InlineData]` to reduce code duplication when applying various inputs to the same validation logic.

### Q3: Why are hash code tests important?
**A**: In hash-based collections such as `Dictionary` and `HashSet`, if `Equals()` returns true but hash codes differ, key lookups can fail. If `x.Equals(y)` is true, then `x.GetHashCode() == y.GetHashCode()` must also be true.

---

## Tests

This project includes unit tests.

### Running Tests
```bash
cd TestingStrategies.Tests.Unit
dotnet test
```

### Test Structure
```
TestingStrategies.Tests.Unit/
├── CreationPatternTests.cs       # Creation pattern tests
├── EqualityPatternTests.cs       # Equality pattern tests
├── ComparabilityPatternTests.cs  # Comparability pattern tests
└── FinTestExtensionsTests.cs     # Fin<T> test extension verification
```

### Key Test Cases

| Test Class | Test Content |
|------------|-------------|
| CreationPatternTests | Valid/invalid input, normalization, boundary values |
| EqualityPatternTests | Same value equality, different value inequality, hash code |
| ComparabilityPatternTests | Sorting, comparison operators |
| FinTestExtensionsTests | ShouldBeSuccess, ShouldBeFail extensions |

Part 4 covered practical integration and testing strategies for value objects. Part 5 examines how value objects are used in specific domains such as e-commerce, finance, user management, and scheduling.

---

→ [Part 5, Chapter 1: E-commerce Domain](../../Part5-Domain-Examples/01-Ecommerce-Domain/)
