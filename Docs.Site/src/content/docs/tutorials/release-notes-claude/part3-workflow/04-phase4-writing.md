---
title: "Writing Release Notes"
---

The analysis is done. Now it is time to turn the data into a document that developers want to read. Phase 4 is the most time-consuming step in the entire workflow, but it is also the most important. No matter how accurate the analysis, it has no value if it becomes a hard-to-read document.

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

Reference Phase 3 analysis results to write each section. Get commit classification from `phase3-commit-analysis.md` and feature groups from `phase3-feature-groups.md`.

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
| Breaking Changes | Yes | Changes that break API compatibility (state if none) |
| New Features | Yes | Based on feat commits |
| Bug Fixes | | Based on fix commits (omit if none) |
| API Changes | Yes | Summary of newly added major APIs |
| Installation | Yes | Installation instructions |

## Core Documentation Rules

### 1. Accuracy First

Never document APIs not in the Uber file. This is the most important rule.

```txt
Verification method:
grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. Code Examples Required

Include executable code examples for all major features.

### 3. Traceability

Include commit SHA as comments so that all content in the release notes is traceable to actual changes.

```markdown
<!-- Related commit: abc1234 -->
```

### 4. Mandatory Value Communication

**Feature documentation without a "Why this matters" section is considered incomplete.** The reason this rule exists is that what matters more than the feature's existence is how it changes developers' daily work. Simply writing "ErrorCodeFactory was added" leaves developers unable to answer the question "so why is this important to me?"

## How to Communicate Value

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

The problem with this documentation is that developers cannot tell why they should use this feature. It only communicates the fact that the feature exists, without explaining what problem it solves or what advantages it has over existing approaches.

### Good Example (Value-Centered)

```markdown
### Functional Error Handling

Provides structured error creation through ErrorCodeFactory.

[Code example]

**Why this matters:**
- Converts exceptions to functional errors, improving type safety
- Automatic integration with Serilog for structured logging support
- Eliminates boilerplate code (reduced try-catch repetition)
- Consistent error code system shortens debugging time
- Separating business logic from error handling improves code readability
```

Here, each item connects to specific problems developers face (type safety, boilerplate, debugging time).

### Why this matters Writing Checklist

- [ ] Specify **specific problems** developers face
- [ ] Explain **how this feature solves** those problems
- [ ] Include **quantitative benefits** (where possible: time saved, code reduced)
- [ ] Include **qualitative benefits** (readability, maintainability, type safety)
- [ ] Present **actual use cases**

### Expressions to Avoid vs Recommended Expressions

| Expressions to Avoid | Recommended Expressions |
|----------------------|------------------------|
| "Provides the feature" | "Provides [specific benefit] by ~" |
| "Supports" | "Achieves [result] by solving [problem]" |
| "Has been added" | "Simplifies [complex task] to [simple method]" |
| "Can be ~" | "Improves productivity through [time/code] reduction" |

## API Documentation Rules

### API Source

**The Uber file is the single source of truth.**

```txt
.analysis-output/api-changes-build-current/all-api-changes.txt
```

### Accurate Documentation Process

When documenting APIs, the following order must always be followed. First search for the API in the Uber file, check details in individual API files, extract the complete API signature, then write code examples with the correct signature. Failing to follow this order leads to documenting non-existent APIs.

```txt
Step 1: Search for API in Uber file
grep -A 10 "ErrorCodeFactory" all-api-changes.txt

Step 2: Check details in individual API file
cat Src/Functorium/.api/Functorium.cs | grep -A 5 "ErrorCodeFactory"

Step 3: Extract complete API signature

Step 4: Write code examples with correct signature
```

### Exact API Signature Match

```csharp
// Exact signature confirmed from the Uber file:
public static Error Create(string errorCode, string errorCurrentValue, string errorMessage)
public static Error CreateFromException(string errorCode, Exception exception)
```

```csharp
// Correct usage example:
var error = ErrorCodeFactory.Create("VALIDATION_001", invalidValue, "Value is not valid");
var errorFromEx = ErrorCodeFactory.CreateFromException("SYSTEM_001", exception);
```

### Do Not Invent APIs

Fabricating non-existent methods or fluent chains is the most common and dangerous mistake. APIs not in the Uber file must never be used in code examples.

```csharp
// Wrong: These methods are not in the Uber file
ErrorCodeFactory.Create("error")
    .WithDetails(details: "Additional info")      // Invented
    .WithInnerError(inner: innerError)             // Invented
    .Build();                                       // Invented
```

## Complete Feature Documentation Example

### Example 1: Basic Feature

````markdown
### Creating Structured Errors from Exceptions

Converts exceptions into structured error codes based on the LanguageExt Error type.

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
### Error Handler Interface Renamed

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
- **Write developer-centric:** Practical examples that developers can immediately apply
- **Be clear and concise:** Avoid unnecessary jargon or verbose explanations

### Code Formatting

- **Specify the language:** Always use ```csharp, ```bash, ```json
- **Show complete examples:** Fully working code examples
- **Highlight new features:** Mark changed parts with `// new feature` comments

### Section Organization

- **Start with the most impactful changes:** Breaking Changes first, then major features
- **Group related features:** Combine similar features into unified sections
- **Use descriptive titles:** Easy to scan

## Intermediate Results Storage

Phase 4 writing results are saved.

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
  Total length: 15,380 lines
  Sections: 8
  Code examples: 24
  API references: 30 types

Major Sections:
  1. Overview (Version: v1.0.0-alpha.1)
  2. Breaking Changes (0)
  3. New Features (8)
  4. Bug Fixes (1)
  5. API Changes (summary)
  6. Installation

Output file:
  .release-notes/RELEASE-v1.0.0-alpha.1.md
```

## Quality Checklist

Items to check before document completion.

### Accuracy Verification

- [ ] All code examples verified against the Uber file
- [ ] API signature parameter names and types exactly match
- [ ] No invented APIs or commands
- [ ] Complete migration guide for Breaking Changes

### Value Communication Verification

- [ ] "Why this matters" section included for all major features
- [ ] Specific problem resolution stated
- [ ] Developer productivity benefits explained
- [ ] Actual use cases presented

### Structure and Formatting

- [ ] Consistent formatting - matches existing template
- [ ] Clear, developer-centric language
- [ ] Appropriate document links

## FAQ

### Q1: Why is feature documentation without a "Why this matters" section considered incomplete?
**A**: "ErrorCodeFactory was added" alone does not tell developers **why they should use** the feature. Release notes only provide practical help in update decisions when they also explain what problem it solves, what advantages it has over existing approaches, and how much code is reduced.

### Q2: What is the specific method for searching APIs in the Uber file during documentation?
**A**: Use `grep -A 10 "ErrorCodeFactory" all-api-changes.txt` to check API existence and exact signature. Parameter names, types, and return values must **exactly match** the Uber file. Be careful that the order does not get swapped when there are multiple similarly named methods.

### Q3: What is the difference between expressions to avoid ("provides the feature") and recommended expressions?
**A**: Expressions to avoid only announce the feature's existence, while recommended expressions **communicate specific benefits.** Instead of "supports", use "achieves [result] by solving [problem]"; instead of "has been added", use "simplifies [complex task] to [simple method]" -- stating value that developers can tangibly feel.

Once release note writing is complete, proceed to [Phase 5: Validation](05-phase5-validation.md) to check the final quality.
