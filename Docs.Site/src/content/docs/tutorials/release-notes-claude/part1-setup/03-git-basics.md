---
title: "Git Basics"
---

The raw materials for the release note automation system reside in the Git repository. It reads "what changed" from the commit history, identifies "what code is different" from diffs, and determines "what range changed since the last release" through branch comparisons. These three pieces of information must be combined to produce a release note saying "N new features, M bug fixes, K Breaking Changes."

In this section, we will look at the Git commands needed for release note automation and the Conventional Commits rules that are key to automatic classification.

## Basic Git Commands

### git log - Checking Commit History

`git log` shows who changed what and when. In release note automation, it is used to parse the type (`feat`, `fix`, etc.) of commit messages to classify features.

```bash
# Basic log
git log

# One-line summary
git log --oneline

# Show specific number
git log --oneline -10

# Commits for a specific folder only
git log --oneline -- Src/Functorium/

# Date range
git log --oneline --since="2025-01-01" --until="2025-12-31"
```

**Output example:**
```
51533b1 refactor(observability): Improve observability abstraction and structure
4683281 feat(linq): Add TraverseSerial method
93ff9e1 chore(api): Update Public API file timestamps
a8ec763 fix(build): Fix NuGet package icon path
```

In this output, `feat(linq)` is automatically classified into the "New Features" section and `fix(build)` into the "Bug Fixes" section.

### git diff - Checking Changes

`git diff` shows what code was actually added and deleted. A particularly important role in release notes is **Breaking Changes detection**. Analyzing diffs of Public API files can automatically find deleted or changed APIs.

```bash
# Compare working directory and staging area
git diff

# Compare two branches
git diff main..feature-branch

# Compare specific commits
git diff abc123..def456

# Changed file list only
git diff --name-only

# Statistical summary
git diff --stat
```

**Output example (--stat):**
```
Src/Functorium/Abstractions/Errors/ErrorCodeFactory.cs | 50 +++++++++++
Src/Functorium/Applications/Linq/FinTUtilites.cs      | 30 +++++++
2 files changed, 80 insertions(+)
```

### git branch - Branch Management

```bash
# Local branch list
git branch

# Including remote branches
git branch -a

# Remote branches only
git branch -r

# Check if a specific branch exists
git branch -r | grep "release/1.0"
```

## Git Commands Used in Release Note Automation

Now let's look specifically at the commands the automation system actually uses. It is important to understand at which stage of the workflow each command is needed and why.

### 1. Base Branch Decision

Release notes cover "changes since the last release", so the comparison baseline (Base Branch) must be determined first.

```bash
# Check for release branch existence
git branch -r | grep "origin/release/1.0"

# Find initial commit (for first deployment)
git rev-list --max-parents=0 HEAD
```

### 2. Collecting Per-Component Changes

Once the Base Branch is determined, changes after it are collected per component (folder).

```bash
# Change statistics for a specific folder between two branches
git diff --stat origin/release/1.0..HEAD -- Src/Functorium/

# Commit list for a specific folder between two branches
git log --oneline origin/release/1.0..HEAD -- Src/Functorium/
```

### 3. Breaking Changes Detection (API diff)

Catching Breaking Changes perfectly from the `!` notation in commit messages alone is difficult. Directly diffing Public API files in the `.api` folder provides more accurate detection.

```bash
# Check changes in the .api folder
git diff HEAD -- 'Src/*/.api/*.cs'

# Check only deleted lines (Breaking Changes candidates)
git diff HEAD -- 'Src/*/.api/*.cs' | grep "^-.*public"
```

### 4. Contributor Statistics

```bash
# Commits per contributor in a specific range
git shortlog -sn origin/release/1.0..HEAD -- Src/Functorium/
```

**Output example:**
```
    23  hhko
     5  contributor1
     2  contributor2
```

## Conventional Commits

Conventional Commits is a standard format for commit messages. For the release note automation system to automatically classify commits, commit messages must follow a consistent format that machines can parse. This is why Conventional Commits are needed.

### Basic Format

