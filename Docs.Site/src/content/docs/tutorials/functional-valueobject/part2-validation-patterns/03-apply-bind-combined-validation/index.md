---
title: "Apply+Bind Combined Validation"
---

## Overview

Suppose you are validating order information. Customer name and email are independent and can be validated in parallel, but the discount amount cannot exceed the order amount, so there is a dependency between the two values. Neither Apply alone nor Bind alone can handle this situation cleanly. A mixed pattern that applies Apply for independent validations and Bind for dependent validations is needed.

## Learning Objectives

- Implement the **combined validation pattern** that efficiently validates complex business logic by appropriately combining Apply and Bind.
- Understand the **step-by-step validation strategy** that distinguishes independent and dependent information and applies the appropriate validation approach to each.
- **Design and solve** complex validation requirements commonly encountered in real domains in a **practical manner**.

## Why Is This Needed?

We covered Apply and Bind separately, but in real business domains, independent information and dependent information coexist in a single object.

Order information is a representative example. Customer name and email can be validated independently, but order amount and discount amount have a dependent relationship. Independent information should be validated in parallel for performance optimization, while dependent information should be validated sequentially to ensure logical consistency. Additionally, errors from each validation stage must be properly distinguished and handled -- the Apply stage may collect multiple errors while the Bind stage may produce a single error.

**The Apply+Bind combined validation pattern** efficiently and logically handles such complex domain requirements.

## Core Concepts

### 2-Stage Validation Strategy

Combined validation executes the Apply (independent) stage and Bind (dependent) stage in order. It first validates basic independent information in parallel, and if the result is successful, it then validates dependent information sequentially.

The following code compares a single-approach treatment versus a 2-stage mixed treatment.

```csharp
// Previous approach (problematic) - treats all validations with a single approach
public static Validation<Error, OrderInfo> ValidateOld(string customerName, string customerEmail, string orderAmountInput, string discountInput)
{
    // Sequential execution of all validations is inefficient
    var nameResult = ValidateCustomerName(customerName);
    var emailResult = ValidateCustomerEmail(customerEmail);
    var amountResult = ValidateOrderAmount(orderAmountInput);
    var discountResult = ValidateDiscount(discountInput);
    // Performance degradation because even independent validations run sequentially
}

// Improved approach (current) - 2-stage validation strategy
public static Validation<Error, (string CustomerName, string CustomerEmail, decimal OrderAmount, decimal FinalAmount)> Validate(
    string customerName, string customerEmail, string orderAmountInput, string discountInput) =>
    // Stage 1: Independent validation (Apply) - validate basic information in parallel
    (ValidateCustomerName(customerName), ValidateCustomerEmail(customerEmail))
        .Apply((validName, validEmail) => (validName, validEmail))
        .As()
        // Stage 2: Dependent validation (Bind) - validate amount information sequentially
        .Bind(_ => ValidateOrderAmount(orderAmountInput))
        .Bind(_ => ValidateFinalAmount(orderAmountInput, discountInput))
        .Map(_ => (customerName: customerName,
                   customerEmail: customerEmail,
                   orderAmount: decimal.Parse(orderAmountInput),
                   finalAmount: decimal.Parse(orderAmountInput) - decimal.Parse(discountInput)));
```

In this approach, the Apply stage collects errors from customer name and email at once, while the Bind stage sequentially validates amount-related dependencies.

### Expressing Business Rule Dependencies

Checking whether the discount amount exceeds the order amount is validating the dependency between two values. Such rules are naturally expressed with Bind.

```csharp
// Dependency validation between discount amount and order amount
private static Validation<Error, decimal> ValidateFinalAmount(string orderAmountInput, string discountInput) =>
    decimal.TryParse(orderAmountInput, out var orderAmount) &&
    decimal.TryParse(discountInput, out var discount) &&
    discount >= 0 && discount <= orderAmount
        ? orderAmount - discount
        : Domain.DiscountAmountExceedsOrder(orderAmountInput, discountInput);
```

## Practical Guidelines

### Expected Output
```
=== Mixed Validation Example ===
An example demonstrating the Apply(independent) + Bind(dependent) mixed pattern.

--- Success ---
Input: 'John Doe', 'john@example.com', '100000', '10000'
Success: John Doe - $100,000 -> $90,000

--- Apply Failure - Both ---
Input: '', 'invalid', '100000', '10000'
Failure:
   -> 2 errors collected in Apply stage
     1. Domain.OrderInfo.CustomerNameTooShort: ''
     2. Domain.OrderInfo.CustomerEmailMissingAt: 'invalid'

--- Bind Failure - Discount Exceeds Order ---
Input: 'John Doe', 'john@example.com', '100000', '150000'
Failure:
   -> Single error in Bind stage: Domain.OrderInfo.DiscountAmountExceedsOrder: '100000:150000'
```

