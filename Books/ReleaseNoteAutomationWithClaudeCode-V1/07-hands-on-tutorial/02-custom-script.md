# 7.2 나만의 스크립트 작성

> 이 절에서는 .NET 10 File-based App을 직접 작성해봅니다.

---

## 실습 목표

이 실습을 완료하면:

- .NET 10 File-based App 작성법 이해
- System.CommandLine으로 CLI 인자 처리
- Spectre.Console로 콘솔 UI 구현

---

## 실습 1: Hello World

### 파일 생성

`hello.cs` 파일을 생성합니다:

```csharp
#!/usr/bin/env dotnet

// hello.cs - 간단한 Hello World
Console.WriteLine("Hello, World!");
```

### 실행

```bash
dotnet hello.cs
# 출력: Hello, World!
```

---

## 실습 2: 인자 처리

### 파일 생성

`greet.cs` 파일을 생성합니다:

```csharp
#!/usr/bin/env dotnet

// greet.cs - 이름을 받아 인사하기
#:package System.CommandLine@2.0.1

using System.CommandLine;

// 인자 정의
var nameArgument = new Argument<string>("name", "Your name");

// 옵션 정의
var loudOption = new Option<bool>("--loud", "Print in uppercase");
loudOption.AddAlias("-l");

// 명령 정의
var rootCommand = new RootCommand("Greet someone")
{
    nameArgument,
    loudOption
};

// 핸들러
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var name = parseResult.GetValue(nameArgument)!;
    var loud = parseResult.GetValue(loudOption);

    var message = $"Hello, {name}!";

    if (loud)
    {
        message = message.ToUpper();
    }

    Console.WriteLine(message);
    return 0;
});

return rootCommand.Parse(args).Invoke();
```

### 실행

```bash
# 기본 실행
dotnet greet.cs Alice
# 출력: Hello, Alice!

# 대문자 옵션
dotnet greet.cs Alice --loud
# 출력: HELLO, ALICE!

# 도움말
dotnet greet.cs --help
```

---

## 실습 3: 파일 분석 도구

### 파일 생성

`file-stats.cs` 파일을 생성합니다:

```csharp
#!/usr/bin/env dotnet

// file-stats.cs - 디렉터리의 파일 통계 분석
#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using Spectre.Console;

// 옵션 정의
var pathOption = new Option<string>("--path", "Directory to analyze");
pathOption.DefaultValueFactory = (_) => ".";
pathOption.AddAlias("-p");

var topOption = new Option<int>("--top", "Number of extensions to show");
topOption.DefaultValueFactory = (_) => 10;
topOption.AddAlias("-t");

// 명령 정의
var rootCommand = new RootCommand("Analyze file statistics")
{
    pathOption,
    topOption
};

// 핸들러
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(pathOption)!;
    var top = parseResult.GetValue(topOption);

    AnalyzeDirectory(path, top);
    return 0;
});

return rootCommand.Parse(args).Invoke();

// 분석 함수
static void AnalyzeDirectory(string path, int top)
{
    // 디렉터리 확인
    if (!Directory.Exists(path))
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] Directory not found: {path}");
        return;
    }

    // 헤더
    AnsiConsole.Write(new Rule("[bold blue]File Statistics[/]").RuleStyle("blue"));
    AnsiConsole.WriteLine();

    // 파일 수집
    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

    if (files.Length == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No files found[/]");
        return;
    }

    // 확장자별 그룹화
    var stats = files
        .GroupBy(f => Path.GetExtension(f).ToLower())
        .Select(g => new
        {
            Extension = string.IsNullOrEmpty(g.Key) ? "(no ext)" : g.Key,
            Count = g.Count(),
            TotalSize = g.Sum(f => new FileInfo(f).Length)
        })
        .OrderByDescending(x => x.Count)
        .Take(top);

    // 테이블 생성
    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Extension")
        .AddColumn(new TableColumn("Files").RightAligned())
        .AddColumn(new TableColumn("Size").RightAligned());

    foreach (var stat in stats)
    {
        var size = FormatSize(stat.TotalSize);
        table.AddRow(
            $"[cyan]{stat.Extension}[/]",
            stat.Count.ToString(),
            $"[dim]{size}[/]"
        );
    }

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();

    // 요약
    var totalSize = files.Sum(f => new FileInfo(f).Length);
    AnsiConsole.MarkupLine($"[dim]Total: {files.Length} files, {FormatSize(totalSize)}[/]");
}

// 크기 포맷 함수
static string FormatSize(long bytes)
{
    string[] units = { "B", "KB", "MB", "GB" };
    double size = bytes;
    int unit = 0;

    while (size >= 1024 && unit < units.Length - 1)
    {
        size /= 1024;
        unit++;
    }

    return $"{size:0.##} {units[unit]}";
}
```

### 실행

```bash
# 현재 디렉터리 분석
dotnet file-stats.cs

# 특정 디렉터리 분석
dotnet file-stats.cs --path ./src

# 상위 5개만 표시
dotnet file-stats.cs --path ./src --top 5
```

### 출력 예시

