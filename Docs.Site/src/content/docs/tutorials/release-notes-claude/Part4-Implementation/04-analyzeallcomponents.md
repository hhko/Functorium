---
title: "AnalyzeAllComponents"
---

프로젝트에 수십 개의 커밋이 쌓였을 때, 어떤 컴포넌트가 얼마나 변경되었는지 수동으로 파악하는 것은 비효율적입니다. 컴포넌트마다 Git 로그를 일일이 뒤지고, 커밋을 분류하고, 변경 통계를 정리하는 과정에서 시간도 오래 걸리고 빠뜨리기도 쉽습니다. AnalyzeAllComponents.cs는 이 데이터 수집 작업을 자동화하는 스크립트입니다. **Phase 2: 데이터 수집의** 핵심으로, 모든 컴포넌트의 변경사항을 체계적으로 수집하여 Markdown 분석 파일로 생성합니다.

## 파일 위치와 사용법

```txt
.release-notes/scripts/AnalyzeAllComponents.cs
```

```bash
# 기본 실행
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD

# 첫 배포 (초기 커밋부터)
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

## 스크립트 구조

먼저 패키지를 설정하고, CLI 옵션을 정의한 뒤, 메인 로직이 실행되는 순서로 구성되어 있습니다.

### 패키지 참조 및 CLI 옵션

스크립트는 System.CommandLine과 Spectre.Console 두 패키지를 사용하며, `--base`와 `--target` 두 Option으로 비교 대상 브랜치를 받습니다.

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

핸들러는 비동기로 설정되어, 파싱된 브랜치 값을 메인 분석 함수에 전달합니다.

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

## 스크립트가 실행되면 일어나는 일

CLI 파싱이 끝나면 스크립트는 네 단계를 순서대로 수행합니다. 컴포넌트 목록을 로드하고, 각 컴포넌트의 Git 변경사항을 분석하고, 커밋을 분류한 뒤, 최종 요약을 생성합니다.

### Step 1: 컴포넌트 목록 로드

먼저 설정 파일에서 분석할 컴포넌트 목록을 읽어옵니다. 설정 파일이 없으면 기본값(Functorium, Functorium.Testing, Docs)을 사용합니다.

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

### Step 2: 각 컴포넌트 Git 분석

컴포넌트 목록이 준비되면, 각 컴포넌트에 대해 Git 명령어로 변경사항을 수집합니다. `git diff --stat`으로 변경 통계를, `git log --oneline`으로 전체 커밋 목록을 가져옵니다.

```csharp
foreach (var component in components)
{
    // 변경 통계
    var diffStat = await RunGitAsync($"diff --stat {baseBranch}..{targetBranch} -- {component}");

    // 모든 커밋
    var commits = await RunGitAsync($"log --oneline {baseBranch}..{targetBranch} -- {component}");

    // 분류된 커밋 (Feature, Bug Fix, Breaking Change)
    // Conventional Commits 규격에 따라 정확한 타입만 검색
    var featureCommits = await RunGitAsync($"log --grep=\"^feat\" --oneline ...");
    var bugFixCommits = await RunGitAsync($"log --grep=\"^fix\" --oneline ...");
    var breakingCommits = FilterBreakingChanges(commits);

    // Markdown 파일 생성
    await WriteAnalysisFileAsync(component, diffStat, commits, ...);
}
```

### Step 3: 커밋 분류

수집된 커밋은 Conventional Commits 규격에 따라 분류됩니다. Feature 커밋은 `^feat` 패턴으로, Bug Fix 커밋은 `^fix` 패턴으로 Git 로그를 검색합니다.

```csharp
// Feature 커밋 - "^feat" 패턴으로 검색
var featResult = await RunGitAsync(
    $"log --grep=\"^feat\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"",
    gitRoot);

// Bug Fix 커밋 - "^fix" 패턴으로 검색
var fixResult = await RunGitAsync(
    $"log --grep=\"^fix\" --oneline --no-merges \"{baseBranch}..{targetBranch}\" -- \"{componentPath}/\"",
    gitRoot);

// Breaking Change 커밋
// 방법 1: 타입 뒤 ! (예: feat!:, fix!:)
// 방법 2: BREAKING CHANGE 키워드
var breakingPattern = new Regex(@"\b\w+!:", RegexOptions.Compiled);
var breakingCommits = allCommitsResult.Output
    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
    .Where(commit =>
        commit.Contains("BREAKING CHANGE", StringComparison.OrdinalIgnoreCase) ||
        breakingPattern.IsMatch(commit))
    .ToList();
