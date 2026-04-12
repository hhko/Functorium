---
title: "Environment Verification and Preparation"
---

Before anything is executed, the system must first verify whether it is in a state where release notes can be generated. You cannot analyze commits in a directory that is not a Git repository, you cannot extract APIs without the .NET SDK, and you cannot collect data without scripts. Phase 1 is the step that thoroughly verifies these prerequisites and determines which range of commits to analyze.

## Prerequisite Checks

The following four conditions must **all** be verified.

### 1. Git Repository Check

```bash
git status
```

This verifies whether the current directory is a Git repository and whether Git is installed. All data for release notes comes from the Git history, which is why this check happens first.

### 2. Script Directory Check

```bash
ls .release-notes/scripts
```

This verifies that the `.release-notes/scripts` directory and the `config/component-priority.json` file exist. The C# scripts to be executed in Phase 2 are in this directory.

### 3. .NET SDK Check

```bash
dotnet --version
```

.NET 10.x or higher must be installed. It is required for C# script execution and project builds.

### 4. Version Parameter Validation

```bash
/release-note v1.2.0
#             ^^^^^^
#             version parameter
```

This verifies that the version is not empty and is in a valid format (e.g., `v1.2.0`, `v1.0.0-alpha.1`).

## Base Branch Decision

Once prerequisites pass, the Base Branch for inter-release comparison is **automatically** determined. This decision is important because the range of commits to analyze changes completely depending on the Base Branch.

### Decision Logic

```txt
Version parameter: v1.2.0
       │
       ▼
┌─────────────────────────────────────┐
│ Does origin/release/1.0 branch     │
│ exist?                              │
└─────────────────────────────────────┘
       │
       ├── Yes ──▶ Base: origin/release/1.0
       │           Target: HEAD
       │
       └── No ───▶ Base: initial commit (first deployment)
                   Target: HEAD
```

Branch existence is checked with the following command.

```bash
# Check release branch existence
git rev-parse --verify origin/release/1.0
```

### Scenario 1: Subsequent Release

If the `origin/release/1.0` branch exists, only changes since the previous release are analyzed.

```txt
Comparison range:
  Base: origin/release/1.0
  Target: HEAD
  Version: v1.2.0
```

### Scenario 2: First Release

If the branch does not exist, it is treated as the first deployment, and all commits in the repository become analysis targets.

```bash
# Find initial commit SHA
git rev-list --max-parents=0 HEAD
```

```txt
Detected as first deployment

Comparison range:
  Base: abc1234 (initial commit)
  Target: HEAD
  Version: v1.0.0
```

## Environment Verification Failure Handling

When environment verification fails, a clear error message is output and **the process stops immediately.** Let's look at the situations where each error occurs and how to resolve them.

### Error 1: Not a Git Repository

This occurs when the command is executed in a directory other than the project root.

```txt
Error: Not a Git repository

Cannot execute 'git status' in the current directory.
Please execute the command from the Git repository root directory.
```

To resolve, navigate to the project root.

```bash
cd /path/to/your/project
```

### Error 2: No .NET SDK

This occurs when the .NET 10 SDK is not installed or not registered in the PATH. Both Phase 2's C# scripts and API extraction depend on the .NET SDK.

```txt
Error: .NET 10 SDK is required

Cannot execute the 'dotnet --version' command.

Installation instructions:
  https://dotnet.microsoft.com/download/dotnet/10.0
```

### Error 3: No Script Directory

The absence of the `.release-notes/scripts` directory means either the release note automation has not been set up for the project or the command was run from the wrong directory.

```txt
Error: Release note scripts not found

The '.release-notes/scripts' directory does not exist.
Please execute the command from the project root directory.
```

### Error 4: No Version Parameter

This occurs when no version was specified when running the command.

```txt
Error: Version parameter is required

Usage:
  /release-note v1.2.0        # Regular release
  /release-note v1.0.0        # First deployment
  /release-note v1.2.0-beta.1 # Pre-release
```

## Console Output Format

### Environment Verification Success

When environment verification completes successfully, the verified prerequisites and determined comparison range are displayed.

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 1: Environment Verification Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Prerequisites:
  Git repository
  .NET SDK 10.x
  Script directory

Comparison range:
  Base: origin/release/1.0
  Target: HEAD
  Version: v1.2.0
```

### First Deployment Detected

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 1: Environment Verification Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Prerequisites:
  Git repository
  .NET SDK 10.x
  Script directory

Detected as first deployment

Comparison range:
  Base: abc1234 (initial commit)
  Target: HEAD
  Version: v1.0.0
```

## Practical Examples

### Example 1: Subsequent Release (v1.2.0)

```bash
$ /release-note v1.2.0

Phase 1: Verifying environment...
  Checking Git repository... OK
  Checking .NET SDK... 10.0.100
  Checking scripts directory... OK
  Checking version parameter... v1.2.0
  Determining base branch...
    Found: origin/release/1.0

Comparison range determined:
  Base: origin/release/1.0
  Target: HEAD
```

### Example 2: First Deployment (v1.0.0)

```bash
$ /release-note v1.0.0

Phase 1: Verifying environment...
  Checking Git repository... OK
  Checking .NET SDK... 10.0.100
  Checking scripts directory... OK
  Checking version parameter... v1.0.0
  Determining base branch...
    No release branch found
    Using initial commit as base

Detected as first deployment:
  Base: 7a8b9c0 (initial commit)
  Target: HEAD
```

## FAQ

### Q1: How does the analysis range differ between first and subsequent deployments when the Base Branch changes?
**A**: For first deployments, the **entire history** is analyzed from the initial commit (`git rev-list --max-parents=0 HEAD`) to the current (`HEAD`). For subsequent deployments, only the **diff** from the previous release branch (`origin/release/1.0`) to the current is analyzed. First deployments document all features of the project, while subsequent deployments cover only newly added changes.

### Q2: Why does the process stop immediately when environment verification fails?
**A**: This follows the principle of **"fail fast."** If it is not a Git repository, or the .NET SDK is missing, or the script directory does not exist, all subsequent Phases 2-5 will fail. It is much more efficient to discover the problem within 10 seconds and resolve it, rather than running scripts for several minutes and then failing.

### Q3: What if the release branch name is not in the `origin/release/1.0` format?
**A**: You can modify the Base Branch decision logic in the Phase 1 guide (`phase1-setup.md`) of the `release-note.md` command. Alternatively, when running the script manually, you can directly specify the desired branch with the `--base` option: `dotnet AnalyzeAllComponents.cs --base origin/main --target HEAD`.

Once environment verification is complete, proceed to [Phase 2: Data Collection](02-phase2-collection.md) with the determined Base/Target range.
