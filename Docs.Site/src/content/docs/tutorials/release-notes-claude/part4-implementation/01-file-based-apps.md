---
title: "File-Based Apps"
---

릴리스 노트 자동화 스크립트를 작성하려면 프로젝트 파일을 만들고, 솔루션에 추가하고, 빌드 구성을 잡아야 할까요? .NET 10의 File-based App을 사용하면 그런 번거로움 없이 **C# 파일 하나로 바로 실행할 수 있습니다.** 이 절에서는 File-based App이 자동화 스크립트에 왜 적합한지, 그리고 실제로 어떻게 활용하는지 살펴보겠습니다.

## File-based App이란?

File-based App은 .NET 10에서 도입된 실행 방식입니다. 기존에는 `.csproj` 프로젝트 파일, `Program.cs`, 필요한 클래스 파일 등 여러 파일을 만들어야 했지만, File-based App에서는 `MyApp.cs` **단일 파일 하나로 모든 것을 처리합니다.** 프로젝트 파일 없이 C# 파일만으로 프로그램을 실행할 수 있는 것입니다.

## 자동화 스크립트에 File-based App이 적합한 이유

가장 큰 장점은 **빠른 시작입니다**. 기존 방식이라면 `dotnet new console`로 프로젝트를 생성하고, 디렉터리를 이동한 뒤 `dotnet run`을 실행해야 했습니다. File-based App에서는 `dotnet MyApp.cs` 한 줄이면 됩니다.

```bash
# 기존 방식: 프로젝트 생성 필요
dotnet new console -n MyApp
cd MyApp
dotnet run

# File-based App: 바로 실행
dotnet MyApp.cs
```

릴리스 노트 자동화처럼 빌드 스크립트, 코드 생성기, 분석 도구 같은 **도구성 프로그램에** 특히 적합합니다. 단일 파일이므로 변경 이력 추적도 간단하고, 파일 하나만 복사하면 다른 환경에서도 바로 사용할 수 있습니다.

다만 여러 파일로 분할할 수 없고, 단위 테스트 작성이 어렵기 때문에 대규모 애플리케이션에는 부적합합니다. 복잡한 빌드 구성이 필요한 경우에도 전통적인 프로젝트 방식이 낫습니다.

## 기본 문법

가장 단순한 형태는 다음과 같습니다.

```csharp
#!/usr/bin/env dotnet

// hello.cs
Console.WriteLine("Hello, World!");
```

```bash
# 실행
dotnet hello.cs
```

파일 첫 줄의 `#!/usr/bin/env dotnet`은 Shebang 라인입니다. Unix 계열에서 `chmod +x hello.cs` 후 `./hello.cs`로 직접 실행할 수 있게 해줍니다.

## 패키지 참조: `#:package` 지시자

File-based App에서는 `.csproj`가 없으므로 NuGet 패키지를 참조할 별도의 방법이 필요합니다. 이를 위해 `#:package` 지시자가 도입되었습니다. 파일 안에서 직접 패키지와 버전을 선언하면, 런타임이 자동으로 패키지를 복원하고 참조합니다.

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System.CommandLine;
using Spectre.Console;

// 이제 System.CommandLine과 Spectre.Console 사용 가능
AnsiConsole.WriteLine("Hello!");
```

이 지시자의 위치에는 규칙이 있습니다. **반드시 파일 상단에 위치해야 하며**, Shebang과 주석 뒤, using 문 앞에 와야 합니다.

```csharp
#!/usr/bin/env dotnet        // 1. Shebang (선택)

// 주석                       // 2. 주석 (선택)

#:package Spectre.Console@0.54.0  // 3. 패키지 지시자

using System;                // 4. using 문
using Spectre.Console;

// 코드 시작                  // 5. 실제 코드
```

## 실행 방법

기본 실행은 `dotnet MyScript.cs`입니다. 인자를 전달하려면 파일명 뒤에 붙이면 됩니다.

```bash
# 기본 실행
dotnet MyScript.cs

# 인자 전달
dotnet MyScript.cs --base origin/release/1.0 --target HEAD

# 작업 디렉터리에서 실행
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

## 릴리스 노트 자동화 스크립트

Functorium 프로젝트에서는 세 개의 File-based App으로 릴리스 노트 자동화를 구현합니다.

