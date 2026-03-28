---
title: "나만의 스크립트 작성"
---

앞 절에서 `/release-note` 명령어로 릴리스 노트 자동화를 직접 실행해봤습니다. 그 과정에서 `AnalyzeAllComponents.cs`, `ExtractApiChanges.cs` 같은 C# 스크립트가 핵심 역할을 한다는 것을 확인했습니다. 이 스크립트들은 모두 .NET 10의 File-based App 기능으로 만들어진 것입니다.

이번 절에서는 .NET 10 File-based App을 직접 작성해봅니다. 단순한 Hello World부터 시작해서 CLI 인자 처리, 파일 시스템 분석, Git 커밋 분석까지 단계적으로 난이도를 높여가겠습니다. 이 과정을 마치면 릴리스 노트 자동화 스크립트의 코드를 읽고 수정할 수 있는 기반이 갖춰집니다.

## 실습 1: Hello World

모든 것의 시작은 가장 단순한 프로그램입니다. .NET 10 File-based App은 `.csproj` 파일 없이 `.cs` 파일 하나로 실행됩니다. 프로젝트 설정이나 빌드 구성 없이 바로 코드를 작성하고 실행할 수 있다는 점이 스크립트 작성에 적합합니다.

```csharp
#!/usr/bin/env dotnet

// hello.cs - 간단한 Hello World
Console.WriteLine("Hello, World!");
```

첫 줄의 `#!/usr/bin/env dotnet`은 Shebang 라인으로, Unix 환경에서 `./hello.cs`로 직접 실행할 수 있게 합니다. Windows에서는 `dotnet hello.cs`로 실행합니다.

```bash
dotnet hello.cs
# 출력: Hello, World!
```

## 실습 2: 인자 처리

실제 도구를 만들려면 사용자로부터 입력을 받아야 합니다. 이름을 받아서 인사하는 간단한 프로그램을 만들되, `System.CommandLine` 패키지로 체계적인 CLI 인터페이스를 구성해보겠습니다. 릴리스 노트 스크립트에서 `--base`, `--target` 같은 옵션을 처리하는 것도 바로 이 패턴입니다.

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

`#:package` 지시자는 File-based App에서 NuGet 패키지를 참조하는 방법입니다. `.csproj`의 `PackageReference` 역할을 합니다.

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

`System.CommandLine`이 `--help` 옵션을 자동으로 생성해준다는 점을 확인해보세요. 인자와 옵션의 설명이 도움말에 그대로 표시됩니다.

## 실습 3: 파일 분석 도구

이제 실용적인 도구를 만들어봅시다. 디렉터리를 순회하며 확장자별 파일 통계를 보여주는 도구입니다. 릴리스 노트 자동화에서 "31 files, 19 commits"같은 통계를 산출하는 것과 비슷한 패턴으로, 파일 시스템을 탐색하고 결과를 정리해서 보여줍니다.

여기서는 `Spectre.Console` 패키지를 추가로 사용합니다. 테이블, 색상, 구분선 같은 시각 요소를 콘솔에 쉽게 출력할 수 있는 라이브러리입니다.

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

실행하면 확장자별로 파일 수와 크기를 정리한 테이블이 출력됩니다.

```bash
# 현재 디렉터리 분석
dotnet file-stats.cs

# 특정 디렉터리 분석
dotnet file-stats.cs --path ./src

# 상위 5개만 표시
dotnet file-stats.cs --path ./src --top 5
```

출력은 다음과 같은 형태입니다.

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

## 실습 4: 커밋 분석 도구

마지막 실습은 릴리스 노트 자동화의 핵심과 가장 가까운 도구입니다. Git 커밋 메시지를 읽어서 Conventional Commits 타입별로 분류하고, 시각적인 막대 그래프로 표시합니다. Phase 3에서 Claude가 수행하는 커밋 분석의 축소판이라고 할 수 있습니다.

이 스크립트는 외부 프로세스(`git log`)를 실행하고 그 출력을 파싱하는 비동기 패턴도 포함하고 있어서, 실전에서 자주 쓰이는 기법을 익힐 수 있습니다.

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

```bash
# 최근 50개 커밋 분석 (기본값)
dotnet commit-analyzer.cs

# 최근 100개 커밋 분석
dotnet commit-analyzer.cs --count 100
```

출력은 다음과 같은 형태입니다.

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

## 핵심 패턴 정리

네 개의 실습을 통해 .NET 10 File-based App의 공통 구조가 보이기 시작했을 것입니다. 릴리스 노트 자동화 스크립트들도 모두 이 패턴을 따릅니다.

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

자주 사용하는 패키지도 정리해두면 새 스크립트를 만들 때 편리합니다.

| 패키지 | 용도 |
|--------|------|
| `System.CommandLine@2.0.1` | CLI 인자 파싱 |
| `Spectre.Console@0.54.0` | 콘솔 UI |
| `System.Text.Json` | JSON 처리 (기본 포함) |

## FAQ

### Q1: File-based App에서 `#:package` 지시자로 참조할 수 있는 패키지에 제한이 있나요?
**A**: NuGet에 공개된 모든 패키지를 참조할 수 있습니다. 다만 **네이티브 종속성이 있는 패키지(예: SQLite)나** 빌드 시 추가 설정이 필요한 패키지는 File-based App 환경에서 정상 동작하지 않을 수 있습니다. `System.CommandLine`, `Spectre.Console`, `System.Text.Json` 같은 순수 .NET 패키지는 문제없이 사용할 수 있습니다.

### Q2: `System.CommandLine`의 `SetAction`과 `SetHandler`는 어떻게 다른가요?
**A**: `SetHandler`는 이전 버전의 API이고, **`SetAction`은 `System.CommandLine` 2.0.1에서** 도입된 새로운 핸들러 등록 방식입니다. `SetAction`은 `ParseResult`를 직접 받아 더 유연하게 인자를 처리할 수 있으며, 이 튜토리얼의 모든 스크립트는 `SetAction` 패턴을 사용합니다.

### Q3: `Spectre.Console` 없이 기본 `Console.WriteLine`만으로 스크립트를 작성해도 되나요?
**A**: 가능합니다. `Spectre.Console`은 테이블, 색상, 스피너 같은 **시각적 요소를 쉽게 추가하기 위한** 선택적 패키지입니다. 기본 `Console.WriteLine`으로도 동일한 기능을 구현할 수 있으며, 출력을 파이프라인으로 전달하거나 로그 파일로 리다이렉트할 때는 오히려 기본 콘솔이 더 적합할 수 있습니다.

### Q4: File-based App의 `.cs` 파일과 일반 C# 프로젝트의 `.cs` 파일은 어떤 차이가 있나요?
**A**: File-based App의 `.cs` 파일은 **Shebang 라인(`#!/usr/bin/env dotnet`)과 `#:package` 지시자를** 포함할 수 있으며, `.csproj` 파일 없이 `dotnet <파일명>.cs`로 직접 실행됩니다. 일반 프로젝트의 `.cs` 파일은 반드시 `.csproj`와 함께 `dotnet run`으로 실행해야 합니다. File-based App은 스크립트성 작업에, 일반 프로젝트는 라이브러리나 대규모 애플리케이션에 적합합니다.

이제 릴리스 노트 자동화 스크립트의 코드를 읽을 때, 각 부분이 어떤 역할을 하는지 파악할 수 있을 것입니다. 다음 절에서는 실습 중 발생할 수 있는 문제와 해결 방법을 살펴봅니다.

- [문제 해결 가이드](03-troubleshooting.md)
