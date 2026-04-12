---
title: "Writing Release Notes"
---

The analysis is done. Now it is time to turn the data into a document that developers want to read. Phase 4 is the most time-consuming step in the entire workflow, but it is also the most important. No matter how accurate the analysis, it has no value if it becomes a document that is hard to read.

## File Information

| Item | Path |
|------|------|
| Template | `.release-notes/TEMPLATE.md` |
| Output | `.release-notes/RELEASE-$VERSION.md` |

## Writing Procedure

### 1. Copy Template

```bash
cp .release-notes/TEMPLATE.md .release-notes/RELEASE-v1.2.0.md
```

### 2. Replace Placeholders

| Placeholder | Conversion Example |
|-------------|-------------------|
| `{VERSION}` | v1.2.0 |
| `{DATE}` | 2025-12-19 |

### 3. Fill Sections

Fill each section by referencing Phase 3 analysis results. Get commit classifications from `phase3-commit-analysis.md` and feature groups from `phase3-feature-groups.md`.

### 4. API Verification

Verify all code examples against the Uber file.

### 5. Clean Up Comments

Delete template guide comments (`<!-- -->`).

## Template Structure

These are the sections that must be included in the release notes.

| Section | Required | Description |
|---------|:--------:|-------------|
| Frontmatter | Yes | YAML header (title, description, date) |
| Overview | Yes | Version introduction, key change summary |
| Breaking Changes | Yes | Changes breaking API compatibility (state if none) |
| New Features | Yes | Based on feat commits |
| Bug Fixes | | Based on fix commits (omit if none) |
| API Changes | Yes | Summary of newly added key APIs |
| Installation | Yes | Installation instructions |

## Key Documentation Rules

### 1. Accuracy First

Never document APIs not in the Uber file. This is the most important rule.

```txt
Verification method:
grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. Code Examples Required

Include executable code examples for all major features.

### 3. Traceability

Include commit SHAs as comments so all content in the release notes is traceable to actual changes.

```markdown
<!-- Related commit: abc1234 -->
```

### 4. Mandatory Value Communication

**Feature documentation without a "Why this matters" section is considered incomplete.** The reason this rule exists is that what matters more than the existence of a feature is how it changes the developer's daily life. Simply writing "ErrorCodeFactory was added" leaves the developer unable to answer the question "so why is it important to me?"

## Value Communication Methods

### Required Structure

Each feature must include **all** of the following four elements.

1. **Feature Description (What)** - What does it do?
2. **Code Example (How)** - How do you use it?
3. **Why this matters (Why)** - Why is it important?
4. **API Reference (Reference)** - Exact API signature

### Bad Example (Simple Fact Listing)

```markdown
### Functional Error Handling

Provides structured error creation through ErrorCodeFactory.

[Code example]
```

The problem with this documentation is that the developer cannot know why they should use this feature. It merely conveys the fact that the feature exists, without explaining what problem it solves or what is better than the existing approach.

### Good Example (Value-Centric)

```markdown
### Functional Error Handling

Provides structured error creation through ErrorCodeFactory.

[Code example]

**Why this matters:**
- Converts exceptions to functional errors, improving type safety
- Automatic integration with Serilog for structured logging support
- Eliminates boilerplate code (reduces try-catch repetition)
- Consistent error code system shortens debugging time
- Separating business logic from error handling improves code readability
```

Here, each item connects to a specific problem developers face (type safety, boilerplate, debugging time).

### Why this matters Writing Checklist

- [ ] State the **specific problem** developers face
- [ ] Explain **how this feature solves** that problem
- [ ] Include **quantitative benefits** (if possible: time saved, code reduced)
- [ ] Include **qualitative benefits** (readability, maintainability, type safety)
- [ ] Present **real use cases**

### Expressions to Avoid vs Recommended Expressions

| Expressions to Avoid | Recommended Expressions |
|----------------------|------------------------|
| "Provides a feature" | "Provides [specific benefit] by doing ~" |
| "Supports" | "Solves [problem] to achieve [result]" |
| "Has been added" | "Simplifies [complex task] into [simple method]" |
| "Can do ~" | "Improves productivity through [time/code] savings" |

## API Documentation Rules

### API Source

**The Uber file is the single source of truth.**

```txt
.analysis-output/api-changes-build-current/all-api-changes.txt
```

### Accurate Documentation Process

When documenting APIs, always follow this order. First search the Uber file for the API, then check details in the individual API files, extract the complete API signature, and write code examples with the correct signature. Failing to follow this order results in the mistake of documenting non-existent APIs.

```txt
Step 1: Search for API in the Uber file
grep -A 10 "ErrorCodeFactory" all-api-changes.txt

Step 2: Check details in the individual API file
cat Src/Functorium/.api/Functorium.cs | grep -A 5 "ErrorCodeFactory"

Step 3: Extract complete API signature

