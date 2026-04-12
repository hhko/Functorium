---
title: "Analyzing release-note.md"
---

We have covered the concept and syntax of commands so far. Now let's open the `release-note.md` command file, the core of release note automation. This file is the "brain" of the automation system. Understanding in what order and why Claude does what it does when executing `/release-note v1.2.0` will enable you to design your own complex commands.

The file is located at `.claude/commands/release-note.md`.

## Frontmatter Analysis

```yaml
---
title: RELEASE-NOTES
description: Automatically generates release notes (data collection, analysis, writing, validation).
argument-hint: "<version> Release version (e.g., v1.2.0)"
---
```

Including "(data collection, analysis, writing, validation)" in the description is intentional. It summarizes the entire process so that when you type `/`, you can immediately see what this command does in the auto-completion list. The `<version>` in argument-hint indicates a required argument, and the example (`v1.2.0`) is provided alongside to guide what format to use.

## Version Parameter Handling

````markdown
## Version Parameter (`$ARGUMENTS`)

**When version is specified:** $ARGUMENTS

The version parameter is required. Specify the version of the release notes to generate.

**Usage examples:**
```
/release-notes v1.2.0        # Regular release
/release-notes v1.0.0        # First deployment
/release-notes v1.2.0-beta.1 # Pre-release
```

**When version is not specified:** Output an error message and stop.
````

There is a reason the version parameter section comes before the workflow. If there is no argument, the remaining work is meaningless, so it is validated first and immediately stopped on failure. Providing diverse version format examples guides Claude to correctly handle any format.

## Workflow Overview

release-note.md defines a workflow consisting of 5-step Phases. It uses a **modularization pattern** that summarizes per-Phase goals in a table and separates detailed instructions into separate documents.

```markdown
| Phase | Goal | Detailed Document |
|-------|------|-------------------|
| **1. Environment Verification** | Prerequisite check, Base Branch decision | [phase1-setup.md](...) |
| **2. Data Collection** | Component/API change analysis | [phase2-collection.md](...) |
| **3. Commit Analysis** | Feature extraction, Breaking Changes detection | [phase3-analysis.md](...) |
| **4. Document Writing** | Release note writing | [phase4-writing.md](...) |
| **5. Validation** | Quality and accuracy verification | [phase5-validation.md](...) |
```

This design has two intentions. First, only the overall flow and key rules are in the master file (release-note.md), while detailed instructions for each Phase are separated into individual files for easier maintenance. Second, Claude can selectively read only the detailed document for the needed Phase, using the context window efficiently.

## Phase 1: Environment Verification

````markdown
## Phase 1: Environment Verification and Preparation

**Goal**: Verify required environment before release note generation

**Prerequisite Checks**:
```bash
git status              # Git repository check
dotnet --version        # .NET 10.x or higher required
ls .release-notes/scripts  # Script directory check
```

**Base Branch Decision**:
- If `origin/release/1.0` exists: Use as Base
- Otherwise (first deployment): Use `git rev-list --max-parents=0 HEAD`

**Success Criteria**:
- [ ] Git repository verified
- [ ] .NET SDK version verified
- [ ] Base/Target decided
````

Phase 1 starting with environment verification follows the "fail fast" principle. If the directory is not a Git repository, or the .NET SDK is missing, or the script directory does not exist, all subsequent Phases will fail. Checking upfront prevents unnecessary work.

The use of the **conditional processing pattern** in the Base Branch decision is also noteworthy. It handles both the common case where a release branch exists and the case of a first deployment, ensuring the command works in any situation.

## Phase 2: Data Collection

````markdown
## Phase 2: Data Collection

**Goal**: Analyze component/API changes using C# scripts

**Change Working Directory**:
```bash
cd .release-notes/scripts
```

**Key Commands**:
```bash
# 1. Component analysis
dotnet AnalyzeAllComponents.cs --base <base-branch> --target HEAD

# 2. API change extraction
dotnet ExtractApiChanges.cs
```

**Success Criteria**:
- [ ] `.analysis-output/*.md` files generated
- [ ] `all-api-changes.txt` Uber file generated
- [ ] `api-changes-diff.txt` Git Diff file generated
````

In Phase 2, the execution order is indicated by numbers and the filenames to be generated are listed specifically. This **input/output specification pattern** clarifies the data flow between Phases so that Claude does not get confused about which files to read in the next Phase.

## Phase 3: Commit Analysis

````markdown
## Phase 3: Commit Analysis and Feature Extraction

**Goal**: Analyze collected data to extract features for the release notes

**Input Files**:
- `.analysis-output/Functorium.md`
- `.analysis-output/Functorium.Testing.md`
- `.analysis-output/api-changes-build-current/api-changes-diff.txt`

**Breaking Changes Detection** (two methods):
1. **Git Diff Analysis (recommended)**: Detect deleted/changed APIs from `api-changes-diff.txt`
2. **Commit Message Patterns**: `!:`, `breaking`, `BREAKING` patterns

**Save Intermediate Results** (required):
- `.analysis-output/work/phase3-commit-analysis.md`
- `.analysis-output/work/phase3-feature-groups.md`
````

In Breaking Changes detection, the pattern of presenting two methods but **marking priority with "(recommended)"** is a strategy of telling Claude the preferred approach while keeping alternatives open. Git Diff analysis is more accurate, but commit message patterns can also be used supplementarily.

