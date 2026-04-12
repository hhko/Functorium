---
title: "Spectre.Console Package"
---

If an automation script only outputs "Processing... Done: 10 files" with `Console.WriteLine`, it is difficult to understand what is happening at which step. This problem gets worse as the number of components increases and analysis time grows. Spectre.Console is a UI library that creates **console output developers actually want to read.** Tables, progress indicators, color highlighting, and more make the script execution status visible at a glance.

## Package Installation

```csharp
#:package Spectre.Console@0.54.0
```

## Basic Output: MarkupLine

The most basic feature is text output with color and style applied. Specify the color or style inside square brackets and close with `[/]`.

```csharp
using Spectre.Console;

// Colors
AnsiConsole.MarkupLine("[red]Error:[/] Something went wrong");
AnsiConsole.MarkupLine("[green]Success:[/] Operation completed");
AnsiConsole.MarkupLine("[yellow]Warning:[/] Please check");
AnsiConsole.MarkupLine("[blue]Info:[/] Processing started");

// Styles
AnsiConsole.MarkupLine("[bold]Bold text[/]");
AnsiConsole.MarkupLine("[dim]Dimmed text[/]");
AnsiConsole.MarkupLine("[italic]Italic text[/]");
AnsiConsole.MarkupLine("[underline]Underlined text[/]");

// Combinations
AnsiConsole.MarkupLine("[bold red]Critical Error![/]");
AnsiConsole.MarkupLine("[bold green]✓[/] [dim]Task completed[/]");
```

Available main colors include `[red]`, `[green]`, `[yellow]`, `[blue]`, `[cyan]`, `[magenta]`, `[grey]`, `[white]`, etc.

## Table: For Structuring Analysis Results

When structured data needs to be shown in rows and columns, like analysis results for multiple components, use Table. You can compare file counts, commit counts, and status of each component at a glance.

```csharp
using Spectre.Console;

var table = new Table();

// Border style
table.Border(TableBorder.Rounded);
table.BorderColor(Color.Grey);

// Add columns
table.AddColumn("Name");
table.AddColumn("Status");
table.AddColumn(new TableColumn("Time").RightAligned());

// Add rows
table.AddRow("App.cs", "[green]Done[/]", "0.3s");
table.AddRow("Config.cs", "[green]Done[/]", "0.1s");
table.AddRow("Program.cs", "[yellow]Processing[/]", "-");

// Output
AnsiConsole.Write(table);
```

Output:
```txt
╭───────────┬────────────┬──────╮
│ Name      │ Status     │ Time │
├───────────┼────────────┼──────┤
│ App.cs    │ Done       │ 0.3s │
│ Config.cs │ Done       │ 0.1s │
│ Program.cs│ Processing │ -    │
╰───────────┴────────────┴──────╯
```

## Rule: For Visually Separating Sections

When script output is divided into multiple steps, Rule clearly marks section boundaries. Placing a divider between "Analysis Start" and "Analysis Complete" immediately shows where one step begins and ends.

```csharp
using Spectre.Console;

// Basic Rule
AnsiConsole.Write(new Rule("[bold blue]Analysis Results[/]"));

// With style
AnsiConsole.Write(new Rule("[bold green]Success[/]").RuleStyle("green"));

// Left-aligned
AnsiConsole.Write(new Rule("[yellow]Warning[/]").LeftJustified());
```

Output:
```txt
─────────────────── Analysis Results ───────────────────
```

## Panel: For Highlighting Important Information

For content that users must check, like error messages or configuration information, place it in a box to make it stand out.

```csharp
using Spectre.Console;

var panel = new Panel("This is the content inside the panel.")
{
    Header = new PanelHeader("[bold]Information[/]"),
    Border = BoxBorder.Rounded,
    BorderStyle = new Style(Color.Blue),
    Padding = new Padding(2, 1)
};

AnsiConsole.Write(panel);
```

Output:
```txt
╭─────────── Information ───────────╮
│                                   │
│  This is the content inside the   │
│  panel.                           │
│                                   │
╰───────────────────────────────────╯
```

## Status: For Showing Progress of Long-Running Tasks

For time-consuming tasks like project builds or Git analysis, show "what is currently in progress" with a spinner. The status message can be updated as the task phase changes.

```csharp
using Spectre.Console;

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("cyan"))
    .StartAsync("Processing...", async ctx =>
    {
        // Task 1
        ctx.Status("Loading files...");
        await Task.Delay(1000);

        // Task 2
        ctx.Status("Analyzing...");
        await Task.Delay(1000);

        // Task 3
        ctx.Status("Generating report...");
        await Task.Delay(1000);
    });

AnsiConsole.MarkupLine("[green]Done![/]");
```

Various spinner types are available, including `Dots`, `Line`(-\\|/), `Star`, `Arrow`, and more.

## Progress: For Tracking Multiple Tasks Simultaneously

Use when processing multiple components in parallel and showing progress for each.

