---
title: "What Is Success-Driven Development?"
---
## Overview

**Success-Driven Development** is a paradigm that designs code around the success path by using explicit result types instead of exceptions.

---

## Exception-Centric vs Success-Centric Development

### Problems with Exception-Centric Development

In traditional development, **exceptions** are used to handle errors:

```csharp
// ❌ Exception-centric development - problematic approach
public User CreateUser(string email, int age)
{
    if (string.IsNullOrEmpty(email))
        throw new ArgumentException("Email is required.");
    if (!email.Contains("@"))
        throw new ArgumentException("Email format is invalid.");
    if (age < 0 || age > 150)
        throw new ArgumentException("Age is not valid.");

    return new User(email, age);
}

// The caller "must remember" to handle exceptions
try
{
    var user = CreateUser("invalid", -5);
}
catch (ArgumentException ex)
{
    // Exception handling... but what if you forget?
}
```

**Problems:**

| Problem | Description |
|---------|-------------|
| **Easy to forget** | The compiler does not enforce exception handling even if the caller omits it |
| **Not visible in signature** | You cannot tell from the function signature which exceptions may be thrown |
| **Performance cost** | Exceptions incur high performance costs such as stack trace generation |
| **Violates pure functions** | Throwing exceptions is a side effect that violates purity |

---

### The Solution: Success-Driven Development

**Success-Driven Development** solves these problems:

```csharp
// ✅ Success-driven development - recommended approach
public Fin<User> CreateUser(string email, int age)
{
    return
        from validEmail in Email.Create(email)
        from validAge in Age.Create(age)
        select new User(validEmail, validAge);
}

// Result handling is "enforced" at the call site
var result = CreateUser("user@example.com", 25);
result.Match(
    Succ: user => Console.WriteLine($"User created: {user.Email}"),
    Fail: error => Console.WriteLine($"Failed: {error.Message}")
);
```

**Advantages:**

| Advantage | Description |
|-----------|-------------|
| **Type system enforces** | The compiler enforces result handling |
| **Failure possibility is explicit** | The function signature explicitly states that failure is possible |
| **Maintains pure functions** | Purity is maintained without exceptions |
| **Performance optimized** | No exception stack traces |

---

## The LanguageExt Library

This tutorial uses the [LanguageExt](https://github.com/louthy/language-ext) library. LanguageExt is a powerful library that enables functional programming in C#.

### Installation

```bash
dotnet add package LanguageExt.Core
```

### Basic using Statements

```csharp
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
```

---

## Core Types: Fin<T> and Validation<Error, T>

### Fin<T> - Final Result

`Fin<T>` is a type representing **Success** or **Failure**.

```csharp
// Success cases
Fin<int> success = 42;                    // Implicit conversion
Fin<int> success2 = Fin<int>.Succ(42);    // Explicit creation

// Failure cases
Fin<int> fail = Error.New("Value is not valid");
Fin<int> fail2 = Fin<int>.Fail(Error.New("Error"));

// Result handling
var result = Fin<int>.Succ(42);
var output = result.Match(
    Succ: value => $"Success: {value}",
    Fail: error => $"Failure: {error.Message}"
);
```

### Validation<Error, T> - Validation Result (Error Accumulation)

`Validation<Error, T>` is a type that can **collect all validation errors**.

```csharp
// Single validation
Validation<Error, string> ValidateEmail(string email) =>
    email.Contains("@")
        ? email
        : Error.New("Email requires @");

// Parallel validation via Apply (collects all errors)
var result = (ValidateEmail(email), ValidateAge(age), ValidateName(name))
    .Apply((e, a, n) => new User(e, a, n));
// On failure, all errors are collected as ManyErrors
```

---

## When to Use Exceptions vs Result Types

### Use Result Types (Predictable Failures)

- User input errors (invalid email, negative age, etc.)
- Business rule violations (division by zero, invalid date, etc.)
- Domain constraints (exceeding maximum, below minimum, etc.)

### Use Exceptions (Unpredictable Failures)

- System resource exhaustion (out of memory, out of disk space)
- External system errors (network connection failure, database connection failure)
- Unexpected system errors (file deletion, insufficient permissions)

## FAQ

### Q1: Does Success-Driven Development mean never using exceptions at all?
**A**: No. Exceptions are still used for **unpredictable system errors** like network connection failures or out-of-memory conditions. Success-Driven Development handles only **predictable failures** such as user input errors or business rule violations with result types.

### Q2: Does using `Fin<T>` degrade performance?
**A**: It actually performs better than exceptions. Exceptions have a high cost for generating stack traces, while `Fin<T>` is a simple value wrapper that only incurs allocation cost.

### Q3: Can it be adopted incrementally in an existing codebase?
**A**: Yes. Start by making the `Create` methods of newly written value objects return `Fin<T>`. Since it can coexist with existing exception-based code, there is no need to change everything at once.

---

## Next Steps

Now that you understand the concept of Success-Driven Development, let's prepare the practice environment. In the next chapter, we proceed with .NET SDK installation and LanguageExt package setup.

→ [0.3 Environment Setup](03-environment-setup.md)
