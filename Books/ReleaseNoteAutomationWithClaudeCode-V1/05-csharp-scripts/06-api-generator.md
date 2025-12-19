# 5.6 ApiGenerator.cs 분석

> 이 절에서는 DLL에서 Public API를 추출하는 ApiGenerator.cs 스크립트를 분석합니다.

---

## 개요

ApiGenerator.cs는 **컴파일된 DLL에서 Public API를 추출**하여 텍스트로 출력하는 스크립트입니다.

```txt
역할:
├── DLL 로드
├── 어셈블리 종속성 해결
├── Public API 추출 (PublicApiGenerator 라이브러리)
└── C# 형식으로 출력
```

---

## 파일 위치

```txt
.release-notes/scripts/ApiGenerator.cs
```

---

## 사용법

```bash
# 파일로 출력
dotnet ApiGenerator.cs <dll-path> <output-file>

# 콘솔로 출력 (- 사용)
dotnet ApiGenerator.cs <dll-path> -
```

---

## 패키지 참조

```csharp
#!/usr/bin/env dotnet

#:package PublicApiGenerator@11.3.0
#:package System.CommandLine@2.0.1

using System;
using System.CommandLine;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using PublicApiGenerator;
```

**PublicApiGenerator**는 Microsoft에서 만든 라이브러리로, 어셈블리의 Public API를 C# 형식으로 추출합니다.

---

## 스크립트 구조

### CLI 정의

```csharp
var dllArgument = new Argument<string>("dll", "Path to the DLL file");
var outputArgument = new Argument<string>("output", "Output file path (use - for stdout)");

var rootCommand = new RootCommand("Generate public API from a DLL")
{
    dllArgument,
    outputArgument
};

rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var dllPath = parseResult.GetValue(dllArgument)!;
    var outputPath = parseResult.GetValue(outputArgument)!;

    GenerateApi(dllPath, outputPath);
    return 0;
});

return rootCommand.Parse(args).Invoke();
```

---

## 주요 로직

### 1. DLL 로드

어셈블리 로드 컨텍스트를 생성하여 DLL을 로드합니다:

```csharp
static void GenerateApi(string dllPath, string outputPath)
{
    // DLL 존재 확인
    if (!File.Exists(dllPath))
    {
        Console.Error.WriteLine($"Error: DLL not found: {dllPath}");
        Environment.Exit(1);
    }

    var dllDirectory = Path.GetDirectoryName(dllPath)!;

    // 커스텀 AssemblyLoadContext 생성
    var loadContext = new CustomAssemblyLoadContext(dllDirectory);

    // 어셈블리 로드
    var assembly = loadContext.LoadFromAssemblyPath(dllPath);
}
```

### 2. 어셈블리 종속성 해결

DLL이 참조하는 다른 어셈블리를 자동으로 로드합니다:

```csharp
class CustomAssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _basePath;

    public CustomAssemblyLoadContext(string basePath) : base(isCollectible: true)
    {
        _basePath = basePath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 기본 컨텍스트에서 먼저 찾기
        try
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }
        catch { }

        // 같은 디렉터리에서 찾기
        var dllPath = Path.Combine(_basePath, $"{assemblyName.Name}.dll");
        if (File.Exists(dllPath))
        {
            return LoadFromAssemblyPath(dllPath);
        }

        return null;
    }
}
```

### 3. Public API 추출

PublicApiGenerator 라이브러리로 API를 추출합니다:

```csharp
// PublicApiGenerator 옵션
var options = new ApiGeneratorOptions
{
    IncludeAssemblyAttributes = false,  // 어셈블리 속성 제외
    DenyNamespacePrefixes = new[]       // 제외할 네임스페이스
    {
        "System.Runtime.CompilerServices",
        "Microsoft.CodeAnalysis"
    }
};

// API 생성
var publicApi = assembly.GeneratePublicApi(options);
```

### 4. 출력

```csharp
// 콘솔 출력 (-) 또는 파일 출력
if (outputPath == "-")
{
    Console.Write(publicApi);
}
else
{
    File.WriteAllText(outputPath, publicApi);
    Console.WriteLine($"API written to: {outputPath}");
}
```