```csharp
using Spectre.Console;

await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        var task1 = ctx.AddTask("[green]Downloading[/]");
        var task2 = ctx.AddTask("[blue]Installing[/]");

        while (!ctx.IsFinished)
        {
            task1.Increment(1.5);
            task2.Increment(0.5);
            await Task.Delay(50);
        }
    });
```

Output:
```txt
Downloading [████████████████████] 100%
Installing  [██████████──────────]  50%
```

## Practical Example: Console Output of AnalyzeAllComponents.cs

Let's see how these elements are combined in release note automation. Rule separates sections, Table shows configuration information, MarkupLine shows step-by-step progress, Status shows analysis work, and finally a result Table is output.

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Threading.Tasks;
using Spectre.Console;

// Header
AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[bold blue]Analyzing All Components[/]").RuleStyle("blue"));
AnsiConsole.WriteLine();

// Information table
var infoTable = new Table()
    .Border(TableBorder.Rounded)
    .BorderColor(Color.Grey);

infoTable.AddColumn(new TableColumn("[grey]Property[/]").NoWrap());
infoTable.AddColumn(new TableColumn("[grey]Value[/]"));
infoTable.AddRow("[white]Config[/]", "[dim]config/component-priority.json[/]");
infoTable.AddRow("[white]Output[/]", "[dim].analysis-output[/]");
infoTable.AddRow("[white]Base Branch[/]", "[cyan]origin/release/1.0[/]");
infoTable.AddRow("[white]Target Branch[/]", "[cyan]HEAD[/]");

AnsiConsole.Write(infoTable);
AnsiConsole.WriteLine();

// Step-by-step progress
AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Loading components...[/]");
AnsiConsole.MarkupLine("   [green]Found[/] Src/Functorium");
AnsiConsole.MarkupLine("   [green]Found[/] Src/Functorium.Testing");
AnsiConsole.MarkupLine("   [dim]Total: 2 components[/]");
AnsiConsole.WriteLine();

// Show analysis progress with spinner
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("cyan"))
    .StartAsync("Analyzing components...", async ctx =>
    {
        await Task.Delay(2000);  // Actual analysis work
    });

// Result table
AnsiConsole.WriteLine();
var resultTable = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("Component")
    .AddColumn("Files")
    .AddColumn("Commits")
    .AddColumn("Status");

resultTable.AddRow("Functorium", "31", "19", "[green]✓[/]");
resultTable.AddRow("Functorium.Testing", "18", "13", "[green]✓[/]");

AnsiConsole.Write(resultTable);
AnsiConsole.WriteLine();

// Completion message
AnsiConsole.Write(new Rule("[bold green]Analysis Complete[/]").RuleStyle("green"));
```

## Frequently Used Patterns

Here is a summary of patterns used repeatedly across release note scripts.

For step-by-step progress display:

```csharp
AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Loading...[/]");
AnsiConsole.MarkupLine("   [green]✓[/] File loaded");
AnsiConsole.MarkupLine("   [green]✓[/] Config parsed");
AnsiConsole.WriteLine();

AnsiConsole.MarkupLine("[bold]Step 2[/] [dim]Processing...[/]");
```

Success, failure, and warning messages:

```csharp
// Success
AnsiConsole.MarkupLine("[green]✓[/] Operation completed successfully");

// Failure
AnsiConsole.MarkupLine("[red]✗[/] Operation failed: {0}", errorMessage);

// Warning
AnsiConsole.MarkupLine("[yellow]⚠[/] Warning: {0}", warningMessage);
```

Highlighting information requiring user attention with Panel:

```csharp
var panel = new Panel($"[yellow]Base branch[/] [cyan]{baseBranch}[/] [yellow]does not exist.[/]")
{
    Border = BoxBorder.Rounded,
    BorderStyle = new Style(Color.Yellow),
    Padding = new Padding(2, 1)
};
AnsiConsole.Write(panel);
```

## FAQ

### Q1: Why use Spectre.Console instead of `Console.WriteLine`?
**A**: `Console.WriteLine` only outputs plain text, making it difficult to compare analysis results of multiple components at a glance. Spectre.Console provides tables, color highlighting, progress indicators, dividers, and more, **showing script execution status visually and transparently.** The difference is especially significant in long-running tasks when understanding what step you are currently at.

### Q2: When should `Status` and `Progress` each be used?
**A**: `Status` is suitable for displaying a spinner and status message when a single task is in progress, updating the message as phases change. `Progress` is used for **simultaneously tracking the progress of multiple tasks**, showing percentage progress bars for each task. In release note scripts, `Status` is used for single component analysis and `Progress` for parallel processing of multiple components.

### Q3: How do you output literal square brackets in Spectre.Console's markup syntax?
**A**: Spectre.Console interprets `[` and `]` as markup tags, so to output literal brackets, escape them with `[[` and `]]`. For example, `AnsiConsole.MarkupLine("Array: [[0]]")` outputs `Array: [0]`.

Thanks to Spectre.Console, the execution process of automation scripts becomes transparent. Starting from the next section, we will analyze the actual scripts utilizing these two packages one by one.