### Key Implementation Points

Three points to note during implementation. Compose the Apply (independent) stage and Bind (dependent) stage in order. The Apply stage collects errors with ManyErrors and the Bind stage short-circuits with a single Error. Compose the final result from original parameters and computed values with `.Map()`.

## Project Description

### Project Structure
```
03-Apply-Bind-Combined-Validation/
├── Program.cs              # Main entry point
├── ValueObjects/
│   └── OrderInfo.cs        # Order information value object (mixed validation pattern implementation)
├── ApplyBindCombinedValidation.csproj
└── README.md               # Main document
```

### Core Code

The OrderInfo value object validates customer information in parallel with Apply and validates amount information sequentially with Bind.

```csharp
public sealed class OrderInfo : ValueObject
{
    public string CustomerName { get; }
    public string CustomerEmail { get; }
    public decimal OrderAmount { get; }
    public decimal FinalAmount { get; }

    // Mixed validation pattern implementation (Apply + Bind)
    public static Validation<Error, (string CustomerName, string CustomerEmail, decimal OrderAmount, decimal FinalAmount)> Validate(
        string customerName, string customerEmail, string orderAmountInput, string discountInput) =>
        // Stage 1: Independent validation (Apply) - validate basic information in parallel
        (ValidateCustomerName(customerName), ValidateCustomerEmail(customerEmail))
            .Apply((validName, validEmail) => (validName, validEmail))
            .As()
            // Stage 2: Dependent validation (Bind) - validate amount information sequentially
            .Bind(_ => ValidateOrderAmount(orderAmountInput))
            .Bind(_ => ValidateFinalAmount(orderAmountInput, discountInput))
            .Map(_ => (customerName: customerName,
                       customerEmail: customerEmail,
                       orderAmount: decimal.Parse(orderAmountInput),
                       finalAmount: decimal.Parse(orderAmountInput) - decimal.Parse(discountInput)));

    // Business rule validation - discount amount cannot exceed order amount
    private static Validation<Error, decimal> ValidateFinalAmount(string orderAmountInput, string discountInput) =>
        decimal.TryParse(orderAmountInput, out var orderAmount) &&
        decimal.TryParse(discountInput, out var discount) &&
        discount >= 0 && discount <= orderAmount
            ? orderAmount - discount
            : Domain.DiscountAmountExceedsOrder(orderAmountInput, discountInput);
}
```

## Summary at a Glance

The following table compares the characteristics of the three validation patterns.

| Aspect | Apply Parallel Validation | Bind Sequential Validation | Apply+Bind Combined Validation |
|------|----------------|----------------|---------------------|
| **Target** | All validations are independent | All validations are dependent | Mixed independent + dependent |
| **Execution method** | Runs all validations in parallel | Runs all validations sequentially | Optimized execution per stage |
| **error handling** | Collects all errors with ManyErrors | Short-circuits with single Error | Errors distinguished per stage |
| **Performance** | Fast via parallel execution | Efficient via short-circuiting | Optimized at each stage |

The following table summarizes the pros and cons of the combined validation pattern.

| Pros | Cons |
|------|------|
| Suitable for complex domain requirements | Validation logic becomes complex |
| Uses appropriate strategy at each stage | Need to identify error cause per stage |
| Close to real business logic | Validation stages must be carefully designed |

## FAQ

### Q1: When should the combined validation pattern be used?
**A:** Use it when the domain object contains both independent and dependent information. A typical case is order information where customer information is independent but amount information is dependent.

### Q2: How do you determine the order of Apply and Bind?
**A:** Generally, run independent validations first and dependent validations later. Perform prerequisite validations first, then validate complex business rules based on those results to avoid unnecessary operations.

### Q3: How do you distinguish error handling?
**A:** The Apply stage collects multiple errors with the ManyErrors type, while the Bind stage short-circuits with a single Error type. It is important to clearly inform the user at which stage the failure occurred.

So far we have combined Apply and Bind in a single flow in sequence. But what if complex multi-stage validation is also needed within each field? The next chapter implements Apply with nested Bind for fine-grained per-field validation.

---

-> [Chapter 4: Apply with Internal Bind](../04-Apply-Internal-Bind-Validation/)
