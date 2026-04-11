---
title: "Release Note Automation"
---

**Do you spend 2--3 hours per release digging through Git logs to manually write release notes?** Missing a Breaking Change that triggers user issues, and inconsistent documentation accumulating as each author uses a different format -- everyone has experienced this at least once.

This tutorial solves that problem. By combining **Claude Code custom Commands** with **.NET 10 file-based apps**, you can build a system where a single line -- `/release-note v1.2.0` -- automatically generates professional release notes. You will analyze the automation system currently in operation in the real open-source project Functorium and learn systematically through a 5-phase workflow.

### Target Audience

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers who know basic C# syntax and want to get started with CLI tool development | Parts 0--1 |
| **Intermediate** | Developers interested in workflow automation and script development | All of Parts 2--3 |
| **Advanced** | Developers interested in Claude Code customization and advanced automation | Parts 4--5 + Appendix |

### Learning Objectives

After completing this tutorial, you will be able to:

1. Author and use **Claude Code custom Commands**
2. Develop CLI scripts with **.NET 10 file-based apps**
3. Handle professional CLI argument processing with **System.CommandLine**
4. Build rich console UIs with **Spectre.Console**

---

### Part 0: Introduction

Start by understanding why release notes matter and get a big-picture view of how the automation system works.

- [0.1 Why Release Notes Are Needed](Part0-Introduction/01-why-release-notes.md)
- [0.2 Automation System Overview](Part0-Introduction/02-automation-overview.md)
- [0.3 Project Structure Introduction](Part0-Introduction/03-project-structure.md)

### Part 1: Setup

Install and configure .NET 10 SDK, Claude Code, and Git to prepare your hands-on environment.

- [1.1 Installing .NET 10](Part1-Setup/01-dotnet10-setup.md)
- [1.2 Introduction to Claude Code](Part1-Setup/02-claude-code-intro.md)
- [1.3 Git Basics](Part1-Setup/03-git-basics.md)

### Part 2: Claude Commands

Learn how to create custom Commands in Claude Code and analyze the internal structure of the release note generation Command.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [What Is a Custom Command?](Part2-Claude-Commands/01-what-is-command.md) | Understanding the Command concept |
| 2 | [Command Syntax and Structure](Part2-Claude-Commands/02-command-syntax.md) | Syntax and authoring guide |
| 3 | [release-note.md Detailed Analysis](Part2-Claude-Commands/03-release-note-command.md) | Release note Command |
| 4 | [commit.md Introduction](Part2-Claude-Commands/04-commit-command.md) | Commit Command |

### Part 3: Workflow

Analyze the 5-phase workflow for release note generation in detail. From environment validation to final verification, examine what input each phase receives and what output it produces.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 0 | [Workflow Overview](Part3-Workflow/00-overview.md) | 5-Phase overall overview |
| 1 | [Phase 1: Environment Validation](Part3-Workflow/01-phase1-setup.md) | Directory, file verification |
| 2 | [Phase 2: Data Collection](Part3-Workflow/02-phase2-collection.md) | Git logs, change history |
| 3 | [Phase 3: Commit Analysis](Part3-Workflow/03-phase3-analysis.md) | Commit classification, grouping |
| 4 | [Phase 4: Document Authoring](Part3-Workflow/04-phase4-writing.md) | Template-based generation |
| 5 | [Phase 5: Validation](Part3-Workflow/05-phase5-validation.md) | Output file validation |

### Part 4: Implementation

Dive into the internals of C# scripts and templates written as .NET 10 file-based apps. Covers processing CLI arguments with System.CommandLine and building rich console UIs with Spectre.Console.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [.NET 10 File-Based Apps](Part4-Implementation/01-file-based-apps.md) | Introduction to file-based apps |
| 2 | [System.CommandLine](Part4-Implementation/02-system-commandline.md) | CLI argument processing |
| 3 | [Spectre.Console](Part4-Implementation/03-spectre-console.md) | Console UI implementation |
| 4 | [AnalyzeAllComponents.cs](Part4-Implementation/04-analyzeallcomponents.md) | Component analysis script |
| 5 | [ExtractApiChanges.cs](Part4-Implementation/05-extractapichanges.md) | API change extraction |
| 6 | [ApiGenerator.cs](Part4-Implementation/06-apigenerator.md) | API generator |
| 7 | [TEMPLATE.md Structure](Part4-Implementation/07-template-structure.md) | Template structure |
| 8 | [component-priority.json](Part4-Implementation/08-component-config.md) | Component configuration |
| 9 | [Output File Formats](Part4-Implementation/09-output-formats.md) | Output formats |

### Part 5: Hands-On

Based on everything you have learned, generate release notes yourself and write your own automation scripts.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Generating Your First Release Note](Part5-Hands-On/01-first-release-note.md) | First hands-on exercise |
| 2 | [Writing Your Own Script](Part5-Hands-On/02-custom-script.md) | Custom scripts |
| 3 | [Troubleshooting Guide](Part5-Hands-On/03-troubleshooting.md) | Troubleshooting |
| 4 | [Quick Reference](Part5-Hands-On/04-quick-reference.md) | Quick reference |

### [Appendix](Appendix/)

- [A. Glossary](Appendix/A-glossary.md)
- [B. API Reference](Appendix/B-api-reference.md)
- [C. Resources and Links](Appendix/C-resources.md)

---

## 5-Phase Workflow

The core of the automation system is a 5-phase pipeline. When a user runs the `/release-note v1.2.0` command, the process progresses sequentially from environment validation through data collection, commit analysis, document authoring, and final verification. Each phase takes the output of the previous phase as input, and clear success criteria are defined so you can immediately identify where a problem occurred.

| Phase | Stage | Key Tasks |
|:-----:|-------|----------|
| 1 | Environment Validation | Directory structure, required file verification |
| 2 | Data Collection | Git commit logs, file change history collection |
| 3 | Commit Analysis | Commit classification, component-level grouping |
| 4 | Document Authoring | Template-based release note generation |
| 5 | Validation | Output file validation, format verification |

---

## Technology Stack

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 10.0 | File-based app execution environment |
| System.CommandLine | 2.0.1 | CLI argument processing |
| Spectre.Console | 0.54.0 | Console UI (tables, panels, spinners) |
| PublicApiGenerator | 11.5.4 | Public API extraction |
| Claude Code | - | AI-powered CLI tool |

---

## Prerequisites

- .NET 10.0 SDK (Preview or release version)
- Claude Code CLI
- Git
- Visual Studio 2022 or VS Code + C# extension

---

## Source Code

All example code for this tutorial can be found in the Functorium project:

```bash
git clone https://github.com/hhko/Functorium.git
cd Functorium
```

Claude custom Commands are located in `.claude/commands/`, C# scripts in `.release-notes/scripts/`, and per-phase detailed documentation in `.release-notes/scripts/docs/`. For the complete project folder structure, see [0.3 Project Structure Introduction](Part0-Introduction/03-project-structure.md).

---

This tutorial was written based on real-world experience developing the release note automation system in the Functorium project.
