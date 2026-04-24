---
title: "Output File Formats"
---

When the workflow runs, several files are generated. Each file exists for a different consumer (Phase). Phase 3 reads the component analysis files to classify commits, Phase 4 checks API accuracy from the Uber file, and Phase 5 produces the validation report. Understanding who each output file is for and what format it uses is essential to grasping how the entire workflow connects.

## Output File Overview

The overall structure of output files is as follows.

```txt
.release-notes/
├── RELEASE-v1.2.0.md                    # Final release notes
└── scripts/
    └── .analysis-output/
        ├── Functorium.md                # Component analysis
        ├── Functorium.Testing.md
        ├── analysis-summary.md          # Analysis summary
        ├── api-changes-build-current/
        │   ├── all-api-changes.txt      # Uber file
        │   ├── api-changes-summary.md
        │   └── api-changes-diff.txt     # Git Diff
        └── work/
            ├── phase3-commit-analysis.md
            ├── phase3-feature-groups.md
            ├── phase4-api-references.md
            └── phase5-validation-report.md
```

It is broadly divided into three areas. At the `.analysis-output/` root are the per-component analysis results and summary, in `api-changes-build-current/` are the API-related files, and in `work/` are the per-Phase work files. Let's examine each file one by one.

---

## Component Analysis Files (*.md)

Component analysis files are generated with names like `.analysis-output/Functorium.md`, `.analysis-output/Functorium.Testing.md`, etc. One is created for each component defined in component-priority.json.

The primary consumer of these files is **Phase 3.** Phase 3 reads these files and uses them as the foundational data for classifying commits by feature and assigning priorities.

```markdown
# Analysis for Src/Functorium

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Change Summary

 Src/Functorium/Abstractions/Errors/ErrorFactory.cs | 45 +++++
 Src/Functorium/Applications/ElapsedTimeCalculator.cs   | 32 +++
 37 files changed, 1542 insertions(+), 89 deletions(-)

## All Commits

6b5ef99 feat(errors): Add ErrorFactory
853c918 feat(logging): Add Serilog integration
c5e604f fix(build): Fix NuGet package icon path
d4eacc6 docs: Update README

## Top Contributors

1. developer@example.com (15 commits)
2. contributor@example.com (4 commits)

## Categorized Commits

### Feature Commits

6b5ef99 feat(errors): Add ErrorFactory
853c918 feat(logging): Add Serilog integration

### Bug Fixes

c5e604f fix(build): Fix NuGet package icon path

### Breaking Changes

(none)
```

The file includes file change summaries, the full commit list, contributor information, and commit categorization, allowing you to see at a glance what changed and how in a single component.

## Analysis Summary File

The analysis summary file is generated at `.analysis-output/analysis-summary.md`. It aggregates the analysis results of all components into one file, showing the overall scale of the release.

```markdown
# Analysis Summary

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Components Analyzed

| Component | Files | Commits | Output |
|-----------|-------|---------|--------|
| Functorium | 37 | 19 | Functorium.md |
| Functorium.Testing | 18 | 13 | Functorium.Testing.md |
| Docs | 38 | 37 | Docs.md |

## Total Statistics

- **Total Components**: 3
- **Total Files Changed**: 93
- **Total Commits**: 69

## Output Files

- `.analysis-output/Functorium.md`
- `.analysis-output/Functorium.Testing.md`
- `.analysis-output/Docs.md`
```

This file is used for the statistical summary in the release notes and is useful for quickly reviewing the overall analysis results.

---

## Uber API File

The Uber API file is generated at `.analysis-output/api-changes-build-current/all-api-changes.txt`. It is called "Uber" because it **consolidates the Public API of all assemblies into a single file.**

The consumers of this file are **Phase 4 and Phase 5.** In Phase 4, it verifies that code examples included in the release notes match the actual API, and in Phase 5, it performs final validation of the accuracy of all documented APIs. It serves as the Single Source of Truth.

