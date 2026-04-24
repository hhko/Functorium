---
title: "AnalyzeAllComponents"
---

When dozens of commits have accumulated in a project, manually determining how much each component has changed is inefficient. Going through Git logs for each component, classifying commits, and compiling change statistics is time-consuming and prone to omissions. AnalyzeAllComponents.cs is a script that automates this data collection work. As the core of **Phase 2: Data Collection**, it systematically collects changes across all components and generates Markdown analysis files.

## File Location and Usage

```txt
.release-notes/scripts/AnalyzeAllComponents.cs
```

```bash
# Basic execution
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD

# First deployment (from initial commit)
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

## Script Structure

The script is organized in the order of first setting up packages, defining CLI options, and then executing the main logic.

### Package References and CLI Options

The script uses two packages, System.CommandLine and Spectre.Console, and receives two Options, `--base` and `--target`, for the comparison target branches.

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;
```

```csharp
var baseOption = new Option<string>("--base")
{
    Description = "Base branch for comparison"
};
baseOption.DefaultValueFactory = (_) => "origin/release/1.0";

var targetOption = new Option<string>("--target")
{
    Description = "Target branch for comparison"
};
targetOption.DefaultValueFactory = (_) => "origin/main";

var rootCommand = new RootCommand("Automated analysis of all components")
{
    baseOption,
    targetOption
};
```

The handler is set up asynchronously, passing parsed branch values to the main analysis function.

```csharp
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    await AnalyzeAllComponentsAsync(baseBranch, targetBranch);
    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();
```

## What Happens When the Script Runs

After CLI parsing, the script performs four steps in sequence: loading the component list, analyzing Git changes for each component, classifying commits, and generating the final summary.

### Step 1: Load Component List

First, the list of components to analyze is read from the configuration file. If the configuration file is missing, defaults (Functorium, Functorium.Testing, Docs) are used.

```csharp
var configFile = Path.Combine(scriptsDir, "config", "component-priority.json");
var components = await LoadComponentsAsync(configFile, gitRoot);

// Use defaults if configuration file is missing
if (components.Count == 0)
{
    components = new List<string>
    {
        "Src/Functorium",
        "Src/Functorium.Testing",
        "Docs"
    };
}
```

### Step 2: Git Analysis for Each Component

Once the component list is ready, changes are collected using Git commands for each component. `git diff --stat` provides change statistics and `git log --oneline` provides the full commit list.

```csharp
foreach (var component in components)
{
    // Change statistics
    var diffStat = await RunGitAsync($"diff --stat {baseBranch}..{targetBranch} -- {component}");

    // All commits
    var commits = await RunGitAsync($"log --oneline {baseBranch}..{targetBranch} -- {component}");

    // Classified commits (Feature, Bug Fix, Breaking Change)
    // Search only exact types according to Conventional Commits specification
    var featureCommits = await RunGitAsync($"log --grep=\"^feat\" --oneline ...");
    var bugFixCommits = await RunGitAsync($"log --grep=\"^fix\" --oneline ...");
    var breakingCommits = FilterBreakingChanges(commits);

    // Generate Markdown file
    await WriteAnalysisFileAsync(component, diffStat, commits, ...);
}
```

### Step 3: Commit Classification

Collected commits are classified according to the Conventional Commits specification. Feature commits are searched with the `^feat` pattern and Bug Fix commits with the `^fix` pattern from the Git log.

```csharp
// Feature commits - search with "^feat" pattern
var featResult = await RunGitAsync(
    $"log --grep=\"^feat\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"",
    gitRoot);

// Bug Fix commits - search with "^fix" pattern
var fixResult = await RunGitAsync(
    $"log --grep=\"^fix\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"",
    gitRoot);

// Breaking Change commits
// Method 1: ! after type (e.g., feat!:, fix!:)
// Method 2: BREAKING CHANGE keyword
var breakingPattern = new Regex(@"\b\w+!:", RegexOptions.Compiled);
var breakingCommits = allCommitsResult.Output
    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
    .Where(commit =>
        commit.Contains("BREAKING CHANGE", StringComparison.OrdinalIgnoreCase) ||
        breakingPattern.IsMatch(commit))
    .ToList();
```

> **Note**: Only exact commit types are searched according to the Conventional Commits specification.
> Earlier versions also included similar keywords like `feat|feature|add`, `fix|bug`,
> but this was improved to search only exact type prefixes for specification compliance.

Breaking Changes are detected in two ways: when an exclamation mark follows the type (`feat!:`, `fix!:`) and when the commit message contains the `BREAKING CHANGE` keyword.

### Step 4: Generate Analysis Summary

After all component analyses are complete, the overall results are compiled into a single summary file.

```csharp
var summaryContent = new StringBuilder();
summaryContent.AppendLine("# Analysis Summary");
summaryContent.AppendLine();
summaryContent.AppendLine($"Generated: {DateTime.Now}");
summaryContent.AppendLine($"Comparing: {baseBranch} -> {targetBranch}");
summaryContent.AppendLine();

foreach (var result in analysisResults)
{
    summaryContent.AppendLine($"## {result.Component}");
    summaryContent.AppendLine($"- Files: {result.FileCount}");
    summaryContent.AppendLine($"- Commits: {result.CommitCount}");
    summaryContent.AppendLine();
}

await File.WriteAllTextAsync(summaryPath, summaryContent.ToString());
```

## Output Files

The script generates two types of files: detailed analysis files per component and an overall summary file.

### Component Analysis Files

