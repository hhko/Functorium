# 5.7 SummarizeSlowestTests.cs 분석

> 이 절에서는 테스트 결과를 분석하여 요약하는 SummarizeSlowestTests.cs 스크립트를 분석합니다.

---

## 개요

SummarizeSlowestTests.cs는 **TRX 테스트 결과 파일을 분석**하여 가장 느린 테스트와 통계를 요약하는 스크립트입니다.

```txt
역할:
├── TRX 파일 탐색
├── 테스트 결과 파싱 (XML)
├── 실행 시간별 정렬
├── Markdown 요약 생성
└── 콘솔 테이블 출력
```

---

## 파일 위치

```txt
.release-notes/scripts/SummarizeSlowestTests.cs
```

---

## 사용법

```bash
# 기본 실행 (TestResults 폴더 자동 탐색)
dotnet SummarizeSlowestTests.cs

# 특정 폴더 지정
dotnet SummarizeSlowestTests.cs --path ./TestResults

# 상위 N개 테스트만 표시
dotnet SummarizeSlowestTests.cs --top 20
```

---

## 패키지 참조

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Spectre.Console;
```

---

## CLI 정의

```csharp
var pathOption = new Option<string>("--path", "Path to TestResults folder");
pathOption.DefaultValueFactory = (_) => "TestResults";

var topOption = new Option<int>("--top", "Number of slowest tests to show");
topOption.DefaultValueFactory = (_) => 10;

var rootCommand = new RootCommand("Summarize slowest tests from TRX files")
{
    pathOption,
    topOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(pathOption)!;
    var top = parseResult.GetValue(topOption);

    await SummarizeTestsAsync(path, top);
    return 0;
});
```

---

## TRX 파일 구조

TRX(Test Results XML)는 Visual Studio/dotnet test가 생성하는 테스트 결과 형식입니다:

```xml
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult
      testId="abc-123"
      testName="Should_Return_Error_When_Invalid"
      outcome="Passed"
      duration="00:00:01.234">
    </UnitTestResult>
    <UnitTestResult
      testId="def-456"
      testName="Should_Process_All_Items"
      outcome="Passed"
      duration="00:00:05.678">
    </UnitTestResult>
  </Results>
  <ResultSummary outcome="Completed">
    <Counters total="150" passed="148" failed="2" />
  </ResultSummary>
</TestRun>
```

---

## 주요 로직

### Step 1: TRX 파일 탐색

```csharp
static async Task SummarizeTestsAsync(string path, int top)
{
    // TRX 파일 탐색
    var trxFiles = Directory.GetFiles(path, "*.trx", SearchOption.AllDirectories);

    if (trxFiles.Length == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No TRX files found[/]");
        return;
    }

    AnsiConsole.MarkupLine($"[green]Found[/] {trxFiles.Length} TRX file(s)");
}
```

### Step 2: TRX 파싱

```csharp
static List<TestResult> ParseTrxFile(string trxPath)
{
    var results = new List<TestResult>();
    var doc = XDocument.Load(trxPath);
    var ns = doc.Root.GetDefaultNamespace();

    var unitTestResults = doc.Descendants(ns + "UnitTestResult");

    foreach (var result in unitTestResults)
    {
        var testName = result.Attribute("testName")?.Value ?? "Unknown";
        var outcome = result.Attribute("outcome")?.Value ?? "Unknown";
        var durationStr = result.Attribute("duration")?.Value ?? "00:00:00";

        if (TimeSpan.TryParse(durationStr, out var duration))
        {
            results.Add(new TestResult
            {
                TestName = testName,
                Outcome = outcome,
                Duration = duration,
                SourceFile = Path.GetFileName(trxPath)
            });
        }
    }

    return results;
}