```csharp
// All API Changes - Uber File
// Generated: 2025-12-19 10:30:00

// ═══════════════════════════════════════════
// Assembly: Functorium
// ═══════════════════════════════════════════

namespace Functorium.Abstractions.Errors
{
    public static class ErrorFactory
    {
        public static LanguageExt.Common.Error Create(
            string errorCode,
            string errorCurrentValue,
            string errorMessage) { }
        public static LanguageExt.Common.Error CreateFromException(
            string errorCode,
            System.Exception exception) { }
    }
}

namespace Functorium.Abstractions.Registrations
{
    public static class OpenTelemetryRegistration
    {
        public static OpenTelemetryBuilder RegisterObservability(
            this IServiceCollection services,
            IConfiguration configuration) { }
    }
}

// ═══════════════════════════════════════════
// Assembly: Functorium.Testing
// ═══════════════════════════════════════════

namespace Functorium.Testing.Arrangements.Hosting
{
    public class HostTestFixture<TProgram> where TProgram : class
    {
        public HttpClient Client { get; }
        public IServiceProvider Services { get; }
    }
}
```

---

## API Diff File

The API Diff file is generated at `.analysis-output/api-changes-build-current/api-changes-diff.txt`. It is the result of comparing the Public API between the previous version and the current version in Git diff format.

The key consumer of this file is **Phase 3.** It identifies deleted APIs (`-` lines) and added APIs (`+` lines) to automatically detect Breaking Changes.

```diff
diff --git a/Src/Functorium/.api/Functorium.cs b/Src/Functorium/.api/Functorium.cs
index abc1234..def5678 100644
--- a/Src/Functorium/.api/Functorium.cs
+++ b/Src/Functorium/.api/Functorium.cs
@@ -10,6 +10,10 @@ namespace Functorium.Abstractions.Errors
     public static class ErrorFactory
     {
         public static Error Create(string errorCode, string errorCurrentValue, string errorMessage) { }
+        public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage)
+            where T : notnull { }
+        public static Error CreateFromException(string errorCode, Exception exception) { }
     }
 }

-namespace Functorium.Abstractions.Handlers
-{
-    public interface IErrorHandler
-    {
-        void Handle(Error error);
-    }
-}
+namespace Functorium.Abstractions.DestructuringPolicies
+{
+    public interface IErrorDestructurer
+    {
+        LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory);
+    }
+}
```

In the example above, you can see that `IErrorHandler` was deleted and `IErrorDestructurer` was added. This pattern is detected as a Breaking Change.

---

## Phase Work Files

Phase work files are generated in the `.analysis-output/work/` directory. These are intermediate artifacts where each Phase records its work results, and the next Phase uses them as input.

```txt
.analysis-output/work/
├── phase3-commit-analysis.md
├── phase3-feature-groups.md
├── phase4-api-references.md
├── phase5-validation-report.md
└── phase5-api-validation.md
```

**phase3-commit-analysis.md** is the result of Phase 3's commit analysis. It classifies commits into Breaking Changes, feature commits (by priority), and bug fixes.

```markdown
# Phase 3: Commit Analysis Results

## Breaking Changes
- IErrorHandler → IErrorDestructurer name change

## Feature Commits (High Priority)
- [6b5ef99] feat(errors): Add ErrorFactory
- [853c918] feat(logging): Add Serilog integration

## Feature Commits (Medium Priority)
- [d4eacc6] feat(config): Add configuration options

## Bug Fixes
- [c5e604f] fix(build): Fix NuGet package icon path
```

**phase3-feature-groups.md** is the result of grouping individual commits into user-facing feature groups. Phase 4 uses this grouping as the basis for writing the "New Features" section of the release notes.

```markdown
# Phase 3: Feature Grouping Results

## Group 1: Functional Error Handling
**Related Commits:**
- ErrorFactory.CreateExpected added
- ErrorFactory.CreateExceptional added

**User Value:**
Structured error creation and Serilog integration

## Group 2: OpenTelemetry Integration
**Related Commits:**
- OpenTelemetryRegistration added
- OpenTelemetryBuilder added

**User Value:**
Unified distributed tracing, metrics, and logging
```

**phase5-validation-report.md** is the final validation report from Phase 5. It validates API accuracy, Breaking Changes documentation status, Markdown format, and checklist completion rate.

