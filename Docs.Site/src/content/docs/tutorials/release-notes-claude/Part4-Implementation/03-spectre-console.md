---
title: "Spectre.Console 패키지"
---

자동화 스크립트가 `Console.WriteLine`으로 "Processing... Done: 10 files"만 출력한다면, 어떤 단계에서 무슨 일이 일어나는지 파악하기 어렵습니다. 컴포넌트가 여러 개이고 분석 시간이 길어질수록 이 문제는 심해집니다. Spectre.Console은 **개발자가 실제로 읽고 싶은 콘솔 출력을** 만들어주는 UI 라이브러리입니다. 테이블, 진행률 표시, 색상 강조 등을 통해 스크립트 실행 상황을 한눈에 파악할 수 있게 해줍니다.

## 패키지 설치

```csharp
#:package Spectre.Console@0.54.0
```

## 기본 출력: MarkupLine

가장 기본적인 기능은 색상과 스타일이 적용된 텍스트 출력입니다. 대괄호 안에 색상이나 스타일을 지정하고, `[/]`로 닫습니다.

```csharp
using Spectre.Console;

// 색상
AnsiConsole.MarkupLine("[red]Error:[/] Something went wrong");
AnsiConsole.MarkupLine("[green]Success:[/] Operation completed");
AnsiConsole.MarkupLine("[yellow]Warning:[/] Please check");
AnsiConsole.MarkupLine("[blue]Info:[/] Processing started");

// 스타일
AnsiConsole.MarkupLine("[bold]Bold text[/]");
AnsiConsole.MarkupLine("[dim]Dimmed text[/]");
AnsiConsole.MarkupLine("[italic]Italic text[/]");
AnsiConsole.MarkupLine("[underline]Underlined text[/]");

// 조합
AnsiConsole.MarkupLine("[bold red]Critical Error![/]");
AnsiConsole.MarkupLine("[bold green]✓[/] [dim]Task completed[/]");
```

사용 가능한 주요 색상은 `[red]`, `[green]`, `[yellow]`, `[blue]`, `[cyan]`, `[magenta]`, `[grey]`, `[white]` 등입니다.

## Table: 분석 결과를 구조화할 때

여러 컴포넌트의 분석 결과처럼 행과 열로 구조화된 데이터를 보여줘야 할 때 Table을 사용합니다. 각 컴포넌트의 파일 수, 커밋 수, 상태를 한눈에 비교할 수 있습니다.

```csharp
using Spectre.Console;

var table = new Table();

// 테두리 스타일
table.Border(TableBorder.Rounded);
table.BorderColor(Color.Grey);

// 열 추가
table.AddColumn("Name");
table.AddColumn("Status");
table.AddColumn(new TableColumn("Time").RightAligned());

// 행 추가
table.AddRow("App.cs", "[green]Done[/]", "0.3s");
table.AddRow("Config.cs", "[green]Done[/]", "0.1s");
table.AddRow("Program.cs", "[yellow]Processing[/]", "-");

// 출력
AnsiConsole.Write(table);
```

출력:
```txt
╭───────────┬────────────┬──────╮
│ Name      │ Status     │ Time │
├───────────┼────────────┼──────┤
│ App.cs    │ Done       │ 0.3s │
│ Config.cs │ Done       │ 0.1s │
│ Program.cs│ Processing │ -    │
╰───────────┴────────────┴──────╯
```

## Rule: 섹션을 시각적으로 구분할 때

스크립트 출력이 여러 단계로 나뉠 때, Rule로 섹션 경계를 명확하게 표시합니다. "분석 시작"과 "분석 완료" 사이에 구분선을 두면 어디서부터 어디까지가 한 단계인지 바로 알 수 있습니다.

```csharp
using Spectre.Console;

// 기본 Rule
AnsiConsole.Write(new Rule("[bold blue]Analysis Results[/]"));

// 스타일 적용
AnsiConsole.Write(new Rule("[bold green]Success[/]").RuleStyle("green"));

// 왼쪽 정렬
AnsiConsole.Write(new Rule("[yellow]Warning[/]").LeftJustified());
```

출력:
```txt
─────────────────── Analysis Results ───────────────────
```

## Panel: 중요한 정보를 강조할 때

오류 메시지나 설정 정보처럼 사용자가 반드시 확인해야 하는 내용을 박스 안에 넣어 눈에 띄게 만듭니다.

```csharp
using Spectre.Console;

var panel = new Panel("This is the content inside the panel.")
{
    Header = new PanelHeader("[bold]Information[/]"),
    Border = BoxBorder.Rounded,
    BorderStyle = new Style(Color.Blue),
    Padding = new Padding(2, 1)
};

AnsiConsole.Write(panel);
```

출력:
```txt
╭─────────── Information ───────────╮
│                                   │
│  This is the content inside the   │
│  panel.                           │
│                                   │
╰───────────────────────────────────╯
```

## Status: 장시간 작업의 진행 상태를 보여줄 때

프로젝트 빌드나 Git 분석처럼 시간이 걸리는 작업에서는 스피너로 "지금 무슨 작업 중인지"를 표시합니다. 작업 단계가 바뀔 때마다 상태 메시지를 갱신할 수 있습니다.

