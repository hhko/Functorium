# 5.1 .NET 10 File-based App 소개

> 이 절에서는 .NET 10에서 도입된 File-based App(파일 기반 앱)의 개념과 사용 방법을 알아봅니다.

---

## File-based App이란?

File-based App은 .NET 10에서 도입된 **단일 C# 파일로 실행 가능한 프로그램**입니다.

```txt
기존 방식:
├── MyApp.csproj    # 프로젝트 파일 (필수)
├── Program.cs      # 메인 코드
└── Class1.cs       # 추가 코드

File-based App:
└── MyApp.cs        # 단일 파일로 모든 것 처리
```

프로젝트 파일(`.csproj`) 없이 **C# 파일 하나**로 프로그램을 실행할 수 있습니다.

---

## 왜 File-based App을 사용하는가?

### 1. 빠른 시작

```bash
# 기존 방식: 프로젝트 생성 필요
dotnet new console -n MyApp
cd MyApp
dotnet run

# File-based App: 바로 실행
dotnet MyApp.cs
```

### 2. 스크립트처럼 사용

릴리스 노트 자동화처럼 **도구성 프로그램**에 적합합니다:

- 빌드 스크립트
- 코드 생성기
- 분석 도구
- 유틸리티

### 3. 버전 관리 용이

단일 파일이므로 변경 이력 추적이 간단합니다.

---

## 기본 문법

### 최소 예제

```csharp
#!/usr/bin/env dotnet

// hello.cs
Console.WriteLine("Hello, World!");
```

```bash
# 실행
dotnet hello.cs
```

### Shebang 라인

파일 첫 줄에 `#!/usr/bin/env dotnet`을 추가하면 Unix 계열에서 직접 실행 가능:

```bash
chmod +x hello.cs
./hello.cs
```

---

## 패키지 참조

File-based App에서는 `#:package` 지시자로 NuGet 패키지를 참조합니다.

### 문법

```csharp
#:package <패키지명>@<버전>
```

### 예시

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System.CommandLine;
using Spectre.Console;

// 이제 System.CommandLine과 Spectre.Console 사용 가능
AnsiConsole.WriteLine("Hello!");
```

### 패키지 지시자 위치

**반드시** 파일 상단에 위치해야 합니다:

```csharp
#!/usr/bin/env dotnet        // 1. Shebang (선택)

// 주석                       // 2. 주석 (선택)

#:package Spectre.Console@0.54.0  // 3. 패키지 지시자

using System;                // 4. using 문
using Spectre.Console;

// 코드 시작                  // 5. 실제 코드
```

---

## 실행 방법

### 기본 실행

```bash
dotnet MyScript.cs
```

### 인자 전달

```bash
dotnet MyScript.cs --base origin/release/1.0 --target HEAD
```

### 작업 디렉터리에서 실행

```bash
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

---

## 릴리스 노트 자동화 스크립트

Functorium 프로젝트의 File-based App들:

```txt
.release-notes/scripts/
├── AnalyzeAllComponents.cs    # 컴포넌트 분석
├── ExtractApiChanges.cs       # API 변경사항 추출
├── ApiGenerator.cs            # Public API 생성
└── SummarizeSlowestTests.cs   # 테스트 결과 요약
```

### 공통 패키지

모든 스크립트에서 사용하는 패키지:

| 패키지 | 버전 | 용도 |
|--------|------|------|
| `System.CommandLine` | 2.0.1 | CLI 인자 파싱 |
| `Spectre.Console` | 0.54.0 | 콘솔 UI |

---

## 실제 예시: 간단한 분석 스크립트

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

---

## 장점과 제한사항

### 장점

| 장점 | 설명 |
|------|------|
| 빠른 시작 | 프로젝트 생성 불필요 |
| 단순함 | 단일 파일 관리 |
| 이식성 | 파일 하나만 복사하면 됨 |
| 버전 관리 | 변경 이력 추적 용이 |
| 도구성 | 스크립트/유틸리티에 적합 |

### 제한사항

| 제한 | 설명 |
|------|------|
| 단일 파일 | 여러 파일 분할 불가 |
| 복잡한 프로젝트 | 대규모 앱에는 부적합 |
| 테스트 | 단위 테스트 작성 어려움 |
| IDE 지원 | 일부 기능 제한 |

---

## 언제 File-based App을 사용하는가?

### 적합한 경우

- 빌드/배포 스크립트
- 코드 생성 도구
- 분석/보고 도구
- 일회성 데이터 처리
- 프로토타입/실험

### 부적합한 경우

- 대규모 애플리케이션
- 여러 파일이 필요한 경우
- 단위 테스트가 필요한 경우
- 복잡한 빌드 구성

---

## 요약

| 항목 | 설명 |
|------|------|
| 정의 | 단일 C# 파일로 실행 가능한 프로그램 |
| 실행 | `dotnet MyScript.cs` |
| 패키지 | `#:package <패키지>@<버전>` |
| 용도 | 도구, 스크립트, 유틸리티 |
| 장점 | 단순함, 빠른 시작, 이식성 |

---

## 다음 단계

- [5.2 System.CommandLine 패키지](02-system-commandline.md)
