#!/usr/bin/env dotnet

// .NET 10 File-based Program - Analyze All Components
// Automated analysis of all components based on configuration
// Usage: dotnet AnalyzeAllComponents.cs [--base <branch>] [--target <branch>]

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

var baseOption = new Option<string>("--base") { Description = "Base branch for comparison" };
baseOption.DefaultValueFactory = (_) => "origin/release/1.0";
var targetOption = new Option<string>("--target") { Description = "Target branch for comparison" };
targetOption.DefaultValueFactory = (_) => "origin/main";

var rootCommand = new RootCommand("Automated analysis of all components based on configuration")
{
    baseOption,
    targetOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    await AnalyzeAllComponentsAsync(baseBranch, targetBranch);
    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();

// Main logic
static async Task AnalyzeAllComponentsAsync(string baseBranch, string targetBranch)
{
    var startTime = DateTime.Now;

    // Get paths
    var toolsDir = Directory.GetCurrentDirectory();
    var configFile = Path.Combine(toolsDir, "Config", "component-priority.json");
    var analysisDir = Path.Combine(toolsDir, ".analysis-output");

    // Get git root
    var gitRoot = await GetGitRootAsync() ?? Path.GetFullPath(Path.Combine(toolsDir, "..", ".."));
    gitRoot = gitRoot.Replace('\\', '/');

    // Calculate relative paths for display
    var relativeConfigFile = Path.GetRelativePath(toolsDir, configFile);
    var relativeAnalysisDir = Path.GetRelativePath(toolsDir, analysisDir);

    // Header
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[bold blue]Analyzing All Components[/]").RuleStyle("blue"));
    AnsiConsole.WriteLine();

    // Info table
    var infoTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
    infoTable.AddColumn(new TableColumn("[grey]Property[/]").NoWrap());
    infoTable.AddColumn(new TableColumn("[grey]Value[/]"));
    infoTable.AddRow("[white]Config[/]", $"[dim]{relativeConfigFile}[/]");
    infoTable.AddRow("[white]Output[/]", $"[dim]{relativeAnalysisDir}[/]");
    infoTable.AddRow("[white]Base Branch[/]", $"[cyan]{baseBranch}[/]");
    infoTable.AddRow("[white]Target Branch[/]", $"[cyan]{targetBranch}[/]");
    AnsiConsole.Write(infoTable);
    AnsiConsole.WriteLine();

    // Validate base branch exists
    var baseBranchExists = await BranchExistsAsync(baseBranch, gitRoot);
    if (!baseBranchExists)
    {
        AnsiConsole.Write(new Rule("[bold yellow]Base Branch Not Found[/]").RuleStyle("yellow"));
        AnsiConsole.WriteLine();

        var warningPanel = new Panel(
            $"[yellow]Base branch[/] [cyan]{baseBranch}[/] [yellow]does not exist.[/]\n\n" +
            $"This is likely your [bold]first deployment[/] or the release branch hasn't been created yet.\n\n" +
            $"[bold white]For first deployment:[/]\n" +
            $"1. Create the release branch:\n" +
            $"   [dim]git checkout -b release/1.0[/]\n" +
            $"   [dim]git push -u origin release/1.0[/]\n\n" +
            $"2. Then run analysis comparing initial commit to current:\n" +
            $"   [dim]dotnet AnalyzeAllComponents.cs --base $(git rev-list --max-parents=0 HEAD) --target HEAD[/]\n\n" +
            $"[bold white]Or specify a different base branch:[/]\n" +
            $"   [dim]dotnet AnalyzeAllComponents.cs --base origin/main --target HEAD[/]\n" +
            $"   [dim]dotnet AnalyzeAllComponents.cs --base <commit-hash> --target HEAD[/]")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(warningPanel);
        AnsiConsole.WriteLine();

        return;
    }

    // Ensure analysis directory exists
    if (Directory.Exists(analysisDir))
    {
        Directory.Delete(analysisDir, true);
    }
    Directory.CreateDirectory(analysisDir);

    // Step 1: Load components from config
    AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Loading components from config...[/]");

    var components = await LoadComponentsAsync(configFile, gitRoot);

    if (components.Count == 0)
    {
        AnsiConsole.MarkupLine("   [yellow]No components found, using fallback[/]");
        components = new List<string> { "Src/Functorium", "Src/Functorium.Testing", "Docs" };
    }

    foreach (var component in components)
    {
        AnsiConsole.MarkupLine($"   [green]Found[/] {component}");
    }
    AnsiConsole.MarkupLine($"   [dim]Total: {components.Count} components[/]");
    AnsiConsole.WriteLine();

    // Step 2: Analyze components
    AnsiConsole.MarkupLine("[bold]Step 2[/] [dim]Analyzing components...[/]");
    AnsiConsole.WriteLine();

    var analysisResults = new List<ComponentAnalysisResult>();
    var analysisStart = DateTime.Now;

    foreach (var (component, index) in components.Select((c, i) => (c, i)))
    {
        var componentName = GetSafeFilename(component);
        var outputFile = Path.Combine(analysisDir, $"{componentName}.md");

        var prefix = $"[[{index + 1}/{components.Count}]] [cyan]{component}[/]";

        ComponentAnalysisResult result = null!;

        // Show spinner while analyzing
        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync($"   {prefix} [dim]Analyzing...[/]", async ctx =>
            {
                result = await AnalyzeComponentAsync(component, baseBranch, targetBranch, gitRoot, outputFile);
            });

        analysisResults.Add(result);

        if (result.HasChanges)
        {
            AnsiConsole.MarkupLine($"   {prefix} [green]OK[/] {result.FileChanges} files, {result.CommitCount} commits");
        }
        else
        {
            AnsiConsole.MarkupLine($"   {prefix} [yellow]SKIP[/] no changes");
        }
    }

    var analysisTime = (DateTime.Now - analysisStart).TotalSeconds;
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"   [dim]Completed in {Math.Round(analysisTime, 2)}s[/]");
    AnsiConsole.WriteLine();

    // Step 3: Generate summary
    AnsiConsole.MarkupLine("[bold]Step 3[/] [dim]Generating summary report...[/]");

    var summaryStart = DateTime.Now;
    var summaryFile = Path.Combine(analysisDir, "analysis-summary.md");

    var summaryContent = new StringBuilder();
    summaryContent.AppendLine("# Component Analysis Summary");
    summaryContent.AppendLine();
    summaryContent.AppendLine($"Generated on: {DateTime.Now}");
    summaryContent.AppendLine($"Branch comparison: {baseBranch} -> {targetBranch}");
    summaryContent.AppendLine();
    summaryContent.AppendLine("## Components Analyzed");
    summaryContent.AppendLine();

    foreach (var result in analysisResults.Where(r => r.HasChanges))
    {
        var componentName = GetSafeFilename(result.ComponentPath);
        summaryContent.AppendLine($"- **{result.ComponentPath}** ({result.FileChanges} files) - [Analysis]({componentName}.md)");
    }

    summaryContent.AppendLine();
    summaryContent.AppendLine("## Analysis Files Generated");
    summaryContent.AppendLine();

    var analysisFiles = Directory.GetFiles(analysisDir, "*.md");
    foreach (var file in analysisFiles)
    {
        var fileInfo = new FileInfo(file);
        summaryContent.AppendLine($"- {fileInfo.Name} ({fileInfo.Length} bytes)");
    }

    await File.WriteAllTextAsync(summaryFile, summaryContent.ToString());

    var summaryTime = (DateTime.Now - summaryStart).TotalSeconds;
    AnsiConsole.MarkupLine($"   [green]Created[/] analysis-summary.md");
    AnsiConsole.WriteLine();

    // Final summary
    var totalTime = (DateTime.Now - startTime).TotalSeconds;
    var filesCreated = analysisResults.Count(r => r.HasChanges);

    AnsiConsole.Write(new Rule("[bold green]Completed[/]").RuleStyle("green"));
    AnsiConsole.WriteLine();

    var resultTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Green);
    resultTable.AddColumn(new TableColumn("[green]Item[/]").NoWrap());
    resultTable.AddColumn(new TableColumn("[green]Value[/]"));
    resultTable.AddRow("Components", $"[white]{components.Count}[/]");
    resultTable.AddRow("Files Created", $"[white]{filesCreated}[/]");
    resultTable.AddRow("Analysis Time", $"[white]{Math.Round(analysisTime, 2)}s[/]");
    resultTable.AddRow("Summary Time", $"[white]{Math.Round(summaryTime, 2)}s[/]");
    resultTable.AddRow("Total Time", $"[white]{Math.Round(totalTime, 2)}s[/]");
    resultTable.AddRow("Output", $"[dim]{relativeAnalysisDir}/[/]");
    AnsiConsole.Write(resultTable);
    AnsiConsole.WriteLine();

    // List generated files
    AnsiConsole.MarkupLine("[bold]Generated Files:[/]");
    foreach (var file in Directory.GetFiles(analysisDir, "*.md").OrderBy(f => f))
    {
        AnsiConsole.MarkupLine($"   [dim]{Path.GetFileName(file)}[/]");
    }
    AnsiConsole.WriteLine();
}

