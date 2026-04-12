---
title: "Commit Analysis and Feature Extraction"
---

Raw data alone is not enough to write release notes. Dozens of commits and API change lists tell you "what changed" but not "what it means to users." In Phase 3, the collected data is analyzed to extract features for the release notes and identify Breaking Changes.

## Input Files

The following files generated in Phase 2 are analyzed.

```txt
.analysis-output/
├── Functorium.md                           # Core library commits
├── Functorium.Testing.md                   # Test utility commits
└── api-changes-build-current/
    └── api-changes-diff.txt                # API change Git Diff
```

## Commit Analysis Method

The analysis proceeds in four steps.

### Step 1: Reading Commit Messages

Check the commit list from the component analysis files.

```markdown
# Example from analysis file:
6b5ef99 Add ErrorCodeFactory for structured error creation (#123)
853c918 Rename IErrorHandler to IErrorDestructurer (#124)
c5e604f Add OpenTelemetry integration support (#125)
4ee28c2 Improve Serilog destructuring for LanguageExt errors (#126)
```

### Step 2: Looking Up GitHub Issues/PRs

If GitHub references (`#123`, `(#124)`) are in commit messages, they **must be looked up.** PRs and issues contain context that cannot be known from commit messages alone. You can identify the specific problem users experienced, the motivation for the change, and related issues, making them key material when writing the "Why this matters" section of the release notes.

```txt
PR #106 description:
- Title: "Error handling improvements"
- Fixes issues: #101 and #102
- Implementation: Better error handling and validation

Issue #101: "ErrorCodeFactory doesn't support nested errors"
- User problem: Information loss when creating nested errors
- Pain point: "Inner error information not showing in logs"

Issue #102: "Serilog destructuring loses error context"
- User problem: LanguageExt error context lost during Serilog logging
```

### Step 3: Identifying Feature Types

Feature types are identified by patterns in commit messages.

| Pattern | Meaning | Priority |
|---------|---------|:--------:|
| `Add` | New feature or API | High |
| `Rename` | Breaking Change or API update | High |
| `Improve/Enhance` | Existing feature improvement | Medium |
| `Fix` | Bug fix | Low |
| `Support for` | New platform/technology integration | High |

### Step 4: Extracting User Impact

For each significant commit, four questions are answered. What capability does this enable (new feature), what changes for developers (API impact), what problem does it solve (use case), and is it a Breaking Change (migration required). The answers to these questions become the material for composing each section of the release notes.

## Breaking Changes Detection

Breaking Changes are identified using **two methods**, and both methods are used together to minimize omissions.

### Method 1: Commit Message Patterns (Developer Intent)

This finds Breaking Changes explicitly marked by developers in commit messages. It searches for patterns containing `breaking` or `BREAKING` strings, or `!` after the type (e.g., `feat!:`, `fix!:`).

```txt
feat!: Change IErrorHandler to IErrorDestructurer
fix!: Remove deprecated Create method
BREAKING: Update authentication flow
```

This method directly reflects developer intent but has the limitation that it cannot detect changes when the notation is omitted.

### Method 2: Git Diff Analysis (Auto-detection, Recommended)

This analyzes the Git diff of the `.api` folder to **objectively** detect actual API changes. Even if not marked in the commit message, it does not miss APIs that were actually deleted or changed.

**Git Diff File Location:**
```txt
.analysis-output/api-changes-build-current/api-changes-diff.txt
```

Breaking Change status is determined by the following patterns.

| Git Diff Pattern | Meaning | Breaking? |
|-----------------|---------|:---------:|
| `- public class Foo` | Class deleted | Yes |
| `- public interface IFoo` | Interface deleted | Yes |
| `- public void Method()` | Method deleted | Yes |
| `- Method(int x)` to `+ Method(string x)` | Type change | Yes |
| `+ public class Bar` | New class added | No |
| `+ public void NewMethod()` | New method added | No |

Let's look at an actual Git Diff example.

```diff
diff --git a/Src/Functorium/.api/Functorium.cs b/Src/Functorium/.api/Functorium.cs
@@ -34,11 +34,11 @@ namespace Functorium.Abstractions.Errors
 {
-    public class ErrorCodeExceptionalDestructurer : IErrorDestructurer
+    public class ErrorCodeExceptionalDestructurer : IErrorProcessor
     {
         public ErrorCodeExceptionalDestructurer() { }
-        public bool CanHandle(Error error) { }
+        public bool CanProcess(Error error) { }
     }
 }
```

In the above example, two Breaking Changes are detected. The interface name was changed from `IErrorDestructurer` to `IErrorProcessor`, and the method name was changed from `CanHandle` to `CanProcess`.

Comparing the two methods, Git Diff analysis is more accurate and reliable. Commit message patterns depend on developer intent and may miss notations, while Git Diff analysis detects actual code changes, provides objective evidence, and covers all changes.

## Commit Priority

Not all commits need to be included in the release notes. Priority is assigned based on the impact on users.

**High priority** commits that must be included are new types (Add Type, Add Factory), new integration support (Add support, Support for), Breaking API changes (Rename, Remove, Change), major features (Add method, Implement), and security improvements (security, validation). These directly impact developers' code or workflows.

