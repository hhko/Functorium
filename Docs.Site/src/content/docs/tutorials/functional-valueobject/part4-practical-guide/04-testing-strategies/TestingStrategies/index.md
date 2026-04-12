---
title: "Testing Strategies"
---
## Overview

Learn testing strategies for value objects. Covers unit test patterns, test helpers, and architecture tests.

---

## Learning Objectives

- Value object creation test patterns
- Equality test patterns
- Comparability test patterns
- Using `Fin<T>` test helpers

---

## How to Run

```bash
cd Docs/tutorials/Functional-ValueObject/04-practical-guide/04-Testing-Strategies/TestingStrategies
dotnet run
```

---

## Expected Output

```
=== Value Object Testing Strategies ===

1. Creation Test Patterns
────────────────────────────────────────
   [Valid input test] user@example.com -> PASS
   [Invalid input test] invalid-email -> PASS
   [Error message verification] Contains '@' -> PASS
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
   [Sorting test] Order after sorting -> PASS

4. Test Helper Usage
────────────────────────────────────────
   [ShouldBeSuccess helper] -> PASS
   [ShouldBeFail helper] -> PASS
   [GetSuccessValue helper] -> PASS
   [GetFailError helper] -> PASS
```

---

## Core Code Explanation

### 1. Fin<T> Test Helpers

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

    public static T GetSuccessValue<T>(this Fin<T> fin) => ...;
    public static Error GetFailError<T>(this Fin<T> fin) => ...;
}
```

### 2. Test Pattern Examples

```csharp
// Creation test
[Fact]
public void Create_WithValidEmail_ShouldSucceed()
{
    var result = Email.Create("user@example.com");

    result.ShouldBeSuccess();
    var email = result.GetSuccessValue();
    ((string)email).ShouldBe("user@example.com");
}

// Equality test
[Fact]
public void Equals_WithSameValue_ShouldBeTrue()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");

    email1.ShouldBe(email2);
    email1.GetHashCode().ShouldBe(email2.GetHashCode());
}

// Comparison test
[Fact]
public void Sort_ShouldOrderByValue()
{
    var ages = new[] { Age.CreateFromValidated(30), Age.CreateFromValidated(10) };

    Array.Sort(ages);

    ages[0].Value.ShouldBe(10);
    ages[1].Value.ShouldBe(30);
}
```

---

## Test Checklist

### Creation Tests
- [ ] Valid input -> success
- [ ] Invalid input -> failure
- [ ] Boundary value tests (empty string, null, min/max values)
- [ ] Error message verification

### Equality Tests
- [ ] Same values -> equal
- [ ] Different values -> not equal
- [ ] Hash code consistency
- [ ] == / != operators

### Comparison Tests (when applicable)
- [ ] CompareTo accuracy
- [ ] <, >, <=, >= operators
- [ ] Sorting behavior

### Immutability Tests
- [ ] Original unchanged after operations

## FAQ

### Q1: Why are `Fin<T>` test helpers needed?
**A**: Directly verifying the success/failure of `Fin<T>` requires calling `Match` and checking conditions repeatedly. Using helpers like `ShouldBeSuccess()` and `ShouldBeFail()` makes test code concise, and error messages are clearly output on failure.

### Q2: Why are boundary value tests important for value objects?
**A**: The `Create` method of a value object is the last line of defense for validation. If validation is not confirmed to work correctly for boundary conditions like empty strings, `null`, min/max values, invalid values may enter the system.

### Q3: Must hash code consistency also be verified in equality tests?
**A**: Yes. C#'s `Dictionary` and `HashSet` use `GetHashCode()` to manage keys. If two equal objects have different hash codes, the collection will not work correctly, so when `Equals` is `true`, hash codes must also be equal.

---

## Next Steps

Learn practical domain examples in Part 5.

-> [5.1 E-commerce Domain](../../../05-domain-examples/01-Ecommerce-Domain/EcommerceDomain/)