```markdown
# Phase 5: Validation Results Report

## Validation Timestamp
2025-12-19T10:30:00

## Validation Target
.release-notes/RELEASE-v1.0.0.md

## Validation Results Summary
- API Accuracy: Passed (30 types verified)
- Breaking Changes: Passed (1 documented)
- Markdown Format: Passed
- Checklist: 100%

## Detailed Results

### API Validation
| API | Status | Uber File Line |
|-----|------|---------------|
| ErrorFactory.CreateExpected | Verified | 75-77 |
| ErrorFactory.CreateExceptional | Verified | 78-79 |
| OpenTelemetryRegistration.RegisterObservability | Verified | 93-95 |
```

## Final Release Notes

The final deliverable is generated at `.release-notes/RELEASE-v1.0.0.md`. It is a document written in Phase 4 based on the template (TEMPLATE.md). See [TEMPLATE.md Structure](07-template-structure.md) for the detailed structure.

---

## File Generation Flow

A summary of which Phase generates each file is as follows.

```txt
Phase 1: Environment Validation
    └─▶ (no files generated)

Phase 2: Data Collection
    ├─▶ .analysis-output/*.md (component analysis)
    ├─▶ .analysis-output/analysis-summary.md
    ├─▶ .analysis-output/api-changes-build-current/all-api-changes.txt
    └─▶ .analysis-output/api-changes-build-current/api-changes-diff.txt

Phase 3: Commit Analysis
    ├─▶ .analysis-output/work/phase3-commit-analysis.md
    └─▶ .analysis-output/work/phase3-feature-groups.md

Phase 4: Document Writing
    ├─▶ .release-notes/RELEASE-v1.0.0.md
    └─▶ .analysis-output/work/phase4-api-references.md

Phase 5: Validation
    ├─▶ .analysis-output/work/phase5-validation-report.md
    └─▶ .analysis-output/work/phase5-api-validation.md
```

Phase 1 only validates the environment and does not generate files. Phase 2 collects raw data, Phase 3 analyzes it, Phase 4 writes the document, and Phase 5 validates it. Since each Phase uses the output files of the previous Phase as input, the dependency relationships between files effectively define the execution order of the workflow.

---

This concludes our examination of all the template and configuration files covered in Chapter 6. TEMPLATE.md defines the standard format of release notes, component-priority.json determines the analysis targets and priorities, and the output files of each Phase serve as the connective tissue of the workflow. Understanding these three elements gives you the complete picture of how the automation system produces consistent release notes.

## FAQ

### Q1: Should intermediate artifacts in the `.analysis-output/work/` directory be committed to Git?
**A**: It is optional. Intermediate artifacts are **useful for debugging and audit purposes,** so committing them allows you to trace what data the release notes were based on later. However, since these files can be regenerated at any time, adding them to `.gitignore` and committing only when needed is also a reasonable choice.

### Q2: How can missing output files between Phases be detected?
**A**: Since each Phase uses the output files of the previous Phase as input, **an error occurs immediately in that Phase if a file is missing.** For example, Phase 3 will output an error saying "Analysis files not found" if `.analysis-output/*.md` files are missing, and Phase 4 will output "all-api-changes.txt not found" if the Uber file is missing.

### Q3: What is the difference between the Uber file and the API Diff file?
**A**: The **Uber file (`all-api-changes.txt`)** is a snapshot containing the entire Public API of the current branch, while the **API Diff file (`api-changes-diff.txt`)** shows only the differences—APIs added, deleted, or changed between the previous and current versions. The Uber file is used for API accuracy validation, and the Diff file is used for Breaking Change detection.

### Q4: What criteria are used for feature grouping in `phase3-feature-groups.md`?
**A**: In Phase 3, Claude analyzes the commit message scopes (`feat(errors)`, `feat(logging)`) and the changed file paths, then **groups commits that provide the same user value** into a single feature group. For example, commits related to `ErrorFactory.CreateExpected` and `ErrorFactory.CreateExceptional` are combined into a "Functional Error Handling" group.

## Next Step

- [Creating Your First Release Note](../Part5-Hands-On/01-first-release-note.md)