record TestResult
{
    public string TestName { get; init; }
    public string Outcome { get; init; }
    public TimeSpan Duration { get; init; }
    public string SourceFile { get; init; }
}
```

### Step 3: 통계 계산

```csharp
static TestStatistics CalculateStatistics(List<TestResult> results)
{
    return new TestStatistics
    {
        TotalTests = results.Count,
        PassedTests = results.Count(r => r.Outcome == "Passed"),
        FailedTests = results.Count(r => r.Outcome == "Failed"),
        SkippedTests = results.Count(r => r.Outcome == "NotExecuted"),
        TotalDuration = TimeSpan.FromTicks(results.Sum(r => r.Duration.Ticks)),
        AverageDuration = TimeSpan.FromTicks((long)results.Average(r => r.Duration.Ticks)),
        SlowestTests = results.OrderByDescending(r => r.Duration).Take(top).ToList()
    };
}
```

### Step 4: 콘솔 출력

```csharp
static void DisplayResults(TestStatistics stats, int top)
{
    // 헤더
    AnsiConsole.Write(new Rule("[bold blue]Test Summary[/]").RuleStyle("blue"));
    AnsiConsole.WriteLine();

    // 통계 테이블
    var statsTable = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Metric")
        .AddColumn("Value");

    statsTable.AddRow("Total Tests", stats.TotalTests.ToString());
    statsTable.AddRow("Passed", $"[green]{stats.PassedTests}[/]");
    statsTable.AddRow("Failed", stats.FailedTests > 0 ? $"[red]{stats.FailedTests}[/]" : "0");
    statsTable.AddRow("Skipped", stats.SkippedTests.ToString());
    statsTable.AddRow("Total Duration", stats.TotalDuration.ToString(@"hh\:mm\:ss\.fff"));
    statsTable.AddRow("Average Duration", stats.AverageDuration.ToString(@"ss\.fff") + "s");

    AnsiConsole.Write(statsTable);
    AnsiConsole.WriteLine();

    // 가장 느린 테스트 테이블
    AnsiConsole.Write(new Rule($"[bold]Top {top} Slowest Tests[/]").RuleStyle("yellow"));
    AnsiConsole.WriteLine();

    var slowestTable = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("#")
        .AddColumn("Test Name")
        .AddColumn("Duration")
        .AddColumn("Status");

    for (int i = 0; i < stats.SlowestTests.Count; i++)
    {
        var test = stats.SlowestTests[i];
        var status = test.Outcome == "Passed" ? "[green]PASS[/]" : "[red]FAIL[/]";
        slowestTable.AddRow(
            (i + 1).ToString(),
            TruncateName(test.TestName, 60),
            test.Duration.ToString(@"ss\.fff") + "s",
            status
        );
    }

    AnsiConsole.Write(slowestTable);
}
```

### Step 5: Markdown 출력

```csharp
static async Task WriteMarkdownSummary(TestStatistics stats, string outputPath)
{
    var sb = new StringBuilder();

    sb.AppendLine("# Test Summary");
    sb.AppendLine();
    sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine();

    sb.AppendLine("## Statistics");
    sb.AppendLine();
    sb.AppendLine("| Metric | Value |");
    sb.AppendLine("|--------|-------|");
    sb.AppendLine($"| Total Tests | {stats.TotalTests} |");
    sb.AppendLine($"| Passed | {stats.PassedTests} |");
    sb.AppendLine($"| Failed | {stats.FailedTests} |");
    sb.AppendLine($"| Total Duration | {stats.TotalDuration:hh\\:mm\\:ss} |");
    sb.AppendLine();

    sb.AppendLine("## Slowest Tests");
    sb.AppendLine();
    sb.AppendLine("| # | Test Name | Duration | Status |");
    sb.AppendLine("|---|-----------|----------|--------|");

    for (int i = 0; i < stats.SlowestTests.Count; i++)
    {
        var test = stats.SlowestTests[i];
        sb.AppendLine($"| {i + 1} | {test.TestName} | {test.Duration:ss\\.fff}s | {test.Outcome} |");
    }

    await File.WriteAllTextAsync(outputPath, sb.ToString());
}
```

---

## 콘솔 출력 예시

```txt
━━━━━━━━━━━━ Test Summary ━━━━━━━━━━━━

