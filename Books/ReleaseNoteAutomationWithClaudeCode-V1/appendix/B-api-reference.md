# 부록 B: API 레퍼런스

> 이 부록에서는 릴리스 노트 자동화 시스템에서 사용하는 주요 API를 정리합니다.

---

## System.CommandLine

### RootCommand

CLI의 최상위 명령어를 정의합니다.

```csharp
var rootCommand = new RootCommand("프로그램 설명")
{
    option1,
    option2,
    argument1
};
```

**생성자:**

| 파라미터 | 타입 | 설명 |
|----------|------|------|
| `description` | `string` | 명령어 설명 (--help에 표시) |

**주요 메서드:**

| 메서드 | 설명 |
|--------|------|
| `SetAction(handler)` | 명령어 실행 핸들러 설정 |
| `Parse(args)` | 인자 파싱 |
| `Invoke()` | 동기 실행 |
| `InvokeAsync()` | 비동기 실행 |

---

### Option<T>

선택적 CLI 인자를 정의합니다.

```csharp
var option = new Option<string>("--name", "이름을 입력하세요");
option.AddAlias("-n");
option.DefaultValueFactory = (_) => "default";
option.IsRequired = true;
```

**생성자:**

| 파라미터 | 타입 | 설명 |
|----------|------|------|
| `name` | `string` | 옵션 이름 (--로 시작) |
| `description` | `string` | 옵션 설명 |

**주요 속성:**

| 속성 | 타입 | 설명 |
|------|------|------|
| `DefaultValueFactory` | `Func<ArgumentResult, T>` | 기본값 생성 함수 |
| `IsRequired` | `bool` | 필수 여부 |

**주요 메서드:**

| 메서드 | 설명 |
|--------|------|
| `AddAlias(alias)` | 별칭 추가 (예: `-n`) |

---

### Argument<T>

위치 기반 CLI 인자를 정의합니다.

```csharp
var argument = new Argument<string>("name", "이름");
argument.DefaultValueFactory = (_) => "default";
```

**생성자:**

| 파라미터 | 타입 | 설명 |
|----------|------|------|
| `name` | `string` | 인자 이름 |
| `description` | `string` | 인자 설명 |

---

### ParseResult

파싱된 CLI 인자에 접근합니다.

```csharp
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var name = parseResult.GetValue(nameOption);
    var count = parseResult.GetValue(countArgument);
    return 0;
});
```

**주요 메서드:**

| 메서드 | 설명 |
|--------|------|
| `GetValue<T>(option)` | Option 값 가져오기 |
| `GetValue<T>(argument)` | Argument 값 가져오기 |

---

### 전체 예제

```csharp
#!/usr/bin/env dotnet
#:package System.CommandLine@2.0.1

using System.CommandLine;

// Option 정의
var nameOption = new Option<string>("--name", "Your name");
nameOption.AddAlias("-n");
nameOption.IsRequired = true;

// Argument 정의
var countArgument = new Argument<int>("count", "Number of greetings");
countArgument.DefaultValueFactory = (_) => 1;

// RootCommand 정의
var rootCommand = new RootCommand("Greeting program")
{
    nameOption,
    countArgument
};

// Handler 설정
rootCommand.SetAction((parseResult, ct) =>
{
    var name = parseResult.GetValue(nameOption);
    var count = parseResult.GetValue(countArgument);

    for (int i = 0; i < count; i++)
    {
        Console.WriteLine($"Hello, {name}!");
    }

    return 0;
});

// 실행
return rootCommand.Parse(args).Invoke();
```

---

## Spectre.Console

### AnsiConsole

정적 콘솔 출력 클래스입니다.

```csharp
AnsiConsole.WriteLine("일반 텍스트");
AnsiConsole.MarkupLine("[bold blue]스타일 적용 텍스트[/]");
AnsiConsole.Write(table);
```

**주요 메서드:**

| 메서드 | 설명 |
|--------|------|
| `WriteLine(text)` | 일반 텍스트 출력 |
| `MarkupLine(markup)` | 마크업 텍스트 출력 |
| `Write(renderable)` | Renderable 객체 출력 |
| `Clear()` | 콘솔 지우기 |

