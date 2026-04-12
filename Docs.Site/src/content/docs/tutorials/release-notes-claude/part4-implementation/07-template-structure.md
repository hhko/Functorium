---
title: "TEMPLATE.md Structure"
---

Starting release notes from a blank document each time means you first have to think about what sections to include. Breaking Changes are easy to forget, installation instructions are often left out, and the document structure varies depending on who writes it. TEMPLATE.md solves this problem. It is a template file that predefines the **standard format** of release notes so that documents are always written in the same structure.

This file is located at `.release-notes/TEMPLATE.md`. It defines the basic structure of release notes, marks where dynamic content goes with placeholders, and includes writing guidelines and checklists, allowing anyone to produce consistent quality documents.

## File Structure

The overall skeleton of the template is as follows.

```markdown
---
title: Functorium {VERSION} New Features
description: Learn about the new features in Functorium {VERSION}.
date: {DATE}
---

# Functorium Release {VERSION}

## Overview
## Breaking Changes
## New Features
## Bug Fixes
## API Changes
## Installation

<!-- Template guide (needs to be deleted) -->
```

It consists of a total of 6 sections from frontmatter to installation. Let's look at why each section is needed and how to write it.

## Frontmatter

The YAML frontmatter at the top of the document defines metadata.

```yaml
---
title: Functorium {VERSION} New Features
description: Learn about the new features in Functorium {VERSION}.
date: {DATE}
---
```

Here, `{VERSION}` and `{DATE}` are placeholders that must be replaced with actual values. `{VERSION}` is replaced with the release version like `v1.2.0`, and `{DATE}` with the release date like `2025-12-19`.

The replacement result looks like this.

```yaml
---
title: Functorium v1.2.0 New Features
description: Learn about the new features in Functorium v1.2.0.
date: 2025-12-19
---
```

## Overview Section

The overview is the first impression of the release. It shows at a glance what goals this version has and what the key features are.

```markdown
## Overview

{Version introduction - main goals and themes of this release}

**Key Features**:

- **{Feature 1 Category}**: {One-line description}
- **{Feature 2 Category}**: {One-line description}
- **{Feature 3 Category}**: {One-line description}
```

When actually written, it looks like this.

```markdown
## Overview

Functorium v1.0.0 is the first official release of the functional programming toolkit for .NET applications.
This release focuses on error handling, observability, and test support.

**Key Features**:

- **Functional Error Handling**: Structured error creation through ErrorCodeFactory
- **OpenTelemetry Integration**: Unified distributed tracing, metrics, and logging configuration
- **Test Fixtures**: ASP.NET Core and Quartz integration test support
```

---

## Breaking Changes Section

Breaking Changes are the most important information for users who are upgrading. Since existing code may break, even if there are none, it must be explicitly stated. When changes exist, before/after code and a migration guide must be included.

````markdown
## Breaking Changes

{When there are no Breaking Changes}
There are no Breaking Changes in this release.

{When there are Breaking Changes}
### {Changed API/Feature Name}

{Description of the change}

**Before ({previous version})**:
```csharp
{Previous code}
```

**After ({current version})**:
```csharp
{New code}
```

**Migration Guide**:
1. {Step 1}
2. {Step 2}
3. {Step 3}
````

Here is a writing example when Breaking Changes exist.

````markdown
## Breaking Changes

### IErrorHandler Interface Name Change

The error handling interface has been renamed to a more descriptive name.

**Before (v0.9.0)**:
```csharp
public class MyHandler : IErrorHandler
{
    public void Handle(Error error) { }
}
```

**After (v1.0.0)**:
```csharp
public class MyDestructurer : IErrorDestructurer
{
    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory) { }
}
```

**Migration Guide**:
1. Change all `IErrorHandler` references to `IErrorDestructurer`
2. Rename `Handle` method to `Destructure`
3. Change return type to `LogEventPropertyValue`
````

---

## New Features Section

New features are the core of the release notes. Each feature should be documented from three perspectives: "What does it do (What)", "How to use it (How)", and "Why is it important (Why)." Include code examples along with the related commit SHA as a comment for traceability.

````markdown
## New Features

### {Component Name} Library

#### 1. {Feature Name}

{Feature description - What: What does it do?}

```csharp
{Code example - How: How do you use it?}
```

**Why this matters:**
- {Problem it solves}
- {Developer productivity}
- {Code quality improvement}
- {Quantitative benefit}

<!-- Related commit: {SHA} {Commit message} -->
````

Every feature must include a feature description, code example, **Why this matters** section, and commit annotation. These four elements are needed for users to understand and immediately use the feature.

Here is an actual writing example.

````markdown
### Functorium Library

#### 1. Creating Structured Errors from Exceptions

Converts exceptions to structured error codes based on the LanguageExt Error type.

```csharp
using Functorium.Abstractions.Errors;

try
{
    await HttpClient.GetAsync(url);
}
catch (HttpRequestException ex)
{
    var error = ErrorCodeFactory.CreateFromException("HTTP_001", ex);
    Log.Error("API request failed: {@Error}", error);
    return Fin<Response>.Fail(error);
}
```

**Why this matters:**
- Converts exceptions to functional errors, improving type safety
- Automatic integration with Serilog for structured logging support
- Eliminates try-catch boilerplate code
- Consistent error code system shortens debugging time