```csharp
using Spectre.Console;

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("cyan"))
    .StartAsync("Processing...", async ctx =>
    {
        // 작업 1
        ctx.Status("Loading files...");
        await Task.Delay(1000);

        // 작업 2
        ctx.Status("Analyzing...");
        await Task.Delay(1000);

        // 작업 3
        ctx.Status("Generating report...");
        await Task.Delay(1000);
    });

AnsiConsole.MarkupLine("[green]Done![/]");
```

스피너는 `Dots`(⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏), `Line`(-\\|/), `Star`(✶), `Arrow`(←↖↑↗→↘↓↙) 등 다양한 종류를 선택할 수 있습니다.

## Progress: 여러 작업의 진행률을 동시에 추적할 때

여러 컴포넌트를 병렬로 처리하면서 각각의 진행률을 보여주고 싶을 때 사용합니다.

```csharp
using Spectre.Console;

await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        var task1 = ctx.AddTask("[green]Downloading[/]");
        var task2 = ctx.AddTask("[blue]Installing[/]");

        while (!ctx.IsFinished)
        {
            task1.Increment(1.5);
            task2.Increment(0.5);
            await Task.Delay(50);
        }
    });
```

출력:
```txt
Downloading [████████████████████] 100%
Installing  [██████████──────────]  50%
```

## 실제 예시: AnalyzeAllComponents.cs의 콘솔 출력

릴리스 노트 자동화에서 이 요소들이 어떻게 조합되는지 살펴보겠습니다. Rule로 섹션을 구분하고, Table로 설정 정보를 보여주고, MarkupLine으로 단계별 진행을 표시하고, Status로 분석 작업을 표시하고, 마지막에 결과 Table을 출력합니다.

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Threading.Tasks;
using Spectre.Console;

// 헤더
AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[bold blue]Analyzing All Components[/]").RuleStyle("blue"));
AnsiConsole.WriteLine();

// 정보 테이블
var infoTable = new Table()
    .Border(TableBorder.Rounded)
    .BorderColor(Color.Grey);

infoTable.AddColumn(new TableColumn("[grey]Property[/]").NoWrap());
infoTable.AddColumn(new TableColumn("[grey]Value[/]"));
infoTable.AddRow("[white]Config[/]", "[dim]config/component-priority.json[/]");
infoTable.AddRow("[white]Output[/]", "[dim].analysis-output[/]");
infoTable.AddRow("[white]Base Branch[/]", "[cyan]origin/release/1.0[/]");
infoTable.AddRow("[white]Target Branch[/]", "[cyan]HEAD[/]");

AnsiConsole.Write(infoTable);
AnsiConsole.WriteLine();

// 단계별 진행
AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Loading components...[/]");
AnsiConsole.MarkupLine("   [green]Found[/] Src/Functorium");
AnsiConsole.MarkupLine("   [green]Found[/] Src/Functorium.Testing");
AnsiConsole.MarkupLine("   [dim]Total: 2 components[/]");
AnsiConsole.WriteLine();

// 스피너로 작업 표시
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("cyan"))
    .StartAsync("Analyzing components...", async ctx =>
    {
        await Task.Delay(2000);  // 실제 분석 작업
    });

// 결과 테이블
AnsiConsole.WriteLine();
var resultTable = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("Component")
    .AddColumn("Files")
    .AddColumn("Commits")
    .AddColumn("Status");

resultTable.AddRow("Functorium", "31", "19", "[green]✓[/]");
resultTable.AddRow("Functorium.Testing", "18", "13", "[green]✓[/]");

AnsiConsole.Write(resultTable);
AnsiConsole.WriteLine();

// 완료 메시지
AnsiConsole.Write(new Rule("[bold green]Analysis Complete[/]").RuleStyle("green"));
```

## 자주 사용하는 패턴

릴리스 노트 스크립트 전반에서 반복적으로 쓰이는 패턴을 정리합니다.

단계별 진행을 표시할 때:

```csharp
AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Loading...[/]");
AnsiConsole.MarkupLine("   [green]✓[/] File loaded");
AnsiConsole.MarkupLine("   [green]✓[/] Config parsed");
AnsiConsole.WriteLine();

AnsiConsole.MarkupLine("[bold]Step 2[/] [dim]Processing...[/]");
```

성공, 실패, 경고 메시지:

```csharp
// 성공
AnsiConsole.MarkupLine("[green]✓[/] Operation completed successfully");

// 실패
AnsiConsole.MarkupLine("[red]✗[/] Operation failed: {0}", errorMessage);

// 경고
AnsiConsole.MarkupLine("[yellow]⚠[/] Warning: {0}", warningMessage);
```

사용자 주의가 필요한 정보를 Panel로 강조:

```csharp
var panel = new Panel($"[yellow]Base branch[/] [cyan]{baseBranch}[/] [yellow]does not exist.[/]")
{
    Border = BoxBorder.Rounded,
    BorderStyle = new Style(Color.Yellow),
    Padding = new Padding(2, 1)
};
AnsiConsole.Write(panel);
```

Spectre.Console 덕분에 자동화 스크립트의 실행 과정이 투명해집니다. 다음 절부터는 이 두 패키지를 활용한 실제 스크립트들을 하나씩 분석해보겠습니다.