// Load components from config file (no console output)
static async Task<List<string>> LoadComponentsAsync(string configFile, string gitRoot)
{
    var components = new List<string>();

    if (!File.Exists(configFile))
    {
        return components;
    }

    try
    {
        var json = await File.ReadAllTextAsync(configFile);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("analysis_priorities", out var priorities))
        {
            foreach (var item in priorities.EnumerateArray())
            {
                var pattern = item.GetString();
                if (string.IsNullOrEmpty(pattern)) continue;

                // Check if pattern contains wildcard
                if (pattern.Contains('*') || pattern.Contains('?'))
                {
                    // Expand glob pattern
                    var fullPattern = Path.Combine(gitRoot, pattern.Replace('/', Path.DirectorySeparatorChar));
                    var parentDir = Path.GetDirectoryName(fullPattern) ?? gitRoot;
                    var searchPattern = Path.GetFileName(fullPattern);

                    if (Directory.Exists(parentDir))
                    {
                        foreach (var dir in Directory.GetDirectories(parentDir, searchPattern))
                        {
                            var relativePath = Path.GetRelativePath(gitRoot, dir).Replace('\\', '/');
                            components.Add(relativePath);
                        }
                    }
                }
                else
                {
                    // Regular path
                    var fullPath = Path.Combine(gitRoot, pattern.Replace('/', Path.DirectorySeparatorChar));
                    if (Directory.Exists(fullPath))
                    {
                        components.Add(pattern.Replace('\\', '/'));
                    }
                }
            }
        }
    }
    catch
    {
        // Silently fail, will use fallback
    }

    return components;
}

