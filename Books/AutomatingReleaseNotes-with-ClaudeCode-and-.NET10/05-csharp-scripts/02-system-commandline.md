# 5.2 System.CommandLine 패키지

> 이 절에서는 CLI 애플리케이션의 인자 파싱을 위한 System.CommandLine 패키지 사용법을 알아봅니다.

---

## System.CommandLine이란?

System.CommandLine은 Microsoft에서 제공하는 **명령줄 인자 파싱 라이브러리**입니다.

```txt
명령어 구조:
dotnet MyApp.cs --base origin/release/1.0 --target HEAD
                ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                System.CommandLine이 파싱하는 부분
```

---

## 패키지 설치

File-based App에서는 `#:package` 지시자로 설치합니다:

```csharp
#:package System.CommandLine@2.0.1
```

---

## 기본 개념

### 주요 구성 요소

| 구성 요소 | 설명 | 예시 |
|----------|------|------|
| RootCommand | 최상위 명령어 | `dotnet MyApp.cs` |
| Option | 이름 붙은 인자 | `--base`, `-b` |
| Argument | 위치 기반 인자 | `<file>` |
| Command | 하위 명령어 | `add`, `remove` |

### 예시 명령어 구조

```bash
dotnet MyApp.cs add --name "Item" --priority 1 file.txt
#               ^^^  ^^^^^^^^^^^^  ^^^^^^^^^^^  ^^^^^^^^
#               |    Option        Option       Argument
#               Command
```

---

## Option 정의

### 기본 Option

```csharp
using System.CommandLine;

// string 타입 Option
var nameOption = new Option<string>("--name")
{
    Description = "The name to use"
};

// int 타입 Option (기본값 포함)
var countOption = new Option<int>("--count")
{
    Description = "Number of items"
};
countOption.DefaultValueFactory = (_) => 10;
```

### 축약형 별칭

```csharp
var verboseOption = new Option<bool>(new[] { "--verbose", "-v" })
{
    Description = "Enable verbose output"
};
```

### 필수 Option

```csharp
var requiredOption = new Option<string>("--required")
{
    Description = "This option is required",
    IsRequired = true
};
```

---

## Argument 정의

```csharp
// 단일 Argument
var fileArgument = new Argument<string>("file")
{
    Description = "The file to process"
};

// 여러 Argument
var filesArgument = new Argument<string[]>("files")
{
    Description = "Files to process",
    Arity = ArgumentArity.ZeroOrMore
};
```

---

## RootCommand 구성

### 기본 구조

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1

using System.CommandLine;

// Option 정의
var baseOption = new Option<string>("--base")
{
    Description = "Base branch for comparison"
};
baseOption.DefaultValueFactory = (_) => "origin/release/1.0";

var targetOption = new Option<string>("--target")
{
    Description = "Target branch for comparison"
};
targetOption.DefaultValueFactory = (_) => "HEAD";

// RootCommand 생성
var rootCommand = new RootCommand("My CLI application")
{
    baseOption,
    targetOption
};

// 핸들러 설정
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    Console.WriteLine($"Base: {baseBranch}");
    Console.WriteLine($"Target: {targetBranch}");

    return 0;
});

// 실행
return await rootCommand.Parse(args).InvokeAsync();
```

실행:
```bash
dotnet MyApp.cs --base origin/main --target HEAD
# 출력:
# Base: origin/main
# Target: HEAD
```

---

## 비동기 핸들러

비동기 작업을 수행하는 핸들러:

```csharp
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    await AnalyzeAsync(baseBranch, targetBranch);
    return 0;
});
```

---

## 하위 Command

```csharp
// add 하위 명령어
var addCommand = new Command("add", "Add a new item")
{
    nameOption
};

addCommand.SetAction((parseResult, cancellationToken) =>
{
    var name = parseResult.GetValue(nameOption)!;
    Console.WriteLine($"Adding: {name}");
    return 0;
});

// remove 하위 명령어
var removeCommand = new Command("remove", "Remove an item")
{
    nameOption
};

removeCommand.SetAction((parseResult, cancellationToken) =>
{
    var name = parseResult.GetValue(nameOption)!;
    Console.WriteLine($"Removing: {name}");
    return 0;
});

// RootCommand에 추가
var rootCommand = new RootCommand("Item manager")
{
    addCommand,
    removeCommand
};
```

실행:
```bash
dotnet MyApp.cs add --name "Item1"
dotnet MyApp.cs remove --name "Item1"
```

---

## 실제 예시: AnalyzeAllComponents.cs

릴리스 노트 자동화의 실제 코드:

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.CommandLine;
using System.Threading.Tasks;

// Option 정의
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

// RootCommand 구성
var rootCommand = new RootCommand("Automated analysis of all components")
{
    baseOption,
    targetOption
};

// 비동기 핸들러
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    await AnalyzeAllComponentsAsync(baseBranch, targetBranch);
    return 0;
});

// 실행
return await rootCommand.Parse(args).InvokeAsync();

// 메인 로직
static async Task AnalyzeAllComponentsAsync(string baseBranch, string targetBranch)
{
    Console.WriteLine($"Analyzing from {baseBranch} to {targetBranch}...");
    // 실제 분석 로직
}
```

---

## 자동 기능

System.CommandLine이 자동으로 제공하는 기능:

### 도움말 (--help)

```bash
$ dotnet MyApp.cs --help

Description:
  Automated analysis of all components

Usage:
  MyApp [options]

Options:
  --base <base>      Base branch for comparison [default: origin/release/1.0]
  --target <target>  Target branch for comparison [default: origin/main]
  --help             Show help and usage information
  --version          Show version information
```

### 버전 (--version)

```bash
$ dotnet MyApp.cs --version
1.0.0
```

### 인자 검증

```bash
$ dotnet MyApp.cs --unknown
Unrecognized command or argument '--unknown'.
```

---

## 패턴 정리

### Option 패턴

```csharp
// 1. 기본 Option
var option = new Option<string>("--name");

// 2. 기본값 설정
option.DefaultValueFactory = (_) => "default";

// 3. 필수 Option
option.IsRequired = true;

// 4. 축약형 별칭
var option = new Option<string>(new[] { "--name", "-n" });
```

### RootCommand 패턴

```csharp
// 1. 생성 및 Option 추가
var rootCommand = new RootCommand("Description")
{
    option1,
    option2
};

// 2. 핸들러 설정
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var value = parseResult.GetValue(option1);
    return 0;
});

// 3. 실행
return await rootCommand.Parse(args).InvokeAsync();
```

---

## 요약

| 항목 | 설명 |
|------|------|
| 패키지 | `System.CommandLine@2.0.1` |
| Option | 이름 붙은 인자 (`--name`) |
| Argument | 위치 기반 인자 |
| RootCommand | 최상위 명령어 |
| SetAction | 핸들러 설정 |
| 자동 기능 | --help, --version, 검증 |

---

## 다음 단계

- [5.3 Spectre.Console 패키지](03-spectre-console.md)
