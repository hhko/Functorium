---
title: "Creating Your First Release Note"
---

So far, we have examined the architecture of release note automation, the 5-Phase workflow, and the role of each script. Now let's actually run the commands to generate a release note from start to finish.

Enter a single command, and the entire pipeline flows automatically from environment validation to document writing. Let's follow along, checking what appears on screen at each Phase and what happens behind the scenes.

## Prerequisites

Before starting the hands-on exercise, you need to verify three environment elements. The .NET 10 SDK must be installed to run the scripts, you must be inside a Git repository to analyze commit history, and Claude Code must be in a runnable state.

```bash
# Verify .NET 10 installation
dotnet --version
# Output: 10.0.100 or higher

# Verify Git repository
git status
# Output: On branch main

# Run Claude Code
claude
```

Let's also check the scripts directory. The analysis scripts must be in the `.release-notes/scripts/` folder for Phase 2 to collect data.

```bash
# Verify required folders
ls .release-notes/scripts/
# Output: AnalyzeAllComponents.cs, ExtractApiChanges.cs, ...

ls .release-notes/
# Output: TEMPLATE.md, scripts/
```

## Step 1: Execute the Command

Once preparation is complete, execute the command in Claude Code interactive mode.

```bash
> /release-note v1.0.0
```

The version string follows the SemVer format. Pre-release tags can also be used in addition to regular releases.

| Version | Description |
|------|------|
| `v1.0.0` | Regular release |
| `v1.0.0-alpha.1` | Alpha release |
| `v1.0.0-beta.2` | Beta release |
| `v1.0.0-rc.1` | Release candidate |

The moment you execute the command, Claude begins processing the 5-Phase workflow sequentially. Let's look at what output appears at each Phase.

## Step 2: Phase 1 - Environment Validation

First, Claude checks whether the Git repository exists, whether the .NET SDK is installed, and whether the scripts directory is present. Once all prerequisites are met, it determines the comparison range (Base and Target).

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 1: Environment Validation
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Prerequisites:
  Git repository
  .NET SDK 10.x
  Scripts directory

Comparison Range:
  Base: abc1234 (initial commit)
  Target: HEAD
  Version: v1.0.0
```

The key part to note here is the **comparison range.** The Base differs depending on whether this is the first deployment or a subsequent one.

- **First deployment** (no release branch): Base is set to the initial commit, analyzing the entire history.
- **Subsequent deployment** (release branch exists): Base is set to the previous release point like `origin/release/1.0`, analyzing only changes since then.

## Step 3: Phase 2 - Data Collection

Once the environment is validated, the C# scripts are executed. `AnalyzeAllComponents.cs` collects changed files and commits for each component, and `ExtractApiChanges.cs` extracts Public API changes.

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 2: Data Collection
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Analyzing components...
  Functorium.md (31 files, 19 commits)
  Functorium.Testing.md (18 files, 13 commits)

Extracting APIs...
  all-api-changes.txt (Uber file)
  api-changes-diff.txt (Git Diff)
```

When this Phase completes, analysis results are saved in the `.analysis-output/` folder. You can verify that the actual files have been generated.

```bash
# Component analysis files
ls .release-notes/scripts/.analysis-output/
# Functorium.md
# Functorium.Testing.md
# analysis-summary.md

# API files
ls .release-notes/scripts/.analysis-output/api-changes-build-current/
# all-api-changes.txt
# api-changes-diff.txt
```

## Step 4: Phase 3 - Commit Analysis

Based on the collected data, Claude analyzes and classifies commits. It determines whether there are Breaking Changes, how many new features there are, how many bug fixes there are, and groups related commits into feature units.

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 3: Commit Analysis
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Analysis Results:
  Breaking Changes: 0
  Feature Commits: 6
  Bug Fixes: 1
  Feature Groups: 8

Identified Key Features:
  1. Functional error handling
  2. OpenTelemetry integration
  3. Test fixtures
  ...
```

## Step 5: Phase 4 - Document Writing

Once the analysis is complete, the release notes are written. Based on TEMPLATE.md, each section is filled in order: overview, Breaking Changes, new features, bug fixes, API changes, and installation guide.

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 4: Document Writing
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Writing...
  Overview section
  Breaking Changes
  New Features (8)
  Bug Fixes (1)
  API Changes
  Installation Guide

Output File:
  .release-notes/RELEASE-v1.0.0.md
```

## Step 6: Phase 5 - Validation

Finally, the quality of the generated document is validated. It checks whether APIs mentioned in the release notes match the actual Uber file, whether any Breaking Changes are missing, whether the Markdown format is correct, and whether all major features include a "Why this matters" section.

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: Validation
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Validation Items:
  API Accuracy - Passed
  Breaking Changes - Passed
  Markdown Format - Passed
  Why this matters sections - Passed