---

### 마크업 문법

```csharp
// 색상
AnsiConsole.MarkupLine("[red]빨간색[/]");
AnsiConsole.MarkupLine("[green]초록색[/]");
AnsiConsole.MarkupLine("[blue]파란색[/]");

// 스타일
AnsiConsole.MarkupLine("[bold]굵게[/]");
AnsiConsole.MarkupLine("[italic]기울임[/]");
AnsiConsole.MarkupLine("[underline]밑줄[/]");
AnsiConsole.MarkupLine("[dim]흐리게[/]");

// 조합
AnsiConsole.MarkupLine("[bold red]굵은 빨간색[/]");
AnsiConsole.MarkupLine("[bold underline green]굵은 밑줄 초록색[/]");
```

**사용 가능한 색상:**

| 기본 색상 | 밝은 색상 |
|-----------|-----------|
| `black` | `grey` |
| `red` | `lightred` |
| `green` | `lightgreen` |
| `yellow` | `lightyellow` |
| `blue` | `lightblue` |
| `magenta` | `lightmagenta` |
| `cyan` | `lightcyan` |
| `white` | |

---

### Table

테이블을 생성합니다.

```csharp
var table = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("Name")
    .AddColumn(new TableColumn("Count").RightAligned())
    .AddColumn("Status");

table.AddRow("Item 1", "10", "[green]OK[/]");
table.AddRow("Item 2", "5", "[red]Failed[/]");

AnsiConsole.Write(table);
```

**주요 메서드:**

| 메서드 | 설명 |
|--------|------|
| `Border(border)` | 테두리 스타일 설정 |
| `AddColumn(column)` | 열 추가 |
| `AddRow(values)` | 행 추가 |

**TableBorder 옵션:**

| 옵션 | 설명 |
|------|------|
| `None` | 테두리 없음 |
| `Ascii` | ASCII 문자 |
| `Square` | 사각형 (기본) |
| `Rounded` | 둥근 모서리 |
| `Heavy` | 두꺼운 선 |
| `Double` | 이중 선 |

**TableColumn 정렬:**

```csharp
new TableColumn("Header").LeftAligned()
new TableColumn("Header").Centered()
new TableColumn("Header").RightAligned()
```

---

### Rule

구분선을 생성합니다.

```csharp
// 기본 구분선
AnsiConsole.Write(new Rule());

// 제목 있는 구분선
AnsiConsole.Write(new Rule("[bold blue]Section Title[/]"));

// 스타일 적용
AnsiConsole.Write(new Rule("Title").RuleStyle("green"));

// 정렬
AnsiConsole.Write(new Rule("Left").LeftJustified());
AnsiConsole.Write(new Rule("Center").Centered());
AnsiConsole.Write(new Rule("Right").RightJustified());
```

---

### Panel

패널(박스)을 생성합니다.

```csharp
var panel = new Panel("Panel content")
    .Header("[bold]Title[/]")
    .Border(BoxBorder.Rounded)
    .BorderColor(Color.Blue);

AnsiConsole.Write(panel);
```

**주요 메서드:**

| 메서드 | 설명 |
|--------|------|
| `Header(text)` | 헤더 설정 |
| `Border(border)` | 테두리 스타일 |
| `BorderColor(color)` | 테두리 색상 |
| `Padding(left, top, right, bottom)` | 내부 여백 |

---

### Progress

진행 상황을 표시합니다.

```csharp
await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("[green]Processing[/]", maxValue: 100);

        while (!ctx.IsFinished)
        {
            await Task.Delay(100);
            task.Increment(10);
        }
    });
```

---

### Status (Spinner)

스피너를 표시합니다.

```csharp
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("green"))
    .StartAsync("Processing...", async ctx =>
    {
        await Task.Delay(2000);
        ctx.Status("Almost done...");
        await Task.Delay(1000);
    });
```

**Spinner 종류:**

| 종류 | 설명 |
|------|------|
| `Dots` | 점 애니메이션 |
| `Line` | 선 애니메이션 |
| `Star` | 별 애니메이션 |
| `Arrow` | 화살표 |
| `Bounce` | 바운스 |

