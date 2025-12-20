# 5.4 AnalyzeAllComponents.cs 분석

> 이 절에서는 컴포넌트별 변경사항을 분석하는 AnalyzeAllComponents.cs 스크립트의 구조와 동작을 분석합니다.

---

## 개요

AnalyzeAllComponents.cs는 **Phase 2: 데이터 수집**에서 사용되는 핵심 스크립트입니다.

```txt
역할:
├── Git 커밋 히스토리 분석
├── 컴포넌트별 변경 파일 추출
├── 커밋 분류 (Feature, Bug Fix, Breaking Change)
└── Markdown 분석 파일 생성
```

---

## 파일 위치

```txt
.release-notes/scripts/AnalyzeAllComponents.cs
```

---

## 사용법

```bash
# 기본 실행
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD

# 첫 배포 (초기 커밋부터)
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

---

## 스크립트 구조

### 1. 패키지 참조 및 using

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

### 2. CLI 옵션 정의

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

### 3. 메인 핸들러

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

---

## 주요 로직

### Step 1: 컴포넌트 로드

설정 파일에서 분석할 컴포넌트 목록을 로드합니다:

```csharp
var configFile = Path.Combine(scriptsDir, "config", "component-priority.json");
var components = await LoadComponentsAsync(configFile, gitRoot);

// 설정 파일이 없으면 기본값 사용
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

### Step 2: 각 컴포넌트 분석

각 컴포넌트에 대해 Git 명령어로 변경사항을 분석합니다:

```csharp
foreach (var component in components)
{
    // 변경 통계
    var diffStat = await RunGitAsync($"diff --stat {baseBranch}..{targetBranch} -- {component}");

    // 모든 커밋
    var commits = await RunGitAsync($"log --oneline {baseBranch}..{targetBranch} -- {component}");

    // 분류된 커밋 (Feature, Bug Fix, Breaking Change)
    var featureCommits = FilterCommits(commits, "feat|feature|add");
    var bugFixCommits = FilterCommits(commits, "fix|bug");
    var breakingCommits = FilterBreakingChanges(commits);

    // Markdown 파일 생성
    await WriteAnalysisFileAsync(component, diffStat, commits, ...);
}
```

### Step 3: 커밋 분류

Conventional Commits 패턴으로 커밋을 분류합니다:

```csharp
// Feature 커밋
static List<string> FilterFeatureCommits(List<string> commits)
{
    var pattern = new Regex(@"\b(feat|feature|add)\b", RegexOptions.IgnoreCase);
    return commits.Where(c => pattern.IsMatch(c)).ToList();
}

// Bug Fix 커밋
static List<string> FilterBugFixCommits(List<string> commits)
{
    var pattern = new Regex(@"\b(fix|bug)\b", RegexOptions.IgnoreCase);
    return commits.Where(c => pattern.IsMatch(c)).ToList();
}

// Breaking Change 커밋
static List<string> FilterBreakingChanges(List<string> commits)
{
    // 방법 1: 타입 뒤 !
    var bangPattern = new Regex(@"\b\w+!:", RegexOptions.IgnoreCase);

    // 방법 2: breaking/BREAKING 키워드
    var keywordPattern = new Regex(@"\b(breaking|BREAKING)\b");

    return commits.Where(c =>
        bangPattern.IsMatch(c) || keywordPattern.IsMatch(c)
    ).ToList();
}
```

### Step 4: 분석 요약 생성

모든 분석 결과를 요약 파일로 생성합니다:

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

---

## 출력 파일 구조

### 컴포넌트 분석 파일

`Functorium.md` 예시:

````markdown
# Analysis for Src/Functorium

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Change Summary

 Src/Functorium/Abstractions/Errors/ErrorCodeFactory.cs | 45 +++++
 Src/Functorium/Applications/ElapsedTimeCalculator.cs   | 32 +++
 37 files changed, 1542 insertions(+), 89 deletions(-)

## All Commits

6b5ef99 feat(errors): Add ErrorCodeFactory
853c918 feat(logging): Add Serilog integration
c5e604f fix(build): Fix NuGet package icon path
...

## Top Contributors

1. developer@example.com (15 commits)
2. other@example.com (4 commits)

## Categorized Commits

### Feature Commits

6b5ef99 feat(errors): Add ErrorCodeFactory
853c918 feat(logging): Add Serilog integration

### Bug Fixes

c5e604f fix(build): Fix NuGet package icon path

### Breaking Changes

(none)
````

### 분석 요약 파일

`analysis-summary.md`:

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

---

## 주요 함수

### LoadComponentsAsync

설정 파일에서 컴포넌트 목록 로드:

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

Git 명령어 실행:

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

브랜치 존재 여부 확인:

```csharp
static async Task<bool> BranchExistsAsync(string branch, string gitRoot)
{
    var result = await RunGitAsync($"rev-parse --verify {branch}");
    return !string.IsNullOrEmpty(result);
}
```

---

## 콘솔 출력

### 진행 상황 표시

```csharp
// 헤더
AnsiConsole.Write(new Rule("[bold blue]Analyzing All Components[/]").RuleStyle("blue"));

// 정보 테이블
var infoTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
infoTable.AddRow("[white]Base Branch[/]", $"[cyan]{baseBranch}[/]");
infoTable.AddRow("[white]Target Branch[/]", $"[cyan]{targetBranch}[/]");
AnsiConsole.Write(infoTable);

// 단계별 진행
AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Loading components...[/]");
AnsiConsole.MarkupLine($"   [green]Found[/] {component}");

// 스피너로 분석 진행
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync($"Analyzing {component}...", async ctx => { ... });
```

### 결과 테이블

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

---

## 오류 처리

### Base Branch 없음

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

---

## 요약

| 항목 | 설명 |
|------|------|
| 목적 | 컴포넌트별 Git 변경사항 분석 |
| 입력 | --base, --target 브랜치 |
| 출력 | .analysis-output/*.md |
| 분류 | Feature, Bug Fix, Breaking Change |
| 패키지 | System.CommandLine, Spectre.Console |

---

## 다음 단계

- [5.5 ExtractApiChanges.cs 분석](05-extract-api-changes.md)
