# 5.3 Spectre.Console 패키지

> 이 절에서는 풍부한 콘솔 UI를 제공하는 Spectre.Console 패키지 사용법을 알아봅니다.

---

## Spectre.Console이란?

Spectre.Console은 **콘솔 애플리케이션을 위한 풍부한 UI 라이브러리**입니다.

```txt
기존 Console.WriteLine:
┌─────────────────────────────────┐
│ Processing...                   │
│ Done: 10 files                  │
└─────────────────────────────────┘

Spectre.Console:
┌─────────────────────────────────┐
│ ━━━ Processing Files ━━━        │
│                                 │
│ │ File        │ Status │ Time  │ │
│ ├─────────────┼────────┼───────┤ │
│ │ App.cs      │ ✓ Done │ 0.3s  │ │
│ │ Config.cs   │ ✓ Done │ 0.1s  │ │
│                                 │
│ [████████████] 100% Complete    │
└─────────────────────────────────┘
```

---

## 패키지 설치

```csharp
#:package Spectre.Console@0.54.0
```

---

## 기본 출력

### MarkupLine

색상과 스타일이 적용된 텍스트 출력:

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

### 색상 표

| 색상 | 마크업 |
|------|--------|
| 빨강 | `[red]` |
| 녹색 | `[green]` |
| 노랑 | `[yellow]` |
| 파랑 | `[blue]` |
| 청록 | `[cyan]` |
| 자홍 | `[magenta]` |
| 회색 | `[grey]` |
| 흰색 | `[white]` |

---

## Table (테이블)

데이터를 표 형태로 출력:

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

---

## Rule (구분선)

섹션을 구분하는 제목 줄:

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

---

## Panel (패널)

박스 안에 내용 표시:

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

---

## Status (상태 표시)

작업 진행 중 상태 표시:

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

### 스피너 종류

| 스피너 | 모양 |
|--------|------|
| `Dots` | ⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏ |
| `Line` | -\\|/ |
| `Star` | ✶ |
| `Arrow` | ←↖↑↗→↘↓↙ |

---

## Progress (진행률)

진행률 표시:

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

---

## 실제 예시: AnalyzeAllComponents.cs

릴리스 노트 자동화에서 사용하는 패턴:

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

---

## 자주 사용하는 패턴

### 단계 표시

```csharp
AnsiConsole.MarkupLine("[bold]Step 1[/] [dim]Loading...[/]");
AnsiConsole.MarkupLine("   [green]✓[/] File loaded");
AnsiConsole.MarkupLine("   [green]✓[/] Config parsed");
AnsiConsole.WriteLine();

AnsiConsole.MarkupLine("[bold]Step 2[/] [dim]Processing...[/]");
```

### 성공/실패 메시지

```csharp
// 성공
AnsiConsole.MarkupLine("[green]✓[/] Operation completed successfully");

// 실패
AnsiConsole.MarkupLine("[red]✗[/] Operation failed: {0}", errorMessage);

// 경고
AnsiConsole.MarkupLine("[yellow]⚠[/] Warning: {0}", warningMessage);
```

### 정보 패널

```csharp
var panel = new Panel($"[yellow]Base branch[/] [cyan]{baseBranch}[/] [yellow]does not exist.[/]")
{
    Border = BoxBorder.Rounded,
    BorderStyle = new Style(Color.Yellow),
    Padding = new Padding(2, 1)
};
AnsiConsole.Write(panel);
```

---

## 요약

| 기능 | 클래스/메서드 | 용도 |
|------|--------------|------|
| 색상 텍스트 | `MarkupLine` | 스타일 적용 출력 |
| 테이블 | `Table` | 데이터 표 형식 |
| 구분선 | `Rule` | 섹션 구분 |
| 패널 | `Panel` | 박스 표시 |
| 상태 | `Status` | 스피너 표시 |
| 진행률 | `Progress` | 프로그레스 바 |

---

## 다음 단계

- [5.4 AnalyzeAllComponents.cs 분석](04-analyze-all-components.md)
