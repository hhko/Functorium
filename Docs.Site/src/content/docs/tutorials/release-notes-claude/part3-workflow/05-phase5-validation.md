---
title: "Validation"
---

Release notes are valuable only when they are accurate. Documenting non-existent APIs, omitting Breaking Changes, or lacking migration guides only creates confusion for developers. Phase 5 is the quality assurance step that confirms whether the completed release notes can be published without such issues.

## Verification Items

### 1. Comprehensive Analysis

- [ ] All component analysis files have been reviewed
- [ ] All important commits have been analyzed
- [ ] Commit patterns have been grouped into cohesive feature sections
- [ ] Multi-component features have been consolidated

### 2. API Accuracy

- [ ] API change summary used to identify new and changed APIs
- [ ] All code examples use APIs verified from the Uber file
- [ ] All API references are accurate and complete
- [ ] No fabricated features documented

### 3. Breaking Changes Completeness

- [ ] Breaking Changes reflect actual API diffs and commits
- [ ] Migration guides provided for all Breaking Changes
- [ ] Before/after examples included for API changes

### 4. Structure and Quality

- [ ] Document follows the established template structure
- [ ] Consistent formatting
- [ ] Professional, developer-centric language

## Verification Process

### 1. Code Example Cross-Reference

Cross-reference all code examples included in the release notes with the Uber file. This step is the most important because code examples written by AI in Phase 4 may contain methods or parameters that do not actually exist.