---

### 전체 예제

```csharp
#!/usr/bin/env dotnet
#:package Spectre.Console@0.54.0

using Spectre.Console;

// 헤더
AnsiConsole.Write(new Rule("[bold blue]Report[/]").RuleStyle("blue"));
AnsiConsole.WriteLine();

// 테이블
var table = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("Component")
    .AddColumn(new TableColumn("Files").RightAligned())
    .AddColumn(new TableColumn("Status").Centered());

table.AddRow("Functorium", "45", "[green]OK[/]");
table.AddRow("Testing", "23", "[green]OK[/]");
table.AddRow("Docs", "12", "[yellow]Warning[/]");

AnsiConsole.Write(table);
AnsiConsole.WriteLine();

// 요약
AnsiConsole.MarkupLine("[dim]Total: 80 files analyzed[/]");
```

---

## PublicApiGenerator

### ApiGenerator

DLL에서 Public API를 추출합니다.

```csharp
using PublicApiGenerator;

// 어셈블리 로드
var assembly = Assembly.LoadFrom("path/to/assembly.dll");

// API 추출
var options = new ApiGeneratorOptions
{
    IncludeAssemblyAttributes = false
};

string publicApi = assembly.GeneratePublicApi(options);
```

**ApiGeneratorOptions:**

| 속성 | 타입 | 설명 |
|------|------|------|
| `IncludeAssemblyAttributes` | `bool` | 어셈블리 속성 포함 |
| `ExcludeAttributes` | `string[]` | 제외할 속성 |
| `AllowNamespacePrefixes` | `string[]` | 포함할 네임스페이스 |
| `DenyNamespacePrefixes` | `string[]` | 제외할 네임스페이스 |

---

## .NET File-based App 지시어

### #:package

NuGet 패키지를 참조합니다.

```csharp
#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0
#:package Newtonsoft.Json@13.0.3
```

**형식:** `#:package <PackageName>@<Version>`

---

### #:sdk

SDK를 지정합니다.

```csharp
#:sdk Microsoft.NET.Sdk.Web
```

**사용 가능한 SDK:**

| SDK | 용도 |
|-----|------|
| `Microsoft.NET.Sdk` | 기본 (콘솔 앱) |
| `Microsoft.NET.Sdk.Web` | 웹 애플리케이션 |
| `Microsoft.NET.Sdk.Worker` | 백그라운드 서비스 |

---

### #:property

프로젝트 속성을 설정합니다.

```csharp
#:property LangVersion=preview
#:property Nullable=enable
#:property ImplicitUsings=enable
```

---

### Shebang

Unix/Linux에서 직접 실행을 위한 인터프리터 지정입니다.

```csharp
#!/usr/bin/env dotnet
```

**사용법 (Unix/Linux):**

```bash
chmod +x script.cs
./script.cs
```

---

## Git 명령어

### git diff

두 지점 간의 차이를 출력합니다.

```bash
# 파일 변경 통계
git diff --stat origin/release/1.0 HEAD

# 특정 폴더만
git diff --stat origin/release/1.0 HEAD -- Src/

# 전체 diff 출력
git diff origin/release/1.0 HEAD
```

---

### git log

커밋 히스토리를 출력합니다.

```bash
# 한 줄 형식
git log --oneline origin/release/1.0..HEAD

# 커밋 메시지만
git log --format=%s origin/release/1.0..HEAD

# 특정 폴더만
git log --oneline origin/release/1.0..HEAD -- Src/
```

---

### git rev-list

커밋 목록을 출력합니다.

```bash
# 첫 번째 커밋 찾기
git rev-list --max-parents=0 HEAD

# 커밋 수 세기
git rev-list --count origin/release/1.0..HEAD
```

---

### git shortlog

커밋을 작성자별로 그룹화합니다.

```bash
# 작성자별 커밋 수
git shortlog -sn origin/release/1.0..HEAD

# 이메일 포함
git shortlog -sne origin/release/1.0..HEAD
```

---

## 다음 단계

- [부록 C: 참고 자료](C-resources.md)
