#!/usr/bin/env dotnet

// .NET 10 File-based Program - Summarize Slowest Tests
// TRX 파일에서 느린 테스트 및 테스트 결과 요약 보고서를 생성합니다.
// Usage: dotnet SummarizeSlowestTests.cs [globPattern] [--threshold <seconds>] [--output-dir <path>]
// Default pattern: "**/TestResults/**/*.trx"
// Default threshold: 30 seconds (slow test threshold)
// Default output: {gitRoot}/.coverage/slowest-tests.md

#:package System.CommandLine@2.0.1
#:package Microsoft.Extensions.FileSystemGlobbing@10.0.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

// Constants
const string DefaultGlobPattern = "**/TestResults/**/*.trx";
const string OutputFileName = "SummarySlowestTests.md";
const double DefaultSlowTestThresholdSeconds = 30.0;

// Command line setup
var globPatternArgument = new Argument<string>("globPattern")
{
    Description = $"Glob pattern to find trx files (default: '{DefaultGlobPattern}')",
    Arity = ArgumentArity.ZeroOrOne
};

var thresholdOption = new Option<double>("--threshold", "-t")
{
    Description = $"Minimum duration in seconds to consider a test as slow (default: {DefaultSlowTestThresholdSeconds})"
};
thresholdOption.DefaultValueFactory = (_) => DefaultSlowTestThresholdSeconds;

var outputDirOption = new Option<string?>("--output-dir", "-o")
{
    Description = "Output directory for the report file (default: {gitRoot}/.coverage)"
};

var rootCommand = new RootCommand("Generate a combined test summary report from trx files")
{
    globPatternArgument,
    thresholdOption,
    outputDirOption
};

rootCommand.SetAction(async (result, cancellationToken) =>
{
    var globPattern = result.GetValue<string>(globPatternArgument);
    if (string.IsNullOrEmpty(globPattern))
    {
        globPattern = DefaultGlobPattern;
    }

    // 스크립트 디렉토리 기준으로 경로 처리 (AnalyzeAllComponents.cs와 동일)
    var scriptsDir = Directory.GetCurrentDirectory();

    // Git 루트 디렉토리 계산 (git 명령어 우선, 실패 시 상위 2단계로 폴백)
    var gitRoot = await GetGitRootAsync() ?? Path.GetFullPath(Path.Combine(scriptsDir, "..", ".."));
    gitRoot = gitRoot.Replace('\\', '/');

    Console.WriteLine($"Scripts directory: {scriptsDir}");
    Console.WriteLine($"Git root: {gitRoot}");
    Console.WriteLine($"Glob pattern: {globPattern}");

    var matcher = new Matcher();
    matcher.AddInclude(globPattern);

    // Git 루트에서 glob 패턴 실행
    var matchResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(gitRoot)));

    var trxFiles = matchResult.Files
        .Select(f => Path.GetFullPath(Path.Combine(gitRoot, f.Path)))
        .ToList();

    if (trxFiles.Count == 0)
    {
        Console.WriteLine($"No trx files found matching pattern: {globPattern}");
        return;
    }

    Console.WriteLine($"Found {trxFiles.Count} trx file(s) matching pattern: {globPattern}");

    var slowTestThreshold = result.GetValue<double>(thresholdOption);
    Console.WriteLine($"Slow test threshold: {slowTestThreshold}s");

    var report = TestSummaryGenerator.CreateCombinedTestSummaryReport(trxFiles, slowTestThreshold);

    if (report.Length == 0)
    {
        Console.WriteLine("No test results found.");
        return;
    }

    // 출력 디렉터리 결정 (옵션 > 기본값)
    var outputDirValue = result.GetValue<string?>(outputDirOption);
    var outputDir = !string.IsNullOrEmpty(outputDirValue)
        ? Path.GetFullPath(outputDirValue)
        : Path.Combine(gitRoot, ".coverage");

    Console.WriteLine($"Output directory: {outputDir}");

    if (!Directory.Exists(outputDir))
    {
        Directory.CreateDirectory(outputDir);
    }

    var outputFilePath = Path.Combine(outputDir, OutputFileName);
    File.WriteAllText(outputFilePath, report);
    Console.WriteLine($"Report written to {outputFilePath}");

    if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true"
        && Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is string summaryPath)
    {
        Console.WriteLine($"Detected GitHub Actions environment. Writing to {summaryPath}");
        File.WriteAllText(summaryPath, report);
    }

    Console.WriteLine(report);
});

