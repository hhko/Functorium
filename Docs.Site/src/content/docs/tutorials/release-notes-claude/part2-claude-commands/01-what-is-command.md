---
title: "Custom Commands"
---

There are recurring patterns in development work. Every time you write release notes, you go through the process of checking Git logs, classifying commits, writing documents, and verifying them. Even if you delegate this work to Claude, it is still inefficient if you have to type the same instructions from scratch each time.

Using a cooking analogy, it is like recalling ingredient preparation methods and cooking sequences from memory each time. Just as opening a recipe book allows anyone to produce the same result in the same order, Claude Code's **custom commands** let you save complex task instructions as a Markdown file and execute them with a simple command. They are, in essence, "recipes" for development work.

## Command File Location

Command files are stored in the `.claude/commands/` folder at the project root.

```txt
Project Root/
├── .claude/
│   ├── commands/
│   │   ├── release-note.md    # /release-note command
│   │   ├── commit.md          # /commit command
│   │   ├── review.md          # /review command
│   │   └── test.md            # /test command
│   └── settings.json          # Claude Code settings
├── src/
└── ...
```

The filename becomes the command name. The extension is `.md`, and hyphens (`-`) are used instead of spaces.

| Filename | Execution Command |
|----------|------------------|
| `release-note.md` | `/release-note` |
| `commit.md` | `/commit` |
| `code-review.md` | `/code-review` |

## Why Use Commands?

**Repetitive tasks are reduced to a single line.** To manually write release notes, you would need to repeatedly give the same instructions each time: "Check the Git log", "Classify the commits", "Document the new features", "Check Breaking Changes", "Organize into Markdown". With commands, a single line `/release-note v1.2.0` automatically runs the entire process.

**Quality is consistent.** If all rules and criteria are specified in the command file, you get the same quality results every time.

```markdown
## Writing Rules

1. "Why this matters" section required for all features
2. Code examples must be executable
3. APIs must be verified against the Uber file
```

**Knowledge is shared across the entire team.** Committing command files to Git means the entire team uses the same workflow, new team members can start working immediately, and workflow improvement history is also tracked.

```bash
git add .claude/commands/release-note.md
git commit -m "feat(claude): Add release note automation command"
git push
```

**Incremental improvements are possible.** If a problem is found, just modify the command file. For example, adding "Why this matters" verification and API validation to the check items is as simple as modifying a few lines of Markdown.

## Basic Command File Structure

A command file consists of three parts: YAML frontmatter, a title, and the body.

```markdown
---
title: COMMAND-NAME
description: Command description
argument-hint: "<arg> Argument description"
---

# Command Title

## Section 1

Task instructions...

## Section 2

More instructions...
```

### 1. YAML Frontmatter

This is the metadata enclosed by `---` at the top of the file.

```yaml
---
title: RELEASE-NOTES
description: Automatically generates release notes
argument-hint: "<version> Release version (e.g., v1.2.0)"
---
```

| Field | Required | Description |
|-------|:--------:|-------------|
| `title` | Yes | Command name (for display) |
| `description` | Yes | Command description |
| `argument-hint` | No | Argument hint message |

### 2. Title

Write a Markdown title after the frontmatter:

```markdown
# Release Note Auto-Generation Rules
```

### 3. Body

These are the detailed instructions sent to Claude. Workflow steps, rules to follow, output formats, etc. can be freely written in Markdown.

## $ARGUMENTS Variable

Arguments passed during command execution are accessed via the `$ARGUMENTS` variable. When Claude executes the command, `$ARGUMENTS` is replaced with the actual argument value.

### Example 1: Version Parameter

**Command file (release-note.md):**
```markdown
---
title: RELEASE-NOTES
argument-hint: "<version> Release version"
---

# Release Note Generation

**Version:** $ARGUMENTS

Generate release notes for version $ARGUMENTS.
Output file: RELEASE-$ARGUMENTS.md
```

**Run:**
```bash
/release-note v1.2.0
```

**Prompt Claude receives:**
```markdown
# Release Note Generation

**Version:** v1.2.0

Generate release notes for version v1.2.0.
Output file: RELEASE-v1.2.0.md
```

### Example 2: File Path

**Command file (analyze.md):**
```markdown
---
title: ANALYZE
argument-hint: "<path> Folder path to analyze"
---

Analyze the $ARGUMENTS folder.
```

**Run:**
```bash
/analyze Src/Functorium
```

### Example 3: No Arguments

If no arguments are passed, `$ARGUMENTS` becomes an empty string. The command can include logic to check for the presence of arguments:

```markdown
## Version Parameter ($ARGUMENTS)

**When version is specified:** $ARGUMENTS

The version parameter is required. If not specified, output an error message and stop.
```

## How to Run Commands

### Running in Interactive Mode

```bash
claude
> /release-note v1.2.0
> /commit
> /review src/main.cs
```

Commands must start with a slash (`/`). Input without a slash is treated as a regular question.

### Auto-completion

In interactive mode, typing `/` displays the list of available commands:

```bash
> /
Available commands:
  /release-note - Automatically generates release notes
  /commit       - Commits according to Conventional Commits specification
  /review       - Performs a code review
```

## Simple Command Writing Examples

Now that you understand the structure of commands, let's get a feel for them through three simple examples.

### Example 1: Greeting Command

**File:** `.claude/commands/greet.md`
```markdown
---
title: GREET
description: Greets the user
argument-hint: "<name> Name"
---

# Greet

Greet "$ARGUMENTS" in a friendly and fun way.
Write in English.
```

**Run:**
```bash
> /greet John
```

### Example 2: Code Explanation Command

**File:** `.claude/commands/explain.md`
```markdown
---
title: EXPLAIN
description: Explains code
argument-hint: "<file> File to explain"
---

# Code Explanation

Explain the code in the $ARGUMENTS file.

## Explanation Format

1. **File Overview**: Purpose of this file
2. **Main Classes/Functions**: Role of each
3. **Flow**: Code execution flow
4. **Dependencies**: External dependencies
```

**Run:**
```bash
> /explain src/main.cs
```

### Example 3: Test Generation Command

**File:** `.claude/commands/test.md`
```markdown
---
title: TEST
description: Generates unit tests
argument-hint: "<class> Class to test"
---

# Test Generation

Generate unit tests for the $ARGUMENTS class.

## Rules

- Use xUnit
- Apply AAA pattern (Arrange-Act-Assert)
- Test all public methods
- Include edge cases
```

**Run:**
```bash
> /test UserService
```

## FAQ

### Q1: Are there rules for command file names?
**A**: The filename becomes the command name. The extension is `.md`, and hyphens (`-`) are used instead of spaces. For example, `release-note.md` runs as `/release-note` and `code-review.md` as `/code-review`. Case is not distinguished.

### Q2: Can a single command reference instructions from multiple files?
**A**: Yes. If you link to other Markdown files in the command body, Claude reads those documents as needed and follows the instructions. The `release-note.md` referencing per-Phase detail documents (`.release-notes/scripts/docs/phase*.md`) is a typical example of this pattern.

### Q3: How do you share command files with the team?
**A**: Commit the `.claude/commands/` folder to Git. Running `git add .claude/commands/release-note.md && git push` means team members can use the `/release-note` command immediately after `git pull`. Command improvement history is also tracked via Git history.

Now that you understand the basic concept of commands, let's look at the detailed syntax and effective prompt writing techniques for command files in the next section.