Step 4: Write code examples with correct signature
```

### Exact API Signature Match

```csharp
// Exact signatures confirmed from the Uber file:
public static Error Create(string errorCode, string errorCurrentValue, string errorMessage)
public static Error CreateFromException(string errorCode, Exception exception)
```

```csharp
// Correct usage examples:
var error = ErrorCodeFactory.Create("VALIDATION_001", invalidValue, "Value is not valid");
var errorFromEx = ErrorCodeFactory.CreateFromException("SYSTEM_001", exception);
```

### Do Not Invent APIs

Creating methods or fluent chains that do not actually exist is the most common and dangerous mistake. APIs not in the Uber file must never be used in code examples.

```csharp
// Wrong: These methods are not in the Uber file
ErrorCodeFactory.Create("error")
    .WithDetails(details: "Additional info")     // Invented
    .WithInnerError(inner: innerError)           // Invented
    .Build();                                     // Invented
```

## Complete Feature Documentation Example

### Example 1: Basic Feature

````markdown
### Creating Structured Errors from Exceptions

Converts exceptions to structured error codes based on the LanguageExt Error type.

```csharp
using Functorium.Abstractions.Errors;
using LanguageExt.Common;

try
{
    await HttpClient.GetAsync(url);
}
catch (HttpRequestException ex)
{
    // Create structured error from exception
    var error = ErrorCodeFactory.CreateFromException("HTTP_001", ex);

    // Automatically logged in structured form to Serilog
    Log.Error("API request failed: {@Error}", error);

    return Fin<Response>.Fail(error);
}
```

**Why this matters:**
- Converts exceptions to functional errors, improving type safety
- Automatic integration with Serilog for structured logging support
- Eliminates try-catch boilerplate code
- Consistent error code system shortens debugging time

**API:**
```csharp
namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static Error CreateFromException(string errorCode, Exception exception);
    }
}
```
````

### Example 2: Breaking Change

````markdown
### Error Handler Interface Name Change

Error handling APIs have been unified for better consistency.

**Before (v1.0):**
```csharp
public class MyHandler : IErrorHandler
{
    public void Handle(Error error) { }
}
```

**After (v1.1):**
```csharp
public class MyDestructurer : IErrorDestructurer
{
    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory) { }
}
```

**Migration Guide:**
1. Update all `IErrorHandler` references to `IErrorDestructurer`
2. Replace `Handle` method with `Destructure` method
3. Change return type to `LogEventPropertyValue`
````

## Writing Style Guide

### Language and Tone

- **Use active voice:** "You can now create errors" (O) / "Errors can be created" (X)
- **Write developer-centric:** Practical examples that developers can use immediately
- **Write clearly and concisely:** Avoid unnecessary jargon or lengthy explanations

### Code Formatting

- **Specify the language:** Always ```csharp, ```bash, ```json
- **Show complete examples:** Fully working code examples
- **Highlight new features:** Mark changed parts with `// New feature` comments

### Section Organization

- **Start with the most impactful changes:** Breaking Changes first, then major features
- **Group related features:** Combine similar features into unified sections
- **Use descriptive headings:** Easy to scan

## Saving Intermediate Results

Save Phase 4's writing results.

```txt
.release-notes/scripts/.analysis-output/work/
├── phase4-draft.md              # Release note draft
├── phase4-api-references.md     # List of APIs used
└── phase4-code-samples.md       # All code examples
```

## Console Output Format

### Writing Complete

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 4: Release Note Writing Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Writing Statistics:
  Total Length: 15,380 lines
  Sections: 8
  Code Examples: 24
  API References: 30 types

Key Sections:
  1. Overview (Version: v1.0.0-alpha.1)
  2. Breaking Changes (0)
  3. New Features (8)
  4. Bug Fixes (1)
  5. API Changes (Summary)
  6. Installation

Output File:
  .release-notes/RELEASE-v1.0.0-alpha.1.md
```

## Quality Checklist

Items to verify before document completion.

### Accuracy Verification

- [ ] All code examples verified against the Uber file
- [ ] API signature parameter names and types match exactly
- [ ] No invented APIs or commands
- [ ] Complete migration guide for Breaking Changes

### Value Communication Verification

- [ ] "Why this matters" section included for all major features
- [ ] Specific problem resolution stated
- [ ] Developer productivity benefits explained
- [ ] Real use cases presented

### Structure and Formatting

- [ ] Consistent formatting - matches existing template
- [ ] Clear, developer-centric language
- [ ] Appropriate documentation links

## FAQ

### Q1: Why is feature documentation without a "Why this matters" section considered incomplete?
**A**: "ErrorCodeFactory was added" alone does not tell developers **why they should use** the feature. The release notes only provide practical help for update decisions when they also explain what problem it solves, what is better than the existing approach, and how much code is reduced.

### Q2: What is the specific method for searching the Uber file when documenting APIs?
**A**: Use the command `grep -A 10 "ErrorCodeFactory" all-api-changes.txt` to check API existence and exact signature. Parameter names, types, and return values must **exactly match** the Uber file. Be careful that the order is not swapped when there are multiple methods with similar names.

### Q3: What is the difference between expressions to avoid ("provides a feature") and recommended expressions?
**A**: Expressions to avoid only announce the feature's existence, while recommended expressions convey **specific benefits.** Instead of "supports", use "solves [problem] to achieve [result]"; instead of "has been added", use "simplifies [complex task] into [simple method]" -- specifying value that developers can tangibly appreciate.

Once release note writing is complete, proceed to [Phase 5: Validation](05-phase5-validation.md) for the final quality check.
