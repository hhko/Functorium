---
title: "Commit Analysis and Feature Extraction"
---

Raw data alone cannot produce release notes. Dozens of commits and API change listings tell us "what changed" but not "what it means to the user." Phase 3 analyzes the collected data to extract features for the release notes and identify Breaking Changes.

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

Analysis proceeds in four steps.

### Step 1: Read Commit Messages

Check the commit list from the component analysis files.

```markdown
# Example from analysis file:
6b5ef99 Add ErrorCodeFactory for structured error creation (#123)
853c918 Rename IErrorHandler to IErrorDestructurer (#124)
c5e604f Add OpenTelemetry integration support (#125)
4ee28c2 Improve Serilog destructuring for LanguageExt errors (#126)
```

### Step 2: Look Up GitHub Issues/PRs

If commit messages contain GitHub references (`#123`, `(#124)`), they **must be looked up.** PRs and issues contain context that cannot be known from commit messages alone. You can identify the specific problems users faced, the motivation for the change, and related issues, making this a key resource when writing the "Why this matters" section of the release notes.

```txt
PR #106 description:
- Title: "Error handling improvements"
- Fixed issues: #101 and #102
- Implementation: Better error handling and validation

Issue #101: "ErrorCodeFactory doesn't support nested errors"
- User problem: Information loss when creating nested errors
- Pain point: "Internal error information is not displayed in logs"

Issue #102: "Serilog destructuring loses error context"
- User problem: LanguageExt error context lost during Serilog logging
```

### Step 3: Identify Feature Types

Identify feature types from commit message patterns.

| Pattern | Meaning | Priority |
|---------|---------|:--------:|
| `Add` | New feature or API | High |
| `Rename` | Breaking Change or API update | High |
| `Improve/Enhance` | Existing feature improvement | Medium |
| `Fix` | Bug fix | Low |
| `Support for` | New platform/technology integration | High |

### Step 4: Extract User Impact

For each important commit, answer four questions. What does this enable (new capability), what changes for the developer (API impact), what problem does it solve (use case), and is it a Breaking Change (migration needed). The answers to these questions become the material composing each section of the release notes.

## Breaking Changes Detection

Breaking Changes are identified using **two methods**, and both are used together to minimize omissions.

### Method 1: Commit Message Patterns (Developer Intent)

Find Breaking Changes explicitly marked by developers in commit messages. Search for patterns containing `breaking`, `BREAKING` strings, or `!` after the type (e.g., `feat!:`, `fix!:`).

```txt
feat!: Change IErrorHandler to IErrorDestructurer
fix!: Remove deprecated Create method
BREAKING: Update authentication flow
```

This method directly reflects the developer's intent but has the limitation that it cannot detect changes if the marking is omitted.

### Method 2: Git Diff Analysis (Automatic Detection, Recommended)

Analyze the Git diff of the `.api` folder to **objectively** detect actual API changes. Even if not marked in the commit message, deleted or changed APIs are not missed.

**Git Diff file location:**
```txt
.analysis-output/api-changes-build-current/api-changes-diff.txt
```

Breaking Change status is determined by the following patterns.

| Git Diff Pattern | Meaning | Breaking? |
|-----------------|---------|:---------:|
| `- public class Foo` | Class deleted | Yes |
| `- public interface IFoo` | Interface deleted | Yes |
| `- public void Method()` | Method deleted | Yes |
| `- Method(int x)` to `+ Method(string x)` | Type changed | Yes |
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

In the example above, two Breaking Changes are detected. The interface name changed from `IErrorDestructurer` to `IErrorProcessor`, and the method name changed from `CanHandle` to `CanProcess`.

Comparing the two methods, Git Diff analysis is more accurate and reliable. Commit message patterns depend on developer intent and markings can be missed, while Git Diff analysis detects actual code changes, providing objective evidence and covering all changes.

## Commit Priority

Not all commits need to be included in the release notes. Priority is assigned based on the impact on users.