```

> **참고**: Conventional Commits 규격에 따라 `feat`, `fix` 등 정확한 커밋 타입만 검색합니다.
> 이전 버전에서는 `feat|feature|add`, `fix|bug` 등 유사 키워드도 포함했으나,
> 규격 준수를 위해 정확한 타입 접두사만 검색하도록 개선되었습니다.

Breaking Change는 두 가지 방식으로 감지합니다. 타입 뒤에 느낌표가 붙는 경우(`feat!:`, `fix!:`)와 커밋 메시지에 `BREAKING CHANGE` 키워드가 포함된 경우입니다.

### Step 4: 분석 요약 생성

모든 컴포넌트 분석이 끝나면, 전체 결과를 하나의 요약 파일로 정리합니다.

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

## 출력 파일

스크립트는 두 종류의 파일을 생성합니다. 컴포넌트별 상세 분석 파일과 전체 요약 파일입니다.

### 컴포넌트 분석 파일

각 컴포넌트에 대해 `Functorium.md`, `Functorium.Testing.md` 같은 파일이 생성됩니다. 변경 통계, 전체 커밋 목록, 기여자, 분류된 커밋 정보가 포함됩니다.

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

`analysis-summary.md`에는 모든 컴포넌트의 분석 결과가 한곳에 모입니다.

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

## 주요 함수

출력 파일이 어떻게 생성되는지 살펴보았으니, 이제 스크립트를 구성하는 핵심 함수들을 살펴보겠습니다.

### LoadComponentsAsync

설정 파일에서 컴포넌트 목록을 로드합니다. JSON을 파싱하여 경로 목록을 반환합니다.

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

Git 명령어를 외부 프로세스로 실행하고 출력을 반환합니다.

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

분석 시작 전에 base 브랜치가 실제로 존재하는지 확인합니다.

```csharp
static async Task<bool> BranchExistsAsync(string branch, string gitRoot)
{
    var result = await RunGitAsync($"rev-parse --verify {branch}");
    return !string.IsNullOrEmpty(result);
}
```

## 콘솔 출력

스크립트는 Spectre.Console을 활용하여 실행 과정을 시각적으로 표시합니다. Rule로 헤더를 그리고, Table로 설정 정보를 보여주고, 각 단계마다 진행 상황을 출력합니다.

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

결과 테이블에서는 각 컴포넌트의 파일 수와 커밋 수를 보여줍니다.

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

## 오류 처리

base 브랜치가 존재하지 않으면 Panel로 안내 메시지를 표시하고, 첫 배포 시 사용할 명령어를 알려줍니다.

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

### Q1: `component-priority.json` 설정 파일이 없으면 어떻게 되나요?
**A**: 설정 파일이 없으면 기본값으로 `Src/Functorium`, `Src/Functorium.Testing`, `Docs` 세 가지 컴포넌트를 분석합니다. 프로젝트에 다른 컴포넌트가 있거나 분석 대상을 커스터마이즈하고 싶다면 설정 파일을 생성해야 합니다.

### Q2: Breaking Change 감지에서 Conventional Commits의 `feat!:` 패턴과 `BREAKING CHANGE` 키워드는 어떻게 다른가요?
**A**: 두 가지 모두 Breaking Change를 표시하는 Conventional Commits 규격의 일부입니다. `feat!:`은 타입 뒤에 느낌표를 붙이는 **축약 표기이고,** `BREAKING CHANGE`는 커밋 본문이나 메시지에 키워드를 포함하는 **명시적 표기입니다.** 스크립트는 두 패턴을 모두 검색하여 누락 없이 감지합니다.

### Q3: `RunGitAsync` 함수가 외부 프로세스를 사용하는 이유는 무엇인가요?
**A**: .NET에는 Git을 직접 조작하는 기본 내장 라이브러리가 없으므로, `git` 명령어를 외부 프로세스(`Process` 클래스)로 실행하고 표준 출력을 캡처합니다. `libgit2sharp` 같은 라이브러리도 있지만, File-based App의 단순성을 유지하면서 Git CLI의 모든 기능을 활용하기 위해 외부 프로세스 방식을 선택한 것입니다.

AnalyzeAllComponents.cs가 수집한 데이터는 이후 Phase 3에서 커밋 분석과 기능 그룹화의 기초 자료로 사용됩니다. 그러나 커밋 로그만으로는 실제 API가 어떻게 변경되었는지 알 수 없습니다. 다음 절에서는 API 정확성을 보장하기 위해 코드에서 직접 Public API를 추출하는 ExtractApiChanges.cs를 살펴보겠습니다.
