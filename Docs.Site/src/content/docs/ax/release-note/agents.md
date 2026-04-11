---
title: "Expert Agents"
description: "The release-engineer agent of the release-note plugin"
---

## release-engineer -- Release Note Generation Expert

Executes C# scripts to analyze component/API changes and automatically generates release notes.

**Areas of Expertise:**
- .NET C# script execution (`AnalyzeAllComponents.cs`, `ExtractApiChanges.cs`)
- Conventional Commits analysis and feature extraction
- Breaking Changes detection (Git Diff analysis takes priority, commit message patterns as supplement)
- API change tracking (Uber file-based validation)
- Release note writing (TEMPLATE.md-based)
- GitHub Issue/PR reference tracking

**Core Principles:**

| Principle | Description |
|-----------|-------------|
| Accuracy first | Never document APIs not found in the Uber file |
| Value delivery required | All major features include a "Why this matters" section |
| Breaking Changes auto-detection | Git Diff analysis takes priority over commit message patterns |
| Traceability | All features tracked by commit SHA |

### Example 1: First Release Note Generation

```text
User: Generate release notes for v1.0.0.
Agent: Proceeding in 5 phases:
       Phase 1: Environment validation -- .NET 10.x, Git, Base Branch determination
       Phase 2: Data collection -- AnalyzeAllComponents.cs + ExtractApiChanges.cs
       Phase 3: Commit analysis -- Breaking Changes, Feature/Fix classification
       Phase 4: Release note writing -- TEMPLATE.md-based, API validation
       Phase 5: Validation -- Frontmatter, Why this matters, size limit check
```

### Example 2: Breaking Changes Detection

```text
User: Are there Breaking Changes in this release?
Agent: Analyzing api-changes-diff.txt:
       - Deleted public APIs: 2
         1. IRepository.GetAll() -> Removed (replaced with Specification pattern)
         2. FinResponse.IsSuccess -> Renamed to IsSucc
       Each Breaking Change includes a migration guide.
```