```txt
.release-notes/scripts/
├── AnalyzeAllComponents.cs    # 컴포넌트 분석
├── ExtractApiChanges.cs       # API 변경사항 추출
└── ApiGenerator.cs            # Public API 생성
```

이 스크립트들은 모두 공통으로 두 가지 패키지를 사용합니다. CLI 인자를 파싱하는 `System.CommandLine@2.0.1`과 풍부한 콘솔 UI를 제공하는 `Spectre.Console@0.54.0`입니다.

## 실제 예시: 간단한 분석 스크립트

File-based App의 실제 모습을 간단한 파일 분석 스크립트로 살펴보겠습니다. 디렉터리를 받아 확장자별 파일 수를 테이블로 보여주는 프로그램입니다.

```csharp
#!/usr/bin/env dotnet

// SimpleAnalyzer.cs - 간단한 파일 분석 스크립트
// Usage: dotnet SimpleAnalyzer.cs <directory>

#:package Spectre.Console@0.54.0

using System;
using System.IO;
using System.Linq;
using Spectre.Console;

// 인자 확인
if (args.Length == 0)
{
    AnsiConsole.MarkupLine("[red]Error:[/] Directory path required");
    AnsiConsole.MarkupLine("[dim]Usage: dotnet SimpleAnalyzer.cs <directory>[/]");
    return 1;
}

var directory = args[0];

// 디렉터리 확인
if (!Directory.Exists(directory))
{
    AnsiConsole.MarkupLine($"[red]Error:[/] Directory not found: {directory}");
    return 1;
}

// 헤더
AnsiConsole.Write(new Rule("[bold blue]File Analysis[/]").RuleStyle("blue"));
AnsiConsole.WriteLine();

// 파일 분석
var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
var groupedFiles = files
    .GroupBy(f => Path.GetExtension(f).ToLower())
    .OrderByDescending(g => g.Count())
    .Take(10);

// 결과 테이블
var table = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("Extension")
    .AddColumn("Count");

foreach (var group in groupedFiles)
{
    var ext = string.IsNullOrEmpty(group.Key) ? "(no ext)" : group.Key;
    table.AddRow(ext, group.Count().ToString());
}

AnsiConsole.Write(table);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine($"[dim]Total: {files.Length} files[/]");

return 0;
```

실행:
```bash
dotnet SimpleAnalyzer.cs ./src
```

패키지 참조, 인자 처리, 콘솔 UI까지 파일 하나에 모두 담겨 있습니다. 이것이 File-based App의 핵심입니다. 프로젝트 구조의 오버헤드 없이 C#의 모든 기능을 활용할 수 있습니다.

## FAQ

### Q1: File-based App은 기존 `.csproj` 프로젝트와 어떤 차이가 있나요?
**A**: File-based App은 프로젝트 파일(`.csproj`) 없이 `.cs` 파일 하나로 실행됩니다. NuGet 패키지 참조는 `#:package` 지시자로 파일 안에 직접 선언하고, `dotnet MyApp.cs`로 바로 실행할 수 있습니다. 반면 여러 파일로 분할하거나 단위 테스트를 작성하기 어려우므로, 자동화 스크립트나 간단한 도구에 적합합니다.

### Q2: `#:package` 지시자와 `#r` 지시자는 무엇이 다른가요?
**A**: `#r`은 C# Interactive(`.csx`)에서 사용하던 구문이고, `#:package`는 .NET 10 File-based App 전용 지시자입니다. `#:package`는 NuGet 패키지 이름과 버전을 명시적으로 지정하며(`패키지명@버전`), 런타임이 자동으로 패키지를 복원합니다. 반드시 파일 상단에 위치해야 하며, `using` 문보다 앞에 와야 합니다.

### Q3: File-based App에서 여러 클래스를 사용할 수 있나요?
**A**: 네, **하나의 `.cs` 파일 안에** 여러 클래스, 레코드, static 메서드를 모두 정의할 수 있습니다. 다만 여러 파일로 분할하는 것은 불가능하므로, 코드가 길어지면 가독성이 떨어질 수 있습니다. 이 경우 기존 `.csproj` 프로젝트 방식을 고려해야 합니다.

이어지는 절에서는 이 스크립트들이 사용하는 두 핵심 패키지인 System.CommandLine과 Spectre.Console을 살펴보겠습니다.
