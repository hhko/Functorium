---
title: "ClassValidator Basics"
---
## Overview

A newly joined team member declared a domain Value Object as `internal` and committed it without `sealed`. Compilation passes without issues, but this class cannot be accessed from other layers and is exposed to unintended inheritance. Should you leave the same review comment every time?

> **A class's visibility and modifiers are expressions of design intent. Let tests -- not humans -- protect that intent.**

In this part, you will learn the **core verification methods of ClassValidator**. You will step by step learn how to enforce class visibility, modifiers, naming, and inheritance/interface implementation through architecture tests.

## Learning Objectives

### Core Learning Goals
1. **Architecture test environment setup**
   - Load assemblies to automatically collect types for verification
   - Understand the roles of `ArchRuleDefinition` and `ValidateAllClasses`
2. **Enforcing visibility and modifier rules**
   - Protect design intent with `RequirePublic`, `RequireSealed`, `RequireAbstract`, etc.
   - Verify C#-specific modifiers with `RequireRecord`, `RequireStatic`, etc.
3. **Automating naming rules**
   - Verify name rules based on suffixes, prefixes, and regular expressions through tests
4. **Verifying inheritance hierarchies and interface implementations**
   - Enforce type relationships with `RequireInherits`, `RequireImplements`

### What You Will Verify Through Practice
- Automatically verify that domain classes are `public sealed`
- Enforce `Spec` suffix naming rules
- Check whether classes inherit from a specific abstract class or implement a specific interface

## Chapter Structure

| Ch | Title | Key Content |
|----|-------|-------------|
| [Ch 1](01-First-Architecture-Test/) | First Architecture Test | ArchLoader, ValidateAllClasses, RequirePublic, RequireSealed |
| [Ch 2](02-Visibility-And-Modifiers/) | Visibility and Modifiers | RequireInternal, RequireAbstract, RequireStatic, RequireRecord |
| [Ch 3](03-Naming-Rules/) | Naming Rules | RequireNameEndsWith, RequireNameStartsWith, RequireNameMatching |
| [Ch 4](04-Inheritance-And-Interface/) | Inheritance and Interfaces | RequireInherits, RequireImplements, RequireImplementsGenericInterface |

---

In the first chapter, you will load an assembly with `ArchLoader` and write your first architecture test using `RequirePublic` and `RequireSealed`.

-> [Ch 1: First Architecture Test](01-First-Architecture-Test/)