```bash
# API existence check
grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt

# Signature check
grep -A 5 "ErrorFactory" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. Breaking Changes Confirmation

When verifying Breaking Changes, cross-check multiple sources.

| File | Purpose |
|------|---------|
| `all-api-changes.txt` | Current API definition |
| `api-changes-diff.txt` | Git Diff-based API changes (recommended) |
| `Src/*/.api/*.cs` | Individual assembly APIs |
| `.analysis-output/*.md` | Commit analysis (Breaking changes section) |

Git Diff-based verification is the most reliable.

```bash
# Check api-changes-diff.txt
cat .release-notes/scripts/.analysis-output/api-changes-build-current/api-changes-diff.txt

# Search for deleted APIs (-)
grep "^-.*public" api-changes-diff.txt

# Search for changed method signatures
grep -A 1 "^-.*public.*(" api-changes-diff.txt
```

Items to verify are as follows.

- [ ] All deleted APIs detected in Git Diff are documented
- [ ] All signature changes detected in Git Diff are documented
- [ ] Commits marked with commit message patterns (`!:`, `breaking`) are also included
- [ ] Migration guide exists for each Breaking Change
- [ ] Before/after code comparison included

### 3. Markdown Validation

```bash
# Run markdownlint
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-v1.2.0.md --disable MD013
```

### 4. Traceability Verification

For each documented change, verify that the commit SHA or message, GitHub issue ID (if referenced), GitHub Pull Request number (if available), and component name are traceable.

### 5. Content Quality Review

- [ ] **Accuracy:** All APIs exist in the Uber file
- [ ] **Completeness:** All major commits included
- [ ] **Clarity:** Developer-centric language, clear examples
- [ ] **Consistency:** Template structure followed
- [ ] **Traceability:** All features traceable to commits/PRs

## Pre-Publication Checklist

### Frontmatter Verification

```bash
# Frontmatter check
head -10 .release-notes/RELEASE-v1.2.0.md
```

- [ ] Correct title with version number
- [ ] Accurate description
- [ ] Current date

### Required Sections Check

```bash
# Section heading list
grep "^##" .release-notes/RELEASE-v1.2.0.md
```

- [ ] Overview
- [ ] Breaking Changes (state if none)
- [ ] New Features
- [ ] API Changes
- [ ] Installation

### "Why this matters" Section Check

```bash
# Count "Why this matters" sections
grep -c "**Why this matters" .release-notes/RELEASE-v1.2.0.md
```

- [ ] "Why this matters" section exists for all major features

### Code Block Language Specification

```bash
# Search for code blocks without language specification
grep -n "^\`\`\`$" .release-notes/RELEASE-v1.2.0.md
```

- [ ] All code blocks have language specified (```csharp, ```bash, ```json)

## Common Problems to Avoid

### API Documentation Errors

Documenting APIs not in the Uber file is the most frequently occurring problem. AI may infer non-existent methods from existing API patterns or create fluent chains. Verification against the Uber file is essential. Incorrect parameter names or types are also common, especially when there are multiple parameters with similar names and the order gets swapped.

### Structural Problems

When a Breaking Change lacks a migration guide, developers cannot know how to upgrade. Also, incorrect section ordering with lower-importance changes appearing first reduces readability. Breaking Changes should be placed first, followed by major features.

### Content Quality Problems

Vague language ("provides a feature") should be replaced with specific, developer-centric expressions. API descriptions without code examples are difficult for developers to actually use. Listing features without commit SHAs or PR numbers makes it impossible to later verify the context of those changes.

## Saving Intermediate Results

Save Phase 5's verification results.

```txt
.release-notes/scripts/.analysis-output/work/
├── phase5-validation-report.md   # Verification result report
├── phase5-api-validation.md      # API verification details
└── phase5-errors.md              # Errors found (on failure)
```

### phase5-validation-report.md Format

````markdown
# Phase 5: Validation Result Report

## Verification Date
2025-12-19T10:30:00

## Verification Target
.release-notes/RELEASE-v1.2.0.md

## Verification Results Summary
- API Accuracy: Passed (30 types verified)
- Breaking Changes: Passed (0)
- Markdown Format: Passed
- Checklist: 100%

## Detailed Results
[Detailed results per verification item]
````

## Console Output Format

### Validation Passed

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: Validation Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Verification Items Passed:
  API Accuracy (0 errors)
    - ErrorFactory
    - OpenTelemetryRegistration
    - ArchitectureValidationEntryPoint
    - HostTestFixture
    - QuartzTestFixture

  Breaking Changes Completeness
    - First release, no Breaking Changes

  Markdown Format
    - H1 headings: 1
    - Consistent heading hierarchy
    - Code block language specification: 100%

  Checklist (100%)
    - Comprehensive analysis
    - API accuracy
    - Structure and quality

Verification Results Saved:
  .analysis-output/work/phase5-validation-report.md
  .analysis-output/work/phase5-api-validation.md

Status: Ready for publication
```

### Validation Failed

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: Validation Failed
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Issues Found:

API Accuracy (2 errors):
  ErrorFactory.FromException (line 123)
    Location: RELEASE-v1.2.0.md:123
    Issue: API not in the Uber file
    Suggestion: Use ErrorFactory.CreateExceptional

  OpenTelemetryBuilder.Register (line 456)
    Location: RELEASE-v1.2.0.md:456
    Issue: Parameter mismatch
    Uber: RegisterObservability(IServiceCollection, IConfiguration)
    Document: Register(IServiceCollection)

Breaking Changes (1 error):
  IErrorHandler → IErrorDestructurer rename
    Issue: Migration guide missing
    Required: Before/after code examples and step-by-step guide

Action Required:
  1. Fix the document
  2. Re-run validation
```

## Quality Metrics

The following quality metrics are tracked.

| Metric | Description | Target |
|--------|-------------|--------|
| Coverage | Percentage of important commits documented | 100% |
| Accuracy | No invented APIs | 0 |
| Traceability | Traceable to source commits/PRs | 100% |
| Completeness | Breaking Changes migration guides | 100% |

## Accuracy Over Completeness

The core principle running through this entire workflow is **accuracy over completeness.** It is better to document fewer but certainly accurate features than to list many speculative features.

The reason this principle is important is that if there is even one piece of incorrect information in the release notes, the credibility of the entire document drops. If a developer writes code based on the documentation and it differs from the actual API, they will not trust the release notes from then on. On the other hand, if some features are missing but everything documented is accurate, developers can trust and use the release notes.

Every documented feature must meet four conditions. It must be **traceable** to an actual commit, **verifiable** through the Uber file or commit analysis, **executable** with a working code example, and **valuable** to the developer.

## Pass Criteria

- [ ] APIs not in the Uber file: 0
- [ ] All Breaking Changes detected in Git Diff documented
- [ ] Migration guide included for each Breaking Change
- [ ] Verification results saved

## Completion Message

When validation passes, the following completion message is displayed.

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Release Note Generation Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Version: v1.2.0
File: .release-notes/RELEASE-v1.2.0.md

Statistics Summary
| Item | Value |
|------|-------|
| Functorium | 31 files, 19 commits |
| Functorium.Testing | 18 files, 13 commits |
| Breaking Changes | 0 |

Next Steps
1. Review the generated release notes
2. Make manual edits if needed
3. Commit to Git
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

## FAQ

### Q1: How do you fix API accuracy errors found in Phase 5 validation?
**A**: Check the API name and location (line number) shown in the error message, then search for the correct signature in the Uber file (`all-api-changes.txt`). Correct the relevant part of the release notes with the exact signature from the Uber file, then re-run Phase 5 validation.

### Q2: Why is the "accuracy over completeness" principle important?
**A**: Because even one piece of incorrect information in the release notes **drops the credibility of the entire document.** If a developer writes code based on the documentation and it differs from the actual API, they will not trust the release notes afterward. If some features are missing but everything documented is accurate, developers can use the notes with confidence.

### Q3: Why are Phase 5 validation result files saved separately?
**A**: Saving validation reports (`phase5-validation-report.md`, `phase5-api-validation.md`) allows tracking the quality history of release notes and identifying recurring error patterns later. They are also used to pinpoint exactly which items were problematic on validation failure, enabling correction and re-validation.

### Q4: Does Phase 5 always fail if a Breaking Changes migration guide is missing?
**A**: Yes, if a Breaking Change is detected but there is no migration guide, it **fails on the "Breaking Changes Completeness" item.** Because developers encountering a Breaking Change without knowing how to upgrade would face significant confusion, before/after code comparison and step-by-step migration guides must be included.

This completes the entire 5-Phase workflow. A single command `/release-note v1.2.0` has gone through environment verification, data collection, commit analysis, document writing, and quality validation to produce a publishable release note. To learn more about the C# scripts that support this workflow, see [.NET 10 File-based App Introduction](../Part4-Implementation/01-file-based-apps.md).
