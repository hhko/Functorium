#!/usr/bin/env dotnet

// .NET 10 File-based Program - Extract API Changes
// Usage: dotnet ExtractApiChanges.cs

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

var rootCommand = new RootCommand("Extract API changes by building current branch");

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await ExtractApiChangesAsync();
    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();

// Main logic
static async Task ExtractApiChangesAsync()
{
    var startTime = DateTime.Now;

    // Get paths
    var toolsDir = Directory.GetCurrentDirectory();
    var analysisDir = Path.Combine(toolsDir, "analysis-output");
    var apiChangesDir = Path.Combine(analysisDir, "api-changes-build-current");
    var gitRoot = await GetGitRootAsync() ?? Path.GetFullPath(Path.Combine(toolsDir, "..", ".."));

    // Header
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[bold blue]Extracting API Changes[/]").RuleStyle("blue"));
    AnsiConsole.WriteLine();

    var currentBranch = await GetCurrentBranchAsync();

    // Info panel
    var infoTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
    infoTable.AddColumn(new TableColumn("[grey]Property[/]").NoWrap());
    infoTable.AddColumn(new TableColumn("[grey]Value[/]"));
    infoTable.AddRow("[white]Branch[/]", $"[cyan]{currentBranch}[/]");
    infoTable.AddRow("[white]Output[/]", $"[dim]{apiChangesDir}[/]");
    infoTable.AddRow("[white]Timestamp[/]", $"[dim]{DateTime.Now:yyyy-MM-dd HH:mm:ss}[/]");
    AnsiConsole.Write(infoTable);
    AnsiConsole.WriteLine();

    // Create output directory
    if (Directory.Exists(apiChangesDir))
    {
        Directory.Delete(apiChangesDir, true);
    }
    Directory.CreateDirectory(apiChangesDir);

    try
    {
        // Step 1: Find ApiGenerator
        AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Locating ApiGenerator...[/]");
        var apiGeneratorPath = Path.Combine(toolsDir, "ApiGenerator.cs");

        if (!File.Exists(apiGeneratorPath))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] ApiGenerator.cs not found");
            Environment.Exit(1);
        }
        AnsiConsole.MarkupLine($"   [green]Found[/] [dim]{apiGeneratorPath}[/]");
        AnsiConsole.WriteLine();

        // Step 2: Find projects
        AnsiConsole.MarkupLine("[bold]Step 2[/] [dim]Finding Functorium projects...[/]");
        var srcDir = Path.Combine(gitRoot, "Src");
        var projectFiles = Directory.GetFiles(srcDir, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains(".Tests.") && Path.GetFileName(p).StartsWith("Functorium"))
            .OrderByDescending(p => p)
            .ToList();

        AnsiConsole.MarkupLine($"   [green]Found[/] [white]{projectFiles.Count}[/] projects");
        AnsiConsole.WriteLine();

        // Step 3: Generate API files
        AnsiConsole.MarkupLine("[bold]Step 3[/] [dim]Publishing projects and generating API files...[/]");
        AnsiConsole.WriteLine();

        var generatedApiFiles = new List<string>();

        foreach (var projectFile in projectFiles)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(projectFile);
            var projectDir = Path.GetDirectoryName(projectFile)!;
            var projectFolderName = Path.GetFileName(projectDir);
            var apiDir = Path.Combine(srcDir, projectFolderName, ".api");
            var outputFile = Path.Combine(apiDir, $"{assemblyName}.cs");
            var publishDir = Path.Combine(projectDir, "bin", "publish");

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync($"Publishing [cyan]{assemblyName}[/]...", async ctx =>
                {
                    try
                    {
                        // Publish the project
                        var publishResult = await RunProcessAsync("dotnet", $"publish \"{projectFile}\" -c Release -o \"{publishDir}\"", quiet: true);

                        if (publishResult.ExitCode != 0)
                        {
                            AnsiConsole.MarkupLine($"   [yellow]WARN[/] [dim]{assemblyName}[/] - Publish failed, skipping");
                            return;
                        }

                        var dllPath = Path.Combine(publishDir, $"{assemblyName}.dll");
                        if (!File.Exists(dllPath))
                        {
                            AnsiConsole.MarkupLine($"   [yellow]WARN[/] [dim]{assemblyName}[/] - DLL not found");
                            return;
                        }

                        ctx.Status($"Generating API for [cyan]{assemblyName}[/]...");

                        // Generate API using ApiGenerator.cs
                        var apiResult = await RunProcessAsync("dotnet", $"\"{apiGeneratorPath}\" \"{dllPath}\" -", quiet: true);

                        if (apiResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(apiResult.Output))
                        {
                            // Create api directory if not exists
                            Directory.CreateDirectory(apiDir);

                            // Create file with header
                            var content = new StringBuilder();
                            content.AppendLine("//------------------------------------------------------------------------------");
                            content.AppendLine("// <auto-generated>");
                            content.AppendLine("//     This code was generated by PublicApiGenerator.");
                            content.AppendLine($"//     Assembly: {assemblyName}");
                            content.AppendLine($"//     Generated at: {DateTime.Now}");
                            content.AppendLine("// </auto-generated>");
                            content.AppendLine("//------------------------------------------------------------------------------");
                            content.AppendLine();
                            content.Append(apiResult.Output);

                            await File.WriteAllTextAsync(outputFile, content.ToString());
                            generatedApiFiles.Add(outputFile);
                            AnsiConsole.MarkupLine($"   [green]OK[/]   [white]{assemblyName}[/] [dim]→ {Path.GetFileName(outputFile)}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"   [yellow]SKIP[/] [dim]{assemblyName}[/] - No public API");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"   [red]ERROR[/] [dim]{assemblyName}[/] - {ex.Message}");
                    }
                });
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"   Generated [green]{generatedApiFiles.Count}[/] API files");
        AnsiConsole.WriteLine();

        // Step 4: Create uber file
        AnsiConsole.MarkupLine("[bold]Step 4[/] [dim]Creating uber API file...[/]");
        var uberFile = Path.Combine(apiChangesDir, "all-api-changes.txt");

        var uberContent = new StringBuilder();
        uberContent.AppendLine("# All API Files - Uber File");
        uberContent.AppendLine($"# Generated from: {currentBranch}");
        uberContent.AppendLine($"# Generated at: {DateTime.Now}");
        uberContent.AppendLine("# Generated by: PublicApiGenerator");
        uberContent.AppendLine($"# Total API files included: {generatedApiFiles.Count}");
        uberContent.AppendLine();

        foreach (var apiFile in generatedApiFiles)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(apiFile);

            uberContent.AppendLine("======================================");
            uberContent.AppendLine($"API FILE: {apiFile}");
            uberContent.AppendLine($"ASSEMBLY: {assemblyName}");
            uberContent.AppendLine("======================================");
            uberContent.AppendLine();

            try
            {
                var fileContent = await File.ReadAllTextAsync(apiFile);
                uberContent.AppendLine(fileContent);
            }
            catch
            {
                uberContent.AppendLine($"# Error reading file {apiFile}");
            }

            uberContent.AppendLine();
            uberContent.AppendLine();
        }

        await File.WriteAllTextAsync(uberFile, uberContent.ToString());
        AnsiConsole.MarkupLine($"   [green]Created[/] [dim]{Path.GetFileName(uberFile)}[/]");
        AnsiConsole.WriteLine();

        // Step 5: Generate git diff
        AnsiConsole.MarkupLine("[bold]Step 5[/] [dim]Generating git diff...[/]");
        var apiDiffPath = Path.Combine(apiChangesDir, "api-changes-diff.txt");

        var diffResult = await RunProcessAsync("git", "diff HEAD -- 'Src/*/.api/*.cs'", quiet: true);
        var diffContent = !string.IsNullOrWhiteSpace(diffResult.Output)
            ? diffResult.Output
            : "# No tracked API file changes (API files are newly generated)";

        await File.WriteAllTextAsync(apiDiffPath, diffContent);
        AnsiConsole.MarkupLine($"   [green]Created[/] [dim]{Path.GetFileName(apiDiffPath)}[/]");
        AnsiConsole.WriteLine();

        // Step 6: Create summary
        AnsiConsole.MarkupLine("[bold]Step 6[/] [dim]Creating summary report...[/]");

        var summary = new StringBuilder();
        summary.AppendLine("# API Files Summary");
        summary.AppendLine();
        summary.AppendLine($"Generated from: {currentBranch}");
        summary.AppendLine($"Generated at: {DateTime.Now}");
        summary.AppendLine("Generated by: PublicApiGenerator");
        summary.AppendLine();
        summary.AppendLine("## Overview");
        summary.AppendLine();
        summary.AppendLine("This document contains all Public API definitions from the Functorium repository.");
        summary.AppendLine();
        summary.AppendLine("## Results");
        summary.AppendLine();
        summary.AppendLine($"- **Total API Files Generated**: {generatedApiFiles.Count}");
        summary.AppendLine();
        summary.AppendLine("## API Files List");
        summary.AppendLine();

        foreach (var apiFile in generatedApiFiles)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(apiFile);
            summary.AppendLine($"- **{assemblyName}**: `{apiFile}`");
        }

        await File.WriteAllTextAsync(Path.Combine(apiChangesDir, "api-changes-summary.md"), summary.ToString());

        // Write projects list
        await File.WriteAllLinesAsync(
            Path.Combine(apiChangesDir, "projects.txt"),
            projectFiles.Select(Path.GetFileName)!);

        AnsiConsole.MarkupLine($"   [green]Created[/] [dim]api-changes-summary.md[/]");
        AnsiConsole.WriteLine();

        // Final summary
        var totalTime = (DateTime.Now - startTime).TotalSeconds;

        AnsiConsole.Write(new Rule("[bold green]Completed[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        var resultTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Green);
        resultTable.AddColumn(new TableColumn("[green]Item[/]").NoWrap());
        resultTable.AddColumn(new TableColumn("[green]Value[/]"));
        resultTable.AddRow("Branch", $"[cyan]{currentBranch}[/]");
        resultTable.AddRow("API Files", $"[white]{generatedApiFiles.Count}[/]");
        resultTable.AddRow("Time", $"[white]{Math.Round(totalTime, 1)}s[/]");
        resultTable.AddRow("Output", $"[dim]{apiChangesDir}[/]");
        resultTable.AddRow("Summary", $"[dim]api-changes-summary.md[/]");
        resultTable.AddRow("Uber File", $"[dim]all-api-changes.txt[/]");
        resultTable.AddRow("API Location", $"[cyan]Src/*/.api/[/]");
        AnsiConsole.Write(resultTable);
        AnsiConsole.WriteLine();
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold red]Error[/]").RuleStyle("red"));
        AnsiConsole.WriteException(ex);
        Environment.Exit(1);
    }
}