**Medium priority** commits to consider including are performance improvements (Improve performance, Optimize), enhanced configuration (Add configuration, Support options), better error handling (Improve error, Add validation), and developer experience improvements (Enhance, Better). These are changes that improve existing features and are worth informing users about, but not essential.

**Low priority** commits that are usually skipped are minor bug fixes, internal refactoring with no user impact, documentation updates, test improvements, and code cleanup. Including them in the release notes can actually bury important changes.

## Feature Grouping

### Consolidating Related Commits

Multiple commits often comprise a single feature. For example, the following three commits are each different tasks, but together they form a single feature called "Enhanced Error Logging System."

```txt
853c918 Rename IErrorHandler to IErrorDestructurer
d4eacc6 Improve error destructuring output formatting
a1b2c3d Add structured logging for all error types
```

This consolidation allows the release notes to describe features in user-understandable units rather than listing individual commits. Describing a single feature from three aspects -- structured destructuring, better error messages, and enhanced tracing -- is much more effective.

### Multi-Component Features

A single feature may also be implemented across multiple components. If the core error factory was changed in Functorium and test utilities were updated in Functorium.Testing, these are consolidated into a single feature called "Error Handling System Improvement."

```txt
Functorium.md:
  - Core error factory change

Functorium.Testing.md:
  - Test utility update

→ Consolidated: "Error Handling System Improvement"
```

## Intermediate Results Storage

Phase 3 analysis results are stored in the following files.

```txt
.release-notes/scripts/.analysis-output/work/
├── phase3-commit-analysis.md     # Commit classification and priority
├── phase3-feature-groups.md      # Feature grouping results
└── phase3-api-mapping.md         # API and commit mapping
```

### phase3-commit-analysis.md Format

````markdown
# Phase 3: Commit Analysis Results

## Breaking Changes
- None (or list)

## Feature Commits (High Priority)
- [cda0a33] feat(functorium): Add core library package references
- [1790c73] feat(observability): OpenTelemetry and Serilog integration

## Feature Commits (Medium Priority)
- [4727bf9] feat(api): Add Public API files

## Bug Fixes
- [a8ec763] fix(build): Fix NuGet package icon path
````

### phase3-feature-groups.md Format

````markdown
# Phase 3: Feature Grouping Results

## Group 1: Functional Error Handling
**Related Commits:**
- Add ErrorCodeFactory.Create
- Add ErrorCodeFactory.CreateFromException
- Add ErrorsDestructuringPolicy

**User Value:**
Structured error creation and Serilog integration

## Group 2: OpenTelemetry Integration
**Related Commits:**
- Add OpenTelemetryRegistration
- Add OpenTelemetryBuilder

**User Value:**
Distributed tracing, metrics, and logging integration support
````

## Console Output Format

### Analysis Complete

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 3: Commit Analysis and Feature Extraction Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Analysis Results:
  Breaking Changes: 0
  Feature Commits: 6 (High: 4, Medium: 2)
  Bug Fixes: 1
  Feature Groups: 8

Identified Major Features:
  1. Functional Error Handling (ErrorCodeFactory)
  2. OpenTelemetry Integration (Observability)
  3. Architecture Validation (ArchUnitNET)
  4. Test Fixtures (Host, Quartz)
  5. Serilog Test Utilities
  6. FinT Utilities (LINQ Extensions)
  7. Options Pattern (FluentValidation)
  8. Utility Extension Methods

Intermediate results saved:
  .analysis-output/work/phase3-commit-analysis.md
  .analysis-output/work/phase3-feature-groups.md
  .analysis-output/work/phase3-api-mapping.md
```

## Verification Step

After analysis is complete, the quality of the results is checked.

First, review whether commit priorities are appropriate. New types, integration support, Breaking Changes, and major features should be at high priority, performance and configuration-related changes at medium priority, and documentation or refactoring at low priority.

Next, verify all API references against the Uber file.

```bash
grep "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt
```

Finally, ensure Breaking Changes are comprehensively included. All deleted/changed APIs from `api-changes-diff.txt` must be documented, and commits marked with commit message patterns (`!:`, `breaking`) must also be included.

## FAQ

### Q1: What is the criteria for grouping multiple commits into a single feature?
**A**: Commits that deal with related APIs or modules, commits linked to the same GitHub issue/PR, and commits that address similar topics (e.g., error handling, logging) are consolidated into a single feature group. Multi-component features (e.g., core change in Functorium, test addition in Functorium.Testing) are also grouped together.

### Q2: Why is looking up GitHub issues/PRs "mandatory"?
**A**: Commit messages alone make it difficult to **understand the motivation and context of the change.** PRs and issues contain the specific problems users experienced, alternative considerations, and related issues, making them key material for writing the "Why this matters" section. Without this information, the result is just a simple feature listing.

### Q3: Why are intermediate results saved to files?
**A**: This is a design for **traceability and debugging.** Saving `phase3-commit-analysis.md` and `phase3-feature-groups.md` as files allows Phase 4 to use them as input, and when problems occur, the Phase 3 analysis results can be directly checked to identify the cause.

Once analysis is complete, proceed to [Phase 4: Release Note Writing](04-phase4-writing.md) to write the actual document based on the extracted feature groups.
