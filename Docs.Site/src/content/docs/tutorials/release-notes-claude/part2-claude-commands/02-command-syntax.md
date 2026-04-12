---
title: "Command Syntax"
---

You understand the basic concept of commands, but what format should the file be written in? Is it simply Markdown, or are there special rules?

A command file consists of YAML frontmatter and a Markdown body. Understanding why each element is needed will help you make better decisions when designing your own commands.

## YAML Frontmatter

The frontmatter defines the metadata of the command file. It is written at the top of the file enclosed by `---`, and Claude Code reads this information to display auto-completion lists and help.

### Basic Format

```yaml
---
title: COMMAND-TITLE
description: Description of the command
argument-hint: "<arg> Description of the argument"
---
```

### Field Details

#### title (Required)

The display name of the command. Uppercase and hyphens are common. This value is used to identify the command in the Claude Code UI, so write it so the purpose of the command is apparent at a glance.

```yaml
title: RELEASE-NOTES
title: CODE-REVIEW
title: GENERATE-TEST
```

#### description (Required)

A brief description of what the command does. It is displayed in the auto-completion list when `/` is typed, serving as a criterion for team members to decide which command to select.

```yaml
description: Automatically generates release notes
description: Analyzes code and provides review opinions
```

#### argument-hint (Optional)

A hint message about the arguments. `<>` denotes required arguments and `[]` denotes optional arguments. With this hint, users immediately know what value to pass.

```yaml
# Single argument
argument-hint: "<version> Release version (e.g., v1.2.0)"

# Optional argument expression
argument-hint: "[topic] Optional topic filter"

# Multiple arguments example
argument-hint: "<source> <target> Source and target paths"
```

## Markdown Body Structure

The body after the frontmatter is the actual prompt sent to Claude. Since the structure of the body determines Claude's workflow, organizing it in a logical order is important.

### Recommended Structure

```markdown
---
(Frontmatter)
---

# Command Title

Command overview description

## Parameters

Parameter description and validation rules

## Workflow/Steps

Task steps to perform

## Rules/Guidelines

Rules to follow

## Output Format

Result format definition
```

The flow starts with an overview, validates parameters, executes the workflow, follows rules, and generates results according to the output format. Claude will perform tasks in this order.

### Example: Complete Command File

````markdown
---
title: API-DOC
description: Generates API documentation
argument-hint: "<class> Class name to document"
---

# API Documentation Generation

Generates API documentation for the $ARGUMENTS class.

## Parameter Validation

**Class name:** $ARGUMENTS

If the class name is not specified, output an error and stop.

## Workflow

1. **Class Search**: Find the class in the project
2. **Analysis**: Extract public members
3. **Documentation Generation**: Write in Markdown format

## Documentation Rules

- Document all public methods
- Include parameter and return value descriptions
- Include usage example code

## Output Format

```markdown
# {ClassName}

## Overview
{Class description}

## Methods
### {MethodName}
{Method description}

**Parameters:**
- `{param}`: {Description}

**Return Value:** {Description}

**Example:**
```csharp
{Code}
```
```
````

## Effective Prompt Writing Techniques

The command body is ultimately a prompt sent to Claude. How you write the prompt significantly affects the quality of the results. Here are five techniques that have been proven effective in practice.

### 1. Use Clear Directives

When instructing Claude on task order, it is important to explicitly say "in order" and use numbers. If you lump things together as "do A, B, C", Claude may arrange the order arbitrarily or skip some steps.

```markdown
# Good example
Execute the following steps **in order**:
1. First, perform A
2. Then perform B
3. Finally perform C

# Example to avoid
Do A, B, C.
```

### 2. Use Conditionals

Since situations may differ depending on the execution time, specifying behavior per condition allows Claude to respond appropriately. Branch logic based on argument presence is needed in almost every command.

```markdown
## Version Parameter ($ARGUMENTS)

**When version is specified:** $ARGUMENTS

The version parameter is required.

**When version is not specified:**
Output the following error message and stop:
> Please specify a version. Example: /release-note v1.2.0
```

### 3. Checklist Format

Using checklists in verification steps guides Claude to check each item one by one. It is effective for preventing omissions.

```markdown
## Verification Checklist

Check all of the following items:

- [ ] Is the frontmatter included?
- [ ] Are all required sections present?
- [ ] Have the code examples been verified?
- [ ] Have Breaking Changes been documented?
```

### 4. Using Tables

Tables are effective for showing the goals and outputs of multiple steps at a glance. Claude also interprets tabular information well.

```markdown
## Per-Phase Goals

| Phase | Goal | Output |
|-------|------|--------|
| 1 | Environment Verification | Base/Target Decision |
| 2 | Data Collection | .analysis-output/*.md |
| 3 | Commit Analysis | phase3-*.md |
| 4 | Document Writing | RELEASE-*.md |
| 5 | Validation | Validation Report |
```