Saving intermediate results to files is a **design for traceability.** When problems occur, Phase 3's analysis results can be directly checked to identify the cause, and Phase 4 uses these files as input.

## Phase 4: Document Writing

````markdown
## Phase 4: Release Note Writing

**Goal**: Write professional release notes based on analysis results

**Template File**: `.release-notes/TEMPLATE.md`
**Output File**: `.release-notes/RELEASE-$ARGUMENTS.md`

### Writing Principles (Mandatory Compliance)

1. **Accuracy First**: Never document APIs not in the Uber file
2. **Code Examples Required**: Include executable code examples for all major features
3. **Traceability**: Include commit SHA as comments (`<!-- Related commit: SHA -->`)
4. **Mandatory Value Communication**: Include **"Why this matters"** section for all major features

> **Important**: Feature documentation without a "Why this matters" section is considered incomplete.
````

Using `$ARGUMENTS` in the output filename to automatically reflect the version is a practical design that prevents file conflicts.

The use of **strong tones** like "never", "required", and "considered incomplete" in the writing principles is also intentional. LLMs often ignore weak instructions ("if possible", "recommended"), but follow strong instructions better. The "Accuracy First" principle in particular is a key guardrail that prevents AI from fabricating non-existent APIs (hallucination).

## Phase 5: Validation

````markdown
## Phase 5: Validation

**Verification Items**:
1. **Frontmatter Existence**: Whether YAML frontmatter is included
2. **Required Sections Existence**: Overview, Breaking Changes, New Features, Installation
3. **"Why this matters" Section Existence**: Value description included for all major features
4. **API Accuracy**: All code examples verified against the Uber file
5. **Breaking Changes Completeness**: Cross-referenced with Git Diff results

**Verification Commands**:
```bash
# Frontmatter check
head -5 .release-notes/RELEASE-$ARGUMENTS.md

# "Why this matters" section existence check
grep -c "**Why this matters" .release-notes/RELEASE-$ARGUMENTS.md

# Breaking Changes Git Diff check
cat .analysis-output/api-changes-build-current/api-changes-diff.txt
```
````

Separating validation into a separate Phase is a pattern of "the author reviewing their own work." After writing the document in Phase 4, mechanically verifying in Phase 5 catches missed items. Accuracy is increased by combining quantitative verification using `grep -c` to count instances and cross-verification by comparing the Git Diff with the document.

## Completion Message Format

````markdown
## Completion Message (Required)

Upon release note generation completion, **display in the following format**:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Release Note Generation Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Version: $ARGUMENTS
File: .release-notes/RELEASE-$ARGUMENTS.md

Statistics Summary
| Item | Value |
|------|-------|
| Functorium | [N files, N commits] |
| Functorium.Testing | [N files, N commits] |
| Breaking Changes | [N] |

Next Steps
1. Review the generated release notes
2. Make manual edits if needed
3. Commit to Git
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```
````

There are two reasons for defining the completion message in a fixed format. The statistics summary allows immediate confirmation of the work results, and the "Next Steps" guidance lets the user continue with follow-up actions right away.

## Core Principles

```markdown
## Core Principles

### 1. Accuracy First

> **Never document APIs not in the Uber file.**

### 2. Mandatory Value Communication

> **Include a "Why this matters" section for all major features.**

### 3. Breaking Changes Auto-Detected via Git Diff

> **Git Diff analysis is more accurate than commit message patterns.**

### 4. Traceability

- Track all features to actual commits
- Commit SHA references
```

Restating the core principles at the end of the command is intentional repetition. In Claude's context window, content read last has a stronger effect, so restating the most important principles reinforces them.

## Design Patterns Summary

The design patterns used in release-note.md are summarized below. Refer to these when creating your own commands.

**Modularization Pattern.** Place core rules and overall flow in the master file, and separate detailed instructions into separate files. This makes maintenance easier and allows Claude to selectively read only the parts it needs.

**Checklist Pattern.** Specify success criteria for each Phase as a checklist. It is easy to track progress and prevents omissions.

**Input/Output Specification Pattern.** Clearly list the files read (input) and generated (output) by each Phase. This makes data flow between Phases clear and debugging easier when problems occur.

**Conditional Processing Pattern.** Define both "when X" and "when not X" to respond to various situations. Error handling is also part of this pattern.

## FAQ

### Q1: Why are the core principles restated at the end of release-note.md?
**A**: Because **content read last has a stronger effect** in an LLM's context window. Repeating core principles like "Accuracy First" and "Mandatory Value Communication" at the end helps Claude better adhere to these principles in Phases 4-5. It is an intentional repetition strategy.

### Q2: What are the advantages of the modularization pattern that separates Phase documents into individual files?
**A**: There are two advantages. First, the master file (`release-note.md`) contains only the overall flow and detailed instructions are separated into individual files, **making maintenance easier.** Second, Claude selectively reads only the detailed document for the needed Phase, **using the context window efficiently.**

### Q3: Does using strong tones like "never" and "required" actually have an effect?
**A**: It does. LLMs often ignore weak instructions like "if possible" or "recommended", but follow strong instructions like "never document" or "required" with higher probability. In particular, **"Never document APIs not in the Uber file"** is a key guardrail preventing AI hallucination.

In the next section, let's look at `commit.md`, another command that underpins the release note automation.
