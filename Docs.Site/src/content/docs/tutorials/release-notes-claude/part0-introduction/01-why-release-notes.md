---
title: "Why Release Notes"
---

Have you ever updated a library only to have your build break due to a Breaking Change that was never mentioned anywhere in the release notes? Or tried to find out "what changed in this version?" only to find a single line reading "bug fixes and performance improvements"?

Release notes are more than a simple change log. They are a **communication tool that bridges the gap between users and developers.** New features, improvements, bug fixes, Breaking Changes, installation instructions -- all the information needed to transition to a new version is captured in this single document.

---

## Why Are Release Notes Important?

### The User's Perspective

Release notes are the first thing users check before updating. "What improved with this update?", "Do I need to modify my code?", "How do I use the new features?", "Are there known issues?" -- if a release note can clearly answer these four questions, users can quickly decide whether to update and plan any necessary migrations.

### The Development Team's Perspective

For the development team, release notes serve as **the official record of change history.** They allow tracking of what changed, when, and why, and act as a channel for sharing changes among team members. The process of documenting changes also has a quality assurance effect by revealing overlooked items, and the notes can be used as reference documentation for customer support.

### Project Credibility

Well-written release notes are a sign of a mature project. Systematic version management, user-centric communication, a transparent development process, and a commitment to long-term maintenance -- all of these are reflected in a single set of release notes.

---

## The Reality of Manual Writing

In most projects, release notes are still written manually. Just before a release, you open the Git log, scan through commits, pick out the important ones, classify them, write the document, and review it. In a typical case, this takes about 15 minutes to check the Git log, 30 minutes to select important commits, 20 minutes to classify changes, 1 hour to write the document, and 30 minutes for review and revisions -- **a total of over 2 hours and 30 minutes.**

Time is not the only issue. Manual writing has structural limitations.

**Omissions and errors are frequent.** It is easy to miss commits or misclassify them. API changes may be omitted, Breaking Changes may go undetected, and typos may creep into code examples.

**Maintaining consistency is difficult.** Even within the same project, the format and depth of release notes vary depending on who writes them. There is a big difference between a one-liner "New feature added" and a detailed explanation with code examples that also describes why the feature matters.

````
Project A's release notes:
"- New feature added"
````
````
Project B's release notes:
### New Features
#### UserService Class Added
`UserService` is a class for user management.
```csharp
var service = new UserService();
```
**Why this matters:**
- Centralized user management logic
- Improved testability
````

Which release notes are more useful?

**Verification is also difficult.** With manual writing, it is practically impossible to confirm whether the documented API actually exists, whether the code examples are runnable, or whether all Breaking Changes are comprehensively included.

---

## What Automation Changes

What happens when we solve these problems with automation?

The most noticeable change is **time.** Running a single command `/release-note v1.2.0` and reviewing the result takes about 15 minutes. That is an **85% reduction** compared to manual writing.

**Accuracy also improves significantly.** Since all commits are automatically collected from Git, nothing is missed. APIs are extracted from actual code and verified, and deleted or changed APIs are automatically detected via Git diff.

**Consistent quality is guaranteed.** All release notes follow the same template structure -- overview, Breaking Changes, new features (with a mandatory "Why this matters" section for each feature), bug fixes, API changes, and installation instructions. Regardless of who writes them or when, the quality remains the same.

**Verification happens automatically.** The system checks that all APIs actually exist in the code, that code examples match API definitions, and that all required sections are included.

---

## The Automation System Covered in This Tutorial

This tutorial analyzes the release note automation system of the Functorium project. The system consists of three core components.

First, the **Claude Code custom command** (`release-note.md`) defines and orchestrates the entire workflow. Second, the **5-Phase workflow** systematically manages the steps from environment verification to final validation. Third, **C# scripts** (`AnalyzeAllComponents.cs`, `ExtractApiChanges.cs`) written as **.NET 10 file-based apps** perform the actual data collection and analysis.

The command invokes the workflow, and the workflow executes the C# scripts in a layered structure. The detailed architecture of each component will be examined in the next section.

## FAQ

### Q1: What is the difference between release notes and a CHANGELOG?
**A**: A CHANGELOG is a technical record listing changes for all versions in chronological order, whereas **release notes** are a communication document that explains the changes of a specific version from the user's perspective. Release notes include value descriptions like "Why this matters", code examples, and migration guides to help users decide whether to update and apply changes immediately.

### Q2: Does adopting automation completely eliminate the need for manual review?
**A**: No. Automation handles data collection, API verification, and consistent formatting, but the contextual explanation in the overview section and the value communication in the "Why this matters" section still require human review. The recommended workflow is **approximately 15 minutes of manual review** after automatic generation.

### Q3: Can this automation system be applied to projects other than Functorium?
**A**: Yes. You can change the analysis target folders in `component-priority.json` and modify `TEMPLATE.md` to fit your project. The core prerequisites are a Git repository, .NET 10 SDK, and a commit history in Conventional Commits format.

### Q4: What is covered in the next section?
**A**: We will look at how the three core components of the automation system (Claude Code command, 5-Phase workflow, C# scripts) are architecturally connected and how data flows through the entire structure.

---

The next section examines the overall architecture and data flow of this automation system.

[0.2 Automation System Overview](02-automation-overview.md)