```txt
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Commit Types

| Type | Description | Release Note Classification |
|------|-------------|---------------------------|
| `feat` | New feature | New Features |
| `fix` | Bug fix | Bug Fixes |
| `docs` | Documentation change | (Usually omitted) |
| `style` | Code style change | (Omitted) |
| `refactor` | Refactoring | (Usually omitted) |
| `perf` | Performance improvement | Improvements |
| `test` | Add/modify tests | (Omitted) |
| `build` | Build system change | (Usually omitted) |
| `ci` | CI configuration change | (Omitted) |
| `chore` | Other changes | (Omitted) |

The most important types for release notes are `feat` and `fix`. Types like `docs`, `style`, and `test` have no direct impact on users and are usually omitted from release notes.

### Breaking Changes Notation

Breaking Changes are indicated in two ways.

**Method 1: Exclamation mark (!) after the type**
```
feat!: Change user authentication method
fix!(api): Fix response format
```

**Method 2: BREAKING CHANGE in the footer**
```
feat(api): Introduce new authentication system

BREAKING CHANGE: The existing token format has been changed.
Migration is required.
```

### Good Commit Message Examples

```bash
# New feature
feat(linq): Add TraverseSerial method and Activity Context utilities

# Bug fix
fix(build): Fix NuGet package icon path

# Refactoring
refactor(observability): Improve observability abstraction and structure

# Breaking Change
feat!(api): Change ErrorCodeFactory.Create method signature

BREAKING CHANGE: The errorMessage parameter is now required.
```

### Bad Commit Message Examples

```bash
# Too vague
fix: Bug fix
update: Update

# Missing type
Add user service

# No description
feat:
```

Such commit messages make automatic classification impossible, or even if classified, they fail to convey meaningful content in the release notes.

## Automation System's Commit Classification Logic

The release note automation system parses commit messages and automatically classifies them. Feature commits are detected by `feat(...)`, `feature(...)`, `add(...)` patterns, and Bug Fix commits by `fix(...)`, `bug(...)` patterns.

Breaking Changes detection uses two methods in parallel. Patterns like `feat!:`, `fix!:`, `BREAKING` in commit messages are checked as a supplement, but the more accurate method is directly analyzing the Git Diff of the `.api` folder. By finding deleted public classes, deleted public methods, changed method signatures, and changed type names directly from the diff, Breaking Changes are not missed even if the `!` is omitted from the commit message.

## Hands-on: Git Command Practice

### 1. Checking Commit History

```bash
# Clone the Functorium project
git clone https://github.com/hhko/Functorium.git
cd Functorium

# Check the last 10 commits
git log --oneline -10

# Commits for the Src/Functorium folder only
git log --oneline -10 -- Src/Functorium/
```

### 2. Checking Change Statistics

```bash
# Change statistics from initial commit to current
git diff --stat $(git rev-list --max-parents=0 HEAD)..HEAD -- Src/Functorium/
```

### 3. Checking API Changes

```bash
# Changes in the .api folder
git diff HEAD~10..HEAD -- 'Src/*/.api/*.cs'
```

## FAQ

### Q1: Can automation be used even if existing commit history does not follow Conventional Commits?
**A**: It can be used, but the accuracy of automatic classification will be lower. Commits without type prefixes like `feat` and `fix` are classified as "other" and require manual judgment in Phase 3. If you start applying Conventional Commits to future commits, the accuracy of automatic classification for new releases will gradually improve.

### Q2: Why is the `.api` folder's Git diff more accurate than commit messages for Breaking Changes detection?
**A**: If the `!` notation is omitted from a commit message, commit message-based detection fails. In contrast, the `.api` folder contains Public API definitions generated by PublicApiGenerator that are tracked by Git, so `git diff` can **objectively** detect **deleted classes and changed method signatures.** It catches actual API changes regardless of notation mistakes.

### Q3: What roles do `git log --oneline` and `git diff --stat` play in release note automation?
**A**: `git log --oneline` collects commit messages and is used for type-based classification such as `feat` and `fix`. `git diff --stat` summarizes the number of changed files and added/deleted lines, and is used to determine the statistics section and per-component change scale in the release notes.

Now that we understand the commit history and diff information provided by Git, let's take a deeper look at Claude Code's custom commands that leverage this data.