// Helper methods
static async Task<string?> GetGitRootAsync()
{
    var result = await RunProcessAsync("git", "rev-parse --show-toplevel", quiet: true);
    return result.ExitCode == 0 ? result.Output.Trim() : null;
}

static async Task<string> GetCurrentBranchAsync()
{
    var result = await RunProcessAsync("git", "branch --show-current", quiet: true);
    if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
    {
        return result.Output.Trim();
    }

    result = await RunProcessAsync("git", "rev-parse HEAD", quiet: true);
    return result.ExitCode == 0 ? result.Output.Trim() : "unknown";
}

static async Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, string arguments, bool quiet = false)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = new Process { StartInfo = startInfo };

    var outputBuilder = new StringBuilder();
    var errorBuilder = new StringBuilder();

    process.OutputDataReceived += (sender, e) =>
    {
        if (e.Data != null)
        {
            outputBuilder.AppendLine(e.Data);
        }
    };

    process.ErrorDataReceived += (sender, e) =>
    {
        if (e.Data != null && !quiet)
        {
            errorBuilder.AppendLine(e.Data);
        }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    await process.WaitForExitAsync();

    var output = outputBuilder.ToString();
    if (!quiet && errorBuilder.Length > 0)
    {
        AnsiConsole.MarkupLine($"[red]{errorBuilder}[/]");
    }

    return (process.ExitCode, output);
}
