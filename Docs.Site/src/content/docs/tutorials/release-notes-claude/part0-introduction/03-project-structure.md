---
title: "Project Structure Introduction"
---

In the previous section, we examined the architecture and data flow of the automation system. Now let's look at how the actual files and folders are arranged. Understanding the project structure in advance will make it natural to understand "where this file is located in the whole project and how it connects to other files" when analyzing each file in subsequent Parts.

---

## Overall Folder Structure

```
Functorium/
├── .claude/
│   └── commands/
│       ├── release-note.md          # Release note generation command
│       └── commit.md                # Commit rules command
│
├── .release-notes/
│   ├── TEMPLATE.md                  # Release note template
│   ├── RELEASE-v1.0.0.md           # Generated release notes
│   ├── RELEASE-v1.0.0-alpha.1.md   # Pre-release notes
│   │
│   └── scripts/
│       ├── AnalyzeAllComponents.cs  # Component analysis script
│       ├── ExtractApiChanges.cs     # API extraction script
│       ├── ApiGenerator.cs          # Public API generator
│       ├── AnalyzeFolder.cs         # Folder analysis (auxiliary)
│       │
│       ├── config/
│       │   └── component-priority.json  # Analysis target configuration
│       │
│       ├── docs/
│       │   ├── README.md            # Overall process overview
│       │   ├── phase1-setup.md      # Phase 1 detailed guide
│       │   ├── phase2-collection.md # Phase 2 detailed guide
│       │   ├── phase3-analysis.md   # Phase 3 detailed guide
│       │   ├── phase4-writing.md    # Phase 4 detailed guide
│       │   └── phase5-validation.md # Phase 5 detailed guide
│       │
│       └── .analysis-output/        # Analysis results (auto-generated)
│           ├── Functorium.md
│           ├── Functorium.Testing.md
│           ├── analysis-summary.md
│           ├── api-changes-build-current/
│           │   ├── all-api-changes.txt
│           │   └── api-changes-diff.txt
│           └── work/
│               ├── phase3-*.md
│               ├── phase4-*.md
│               └── phase5-*.md
│
└── Src/
    ├── Functorium/                  # Core library
    │   ├── .api/
    │   │   └── Functorium.cs        # Public API definition
    │   └── *.cs
    │
    └── Functorium.Testing/          # Test library
        ├── .api/
        │   └── Functorium.Testing.cs
        └── *.cs
```

This broadly divides into three areas. `.claude/commands/` is the entry point for automation, `.release-notes/` is the body of automation, and `Src/` is the analysis target. Let's look at each area in turn.

---

## .claude/commands/ -- Entry Point for Automation

This folder stores custom commands that can be executed as slash commands in Claude Code.

**release-note.md** is the key file. It defines the title, description, and argument hints in YAML frontmatter, and describes the entire 5-Phase workflow in the body. Claude Code reads this document and executes each Phase in order. In other words, release-note.md references the Phase documents under `.release-notes/scripts/docs/`, and the Phase documents instruct the execution of C# scripts under `.release-notes/scripts/` -- a chain structure.

```yaml
---
title: RELEASE-NOTES
description: Automatically generates release notes
argument-hint: "<version> Release version (e.g., v1.2.0)"
---
```

**commit.md** defines commit message rules. It follows the Conventional Commits format and contains commit types such as `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, and how to use the Topic parameter. A consistent commit history written according to these rules is necessary for Phase 3's automatic classification to work accurately.

---

## .release-notes/ -- Body of Automation

This is the folder where all release note-related files are gathered. Templates, generated release notes, C# scripts, Phase guides, and analysis results are all contained here.

### TEMPLATE.md and RELEASE-*.md

`TEMPLATE.md` is the skeleton of the release notes. It contains two placeholders, `{VERSION}` and `{DATE}`, and defines the standard section structure of overview, Breaking Changes, new features, bug fixes, API changes, and installation.

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
```

In Phase 4, this template is copied, placeholders are replaced with actual values, and each section is filled with analysis results to produce the final release notes such as `RELEASE-v1.0.0.md`. They are managed as separate files per version: regular releases (`RELEASE-v1.0.0.md`), alpha (`RELEASE-v1.0.0-alpha.1.md`), beta (`RELEASE-v1.0.0-beta.1.md`), etc.

### scripts/ -- C# Scripts and Configuration

Each C# script file has a designated Phase.

| File | Role | Phase Used |
|------|------|-----------|
| AnalyzeAllComponents.cs | Analyze all component changes | Phase 2 |
| ExtractApiChanges.cs | Extract Public API and generate Uber file | Phase 2 |
| ApiGenerator.cs | Extract Public API from DLL | Phase 2 (auxiliary) |
| AnalyzeFolder.cs | Single folder analysis | Independent execution |

