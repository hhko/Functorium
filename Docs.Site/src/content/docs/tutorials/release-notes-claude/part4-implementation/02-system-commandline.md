---
title: "System.CommandLine Package"
---

자동화 스크립트가 `args[0]`, `args[1]`을 직접 파싱하면 금방 한계에 부딪힙니다. 인자 순서가 바뀌거나, 기본값이 필요하거나, `--help`를 보여줘야 할 때마다 코드가 복잡해집니다. System.CommandLine은 이런 문제를 해결하는 Microsoft의 **명령줄 인자 파싱 라이브러리로**, 선언적으로 CLI를 정의하면 파싱, 검증, 도움말 생성까지 자동으로 처리해줍니다.

## 패키지 설치

File-based App에서는 `#:package` 지시자로 설치합니다.

```csharp
#:package System.CommandLine@2.0.1
```

## CLI를 구성하는 핵심 요소

System.CommandLine의 CLI는 네 가지 구성 요소로 이루어집니다.

| 구성 요소 | 설명 | 예시 |
|----------|------|------|
| RootCommand | 최상위 명령어 | `dotnet MyApp.cs` |
| Option | 이름 붙은 인자 | `--base`, `-b` |
| Argument | 위치 기반 인자 | `<file>` |
| Command | 하위 명령어 | `add`, `remove` |

실제 명령어에서 이 요소들이 어떻게 대응되는지 보겠습니다.

```bash
dotnet MyApp.cs add --name "Item" --priority 1 file.txt
#               ^^^  ^^^^^^^^^^^^  ^^^^^^^^^^^  ^^^^^^^^
#               |    Option        Option       Argument
#               Command
```

## Option 정의하기

Option은 이름 붙은 인자입니다. 가장 기본적인 형태부터 살펴보겠습니다.

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

축약형 별칭을 추가하면 긴 이름과 짧은 이름 모두 사용할 수 있습니다.

```csharp
var verboseOption = new Option<bool>(new[] { "--verbose", "-v" })
{
    Description = "Enable verbose output"
};
```

필수 Option으로 만들려면 `IsRequired`를 설정합니다.

```csharp
var requiredOption = new Option<string>("--required")
{
    Description = "This option is required",
    IsRequired = true
};
```

## Argument 정의하기

Argument는 이름 없이 위치로 구분되는 인자입니다.

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

## RootCommand로 CLI 조립하기

Option과 Argument를 정의했다면, RootCommand에 등록하고 핸들러를 설정합니다. 릴리스 노트 스크립트의 전형적인 패턴을 따라 하나씩 만들어보겠습니다.

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

핸들러에서 비동기 작업이 필요하다면 `async` 키워드를 추가하면 됩니다.

```csharp
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    await AnalyzeAsync(baseBranch, targetBranch);
    return 0;
});
```

## 하위 Command 추가하기

여러 동작을 하나의 CLI에서 제공하려면 하위 Command를 사용합니다.

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

## 실제 예시: AnalyzeAllComponents.cs의 CLI 구성

릴리스 노트 자동화의 실제 코드에서 System.CommandLine이 어떻게 사용되는지 살펴보겠습니다. `--base`와 `--target` 두 Option으로 비교 대상 브랜치를 받고, 비동기 핸들러에서 분석을 수행합니다.

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

## 자동으로 제공되는 기능

System.CommandLine을 사용하면 별도 코드 없이 세 가지 기능이 자동으로 제공됩니다.

`--help`를 전달하면 Option 목록과 기본값이 포함된 도움말이 출력됩니다.

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

`--version`으로 버전 정보를 확인할 수 있고, 알 수 없는 인자를 전달하면 오류 메시지와 함께 올바른 사용법을 안내합니다.

```bash
$ dotnet MyApp.cs --unknown
Unrecognized command or argument '--unknown'.
```

## 패턴 정리

릴리스 노트 스크립트에서 반복적으로 사용하는 패턴을 정리하면 다음과 같습니다.

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

## FAQ

### Q1: `args[0]` 직접 파싱 대신 System.CommandLine을 사용하는 이유는 무엇인가요?
**A**: `args[]` 직접 파싱은 인자가 2~3개만 되어도 순서 관리, 기본값 처리, 오류 메시지 생성 코드가 급격히 복잡해집니다. System.CommandLine은 선언적으로 Option과 Argument를 정의하면 **파싱, 검증, `--help` 생성까지 자동으로** 처리해주므로, 스크립트 코드가 실제 비즈니스 로직에만 집중할 수 있습니다.

### Q2: `SetAction` 핸들러에서 반환하는 `0`은 무엇을 의미하나요?
**A**: 프로세스 종료 코드(exit code)입니다. `0`은 정상 종료를, `0`이 아닌 값은 오류를 의미합니다. 이 종료 코드는 CI/CD 파이프라인이나 셸 스크립트에서 명령 성공 여부를 판단하는 데 사용되므로, 오류 상황에서는 `1` 등 다른 값을 반환해야 합니다.

### Q3: `DefaultValueFactory`와 생성자에서 기본값을 직접 설정하는 것의 차이는 무엇인가요?
**A**: `DefaultValueFactory`는 **람다를 통해 기본값을 지연 생성합니다.** 즉, 사용자가 해당 Option을 지정하지 않았을 때만 팩토리가 호출됩니다. 기본값이 단순 상수가 아니라 환경 변수나 설정 파일에서 읽어야 하는 경우 특히 유용합니다.

System.CommandLine이 인자 파싱과 검증을 맡아주면, 스크립트 코드는 **실제 로직에만 집중**할 수 있습니다. 다음 절에서는 이 스크립트들의 콘솔 출력을 풍부하게 만들어주는 Spectre.Console을 살펴보겠습니다.