**High priority** commits that must be included are new types (Add Type, Add Factory), new integration support (Add support, Support for), Breaking API changes (Rename, Remove, Change), major features (Add method, Implement), and security improvements (security, validation). These directly affect developer code or workflows.

**Medium priority** commits to consider include performance improvements (Improve performance, Optimize), enhanced configuration (Add configuration, Support options), better error handling (Improve error, Add validation), and developer experience improvements (Enhance, Better). These are changes that improve existing features and have value in communicating to users, but are not essential.

**Low priority** commits usually skipped are minor bug fixes, internal refactoring with no user impact, documentation updates, test improvements, and code cleanup. Including them in the release notes would bury the important changes.

## Feature Grouping

### Consolidating Related Commits

Multiple commits often compose a single feature. For example, the following three commits are different tasks but together form a single feature called "Enhanced Error Logging System."

```txt
853c918 Rename IErrorHandler to IErrorDestructurer
d4eacc6 Improve error destructuring output formatting
a1b2c3d Add structured logging for all error types
```

By consolidating this way, the release notes can describe in feature units that users can understand, rather than listing individual commits. Describing a single feature from three perspectives -- structured destructuring, better error messages, and enhanced tracing -- is much more effective.

### Multi-Component Features

A single feature may be implemented across multiple components. If the core error factory was changed in Functorium and test utilities were updated in Functorium.Testing, these are consolidated into a single feature called "Error Handling System Improvement."

```txt
Functorium.md:
  - Core error factory changes

Functorium.Testing.md:
  - Test utility updates

→ Consolidated: "Error Handling System Improvement"
```

## Saving Intermediate Results

Phase 3's analysis results are saved in the following files.

```txt
.release-notes/scripts/.analysis-output/work/
├── phase3-commit-analysis.md     # Commit classification and priority
├── phase3-feature-groups.md      # Feature grouping results
└── phase3-api-mapping.md         # API to commit mapping
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

Identified Key Features:
  1. Functional Error Handling (ErrorCodeFactory)
  2. OpenTelemetry Integration (Observability)
  3. Architecture Validation (ArchUnitNET)
  4. Test Fixtures (Host, Quartz)
  5. Serilog Test Utilities
  6. FinT Utilities (LINQ Extensions)
  7. Options Pattern (FluentValidation)
  8. Utility Extension Methods

Intermediate Results Saved:
  .analysis-output/work/phase3-commit-analysis.md
  .analysis-output/work/phase3-feature-groups.md
  .analysis-output/work/phase3-api-mapping.md
```

## Verification Step

After analysis is complete, check the quality of the results.

First, review whether commit priorities are appropriate. New types, integration support, Breaking Changes, and major features should be at high priority; performance and configuration changes at medium priority; and documentation and refactoring at low priority.

Next, verify all API references against the Uber file.

```bash
grep "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt
```

Finally, confirm that Breaking Changes are comprehensively included. All deleted/changed APIs in `api-changes-diff.txt` must be documented, and commits marked with commit message patterns (`!:`, `breaking`) must also be included.

## FAQ

### Q1: What criteria are used to group multiple commits into a single feature?
**A**: Commits that deal with related APIs or modules, commits connected to the same GitHub issue/PR, and commits addressing similar topics (e.g., error handling, logging) are consolidated into a single feature group. Multi-component features (e.g., core changes in Functorium and test additions in Functorium.Testing) are also grouped together.

### Q2: Why is GitHub issue/PR lookup "mandatory"?
**A**: Commit messages alone make it difficult to understand **the motivation and context of the change.** PRs and issues contain the specific problems users faced, alternative considerations, and related issues, making them key material for writing the "Why this matters" section. Without this information, the result is merely a list of features.

### Q3: Why are intermediate results saved to files?
**A**: This is a design for **traceability and debugging.** Saving `phase3-commit-analysis.md` and `phase3-feature-groups.md` to files allows Phase 4 to use them as input, and when problems occur, Phase 3's analysis results can be directly examined to identify the cause.

Once analysis is complete, proceed to [Phase 4: Release Note Writing](04-phase4-writing.md) to write the actual document based on the extracted feature groups.