```txt
───────────────── File Statistics ─────────────────

╭───────────┬───────┬──────────╮
│ Extension │ Files │     Size │
├───────────┼───────┼──────────┤
│ .cs       │    45 │ 125.3 KB │
│ .json     │    12 │   8.5 KB │
│ .md       │     8 │  15.2 KB │
│ .csproj   │     5 │   3.1 KB │
│ .txt      │     3 │   1.2 KB │
╰───────────┴───────┴──────────╯

Total: 73 files, 153.3 KB
```

---

## 실습 4: 커밋 분석 도구

### 파일 생성

`commit-analyzer.cs` 파일을 생성합니다:

```csharp
#!/usr/bin/env dotnet

// commit-analyzer.cs - Git 커밋 분석 도구
#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;

// 옵션 정의
var countOption = new Option<int>("--count", "Number of commits to analyze");
countOption.DefaultValueFactory = (_) => 50;
countOption.AddAlias("-n");

// 명령 정의
var rootCommand = new RootCommand("Analyze git commits by type")
{
    countOption
};

// 핸들러
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var count = parseResult.GetValue(countOption);
    await AnalyzeCommitsAsync(count);
    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();

// 분석 함수
static async Task AnalyzeCommitsAsync(int count)
{
    // 헤더
    AnsiConsole.Write(new Rule("[bold blue]Commit Analysis[/]").RuleStyle("blue"));
    AnsiConsole.WriteLine();

    // Git 커밋 가져오기
    var commits = await GetCommitsAsync(count);

    if (commits.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No commits found[/]");
        return;
    }

    // 커밋 타입별 분류
    var types = new Dictionary<string, int>
    {
        { "feat", 0 },
        { "fix", 0 },
        { "docs", 0 },
        { "refactor", 0 },
        { "test", 0 },
        { "chore", 0 },
        { "other", 0 }
    };

    var typePattern = new Regex(@"^(\w+)(\(.+\))?!?:");

    foreach (var commit in commits)
    {
        var match = typePattern.Match(commit);
        if (match.Success)
        {
            var type = match.Groups[1].Value.ToLower();
            if (types.ContainsKey(type))
                types[type]++;
            else
                types["other"]++;
        }
        else
        {
            types["other"]++;
        }
    }

    // 결과 테이블
    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Type")
        .AddColumn(new TableColumn("Count").RightAligned())
        .AddColumn("Bar");

    var maxCount = types.Values.Max();

    foreach (var kvp in types.OrderByDescending(x => x.Value))
    {
        if (kvp.Value > 0)
        {
            var barLength = (int)((double)kvp.Value / maxCount * 20);
            var bar = new string('█', barLength);
            var color = GetTypeColor(kvp.Key);

            table.AddRow(
                $"[{color}]{kvp.Key}[/]",
                kvp.Value.ToString(),
                $"[{color}]{bar}[/]"
            );
        }
    }

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[dim]Analyzed {commits.Count} commits[/]");
}

// Git 명령 실행
static async Task<List<string>> GetCommitsAsync(int count)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"log --oneline -n {count} --format=%s",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.Start();
    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();

    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
}

// 타입별 색상
static string GetTypeColor(string type) => type switch
{
    "feat" => "green",
    "fix" => "red",
    "docs" => "blue",
    "refactor" => "yellow",
    "test" => "cyan",
    "chore" => "grey",
    _ => "white"
};
```

### 실행

```bash
# 최근 50개 커밋 분석 (기본값)
dotnet commit-analyzer.cs

# 최근 100개 커밋 분석
dotnet commit-analyzer.cs --count 100
```

### 출력 예시

```txt
───────────────── Commit Analysis ─────────────────

╭──────────┬───────┬──────────────────────╮
│ Type     │ Count │ Bar                  │
├──────────┼───────┼──────────────────────┤
│ feat     │    15 │ ████████████████████ │
│ fix      │     8 │ ██████████           │
│ docs     │     6 │ ████████             │
│ chore    │     5 │ ██████               │
│ refactor │     4 │ █████                │
│ test     │     3 │ ████                 │
│ other    │     2 │ ██                   │
╰──────────┴───────┴──────────────────────╯

Analyzed 50 commits
```

---

## 실습 정리

### 핵심 패턴

```csharp
#!/usr/bin/env dotnet              // 1. Shebang

#:package <패키지>@<버전>           // 2. 패키지 참조

using System;                       // 3. using 문

// 옵션/인자 정의                   // 4. CLI 정의
var option = new Option<string>("--name");
var rootCommand = new RootCommand { option };

// 핸들러                           // 5. 실행 로직
rootCommand.SetAction((parseResult, ct) => {
    var value = parseResult.GetValue(option);
    // 작업 수행
    return 0;
});

return rootCommand.Parse(args).Invoke();  // 6. 실행
```

### 자주 사용하는 패키지

| 패키지 | 용도 |
|--------|------|
| `System.CommandLine@2.0.1` | CLI 인자 파싱 |
| `Spectre.Console@0.54.0` | 콘솔 UI |
| `System.Text.Json` | JSON 처리 (기본 포함) |

---

## 다음 단계

- [7.3 문제 해결 가이드](03-troubleshooting.md)