<!-- Related commit: abc1234 feat(errors): Add ErrorCodeFactory.CreateFromException -->
````

---

## Bug Fixes Section

Bug fixes are written concisely. A brief description and commit SHA are sufficient. If there are no bugs to fix, delete this section entirely.

```markdown
## Bug Fixes

{Delete this section if there are no bug fixes}

- {Bug description} ({SHA})
- {Bug description} ({SHA})
```

Writing example.

```markdown
## Bug Fixes

- Fixed incorrect NuGet package icon path (a8ec763)
- Fixed null reference exception under certain conditions (b9fd874)
```

## API Changes Section

API changes show the namespace structure in tree format. The purpose is for library users to see at a glance what types are located where.

````markdown
## API Changes

### {Component Name} Namespace Structure

```
{Namespace.Root}
├── {SubNamespace1}/
│   ├── {Class1}
│   └── {Class2}
└── {SubNamespace2}/
    └── {Class3}
```
````

Writing example.

````markdown
## API Changes

### Functorium Namespace Structure

```
Functorium
├── Abstractions/
│   ├── Errors/
│   │   ├── ErrorCodeFactory
│   │   └── ErrorsDestructuringPolicy
│   └── Registrations/
│       └── OpenTelemetryRegistration
├── Adapters/
│   └── Observabilities/
│       └── Builders/
│           └── OpenTelemetryBuilder
└── Applications/
    └── Linq/
        └── FinTUtilities
```
````

## Installation Section

The installation section provides NuGet installation commands that users can follow immediately and required dependency information.

````markdown
## Installation

### NuGet Package Installation

```bash
# {Package name} core library
dotnet add package {PackageName} --version {VERSION}

# {Package name} test library (optional)
dotnet add package {PackageName}.Testing --version {VERSION}
```

### Required Dependencies

- .NET {version} or higher
- {Dependency 1}
- {Dependency 2}
````

Writing example.

````markdown
## Installation

### NuGet Package Installation

```bash
# Functorium core library
dotnet add package Functorium --version 1.0.0

# Functorium test library (optional)
dotnet add package Functorium.Testing --version 1.0.0
```

### Required Dependencies

- .NET 10.0 or higher
- LanguageExt.Core 5.0.0 or higher
- Serilog 4.0.0 or higher (when using logging features)
````

---

## Template Guide (Comments)

The comments at the bottom of the template are for reference during writing and must be deleted from the final document.

```markdown
<!--
============================================================
Template Usage Guide
============================================================

1. Replace {VERSION} with the actual version (e.g., v1.0.0)
2. Replace {DATE} with today's date (e.g., 2025-12-19)
3. Replace each section's {placeholder} with actual content
4. Delete comments (`<!-- -->` format) from the final document
5. Delete sections that do not apply

Required Checklist:
- [ ] Frontmatter completed
- [ ] Overview section written
- [ ] Breaking Changes confirmed (api-changes-diff.txt)
- [ ] Feature documentation for all feat commits
- [ ] Bug fix documentation for all fix commits
- [ ] "Why this matters" section included for all features
- [ ] All code examples verified against the Uber file
- [ ] Commit SHA annotations added

Reference Documents:
- Writing Rules: .release-notes/scripts/docs/phase4-writing.md
- Validation Criteria: .release-notes/scripts/docs/phase5-validation.md
- Uber File: .analysis-output/api-changes-build-current/all-api-changes.txt
-->
```

These comments include the placeholder replacement order, required checklist, and reference document links, allowing you to check items that are easy to miss during the writing process.

---

TEMPLATE.md consists of 7 sections: frontmatter, overview, Breaking Changes, new features, bug fixes, API changes, and installation. Only bug fixes can be omitted when not applicable; all others are required. In particular, every item in the new features section must include a feature description, code example, "Why this matters", and commit annotation. Following this structure ensures consistent quality release notes regardless of who writes them.

## FAQ

### Q1: Can the template section order be changed?
**A**: This is not recommended. The current order (Overview -> Breaking Changes -> New Features -> Bug Fixes -> API Changes -> Installation) places **the information users need to check first** at the top. In particular, Breaking Changes coming before new features is because users need to first determine whether code modifications are needed when upgrading.

### Q2: Why is the "Why this matters" section required for all features?
**A**: API signatures and code examples alone make it difficult for users to understand **what problem the feature solves.** The "Why this matters" section communicates the practical value of the feature, helping users judge whether to update and determine adoption priority.

### Q3: Why can only the bug fixes section be deleted while the rest are required?
**A**: Releases without bug fixes actually exist, but **overview, Breaking Changes (even if "none" must be stated), new features, API changes, and installation guides** are information users must check in any release. Explicitly stating "none" in an empty Breaking Changes section is important for user trust.

### Q4: Must the HTML comment guide at the bottom be deleted from the final document?
**A**: Yes. `<!-- -->` format comments may be hidden by some Markdown renderers, but **they can be exposed in GitHub's Raw view or some documentation sites.** The Phase 5 validation checks comment deletion as a checklist item, so they must be removed before final submission.

## Next Step

- [component-priority.json Configuration](08-component-config.md)