Status: Ready to publish
```

## Step 7: Review Results

When all 5 Phases are complete, a final summary is displayed. Per-component statistics and the path of the generated file are shown.

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Release Note Generation Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Version: v1.0.0
File: .release-notes/RELEASE-v1.0.0.md

Statistics Summary
| Item | Value |
|------|-----|
| Functorium | 31 files, 19 commits |
| Functorium.Testing | 18 files, 13 commits |
| Breaking Changes | 0 |
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

Your first release note has been generated. Now let's open the deliverable and review its content.

```bash
# Review the release notes
cat .release-notes/RELEASE-v1.0.0.md

# Or open in an editor
code .release-notes/RELEASE-v1.0.0.md
```

## Step 8: Manual Review and Revision

Auto-generated release notes are not always perfect. Reviewing with human eyes and supplementing where necessary is an important step. Let's check the following items one by one.

- [ ] Is the frontmatter (title, description, date) accurate?
- [ ] Does the overview explain this version's goals well?
- [ ] Are the Breaking Changes accurate?
- [ ] Are all major features included?
- [ ] Does every feature have a "Why this matters" section?
- [ ] Are the code examples correct?

The overview section in particular may have somewhat plain auto-generated text. Someone who knows the project context can polish it to produce a much better result.

````markdown
## Overview

<!-- Before revision -->
Functorium v1.0.0 is the first release.

<!-- After revision -->
Functorium v1.0.0 is the first official release of the functional programming
toolkit for .NET applications. This version focuses on error handling,
observability, and test support.
````

## Step 9: Git Commit

Once review and revision are complete, commit the deliverables to Git. The release note file must be committed, and analysis result files can optionally be saved alongside.

```bash
# Commit the release notes
git add .release-notes/RELEASE-v1.0.0.md
git commit -m "docs(release): Add v1.0.0 release notes"

# Also commit analysis results (optional)
git add .release-notes/scripts/.analysis-output/
git commit -m "chore(release): Save v1.0.0 analysis results"
```

## Troubleshooting

Problems may occur during the exercise. Here are the three most common situations and their solutions.

### Environment Validation Failure

```txt
Error: .NET 10 SDK is required
```

**Solution:**
```bash
# Install .NET 10
# https://dotnet.microsoft.com/download/dotnet/10.0
```

### Script Execution Failure

```txt
Script execution failed: AnalyzeAllComponents.cs
```

This occurs when the NuGet package cache is corrupted or output files from a previous run are locked.

**Solution:**
```bash
cd .release-notes/scripts

# Clear NuGet cache
dotnet nuget locals all --clear

# Delete output folder and retry
rm -rf .analysis-output
```

### Base Branch Not Found

```txt
Base branch origin/release/1.0 does not exist
```

For first deployments, the release branch does not exist yet, so this message may appear. The command automatically adjusts to analyze from the initial commit, but if you need to run it manually:

**Solution:**
```bash
cd .release-notes/scripts
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

## Exercise Complete

## FAQ

### Q1: What happens if one Phase fails during `/release-note` command execution?
**A**: The workflow proceeds sequentially, so it **stops at the failed Phase and displays an error message.** Output files from previous Phases are preserved, so you can fix the issue and re-run the command. If Phase 2 data collection has already completed, it will not re-collect as long as the `.analysis-output/` folder is not deleted.

### Q2: Is the `/release-note` command used differently for first deployments versus subsequent deployments?
**A**: The command itself is identical (`/release-note v1.0.0`). The difference lies in the **Base Branch determination logic.** If a previous release branch like `origin/release/1.0` exists, analysis starts from that point; if not, it starts from the initial commit. No special handling is needed from the user.

### Q3: What parts of the auto-generated release notes must be manually reviewed?
**A**: Focus on three things. First, verify that the **overview section** accurately conveys the context and goals of this release. Second, review whether the **"Why this matters" sections** effectively explain the practical value of each feature. Third, confirm that **code examples** match actual usage scenarios. API accuracy is automatically verified in Phase 5, but conveying context and value requires human judgment.

### Q4: Is it good to commit the analysis result files (`.analysis-output/`) together?
**A**: It depends on project policy. Committing them allows you to **trace what data the release notes were based on,** which is useful for audit purposes. On the other hand, since these files can be regenerated at any time, adding them to `.gitignore` to reduce repository size is also fine.

We have successfully generated, reviewed, and committed our first release note. We directly observed the 5-Phase workflow, triggered by a single command, proceeding automatically from environment validation to final verification. In the next section, we will write .NET 10 File-based Apps that underpin this system.

- [Writing Your Own Script](02-custom-script.md)
