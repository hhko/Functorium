---
title: "Introduction to commit.md"
---

The release note automation system parses commit messages to classify features. `feat(api): Add new endpoint` is automatically classified into the "New Features" section, `fix(auth): Fix token expiration error` into the "Bug Fixes" section, and `feat!: Change API response format` into the "Breaking Changes" section. For this automatic classification to work, commit messages must follow a consistent format. If formats are inconsistent, parsing becomes impossible and the premise of release note automation collapses.

The `commit.md` command solves this problem. It instructs Claude to write commit messages according to the Conventional Commits specification, preventing humans from accidentally violating the format. In other words, `commit.md` is the **foundation** for `release-note.md` to function properly.

The file is located at `.claude/commands/commit.md`.

## Frontmatter

```yaml
---
title: COMMIT
description: Commits changes according to the Conventional Commits specification.
argument-hint: "[topic] Pass a topic to select and commit only files related to that topic"
---
```

Wrapping `[topic]` in square brackets in the argument-hint indicates it is an optional argument. You can run `/commit` without a topic, or specify a specific topic like `/commit build`.

## Conventional Commits Format

commit.md follows the Conventional Commits specification.

```txt
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Example

```txt
feat(calculator): Implement division feature

- Add Divide method
- Include division by zero exception handling

Closes #42
```

## Commit Types

It is important to understand which commit type maps to which section of the release notes. `feat` and `fix` have direct impact on users and are included in the release notes, while types like `docs`, `style`, and `test` are internal changes that are usually omitted.

| Type | Description | Release Note Classification |
|------|-------------|---------------------------|
| `feat` | New feature added | New Features |
| `fix` | Bug fix | Bug Fixes |
| `docs` | Documentation change | (Usually omitted) |
| `style` | Code formatting | (Omitted) |
| `refactor` | Refactoring | (Usually omitted) |
| `perf` | Performance improvement | Improvements |
| `test` | Add/modify tests | (Omitted) |
| `build` | Build system change | (Usually omitted) |
| `ci` | CI configuration change | (Omitted) |
| `chore` | Other changes | (Omitted) |

## Topic Parameter

One of the key features of the commit command is **Topic filtering.** It is common to modify multiple types of files in a single work session, and putting all changes into a single commit makes the commit history messy.

### Without Topic

Running `/commit` without a topic causes Claude to analyze all changes, separate them into logical units, and create multiple commits.

```bash
/commit
```

For example, if `UserService.cs`, `UserServiceTests.cs`, `README.md`, and `.gitignore` are all changed, Claude separates feature changes, tests, documentation, and configuration files and creates individual commits like `feat(user): Add user service`, `test(user): Add user service tests`, `docs: Update README`, `chore: Update .gitignore`.

### With Topic

Specifying a particular topic causes Claude to select only files related to that topic and create a single commit.

```bash
/commit build
```

Among the changed files, `Build-Local.ps1` and `Directory.Build.props` are selected as build-related, while `README.md` and `UserService.cs` are excluded as unrelated to build. As a result, a commit `feat(build): Improve build configuration` containing only build-related files is created, and the remaining files stay in the unstaged state.

Topic selection is done by comprehensively evaluating keywords in filenames, relevance to file contents, keywords in directory paths, and the subject of the changes.

## Commit Message Writing Rules

Each part of the commit message has rules to follow.

**The title (first line)** should be within 72 characters, written in imperative form ("add" not "added"). No period is used.

**The body (optional)** is separated from the title by a blank line and explains "what" and "why" the change was made. Line breaks every 72 characters are recommended.

**The footer (optional)** is used for Breaking Change information or related issue references (`Closes #123`).

## Commit Message Examples

### Adding a New Feature

```txt
feat(calculator): Implement division feature

- Add Divide method
- Include division by zero exception handling
```

### Bug Fix

```txt
fix(auth): Fix auto-refresh failure on token expiration

Fixed infinite loop issue when attempting refresh
with an expired refresh token

Closes #42
```

### Breaking Change

```txt
feat!: Change API response format

BREAKING CHANGE: The response JSON structure has been changed.
```

### Refactoring

```txt
refactor: Extract common configuration to test fixture

Moved duplicated initialization code from each test class
to BaseTestFixture
```

## Commit Conditions and Prohibitions

When code is changed, all tests must pass before committing, all compiler warnings must be resolved, and it must be a single logical unit of work.

There are also things that must never be included in commit messages: Claude/AI generation-related messages (`Co-Authored-By: Claude`, etc.), emojis, and commits while there are test failures or warnings.

## Commit Procedure

### Basic Procedure (Without Topic)

```bash
# 1. Check changes
git status

# 2. Review change contents
git diff

# 3. Check recent commit style
git log --oneline -5

# 4. Stage and commit in logical units
git add <files>
git commit -m "type(scope): description"
```

### Procedure With Topic

```bash
# 1. Check changes
git status
git diff

# 2. Select topic-related files
# Example: /commit build

# 3. Stage only selected files
git add Build-Local.ps1 Directory.Build.props

# 4. Create single commit
git commit -m "feat(build): Improve build configuration"

# 5. Verify (topic-unrelated files remain unstaged)
git status
```

## Completion Message Format

```txt
Commit Complete

Commit Information:
  - Type: feat
  - Message: Implement division feature
  - Changed Files: 3
```

## Relationship with release-note.md

```txt
commit.md                            release-note.md
    │                                      │
    │  Conventional Commits format         │
    │  ─────────────────────────▶          │
    │                                      │
    │  Commit history                      │
    │  ─────────────────────────▶          │
    │                                      │
    ▼                                      ▼
Consistent commit messages     ────▶   Auto-classification and release note generation
```

This diagram illustrates the relationship between the two commands well. The consistent commit messages generated by `commit.md` are the data consumed by `release-note.md`'s auto-classification engine. Only when a commit history following the Conventional Commits format accumulates can `release-note.md` accurately classify commits into "New Features", "Bug Fixes", and "Breaking Changes". The two commands exist independently, but the full value of the automation system is realized when they are used together.

## FAQ

### Q1: Does running `/commit` without a topic put all changes into a single commit?
**A**: No. Running `/commit` without a topic causes Claude to analyze all changes and **separate them into logical units to create multiple commits.** For example, it creates separate commits for feature changes, tests, documentation, and configuration files.

### Q2: Why should `commit.md` and `release-note.md` be used together?
**A**: The consistent Conventional Commits format commit messages generated by `commit.md` are the data consumed by `release-note.md`'s auto-classification engine. Formats like `feat(api):` must be followed for Phase 3 to accurately classify commits into "New Features", "Bug Fixes", and "Breaking Changes". The two commands have a **producer-consumer relationship.**

### Q3: Why should AI-generated messages like `Co-Authored-By: Claude` not be included in commit messages?
**A**: They become noise when parsing commit messages in release note automation. Commits should be classified solely by the Conventional Commits format of `type(scope): description`, and including unnecessary metadata reduces parsing accuracy and makes the commit history messy.

Based on the four topics covered in Part 2 (command concept, syntax, release-note.md structure, commit.md structure), the next Part will examine the actual operation of the 5-step workflow one by one.