`config/component-priority.json` defines which components to analyze and in what order. The array order determines analysis priority, and Glob patterns are also supported.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs",
    ".release-notes/scripts"
  ]
}
```

The `docs/` folder contains detailed guide documents for each of the 5 Phases (`phase1-setup.md` through `phase5-validation.md`) and an overall process overview (`README.md`). When Claude Code executes release-note.md, it references the corresponding Phase's guide document to perform specific tasks.

### .analysis-output/ -- Analysis Results (Auto-generated)

Running the scripts automatically generates results in this folder. The root contains per-component analysis results (`Functorium.md`, `Functorium.Testing.md`) and an overall summary (`analysis-summary.md`). Under `api-changes-build-current/` are the Uber file combining all Public APIs (`all-api-changes.txt`), the API changes diff (`api-changes-diff.txt`), an API summary, and a list of analyzed projects. Under `work/` are intermediate results from Phases 3-5 (`phase3-commit-analysis.md`, `phase4-draft.md`, `phase5-validation-report.md`, etc.).

---

## .api Subfolders in Src/

Each project folder contains a `.api` subfolder. This is where Public API definition files generated by PublicApiGenerator are stored.

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by PublicApiGenerator.
//     Assembly: Functorium
//     Generated at: 2025-12-20
// </auto-generated>
//------------------------------------------------------------------------------

namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static Error Create(string errorCode, ...) { }
        public static Error CreateFromException(string errorCode, ...) { }
    }
}
```

Because these files are tracked by Git, they serve three purposes. First, API change history can be managed through Git history. Second, Breaking Changes are automatically detected by comparing previous and current versions of `.api` files via Git diff. Third, they serve as reference material for verifying whether APIs documented in the release notes actually exist.

---

## File Relationships

Let's summarize how the files introduced so far connect in order during actual execution. When a user enters the `/release-note v1.2.0` command, data flows in the following sequence.

```
User Input
    │
    │  /release-note v1.2.0
    ▼
┌───────────────────────────┐
│ .claude/commands/         │
│ release-note.md           │──── Workflow definition
└─────────────┬─────────────┘
              │
              │  Phase document references
              ▼
┌───────────────────────────┐
│ .release-notes/scripts/   │
│ docs/*.md                 │──── Detailed guide for each Phase
└─────────────┬─────────────┘
              │
              │  C# script execution
              ▼
┌───────────────────────────┐
│ .release-notes/scripts/   │
│ *.cs                      │──── Data collection/analysis
└─────────────┬─────────────┘
              │
              │  Save analysis results
              ▼
┌───────────────────────────┐
│ .analysis-output/         │──── Analysis results
│ ├── *.md                  │
│ └── work/*.md             │
└─────────────┬─────────────┘
              │
              │  Use template
              ▼
┌───────────────────────────┐
│ .release-notes/           │
│ TEMPLATE.md               │──── Release note template
└─────────────┬─────────────┘
              │
              │  Generate final document
              ▼
┌───────────────────────────┐
│ .release-notes/           │
│ RELEASE-v1.2.0.md         │──── Final release notes
└───────────────────────────┘
```

The command references Phase documents, Phase documents execute C# scripts, scripts save analysis results, and analysis results combined with the template produce the final release notes -- this is the sequence of the flow. Looking at modification frequency for each folder, the command, Phase guides, and template are rarely changed once set up, C# scripts are improved as needed, and release notes and analysis results are newly generated with each release.

## FAQ

### Q1: Who generates and manages the files in `.api` subfolders?
**A**: Files like `Src/Functorium/.api/Functorium.cs` are automatically generated by the `ApiGenerator.cs` script using the PublicApiGenerator library from DLLs. These files are tracked in Git to manage API change history and are used to automatically detect Breaking Changes via Git diff.

### Q2: Should the `.analysis-output/` folder be committed to Git?
**A**: It is optional. Release note files (`.release-notes/RELEASE-*.md`) must be committed, but `.analysis-output/` contains regeneratable intermediate results, so the decision depends on project policy. If you want to keep analysis results as history, you can commit them together.

### Q3: Why are the command file, Phase guide documents, and C# scripts all in different folders?
**A**: This follows the **separation of concerns principle.** `.claude/commands/` is Claude Code's entry point, `.release-notes/scripts/docs/` contains detailed instructions per Phase, and `.release-notes/scripts/*.cs` is the executable code. This separation means the command only defines the overall flow, while detailed instructions and code can be modified independently.

### Q4: Which files are essential when copying this project structure to another project?
**A**: At minimum, you need `.claude/commands/release-note.md`, `.release-notes/TEMPLATE.md`, and the `.release-notes/scripts/` folder (C# scripts, Phase guides, configuration files). The `.api` subfolders in `Src/` are automatically generated during the first API extraction.

---

Now that you understand the project structure, the next Part will guide you through setting up the development environment and installing the necessary tools.

[1.1 .NET 10 Setup](../Part1-Setup/01-dotnet10-setup.md)