### 5. Code Block Specification

Wrapping commands Claude needs to execute or results it needs to generate in code blocks improves accuracy. Adding language tags (bash, markdown, etc.) helps Claude better understand the context.

````markdown
## Commands to Execute

```bash
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

## Output Example

```markdown
# Analysis for Src/Functorium

## Change Summary
37 files changed
```
````

## Document Reference Methods

When a command becomes complex, it is difficult to fit all instructions in a single file. You can reference other documents to separate detailed information, and Claude reads the referenced documents as needed and follows the instructions.

### Relative Path Reference

```markdown
## Phase 1: Environment Verification

**Details**: [phase1-setup.md](.release-notes/scripts/docs/phase1-setup.md)

Follow the instructions in the above document to verify the environment.
```

### Reference Table

```markdown
## Reference Documents

| Phase | Document | Description |
|-------|----------|-------------|
| 1 | [phase1-setup.md](...) | Environment Verification |
| 2 | [phase2-collection.md](...) | Data Collection |
| 3 | [phase3-analysis.md](...) | Commit Analysis |
```

## Output Format Definition

Clearly defining the result format of a command ensures consistent output. Without a format definition, Claude may generate results in a different form each time, so format definition is essential especially for commands with file output.

### File Output

````markdown
## Output File

**Filename:** `.release-notes/RELEASE-$ARGUMENTS.md`

**Format:**
```markdown
---
title: Functorium $ARGUMENTS New Features
date: {today's date}
---

# Functorium Release $ARGUMENTS

## Overview
...
```
````

### Console Output

````markdown
## Completion Message

Upon completion, output in the following format:

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Release Note Generation Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Version: $ARGUMENTS
File: .release-notes/RELEASE-$ARGUMENTS.md

Statistics:
| Item | Value |
|------|-------|
| New Features | N |
| Bug Fixes | N |
| Breaking Changes | N |
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```
````

## Advanced Techniques

The basic syntax covered so far is sufficient for writing most commands. However, for complex workflows consisting of multiple steps, like release note automation, a few additional techniques are needed.

### 1. Saving Intermediate Results Per Step

In complex workflows, saving intermediate results of each step to files makes debugging easier and allows reuse as input for the next step.

```markdown
## Save Intermediate Results (Required)

After completing each Phase, save intermediate results:

- `.analysis-output/work/phase3-commit-analysis.md`
- `.analysis-output/work/phase3-feature-groups.md`

These files are used as input for the next Phase.
```

### 2. Error Handling

Predefining anticipated error situations allows Claude to respond appropriately.

```markdown
## Error Handling

Stop and notify the user in the following situations:

1. **No Base Branch**:
   > origin/release/1.0 branch does not exist.
   > For first deployment, analysis starts from the initial commit.

2. **No .NET SDK**:
   > .NET 10 SDK is not installed.
   > Please install it and try again.
```

### 3. Specifying Success Criteria

Clearly defining success criteria allows Claude to judge on its own whether the task is complete.

```markdown
## Success Criteria

The Phase is complete only when all of the following conditions are met:

- [ ] Frontmatter included
- [ ] All required sections included
- [ ] "Why this matters" section included for all major features
- [ ] APIs not in the Uber file: 0
```

## FAQ

### Q1: What is the difference between `<>` and `[]` in the frontmatter's `argument-hint`?
**A**: `<>` denotes required arguments and `[]` denotes optional arguments. For example, `<version>` is an argument that must be passed, and `[topic]` is an argument that can be omitted. This notation follows CLI tool conventions and is displayed to users in Claude Code's auto-completion list.

### Q2: Why is it important to explicitly state "execute in order" in the command body?
**A**: When Claude receives weak instructions ("do A, B, C"), it may arrange the order arbitrarily or skip some steps. **Numbering and explicitly stating "in order"** ensures Claude performs each step completely in the specified order. Order guarantee is essential especially in workflows with data dependencies between Phases.

### Q3: What problems occur if the output format is not defined?
**A**: Claude generates results in a different form each time. In commands with file output like release notes, the frontmatter structure, section order, and code block format may vary, breaking consistency. **It is recommended to explicitly define the format for both console and file output.**

### Q4: What is covered in the next section?
**A**: We will analyze the `release-note.md` command that actually applies this syntax. We will examine the design patterns used to design complex workflows, including the modularization pattern, checklist pattern, input/output specification pattern, and conditional processing pattern.

We have covered both basic and advanced command syntax techniques. In the next section, let's analyze the internal structure of the `release-note.md` command that applies this syntax in practice.
