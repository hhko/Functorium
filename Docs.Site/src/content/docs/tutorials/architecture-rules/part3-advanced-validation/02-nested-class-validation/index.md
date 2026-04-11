---
title: "Nested Class Validation"
---

## Overview

What if a Command is missing its `Request`? What if a Query has no `Response`? You only discover "I forgot the nested class" after the Mediator pipeline fails at runtime. The compiler does not catch such structural omissions.

In this chapter, you will learn how to use `RequireNestedClass()` and `RequireNestedClassIfExists()` to automatically verify the existence and structure of nested classes **at test time rather than compile time**.

> **"Structural rules are too easily missed in code reviews. When a test tells you 'this Command has no Request', the omission is discovered before commit."**

## Learning Objectives

### Core Learning Goals

1. **Verifying required nested classes with `RequireNestedClass()`**
   - Rule that a nested class with a specified name must exist
   - Chain additional rules on the nested class via the second parameter

2. **Verifying optional nested classes with `RequireNestedClassIfExists()`**
   - Pattern that verifies only when the nested class exists, and passes when absent
   - Suitable verification strategy for optional elements like Validators

3. **The role of the `.AreNotNested()` filter**
   - Targets only top-level classes to prevent nested classes themselves from becoming verification targets

### What You Will Verify Through Practice
- **CreateOrder**: Command pattern -- includes sealed `Request` and `Response` nested classes
- **GetOrderById**: Query pattern -- same nested class structure
- **Optional Validator**: Pattern that verifies sealed status only when present

## Domain Code

### CreateOrder - Command Pattern

```csharp
public sealed class CreateOrder
{
    public sealed class Request
    {
        public string CustomerName { get; }
        public string ProductName { get; }

        private Request(string customerName, string productName)
        {
            CustomerName = customerName;
            ProductName = productName;
        }

        public static Request Create(string customerName, string productName)
            => new(customerName, productName);
    }

    public sealed class Response
    {
        public string OrderId { get; }
        public bool Success { get; }

        private Response(string orderId, bool success)
        {
            OrderId = orderId;
            Success = success;
        }

        public static Response Create(string orderId, bool success)
            => new(orderId, success);
    }
}
```

### GetOrderById - Query Pattern

```csharp
public sealed class GetOrderById
{
    public sealed class Request
    {
        public string OrderId { get; }
        private Request(string orderId) => OrderId = orderId;
        public static Request Create(string orderId) => new(orderId);
    }

    public sealed class Response
    {
        public string OrderId { get; }
        public string CustomerName { get; }

        private Response(string orderId, string customerName)
        {
            OrderId = orderId;
            CustomerName = customerName;
        }

        public static Response Create(string orderId, string customerName)
            => new(orderId, customerName);
    }
}
```

Both classes contain `Request` and `Response` nested classes, each following the immutable pattern.

## Test Code

### Required Nested Class Verification

`RequireNestedClass()` requires that a nested class with the specified name must exist, and reports a violation if it does not.
Additional verification on the nested class can be performed through the second parameter.

```csharp
[Fact]
public void CommandClasses_ShouldHave_SealedRequestAndResponse()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ApplicationNamespace)
        .And()
        .AreNotNested()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNestedClass("Request", nested => nested
                .RequireSealed())
            .RequireNestedClass("Response", nested => nested
                .RequireSealed()),
            verbose: true)
        .ThrowIfAnyFailures("Command Nested Class Rule");
}
```

`.AreNotNested()` is used to target only top-level classes.
This prevents nested classes themselves from becoming verification targets.

### Nested Class Immutability Verification

```csharp
[Fact]
public void CommandClasses_ShouldHave_ImmutableNestedClasses()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ApplicationNamespace)
        .And()
        .AreNotNested()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNestedClass("Request", nested => nested
                .RequireSealed()
                .RequireImmutable())
            .RequireNestedClass("Response", nested => nested
                .RequireSealed()
                .RequireImmutable()),
            verbose: true)
        .ThrowIfAnyFailures("Command Nested Immutability Rule");
}
```

`RequireImmutable()` can be chained inside the nested class verification callback.

### Optional Nested Class Verification

`RequireNestedClassIfExists()` verifies only when the nested class exists, and passes when absent.

```csharp
[Fact]
public void CommandClasses_ShouldOptionallyHave_Validator()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ApplicationNamespace)
        .And()
        .AreNotNested()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNestedClassIfExists("Validator", nested => nested
                .RequireSealed()),
            verbose: true)
        .ThrowIfAnyFailures("Optional Validator Nested Class Rule");
}
```

## Summary at a Glance

The following table compares the behavior differences of nested class verification methods.

### Nested Class Verification Method Comparison

| Method | When Nested Class Is Absent | When Nested Class Exists | Use Scenario |
|--------|---------------------------|-------------------------|--------------|
| **`RequireNestedClass(name, validation)`** | Reports violation | Verifies callback rules | Required elements like Request, Response |
| **`RequireNestedClassIfExists(name, validation)`** | Passes (ignored) | Verifies callback rules | Optional elements like Validator |

The following table organizes the key filters and verification rules used in this chapter.

### Filter and Verification Rule Summary

| Aspect | Role |
|--------|------|
| `.AreNotNested()` | Filters to top-level classes only (excludes nested classes) |
| `RequireSealed()` | Verifies that nested class is sealed |
| `RequireImmutable()` | Verifies nested class immutability |
| Callback chaining | Sequentially applies multiple rules to nested classes |

## FAQ

### Q1: What message is output when a nested class is missing in `RequireNestedClass()`?
**A**: A specific violation message is reported as a `RuleViolation`, such as "Class 'CreateOrder' must have nested class 'Request'". You can immediately see which class is missing which nested class.

### Q2: What happens if `.AreNotNested()` is removed?
**A**: Nested classes like `Request` and `Response` themselves also become verification targets. Then it tries to find another `Request` nested class inside `Request`, resulting in unintended violations being reported.

### Q3: Can another `RequireNestedClass()` be called within a nested class callback?
**A**: Yes, nested verification can be used recursively. For example, multi-level nested structures like `RequireNestedClass("Request", nested => nested.RequireNestedClass("Metadata", ...))` can also be verified.

### Q4: When should `RequireNestedClassIfExists()` be used?
**A**: It is suitable for optional nested classes like Validator, Mapper, or Profile that are not mandatory for all Commands but must follow specific rules when present. It provides flexible verification with "follow the rules if present, it's fine if absent".

---

By automatically verifying the existence and structure of nested classes, structural omissions can be caught early through test failures instead of runtime failures. The next chapter examines how to verify interface naming rules and method signatures.

-> [Ch 3: Interface Verification](../03-Interface-Validation/)
