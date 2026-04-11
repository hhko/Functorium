---
title: "Method and Property Validation"
---

## Overview

In Part 1, class visibility, modifiers, and naming rules all passed. But what if a `public` method is returning `Task` where it should return `void`, and a property that should be immutable has a `public set` exposed? Class-level verification alone cannot catch these internal design violations.

> **Just because the class shell is correct does not mean the internals are correct. Design intent is fully protected only when verification extends to the member level.**

In this part, you will learn the **method verification and property/field verification capabilities of ClassValidator**. This covers enforcing method visibility, static status, return types, and parameters, as well as property existence and immutability through architecture tests.

## Learning Objectives

### Core Learning Goals
1. **Method signature verification**
   - Verify method existence and visibility with `RequireMethod`, `RequireAllMethods`
   - Enforce extension method patterns with `RequireExtensionMethod`
2. **Return type rule enforcement**
   - Verify factory method patterns with `RequireReturnType`, `RequireReturnTypeOfDeclaringClass`
   - Flexible return type matching with `RequireReturnTypeContaining`
3. **Parameter rule verification**
   - Control method signatures with `RequireParameterCount`, `RequireFirstParameterTypeContaining`
4. **Property and field immutability protection**
   - Enforce immutable design with `RequireNoPublicSetters`
   - Verify field access rules with `RequireNoInstanceFields`

### What You Will Verify Through Practice
- Verify that a Usecase class's `Execute` method has the correct signature
- Confirm that a factory method returns the declaring class type
- Automatically verify that Value Objects have no `public set` properties

## Chapter Structure

| Chapter | Topic | Key API |
|---------|-------|---------|
| [Chapter 1](01-Method-Validation/) | Method Verification | `RequireMethod`, `RequireAllMethods`, `RequireVisibility`, `RequireExtensionMethod` |
| [Chapter 2](02-Return-Type-Validation/) | Return Type Verification | `RequireReturnType`, `RequireReturnTypeOfDeclaringClass`, `RequireReturnTypeContaining` |
| [Chapter 3](03-Parameter-Validation/) | Parameter Verification | `RequireParameterCount`, `RequireFirstParameterTypeContaining` |
| [Chapter 4](04-Property-And-Field-Validation/) | Property and Field Verification | `RequireProperty`, `RequireNoPublicSetters`, `RequireNoInstanceFields` |

## Learning Flow

```
Method Verification -> Return Type Verification -> Parameter Verification -> Property/Field Verification
```

---

In the first chapter, you will verify method existence, visibility, and static status using `RequireMethod` and `RequireAllMethods`.

-> [Ch 1: Method Verification](01-Method-Validation/)
