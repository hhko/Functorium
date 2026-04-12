---
title: "Introduction to Claude Code"
---

Every time you write release notes, you repeat the same process. Check the Git log, classify commits, find Breaking Changes, and organize everything into a Markdown document. The work itself is not difficult, but doing it manually each time takes time and inevitably leads to missed items.

**Claude Code** is an AI-powered CLI tool developed by Anthropic that allows you to interact directly with Claude AI from the terminal. It can read code, edit files, and execute commands like Git and dotnet. But the real power of this tool lies in **custom commands.** If you predefine complex task instructions as a Markdown file, you can execute the entire workflow with a single command. Think of it like opening a recipe book and following along.

Instead of repeatedly saying "check the git log", "classify the commits", "write the document" each time, a single line `/release-note v1.2.0` is all you need. Because there is a recipe, you always get consistent quality results, and if you commit that recipe to Git, the entire team shares the same workflow.

## Claude Code Installation

### Installation via npm

```bash
npm install -g @anthropic-ai/claude-code
```

### Verify Installation

```bash
claude --version
```

### Initial Setup

You need to set up the Anthropic API key on first run:

```bash
claude
# Follow the prompts to enter the API key
```

## Basic Usage

### Interactive Mode

Enter `claude` in the terminal to enter interactive mode:

```bash
claude
> Explain the structure of this project
```

### Single Question

```bash
claude "Check the dependencies in package.json"
```

### File Reference

You can ask questions while referencing specific files:

```bash
claude @src/main.cs "Explain the role of this file"
```

Claude Code automatically selects the appropriate built-in tool depending on the request. If asked to find files, it uses Glob; to search for patterns in code, Grep; and when Git or dotnet commands are needed, Bash. The table below shows the main built-in tools:

| Tool | Purpose | Example |
|------|---------|---------|
| Read | Read files | Check source code |
| Write | Write files | Create new files |
| Edit | Edit files | Modify code |
| Bash | Execute commands | `git`, `dotnet`, etc. |
| Glob | File search | Find files by pattern |
| Grep | Content search | Find patterns in code |
| Task | Sub-tasks | Split complex tasks |

## Custom Commands

Let's look at custom commands, the core of this tutorial. Command files are stored as Markdown files in the `.claude/commands/` folder at the project root, and the filename becomes the command name.

```txt
Project Root/
├── .claude/
│   └── commands/
│       ├── release-note.md    # /release-note command
│       ├── commit.md          # /commit command
│       └── my-command.md      # /my-command command
└── ...
```

### Command File Structure

A command file consists of YAML frontmatter and a Markdown body. The frontmatter contains the command's metadata, and the body contains the task instructions to be sent to Claude.

```markdown
---
title: MY-COMMAND
description: Description of this command
argument-hint: "<arg> Argument description"
---

# Command Name

This is the prompt content sent to Claude when the command is executed.

## Task Instructions

1. First step
2. Second step
3. Third step
```

### Running Commands

In interactive mode, enter a command starting with a slash (/):

```bash
claude
> /release-note v1.2.0
> /commit
> /my-command argument
```

### $ARGUMENTS Variable

Arguments passed to a command can be accessed via the `$ARGUMENTS` variable:

```markdown
---
title: GREET
description: Output a greeting message
argument-hint: "<name> Name"
---

# Greet

Please greet "$ARGUMENTS" in a friendly manner.
```

**Run:**
```bash
> /greet John
# Result: A response in the form of "Hello, John!"
```

## Release Note Command Example

Let's preview the core structure of the `release-note` command covered in this tutorial. The entire workflow is divided into 5 steps, executed sequentially from environment verification to final validation.

```markdown
---
title: RELEASE-NOTES
description: Automatically generates release notes
argument-hint: "<version> Release version (e.g., v1.2.0)"
---

# Release Note Auto-Generation Rules

## Version Parameter ($ARGUMENTS)

**When version is specified:** $ARGUMENTS

## Automation Workflow

| Phase | Goal |
|-------|------|
| 1 | Environment Verification |
| 2 | Data Collection |
| 3 | Commit Analysis |
| 4 | Document Writing |
| 5 | Validation |

## Phase 1: Environment Verification

**Prerequisite Checks**:
```bash
git status
dotnet --version
```
...
```

**Run:**
```bash
> /release-note v1.2.0
```

Claude reads this prompt and executes the 5-step workflow sequentially.

## What Commands Change

Let's return to the recipe analogy. There is a big difference between recalling ingredients and cooking steps from memory each time, and opening a recipe and following along. Commands are the same.

**Repetitive tasks are automated.** All the steps needed to write release notes (checking Git logs, classifying changes, writing documents, validation) can be run with a single line `/release-note v1.2.0`. There is no need to repeat the same instructions each time.

**Quality is consistent.** If all rules and criteria are specified in the command file, the same level of results is obtained regardless of who runs it. Missing "Why this matters" sections or overlooked Breaking Changes are reduced.

**The entire team shares the same workflow.** Committing command files to Git means new team members can immediately use the same automation.

```bash
git add .claude/commands/release-note.md
git commit -m "feat(claude): Add release note automation command"
```

**Incremental improvements are easy.** If a problem is found, just modify the command file. Adding verification items or reflecting new rules is as easy as modifying a line of code.

## Claude Code Configuration

### .claude/settings.json

Per-project Claude Code configuration:

```json
{
  "permissions": {
    "allow": [
      "Bash(git:*)",
      "Bash(dotnet:*)",
      "Read",
      "Write",
      "Edit"
    ]
  }
}
```

### CLAUDE.md

Create a `CLAUDE.md` file at the project root to provide context to Claude:

```markdown
# Project Guide

## Commit Rules
When committing, follow the rules in `.claude/commands/commit.md`.

## Code Style
- Use C# 10 syntax
- Enable nullable reference types
```

## FAQ

### Q1: What is the difference between Claude Code's custom commands and regular conversations?
**A**: In regular conversations, you must repeatedly type the same instructions each time, but **custom commands** save complex task instructions as a Markdown file and run them with a single line like `/release-note v1.2.0`. Because the rules and verification criteria are specified in the command file, anyone who runs it gets consistent quality results.

### Q2: Can the `$ARGUMENTS` variable accept multiple arguments?
**A**: `$ARGUMENTS` receives all arguments passed to the command as a single string. Running `/release-note v1.2.0` makes `$ARGUMENTS` equal to `v1.2.0`. If multiple values are passed, they become a single space-separated string, so the command body must provide guidance on how to parse them.

### Q3: Are command file changes reflected immediately?
**A**: Yes. Command files are read at execution time, so if you modify `.claude/commands/release-note.md`, the changes take effect from the next `/release-note` run. Committing to Git means the entire team shares the same update.

Now that we understand what Claude Code is and how it works, let's next look at the basics of Git, the data source for automation.