Files like `Functorium.md`, `Functorium.Testing.md` are generated for each component, containing change statistics, complete commit list, contributors, and classified commit information.

````markdown
# Analysis for Src/Functorium

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Change Summary

 Src/Functorium/Abstractions/Errors/ErrorFactory.cs | 45 +++++
 Src/Functorium/Applications/ElapsedTimeCalculator.cs   | 32 +++
 37 files changed, 1542 insertions(+), 89 deletions(-)

## All Commits

6b5ef99 feat(errors): Add ErrorFactory
853c918 feat(logging): Add Serilog integration
c5e604f fix(build): Fix NuGet package icon path
...

## Top Contributors

1. developer@example.com (15 commits)
2. other@example.com (4 commits)

## Categorized Commits

### Feature Commits

6b5ef99 feat(errors): Add ErrorFactory
853c918 feat(logging): Add Serilog integration

### Bug Fixes

c5e604f fix(build): Fix NuGet package icon path

### Breaking Changes

(none)
````

### Analysis Summary File

`analysis-summary.md` gathers all component analysis results in one place.

````markdown
# Analysis Summary

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Functorium
- Files: 37
- Commits: 19
- Output: Functorium.md

## Functorium.Testing
- Files: 18
- Commits: 13
- Output: Functorium.Testing.md

## Total
- Components: 2
- Total Files: 55
- Total Commits: 32
````

## Key Functions

Now that we have seen how the output files are generated, let's look at the core functions that make up the script.

### LoadComponentsAsync

Loads the component list from the configuration file, parsing JSON to return a list of paths.

```csharp
static async Task<List<string>> LoadComponentsAsync(string configFile, string gitRoot)
{
    if (!File.Exists(configFile))
        return new List<string>();

    var json = await File.ReadAllTextAsync(configFile);
    var config = JsonSerializer.Deserialize<ComponentConfig>(json);

    return config?.Components?.Select(c => c.Path).ToList()
        ?? new List<string>();
}
```

### RunGitAsync

Executes a Git command as an external process and returns the output.

```csharp
static async Task<string> RunGitAsync(string arguments)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.Start();
    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();

    return output.Trim();
}
```

### BranchExistsAsync

Checks whether the base branch actually exists before starting analysis.

```csharp
static async Task<bool> BranchExistsAsync(string branch, string gitRoot)
{
    var result = await RunGitAsync($"rev-parse --verify {branch}");
    return !string.IsNullOrEmpty(result);
}
```

## Console Output

The script uses Spectre.Console to visually display the execution process. It draws a header with Rule, shows configuration information with Table, and outputs progress at each step.

```csharp
// Header
AnsiConsole.Write(new Rule("[bold blue]Analyzing All Components[/]").RuleStyle("blue"));

// Information table
var infoTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
infoTable.AddRow("[white]Base Branch[/]", $"[cyan]{baseBranch}[/]");
infoTable.AddRow("[white]Target Branch[/]", $"[cyan]{targetBranch}[/]");
AnsiConsole.Write(infoTable);

// Step-by-step progress
AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Loading components...[/]");
AnsiConsole.MarkupLine($"   [green]Found[/] {component}");

// Show analysis progress with spinner
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync($"Analyzing {component}...", async ctx => { ... });
```

The result table shows file counts and commit counts for each component.

```csharp
var resultTable = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("Component")
    .AddColumn("Files")
    .AddColumn("Commits");

foreach (var result in analysisResults)
{
    resultTable.AddRow(result.Component, result.FileCount.ToString(), result.CommitCount.ToString());
}

AnsiConsole.Write(resultTable);
```

## Error Handling

If the base branch does not exist, a guidance message is displayed with a Panel, along with the command to use for first deployment.

```csharp
var baseBranchExists = await BranchExistsAsync(baseBranch, gitRoot);
if (!baseBranchExists)
{
    var panel = new Panel(
        $"[yellow]Base branch[/] [cyan]{baseBranch}[/] [yellow]does not exist.[/]\n\n" +
        $"[bold]For first deployment:[/]\n" +
        $"dotnet AnalyzeAllComponents.cs --base $(git rev-list --max-parents=0 HEAD) --target HEAD")
    {
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Yellow)
    };

    AnsiConsole.Write(panel);
    return;
}
```

## FAQ

### Q1: What happens if the `component-priority.json` configuration file is missing?
**A**: If the configuration file is missing, the defaults of `Src/Functorium`, `Src/Functorium.Testing`, and `Docs` are used for analysis. If your project has other components or you want to customize analysis targets, you need to create the configuration file.

### Q2: How do the `feat!:` pattern and `BREAKING CHANGE` keyword differ in Breaking Change detection?
**A**: Both are part of the Conventional Commits specification for indicating Breaking Changes. `feat!:` is a **shorthand notation** with an exclamation mark after the type, and `BREAKING CHANGE` is an **explicit notation** including the keyword in the commit body or message. The script searches for both patterns to ensure complete detection.

### Q3: Why does the `RunGitAsync` function use an external process?
**A**: .NET does not have a built-in library for directly manipulating Git, so the `git` command is executed as an external process (`Process` class) and its standard output is captured. Libraries like `libgit2sharp` exist, but the external process approach was chosen to maintain the simplicity of File-based Apps while leveraging all Git CLI capabilities.

The data collected by AnalyzeAllComponents.cs is used as the foundational material for commit analysis and feature grouping in Phase 3. However, commit logs alone cannot tell us how the actual API has changed. The next section examines ExtractApiChanges.cs, which extracts Public APIs directly from code to ensure API accuracy.