// Analyze a single component
static async Task<ComponentAnalysisResult> AnalyzeComponentAsync(string componentPath, string baseBranch, string targetBranch, string gitRoot, string outputFile)
{
    var result = new ComponentAnalysisResult { ComponentPath = componentPath };

    // Get change and commit counts
    var changeResult = await RunGitAsync($"diff --name-status \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"", gitRoot);
    var commitResult = await RunGitAsync($"log --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"", gitRoot);

    result.FileChanges = changeResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
    result.CommitCount = commitResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

    if (result.FileChanges == 0 && result.CommitCount == 0)
    {
        result.HasChanges = false;
        return result;
    }

    result.HasChanges = true;

    // Generate analysis file
    var content = new StringBuilder();
    content.AppendLine($"# Analysis for {componentPath}");
    content.AppendLine();
    content.AppendLine($"Generated: {DateTime.Now}");
    content.AppendLine($"Comparing: {baseBranch} -> {targetBranch}");
    content.AppendLine();

    // Change Summary
    content.AppendLine("## Change Summary");
    content.AppendLine();
    var statsResult = await RunGitAsync($"diff --stat \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"", gitRoot);
    content.AppendLine("```");
    content.AppendLine(statsResult.Output.Trim());
    content.AppendLine("```");
    content.AppendLine();

    // All Commits
    content.AppendLine("## All Commits");
    content.AppendLine();
    content.AppendLine("```");
    content.AppendLine(commitResult.Output.Trim());
    content.AppendLine("```");
    content.AppendLine();

    // Top Contributors
    content.AppendLine("## Top Contributors");
    content.AppendLine();
    var contributorsResult = await RunGitAsync($"log --format=\"%an\" \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"", gitRoot);
    var contributors = contributorsResult.Output
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .GroupBy(x => x)
        .OrderByDescending(g => g.Count())
        .Take(5);

    foreach (var contributor in contributors)
    {
        content.AppendLine($"- {contributor.Count()} {contributor.Key}");
    }
    content.AppendLine();

    // Categorized Commits
    content.AppendLine("## Categorized Commits");
    content.AppendLine();

    // Feature commits
    content.AppendLine("### Feature Commits");
    content.AppendLine();
    var featResult = await RunGitAsync($"log --grep=\"feat\" --grep=\"feature\" --grep=\"add\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"", gitRoot);
    var featCommits = featResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(10).ToList();
    if (featCommits.Count > 0)
    {
        foreach (var commit in featCommits)
        {
            content.AppendLine($"- {commit}");
        }
    }
    else
    {
        content.AppendLine("None found");
    }
    content.AppendLine();

    // Bug fixes
    content.AppendLine("### Bug Fixes");
    content.AppendLine();
    var fixResult = await RunGitAsync($"log --grep=\"fix\" --grep=\"bug\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"", gitRoot);
    var fixCommits = fixResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(10).ToList();
    if (fixCommits.Count > 0)
    {
        foreach (var commit in fixCommits)
        {
            content.AppendLine($"- {commit}");
        }
    }
    else
    {
        content.AppendLine("None found");
    }
    content.AppendLine();

    // Breaking changes
    content.AppendLine("### Breaking Changes");
    content.AppendLine();

    // Get all commits for the component to filter for breaking changes
    var allCommitsResult = await RunGitAsync($"log --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"", gitRoot);

    // Filter for breaking changes:
    // 1. Contains "breaking" or "BREAKING" keyword
    // 2. Type followed by ! (e.g., feat!:, fix!:)
    var breakingPattern = new Regex(@"\b\w+!:", RegexOptions.Compiled);
    var breakingCommits = allCommitsResult.Output
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Where(commit =>
            commit.Contains("breaking", StringComparison.OrdinalIgnoreCase) ||
            commit.Contains("BREAKING") ||
            breakingPattern.IsMatch(commit))
        .Take(10)
        .ToList();

    if (breakingCommits.Count > 0)
    {
        foreach (var commit in breakingCommits)
        {
            content.AppendLine($"- {commit}");
        }
    }
    else
    {
        content.AppendLine("None found");
    }

    await File.WriteAllTextAsync(outputFile, content.ToString());
    return result;
}

// Generate safe filename from component path
static string GetSafeFilename(string path)
{
    var safeName = path.Replace('/', '-').Replace('\\', '-').Replace(':', '-');
    // Remove src- or Src- prefix
    if (safeName.StartsWith("Src-", StringComparison.OrdinalIgnoreCase))
    {
        safeName = safeName[4..];
    }
    // Remove trailing dash
    safeName = safeName.TrimEnd('-');
    return safeName;
}

// Helper methods
static async Task<string?> GetGitRootAsync()
{
    var result = await RunGitAsync("rev-parse --show-toplevel", Directory.GetCurrentDirectory());
    return result.ExitCode == 0 ? result.Output.Trim() : null;
}

static async Task<bool> BranchExistsAsync(string branchName, string workingDirectory)
{
    var result = await RunGitAsync($"rev-parse --verify {branchName}", workingDirectory);
    return result.ExitCode == 0;
}

static async Task<(int ExitCode, string Output)> RunGitAsync(string arguments, string workingDirectory)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = arguments,
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = new Process { StartInfo = startInfo };
    var output = new StringBuilder();

    process.OutputDataReceived += (sender, e) =>
    {
        if (e.Data != null) output.AppendLine(e.Data);
    };

    process.Start();
    process.BeginOutputReadLine();
    await process.WaitForExitAsync();

    return (process.ExitCode, output.ToString());
}

// Result class
record ComponentAnalysisResult
{
    public string ComponentPath { get; init; } = "";
    public int FileChanges { get; set; }
    public int CommitCount { get; set; }
    public bool HasChanges { get; set; }
}