---

## 출력 형식

### Public API 예시

```csharp
namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static LanguageExt.Common.Error Create(string errorCode, string errorCurrentValue, string errorMessage) { }
        public static LanguageExt.Common.Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage)
            where T : notnull { }
        public static LanguageExt.Common.Error CreateFromException(string errorCode, System.Exception exception) { }
    }
}

namespace Functorium.Abstractions.Registrations
{
    public static class OpenTelemetryRegistration
    {
        public static Functorium.Adapters.Observabilities.Builders.OpenTelemetryBuilder RegisterObservability(
            this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration configuration) { }
    }
}
```

### 특징

- **메서드 본문 없음**: `{ }` 로만 표시
- **전체 타입 이름**: `LanguageExt.Common.Error`, `System.Exception`
- **제네릭 제약 조건**: `where T : notnull`
- **확장 메서드**: `this` 키워드 표시

---

## PublicApiGenerator 옵션

| 옵션 | 기본값 | 설명 |
|------|--------|------|
| `IncludeAssemblyAttributes` | true | 어셈블리 속성 포함 |
| `DenyNamespacePrefixes` | (없음) | 제외할 네임스페이스 |
| `AllowNamespacePrefixes` | (없음) | 포함할 네임스페이스 |
| `ExcludeAttributes` | (없음) | 제외할 속성 |

### 사용 예시

```csharp
var options = new ApiGeneratorOptions
{
    IncludeAssemblyAttributes = false,
    DenyNamespacePrefixes = new[]
    {
        "System.Runtime.CompilerServices",
        "Microsoft.CodeAnalysis"
    },
    ExcludeAttributes = new[]
    {
        "System.Diagnostics.DebuggerNonUserCodeAttribute"
    }
};
```

---

## ExtractApiChanges.cs와의 연동

ExtractApiChanges.cs에서 ApiGenerator.cs를 호출하는 방식:

```csharp
// ExtractApiChanges.cs에서
var apiResult = await RunProcessAsync(
    "dotnet",
    $"\"{apiGeneratorPath}\" \"{dllPath}\" -"  // 콘솔로 출력 (-)
);

if (apiResult.ExitCode == 0)
{
    // API 텍스트를 파일로 저장
    var content = new StringBuilder();
    content.AppendLine("// <auto-generated>");
    content.Append(apiResult.Output);
    await File.WriteAllTextAsync(outputFile, content.ToString());
}
```

---

## 오류 처리

### DLL 없음

```csharp
if (!File.Exists(dllPath))
{
    Console.Error.WriteLine($"Error: DLL not found: {dllPath}");
    Environment.Exit(1);
}
```

### 종속성 해결 실패

```csharp
protected override Assembly? Load(AssemblyName assemblyName)
{
    // ... 로드 시도 ...

    // 찾지 못하면 null 반환 (예외 대신)
    return null;
}
```

### API 생성 실패

```csharp
try
{
    var publicApi = assembly.GeneratePublicApi(options);
    // ...
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error generating API: {ex.Message}");
    Environment.Exit(1);
}
```

---

## 실제 사용 예시

### 명령줄에서 직접 실행

```bash
# Functorium.dll의 API 추출
dotnet ApiGenerator.cs bin/Release/net10.0/Functorium.dll api-output.cs

# 콘솔로 출력하여 확인
dotnet ApiGenerator.cs bin/Release/net10.0/Functorium.dll - | head -50
```

### ExtractApiChanges.cs에서 자동 호출

```bash
# ExtractApiChanges.cs 실행 시 내부적으로 ApiGenerator.cs 호출
dotnet ExtractApiChanges.cs

# 결과 확인
cat Src/Functorium/.api/Functorium.cs
```

---

## 요약

| 항목 | 설명 |
|------|------|
| 목적 | DLL에서 Public API 추출 |
| 입력 | DLL 경로, 출력 경로 |
| 출력 | C# 형식의 API 텍스트 |
| 핵심 라이브러리 | PublicApiGenerator |
| 종속성 해결 | CustomAssemblyLoadContext |

---

## 다음 단계

- [5.7 SummarizeSlowestTests.cs 분석](07-summarize-tests.md)
