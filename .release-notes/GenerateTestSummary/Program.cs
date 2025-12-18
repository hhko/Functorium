// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.Extensions.FileSystemGlobbing;
using Aspire.TestTools;

// Usage: dotnet run --project GenerateTestSummary -- [globPattern] [--output <output>]
// Generate a combined summary report from trx files matching the glob pattern.
// Default pattern: "**/TestResults/**/*.trx"
// And write to $GITHUB_STEP_SUMMARY if running in GitHub Actions.

const string DefaultGlobPattern = "**/TestResults/**/*.trx";

var globPatternArgument = new Argument<string>("globPattern")
{
    Description = $"Glob pattern to find trx files (default: '{DefaultGlobPattern}')",
    Arity = ArgumentArity.ZeroOrOne
};
var outputOption = new Option<string>("--output", "-o") { Description = "Output file path" };

var rootCommand = new RootCommand("Generate a combined test summary report from trx files")
{
    globPatternArgument,
    outputOption
};

rootCommand.SetAction(result =>
{
    var globPattern = result.GetValue<string>(globPatternArgument);
    if (string.IsNullOrEmpty(globPattern))
    {
        globPattern = DefaultGlobPattern;
    }

    var matcher = new Matcher();
    matcher.AddInclude(globPattern);

    var currentDir = Directory.GetCurrentDirectory();
    var matchResult = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(currentDir)));

    var trxFiles = matchResult.Files
        .Select(f => Path.GetFullPath(Path.Combine(currentDir, f.Path)))
        .ToList();

    if (trxFiles.Count == 0)
    {
        Console.WriteLine($"No trx files found matching pattern: {globPattern}");
        return;
    }

    Console.WriteLine($"Found {trxFiles.Count} trx file(s) matching pattern: {globPattern}");

    var report = TestSummaryGenerator.CreateCombinedTestSummaryReport(trxFiles);

    if (report.Length == 0)
    {
        Console.WriteLine("No test results found.");
        return;
    }

    var outputFilePath = result.GetValue<string>(outputOption);
    if (outputFilePath is not null)
    {
        File.WriteAllText(outputFilePath, report);
        Console.WriteLine($"Report written to {outputFilePath}");
    }

    if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true"
        && Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is string summaryPath)
    {
        Console.WriteLine($"Detected GitHub Actions environment. Writing to {summaryPath}");
        File.WriteAllText(summaryPath, report);
    }

    Console.WriteLine(report);
});

return rootCommand.Parse(args).Invoke();