return await rootCommand.Parse(args).InvokeAsync();

// ============================================================================
// Git Helper Methods
// ============================================================================

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

// ============================================================================
// TestSummaryGenerator
// ============================================================================

static class TestSummaryGenerator
{
    /// <summary>
    /// TRX 파일 경로에서 테스트 프로젝트 이름을 추출하기 위한 패턴입니다.
    /// 예: "Functorium.Tests.Unit", "MyApp.Tests.Integration" 등
    /// </summary>
    private const string TestProjectPattern = @"\.Tests\.";

    private static readonly Regex TestNameFromTrxFileNameRegex =
        new(@"(?<testName>.*)_(?<tfm>net\d+\.0)_.*", RegexOptions.Compiled);

    private static readonly Regex TestProjectPatternRegex =
        new(TestProjectPattern, RegexOptions.Compiled);

    public static string CreateCombinedTestSummaryReport(IReadOnlyList<string> trxFilePaths, double slowTestThresholdSeconds = 30.0)
    {
        if (trxFilePaths.Count == 0)
        {
            return string.Empty;
        }

        int overallTotalTestCount = 0;
        int overallPassedTestCount = 0;
        int overallFailedTestCount = 0;
        int overallSkippedTestCount = 0;

        // Collect all test run data first so we can sort by duration
        var testRunData = new List<TestRunSummary>();

        foreach (var filePath in trxFilePaths.OrderBy(f => Path.GetFileName(f)))
        {
            TestRun? testRun;
            try
            {
                testRun = TrxReader.DeserializeTrxFile(filePath);
                if (testRun?.ResultSummary?.Counters is null)
                {
                    Console.WriteLine($"Failed to deserialize or find results in file: {filePath}, tr: {testRun}");
                    continue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize file: {filePath}, exception: {ex}");
                continue;
            }

            // collect data for each trx file
            var counters = testRun.ResultSummary.Counters;
            int total = counters.Total;
            int passed = counters.Passed;
            int failed = counters.Failed;
            int skipped = counters.NotExecuted;

            overallTotalTestCount += total;
            overallPassedTestCount += passed;
            overallFailedTestCount += failed;
            overallSkippedTestCount += skipped;

            // Determine the OS from the path, assuming the path contains
            // os runner name like `windows-latest`, `ubuntu-latest`, or `macos-latest`
            // Falls back to current OS if not found in path
            var os = GetOsFromPath(filePath);

            var icon = total == 0 ? "⚠️"
                        : failed > 0 ? "❌"
                            : passed > 0 ? "✅"
                                : "❓";

            var duration = TrxReader.GetTestRunDurationInMinutes(testRun);

            testRunData.Add(new TestRunSummary(
                Icon: icon,
                Os: os,
                Title: GetTestTitle(filePath),
                Passed: passed,
                Failed: failed,
                Skipped: skipped,
                Total: total,
                DurationMinutes: duration
            ));
        }

        // Sort by duration descending
        testRunData.Sort((x, y) => y.DurationMinutes.CompareTo(x.DurationMinutes));

        // Build the table with sorted data
        var tableBuilder = new StringBuilder();
        tableBuilder.AppendLine("| Name | Passed | Failed | Skipped | Total | Duration (minutes) |");
        tableBuilder.AppendLine("|------|--------|--------|---------|-------|-------------------|");

        foreach (var data in testRunData)
        {
            tableBuilder.AppendLine(CultureInfo.InvariantCulture,
                $"| {data.Icon} [{data.Os}] {data.Title} | {data.Passed} | {data.Failed} | {data.Skipped} | {data.Total} | {data.DurationMinutes:F2} |");
        }

        var overallTableBuilder = new StringBuilder();

        overallTableBuilder.AppendLine("# Test Summary");
        overallTableBuilder.AppendLine();

        overallTableBuilder.AppendLine("## Overall Summary");

        overallTableBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
        overallTableBuilder.AppendLine("|--------|--------|---------|-------|");
        overallTableBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {overallPassedTestCount} | {overallFailedTestCount} | {overallSkippedTestCount} | {overallTotalTestCount} |");

        overallTableBuilder.AppendLine();

        // Split test projects into > 5 mins and the rest
        var slowProjects = testRunData.Where(t => t.DurationMinutes > 5).ToList();
        var fastProjects = testRunData.Where(t => t.DurationMinutes <= 5).ToList();

        if (slowProjects.Count > 0)
        {
            overallTableBuilder.AppendLine("### Test Projects > 5mins");
            overallTableBuilder.AppendLine();
            overallTableBuilder.AppendLine("| Name | Passed | Failed | Skipped | Total | Duration (minutes) |");
            overallTableBuilder.AppendLine("|------|--------|--------|---------|-------|-------------------|");

            foreach (var data in slowProjects)
            {
                overallTableBuilder.AppendLine(CultureInfo.InvariantCulture,
                    $"| {data.Icon} [{data.Os}] {data.Title} | {data.Passed} | {data.Failed} | {data.Skipped} | {data.Total} | {data.DurationMinutes:F2} |");
            }
            overallTableBuilder.AppendLine();
        }

        if (fastProjects.Count > 0)
        {
            overallTableBuilder.AppendLine("<details>");
            overallTableBuilder.AppendLine("<summary>All Other Test Projects</summary>");
            overallTableBuilder.AppendLine();
            overallTableBuilder.AppendLine("| Name | Passed | Failed | Skipped | Total | Duration (minutes) |");
            overallTableBuilder.AppendLine("|------|--------|--------|---------|-------|-------------------|");

            foreach (var data in fastProjects)
            {
                overallTableBuilder.AppendLine(CultureInfo.InvariantCulture,
                    $"| {data.Icon} [{data.Os}] {data.Title} | {data.Passed} | {data.Failed} | {data.Skipped} | {data.Total} | {data.DurationMinutes:F2} |");
            }

            overallTableBuilder.AppendLine("</details>");
            overallTableBuilder.AppendLine();
        }

        // Add test project duration distribution
        overallTableBuilder.AppendLine();
        overallTableBuilder.AppendLine("## Test Project Duration Distribution");
        overallTableBuilder.AppendLine();

        var projectBuckets = new (string Label, double Min, double Max, int Count)[]
        {
            ("< 5 min", 0, 5, 0),
            ("5-10 min", 5, 10, 0),
            ("10-15 min", 10, 15, 0),
            ("15-20 min", 15, 20, 0),
            ("20-30 min", 20, 30, 0),
            ("> 30 min", 30, double.MaxValue, 0)
        };

        var projectBucketCounts = new int[projectBuckets.Length];
        foreach (var testRun in testRunData)
        {
            for (int i = 0; i < projectBuckets.Length; i++)
            {
                if (testRun.DurationMinutes >= projectBuckets[i].Min && testRun.DurationMinutes < projectBuckets[i].Max)
                {
                    projectBucketCounts[i]++;
                    break;
                }
            }
        }

        overallTableBuilder.AppendLine("| Duration Range | Count | Percentage |");
        overallTableBuilder.AppendLine("|----------------|-------|------------|");
        var totalProjectCount = testRunData.Count;
        for (int i = 0; i < projectBuckets.Length; i++)
        {
            var percentage = totalProjectCount > 0 ? (projectBucketCounts[i] / (double)totalProjectCount) * 100 : 0;
            overallTableBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {projectBuckets[i].Label} | {projectBucketCounts[i]:N0} | {percentage:F1}% |");
        }

        // Add top tests per run
        overallTableBuilder.AppendLine();
        overallTableBuilder.AppendLine("## Slowest Tests Per Test Run");
        overallTableBuilder.Append(GenerateTopTestsPerRun(trxFilePaths, slowTestThresholdSeconds));

        // Add duration statistics
        overallTableBuilder.AppendLine();
        overallTableBuilder.AppendLine("## Duration Statistics");
        overallTableBuilder.Append(GenerateDurationStatistics(trxFilePaths));

        return overallTableBuilder.ToString();
    }

    private static string GenerateDurationStatistics(IReadOnlyList<string> trxFilePaths)
    {
        var allDurations = new List<double>();
        var testDetails = new List<(string TestName, double DurationSeconds, string Outcome, string TestRun)>();

        foreach (var filePath in trxFilePaths)
        {
            TestRun? testRun;
            try
            {
                testRun = TrxReader.DeserializeTrxFile(filePath);
                if (testRun?.Results?.UnitTestResults is null)
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            var testRunName = GetTestTitle(filePath);

            foreach (var test in testRun.Results.UnitTestResults)
            {
                if (test.Duration is string durationStr && TimeSpan.TryParse(durationStr, out var duration))
                {
                    var seconds = duration.TotalSeconds;
                    allDurations.Add(seconds);
                    testDetails.Add((test.TestName ?? "Unknown", seconds, test.Outcome ?? "Unknown", testRunName));
                }
            }
        }

        if (allDurations.Count == 0)
        {
            return "No test duration data available.\n";
        }

        var statsBuilder = new StringBuilder();

        // Calculate statistics
        allDurations.Sort();
        var count = allDurations.Count;
        var sum = allDurations.Sum();
        var mean = sum / count;
        var median = count % 2 == 0
            ? (allDurations[(count - 1) / 2] + allDurations[count / 2]) / 2.0
            : allDurations[count / 2];

        // Calculate standard deviation
        var variance = allDurations.Select(d => Math.Pow(d - mean, 2)).Sum() / count;
        var stdDev = Math.Sqrt(variance);

        // Percentiles
        var p50 = allDurations[Math.Min((int)(count * 0.50), count - 1)];
        var p90 = allDurations[Math.Min((int)(count * 0.90), count - 1)];
        var p95 = allDurations[Math.Min((int)(count * 0.95), count - 1)];
        var p99 = allDurations[Math.Min((int)(count * 0.99), count - 1)];

        // Basic statistics table
        statsBuilder.AppendLine("### Overall Statistics");
        statsBuilder.AppendLine();
        statsBuilder.AppendLine("| Metric | Value |");
        statsBuilder.AppendLine("|--------|-------|");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Total Tests | {count:N0} |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Total Time | {sum / 60:F2} minutes |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Mean | {mean:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Median | {median:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Std Dev | {stdDev:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Min | {allDurations[0]:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Max | {allDurations[^1]:F3}s |");
        statsBuilder.AppendLine();

        // Percentiles table
        statsBuilder.AppendLine("### Percentiles");
        statsBuilder.AppendLine();
        statsBuilder.AppendLine("| Percentile | Duration |");
        statsBuilder.AppendLine("|------------|----------|");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| 50th (Median) | {p50:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| 90th | {p90:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| 95th | {p95:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| 99th | {p99:F3}s |");
        statsBuilder.AppendLine();

        // Distribution buckets
        statsBuilder.AppendLine("### Duration Distribution");
        statsBuilder.AppendLine();
        var buckets = new (string Label, double Min, double Max, int Count)[]
        {
            ("< 1s", 0, 1, 0),
            ("1-5s", 1, 5, 0),
            ("5-10s", 5, 10, 0),
            ("10-30s", 10, 30, 0),
            ("30-60s", 30, 60, 0),
            ("1-5 min", 60, 300, 0),
            ("> 5 min", 300, double.MaxValue, 0)
        };

        var bucketCounts = new int[buckets.Length];
        foreach (var duration in allDurations)
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                if (duration >= buckets[i].Min && duration < buckets[i].Max)
                {
                    bucketCounts[i]++;
                    break;
                }
            }
        }

        statsBuilder.AppendLine("| Range | Count | Percentage |");
        statsBuilder.AppendLine("|-------|-------|------------|");
        for (int i = 0; i < buckets.Length; i++)
        {
            var percentage = (bucketCounts[i] / (double)count) * 100;
            statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {buckets[i].Label} | {bucketCounts[i]:N0} | {percentage:F1}% |");
        }
        statsBuilder.AppendLine();

        // Top 100 slowest tests
        statsBuilder.AppendLine("### Top 100 Slowest Tests");
        statsBuilder.AppendLine();
        var slowestTests = testDetails.OrderByDescending(t => t.DurationSeconds).Take(100);
        statsBuilder.AppendLine("| Duration | Status | Test Name | Test Run |");
        statsBuilder.AppendLine("|----------|--------|-----------|----------|");

        foreach (var test in slowestTests)
        {
            var icon = test.Outcome == "Passed" ? "✅" : test.Outcome == "Failed" ? "❌" : "⚠️";
            statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {test.DurationSeconds:F2}s | {icon} {test.Outcome} | {test.TestName} | {test.TestRun} |");
        }
        statsBuilder.AppendLine();

        // Failed tests section
        var failedTests = testDetails.Where(t => t.Outcome == "Failed").OrderByDescending(t => t.DurationSeconds).ToList();
        statsBuilder.AppendLine("### Failed Tests");
        statsBuilder.AppendLine();

        if (failedTests.Count == 0)
        {
            statsBuilder.AppendLine("*No failed tests.*");
        }
        else
        {
            statsBuilder.AppendLine($"**{failedTests.Count} failed test(s)**");
            statsBuilder.AppendLine();
            statsBuilder.AppendLine("| # | Duration | Test Name | Test Run |");
            statsBuilder.AppendLine("|---|----------|-----------|----------|");

            int rank = 1;
            foreach (var test in failedTests)
            {
                statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {rank} | {test.DurationSeconds:F2}s | {test.TestName} | {test.TestRun} |");
                rank++;
            }
        }
        statsBuilder.AppendLine();

        return statsBuilder.ToString();
    }

    private static string GenerateTopTestsPerRun(IReadOnlyList<string> trxFilePaths, double slowTestThresholdSeconds)
    {
        var resultBuilder = new StringBuilder();

        foreach (var filePath in trxFilePaths.OrderBy(f => Path.GetFileName(f)))
        {
            TestRun? testRun;
            try
            {
                testRun = TrxReader.DeserializeTrxFile(filePath);
                if (testRun?.Results?.UnitTestResults is null)
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            var testRunName = GetTestTitle(filePath);

            // Collect test durations for this run, filtering for slow tests
            var testDetails = new List<(string TestName, double DurationSeconds, string Outcome)>();
            foreach (var test in testRun.Results.UnitTestResults)
            {
                if (test.Duration is string durationStr && TimeSpan.TryParse(durationStr, out var duration))
                {
                    var seconds = duration.TotalSeconds;
                    if (seconds > slowTestThresholdSeconds)
                    {
                        testDetails.Add((test.TestName ?? "Unknown", seconds, test.Outcome ?? "Unknown"));
                    }
                }
            }

            // Only show test runs that have slow tests
            if (testDetails.Count == 0)
            {
                continue;
            }

            // Determine the OS from the path
            var os = GetOsFromPath(filePath);

            // Get total duration for this test run
            var totalDurationMinutes = TrxReader.GetTestRunDurationInMinutes(testRun);

            // Get top 10 slowest tests
            var slowestTests = testDetails.OrderByDescending(t => t.DurationSeconds).Take(10);
            var totalSlowTestCount = testDetails.Count;

            resultBuilder.AppendLine();
            resultBuilder.AppendLine(CultureInfo.InvariantCulture, $"### [{os}] {testRunName} (total time: {totalDurationMinutes:F2} mins)");
            resultBuilder.AppendLine();
            resultBuilder.AppendLine(CultureInfo.InvariantCulture, $"**{totalSlowTestCount} tests > {slowTestThresholdSeconds}s** (showing top 10)");
            resultBuilder.AppendLine();
            resultBuilder.AppendLine("| # | Duration | Status | Test Name |");
            resultBuilder.AppendLine("|---|----------|--------|-----------|");

            int rank = 1;
            foreach (var test in slowestTests)
            {
                var icon = test.Outcome == "Passed" ? "✅" : test.Outcome == "Failed" ? "❌" : "⚠️";
                resultBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {rank} | {test.DurationSeconds:F2}s | {icon} {test.Outcome} | {test.TestName} |");
                rank++;
            }
        }

        if (resultBuilder.Length == 0)
        {
            resultBuilder.AppendLine();
            resultBuilder.AppendLine(CultureInfo.InvariantCulture, $"*No tests found that take longer than {slowTestThresholdSeconds} seconds.*");
        }

        return resultBuilder.ToString();
    }

    public static string GetTestTitle(string trxFilePath)
    {
        // 경로에서 테스트 프로젝트 이름 추출 시도 (예: "Functorium.Tests.Unit")
        var testProjectName = ExtractTestProjectNameFromPath(trxFilePath);
        if (!string.IsNullOrEmpty(testProjectName))
        {
            return testProjectName;
        }

        // 기존 로직: 파일명에서 테스트 이름 추출
        var filename = Path.GetFileNameWithoutExtension(trxFilePath);
        var match = TestNameFromTrxFileNameRegex.Match(filename);
        if (match.Success)
        {
            return $"{match.Groups["testName"].Value} ({match.Groups["tfm"].Value})";
        }

        return filename;
    }

    /// <summary>
    /// TRX 파일 경로에서 테스트 프로젝트 이름을 추출합니다.
    /// 경로의 디렉토리 중 TestProjectPattern (*.Tests.*)과 일치하는 이름을 찾습니다.
    /// </summary>
    private static string? ExtractTestProjectNameFromPath(string trxFilePath)
    {
        var pathParts = trxFilePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var part in pathParts)
        {
            if (TestProjectPatternRegex.IsMatch(part))
            {
                return part;
            }
        }

        return null;
    }

    private static string GetOsFromPath(string filePath)
    {
        if (filePath.Contains("windows-"))
            return "win";
        if (filePath.Contains("ubuntu-"))
            return "lin";
        if (filePath.Contains("macos-"))
            return "mac";

        // Fall back to current OS
        return OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsLinux() ? "lin"
            : OperatingSystem.IsMacOS() ? "mac"
            : "unk";
    }
}

// ============================================================================
// TrxReader
// ============================================================================

static class TrxReader
{
    public static IList<TestResult> GetTestResultsFromTrx(string filepath, Func<string, string, bool>? testFilter = null)
    {
        XmlSerializer serializer = new(typeof(TestRun));
        using FileStream fileStream = new(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        if (serializer.Deserialize(fileStream) is not TestRun testRun || testRun.Results?.UnitTestResults is null)
        {
            return Array.Empty<TestResult>();
        }

        var testResults = new List<TestResult>();

        foreach (var unitTestResult in testRun.Results.UnitTestResults)
        {
            if (string.IsNullOrEmpty(unitTestResult.TestName) || string.IsNullOrEmpty(unitTestResult.Outcome))
            {
                continue;
            }

            if (testFilter is not null && !testFilter(unitTestResult.TestName, unitTestResult.Outcome))
            {
                continue;
            }

            var startTime = unitTestResult.StartTime;
            var endTime = unitTestResult.EndTime;

            testResults.Add(new TestResult(
                Name: unitTestResult.TestName,
                Outcome: unitTestResult.Outcome,
                StartTime: startTime is null ? TimeSpan.MinValue : TimeSpan.Parse(startTime, CultureInfo.InvariantCulture),
                EndTime: endTime is null ? TimeSpan.MinValue : TimeSpan.Parse(endTime, CultureInfo.InvariantCulture),
                ErrorMessage: unitTestResult.Output?.ErrorInfoString,
                Stdout: unitTestResult.Output?.StdOut
            ));
        }

        return testResults;
    }

    public static TestRun? DeserializeTrxFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException($"{nameof(filePath)} cannot be null or empty.", nameof(filePath));
        }

        XmlSerializer serializer = new(typeof(TestRun));

        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return serializer.Deserialize(fileStream) as TestRun;
    }

    public static double GetTestRunDurationInMinutes(TestRun testRun)
    {
        // First try to use the Times element if available
        if (testRun?.Times is not null
            && !string.IsNullOrEmpty(testRun.Times.Start)
            && !string.IsNullOrEmpty(testRun.Times.Finish))
        {
            if (DateTime.TryParse(testRun.Times.Start, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var start)
                && DateTime.TryParse(testRun.Times.Finish, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var finish))
            {
                return (finish - start).TotalMinutes;
            }
        }

        // Fall back to computing from individual test results if Times element is not available
        if (testRun?.Results?.UnitTestResults is null || testRun.Results.UnitTestResults.Count == 0)
        {
            return 0.0;
        }

        DateTime? earliestStartTime = null;
        DateTime? latestEndTime = null;

        foreach (var unitTestResult in testRun.Results.UnitTestResults)
        {
            if (DateTime.TryParse(unitTestResult.StartTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var startTime))
            {
                if (earliestStartTime is null || startTime < earliestStartTime)
                {
                    earliestStartTime = startTime;
                }
            }

            if (DateTime.TryParse(unitTestResult.EndTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var endTime))
            {
                if (latestEndTime is null || endTime > latestEndTime)
                {
                    latestEndTime = endTime;
                }
            }
        }

        if (earliestStartTime is null || latestEndTime is null)
        {
            return 0.0;
        }

        return (latestEndTime.Value - earliestStartTime.Value).TotalMinutes;
    }
}

// ============================================================================
// XML Model Classes
// ============================================================================

[XmlRoot("TestRun", Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")]
public class TestRun
{
    public Results? Results { get; set; }
    public ResultSummary? ResultSummary { get; set; }
    public Times? Times { get; set; }
}

public class Results
{
    [XmlElement("UnitTestResult")]
    public List<UnitTestResult>? UnitTestResults { get; set; }
}

public class Times
{
    [XmlAttribute("start")]
    public string? Start { get; set; }

    [XmlAttribute("finish")]
    public string? Finish { get; set; }

    [XmlAttribute("creation")]
    public string? Creation { get; set; }

    [XmlAttribute("queuing")]
    public string? Queuing { get; set; }
}

public class UnitTestResult
{
    [XmlAttribute("testName")]
    public string? TestName { get; set; }

    [XmlAttribute("outcome")]
    public string? Outcome { get; set; }

    [XmlAttribute("startTime")]
    public string? StartTime { get; set; }

    [XmlAttribute("endTime")]
    public string? EndTime { get; set; }

    [XmlAttribute("duration")]
    public string? Duration { get; set; }

    public Output? Output { get; set; }
}

public class Output
{
    [XmlAnyElement]
    public XmlElement? ErrorInfo { get; set; }
    public string? StdOut { get; set; }

    [XmlIgnore]
    public string ErrorInfoString => ErrorInfo?.InnerText ?? string.Empty;
}

public class ResultSummary
{
    public string? Outcome { get; set; }
    public Counters? Counters { get; set; }
}

public class Counters
{
    [XmlAttribute("total")]
    public int Total { get; set; }

    [XmlAttribute("executed")]
    public int Executed { get; set; }

    [XmlAttribute("passed")]
    public int Passed { get; set; }

    [XmlAttribute("failed")]
    public int Failed { get; set; }

    [XmlAttribute("error")]
    public int Error { get; set; }

    [XmlAttribute("timeout")]
    public int Timeout { get; set; }

    [XmlAttribute("aborted")]
    public int Aborted { get; set; }

    [XmlAttribute("inconclusive")]
    public int Inconclusive { get; set; }

    [XmlAttribute("passedButRunAborted")]
    public int PassedButRunAborted { get; set; }

    [XmlAttribute("notRunnable")]
    public int NotRunnable { get; set; }

    [XmlAttribute("notExecuted")]
    public int NotExecuted { get; set; }
}

// ============================================================================
// Record Types
// ============================================================================

record TestRunSummary(string Icon, string Os, string Title, int Passed, int Failed, int Skipped, int Total, double DurationMinutes);

record TestResult(string Name, string Outcome, TimeSpan StartTime, TimeSpan EndTime, string? ErrorMessage = null, string? Stdout = null);