╭─────────────────┬────────────╮
│ Metric          │ Value      │
├─────────────────┼────────────┤
│ Total Tests     │ 156        │
│ Passed          │ 154        │
│ Failed          │ 2          │
│ Skipped         │ 0          │
│ Total Duration  │ 00:02:34   │
│ Average Duration│ 0.989s     │
╰─────────────────┴────────────╯

━━━━━━━━━ Top 10 Slowest Tests ━━━━━━━━━

╭───┬─────────────────────────────────────────────┬──────────┬────────╮
│ # │ Test Name                                   │ Duration │ Status │
├───┼─────────────────────────────────────────────┼──────────┼────────┤
│ 1 │ Integration_Should_Process_Large_Dataset   │ 15.234s  │ PASS   │
│ 2 │ E2E_Full_Pipeline_Test                     │ 12.456s  │ PASS   │
│ 3 │ Should_Handle_Concurrent_Requests          │  8.123s  │ PASS   │
│ 4 │ Database_Migration_Test                    │  5.678s  │ PASS   │
│ 5 │ Should_Timeout_After_30_Seconds            │  3.456s  │ FAIL   │
╰───┴─────────────────────────────────────────────┴──────────┴────────╯
```

---

## Markdown 출력 예시

`test-summary.md`:

```markdown
# Test Summary

Generated: 2025-12-19 10:30:00

## Statistics

| Metric | Value |
|--------|-------|
| Total Tests | 156 |
| Passed | 154 |
| Failed | 2 |
| Total Duration | 00:02:34 |

## Slowest Tests

| # | Test Name | Duration | Status |
|---|-----------|----------|--------|
| 1 | Integration_Should_Process_Large_Dataset | 15.234s | Passed |
| 2 | E2E_Full_Pipeline_Test | 12.456s | Passed |
| 3 | Should_Handle_Concurrent_Requests | 8.123s | Passed |
| 4 | Database_Migration_Test | 5.678s | Passed |
| 5 | Should_Timeout_After_30_Seconds | 3.456s | Failed |
```

---

## 릴리스 노트에서의 활용

테스트 요약은 선택적으로 릴리스 노트에 포함될 수 있습니다:

```markdown
## 테스트 커버리지

| 항목 | 값 |
|------|-----|
| 전체 테스트 | 156개 |
| 성공 | 154개 (98.7%) |
| 실패 | 2개 |
| 실행 시간 | 2분 34초 |

### 성능 주의 테스트

다음 테스트는 실행 시간이 5초를 초과합니다:
- `Integration_Should_Process_Large_Dataset` (15.2s)
- `E2E_Full_Pipeline_Test` (12.5s)
```

---

## 요약

| 항목 | 설명 |
|------|------|
| 목적 | TRX 테스트 결과 분석 및 요약 |
| 입력 | --path (TestResults 폴더), --top (표시 개수) |
| 출력 | 콘솔 테이블, Markdown 파일 |
| 분석 | 통계, 가장 느린 테스트 |
| 패키지 | System.CommandLine, Spectre.Console |

---

## 5장 완료

5장에서 다룬 C# 스크립트들의 요약:

| 스크립트 | 역할 | Phase |
|----------|------|-------|
| AnalyzeAllComponents.cs | 컴포넌트별 Git 변경 분석 | Phase 2 |
| ExtractApiChanges.cs | Public API 추출 | Phase 2 |
| ApiGenerator.cs | DLL에서 API 생성 | Phase 2 |
| SummarizeSlowestTests.cs | 테스트 결과 요약 | Phase 2 (선택) |

---

## 다음 단계

- [6.1 TEMPLATE.md 구조](../06-templates-and-config/01-template-md.md)
