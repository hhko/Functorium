#!/usr/bin/env dotnet

// .NET 10 File-based Program - Analyze Folder
// Comprehensive folder analysis script for release notes generation
// Usage: dotnet AnalyzeFolder.cs <folder-path> [--base <branch>] [--target <branch>]

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

var folderArg = new Argument<string>("folder") { Description = "The folder path to analyze" };
var baseOption = new Option<string>("--base") { Description = "Base branch for comparison" };
baseOption.DefaultValueFactory = (_) => "origin/release/1.0";
var targetOption = new Option<string>("--target") { Description = "Target branch for comparison" };
targetOption.DefaultValueFactory = (_) => "origin/main";

var rootCommand = new RootCommand("Comprehensive folder analysis for release notes generation")
{
    folderArg,
    baseOption,
    targetOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var folderPath = parseResult.GetValue(folderArg)!;
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    await AnalyzeFolderAsync(folderPath, baseBranch, targetBranch);
    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();

// Main logic
static async Task AnalyzeFolderAsync(string folderPath, string baseBranch, string targetBranch)
{
    var startTime = DateTime.Now;

    // Get git root
    var gitRoot = await GetGitRootAsync() ?? Directory.GetCurrentDirectory();
    gitRoot = gitRoot.Replace('\\', '/');

    // Normalize folder path to relative
    var relativePath = folderPath.Replace('\\', '/');
    relativePath = relativePath.Replace(gitRoot + "/", "").TrimStart('/');

    // Header
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule($"[bold blue]Analyzing: {relativePath}[/]").RuleStyle("blue"));
    AnsiConsole.WriteLine();

    // Info table
    var infoTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
    infoTable.AddColumn(new TableColumn("[grey]Property[/]").NoWrap());
    infoTable.AddColumn(new TableColumn("[grey]Value[/]"));
    infoTable.AddRow("[white]Base Branch[/]", $"[cyan]{baseBranch}[/]");
    infoTable.AddRow("[white]Target Branch[/]", $"[cyan]{targetBranch}[/]");
    infoTable.AddRow("[white]Working Dir[/]", $"[dim]{gitRoot}[/]");
    AnsiConsole.Write(infoTable);
    AnsiConsole.WriteLine();

    AnsiConsole.MarkupLine("[dim]Note: Only analyzing commits in target that are NOT in base[/]");
    AnsiConsole.WriteLine();

    // Change Summary
    AnsiConsole.MarkupLine("[bold]Change Summary[/]");
    AnsiConsole.WriteLine();

    var statsResult = await RunGitAsync($"diff --stat \"{baseBranch}..{targetBranch}\" -- \"{relativePath}/\"", gitRoot);
    if (string.IsNullOrWhiteSpace(statsResult.Output))
    {
        AnsiConsole.MarkupLine("[yellow]No changes found in this folder[/]");
        return;
    }

    // Parse and display stats
    var statsLines = statsResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    foreach (var line in statsLines.Take(20))
    {
        if (line.Contains("|"))
        {
            var parts = line.Split('|');
            if (parts.Length == 2)
            {
                var fileName = parts[0].Trim();
                var changes = parts[1].Trim();
                AnsiConsole.MarkupLine($"  [dim]{fileName}[/] | [green]{changes}[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"  [dim]{line}[/]");
        }
    }
    if (statsLines.Length > 20)
    {
        AnsiConsole.MarkupLine($"  [dim]... and {statsLines.Length - 20} more files[/]");
    }
    AnsiConsole.WriteLine();

    // All Commits
    AnsiConsole.MarkupLine($"[bold]All Commits[/] [dim](new in {targetBranch})[/]");
    AnsiConsole.WriteLine();

    var commitsResult = await RunGitAsync($"log --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{relativePath}/\"", gitRoot);
    var commits = commitsResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    foreach (var commit in commits.Take(30))
    {
        var hash = commit.Length > 7 ? commit[..7] : commit;
        var message = commit.Length > 8 ? commit[8..] : "";
        AnsiConsole.MarkupLine($"  [yellow]{hash}[/] {message}");
    }
    if (commits.Length > 30)
    {
        AnsiConsole.MarkupLine($"  [dim]... and {commits.Length - 30} more commits[/]");
    }
    AnsiConsole.WriteLine();

    // Top Contributors
    AnsiConsole.MarkupLine("[bold]Top Contributors[/]");
    AnsiConsole.WriteLine();

    var contributorsResult = await RunGitAsync($"log --format=\"%an\" \"{baseBranch}..{targetBranch}\" -- \"{relativePath}/\"", gitRoot);
    var contributors = contributorsResult.Output
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .GroupBy(x => x)
        .OrderByDescending(g => g.Count())
        .Take(5)
        .ToList();

    var contributorTable = new Table().Border(TableBorder.Simple).BorderColor(Color.Grey);
    contributorTable.AddColumn(new TableColumn("[grey]Commits[/]").RightAligned());
    contributorTable.AddColumn(new TableColumn("[grey]Contributor[/]"));

    foreach (var contributor in contributors)
    {
        contributorTable.AddRow($"[white]{contributor.Count()}[/]", $"[cyan]{contributor.Key}[/]");
    }
    AnsiConsole.Write(contributorTable);
    AnsiConsole.WriteLine();

    // Categorized Commits
    AnsiConsole.MarkupLine("[bold]Categorized Commits[/]");
    AnsiConsole.WriteLine();

    // Feature commits
    AnsiConsole.MarkupLine("  [green]Feature commits:[/]");
    var featResult = await RunGitAsync($"log --grep=\"feat\" --grep=\"feature\" --grep=\"add\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{relativePath}/\"", gitRoot);
    var featCommits = featResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(5).ToList();
    if (featCommits.Count > 0)
    {
        foreach (var commit in featCommits)
        {
            AnsiConsole.MarkupLine($"    [dim]{commit}[/]");
        }
    }
    else
    {
        AnsiConsole.MarkupLine("    [dim]None found[/]");
    }
    AnsiConsole.WriteLine();

    // Bug fixes
    AnsiConsole.MarkupLine("  [yellow]Bug fixes:[/]");
    var fixResult = await RunGitAsync($"log --grep=\"fix\" --grep=\"bug\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{relativePath}/\"", gitRoot);
    var fixCommits = fixResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(5).ToList();
    if (fixCommits.Count > 0)
    {
        foreach (var commit in fixCommits)
        {
            AnsiConsole.MarkupLine($"    [dim]{commit}[/]");
        }
    }
    else
    {
        AnsiConsole.MarkupLine("    [dim]None found[/]");
    }
    AnsiConsole.WriteLine();

    // Breaking changes
    AnsiConsole.MarkupLine("  [red]Breaking changes:[/]");
    var breakingResult = await RunGitAsync($"log --grep=\"breaking\" --grep=\"BREAKING\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{relativePath}/\"", gitRoot);
    var breakingCommits = breakingResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(5).ToList();
    if (breakingCommits.Count > 0)
    {
        foreach (var commit in breakingCommits)
        {
            AnsiConsole.MarkupLine($"    [dim]{commit}[/]");
        }
    }
    else
    {
        AnsiConsole.MarkupLine("    [dim]None found[/]");
    }
    AnsiConsole.WriteLine();

    // Summary
    var totalTime = (DateTime.Now - startTime).TotalSeconds;

    AnsiConsole.Write(new Rule("[bold green]Analysis Complete[/]").RuleStyle("green"));
    AnsiConsole.WriteLine();

    var summaryTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Green);
    summaryTable.AddColumn(new TableColumn("[green]Item[/]").NoWrap());
    summaryTable.AddColumn(new TableColumn("[green]Value[/]"));
    summaryTable.AddRow("Folder", $"[cyan]{relativePath}[/]");
    summaryTable.AddRow("Commits", $"[white]{commits.Length}[/]");
    summaryTable.AddRow("Contributors", $"[white]{contributors.Count}[/]");
    summaryTable.AddRow("Time", $"[white]{Math.Round(totalTime, 2)}s[/]");
    AnsiConsole.Write(summaryTable);
    AnsiConsole.WriteLine();
}

// Helper methods
static async Task<string?> GetGitRootAsync()
{
    var result = await RunGitAsync("rev-parse --show-toplevel", Directory.GetCurrentDirectory());
    return result.ExitCode == 0 ? result.Output.Trim() : null;
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
