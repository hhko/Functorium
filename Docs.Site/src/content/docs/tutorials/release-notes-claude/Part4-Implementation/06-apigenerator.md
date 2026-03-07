---
title: "ApiGenerator"
---

.NET DLL에서 Public API만 정확하게 추출하려면 어떻게 해야 할까요? 리플렉션으로 타입을 열거할 수도 있지만, 제네릭 제약 조건, 확장 메서드, 네임스페이스 구조까지 C# 형식으로 깔끔하게 출력하려면 상당한 작업이 필요합니다. ApiGenerator.cs는 PublicApiGenerator 라이브러리를 활용하여 이 문제를 해결합니다. ExtractApiChanges.cs가 프로젝트를 빌드하고 결과를 조합하는 오케스트레이터라면, ApiGenerator.cs는 실제로 DLL을 열어 Public API를 추출하는 실무 담당자입니다.

## 파일 위치와 사용법

```txt
.release-notes/scripts/ApiGenerator.cs
```

```bash
# 파일로 출력
dotnet ApiGenerator.cs <dll-path> <output-file>

# 콘솔로 출력 (- 사용)
dotnet ApiGenerator.cs <dll-path> -
```

ExtractApiChanges.cs에서 호출할 때는 콘솔 출력 모드(`-`)를 사용하여 결과를 파이프라인으로 받습니다.

## 패키지 참조

이 스크립트는 다른 스크립트들과 달리 **PublicApiGenerator 패키지를** 사용합니다. Microsoft에서 만든 이 라이브러리가 어셈블리의 Public API를 C# 형식으로 추출하는 핵심 기능을 제공합니다.

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

## 스크립트 구조

CLI는 두 개의 Argument를 받습니다. DLL 경로와 출력 파일 경로입니다.

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

## DLL 로드와 종속성 해결

API를 추출하려면 DLL을 메모리에 로드해야 합니다. 그런데 단순히 `Assembly.LoadFrom()`을 사용하면 문제가 생깁니다. 대상 DLL이 참조하는 다른 어셈블리를 찾지 못해 로드에 실패할 수 있기 때문입니다.

이 문제를 해결하기 위해 **커스텀 AssemblyLoadContext를** 사용합니다. .NET의 AssemblyLoadContext는 어셈블리 로딩을 격리하고 커스터마이즈할 수 있는 메커니즘입니다. 종속성을 찾을 때 먼저 기본 컨텍스트(런타임에 이미 로드된 어셈블리)에서 찾고, 없으면 DLL과 같은 디렉터리에서 찾습니다. `dotnet publish`가 모든 종속성을 출력 디렉터리에 복사해두므로, 같은 디렉터리 검색으로 대부분의 종속성을 해결할 수 있습니다.

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

`isCollectible: true`로 생성하면 사용 후 가비지 컬렉션이 가능합니다. 종속성을 찾지 못하면 예외 대신 `null`을 반환하여, 꼭 필요하지 않은 어셈블리 때문에 전체가 실패하는 것을 방지합니다.

## Public API 추출

어셈블리가 로드되면 PublicApiGenerator로 API를 추출합니다. 옵션을 통해 불필요한 어셈블리 속성과 컴파일러 생성 네임스페이스를 제외합니다.

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

결과는 콘솔(`-`)이나 파일로 출력됩니다.

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

## 출력 형식

생성되는 API 텍스트는 실제 C# 코드와 유사하지만 몇 가지 특징이 있습니다. 메서드 본문은 `{ }`로만 표시되고, 타입 이름은 전체 경로(`LanguageExt.Common.Error`, `System.Exception`)로 출력됩니다. 제네릭 제약 조건과 확장 메서드의 `this` 키워드도 그대로 유지됩니다.

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

## PublicApiGenerator 옵션

필요에 따라 추출 범위를 조정할 수 있습니다.

| 옵션 | 기본값 | 설명 |
|------|--------|------|
| `IncludeAssemblyAttributes` | true | 어셈블리 속성 포함 |
| `DenyNamespacePrefixes` | (없음) | 제외할 네임스페이스 |
| `AllowNamespacePrefixes` | (없음) | 포함할 네임스페이스 |
| `ExcludeAttributes` | (없음) | 제외할 속성 |

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

## ExtractApiChanges.cs와의 연동

ExtractApiChanges.cs에서 ApiGenerator.cs를 호출할 때는 콘솔 출력 모드를 사용합니다. 출력된 API 텍스트를 받아 `<auto-generated>` 헤더를 추가한 뒤 파일로 저장합니다.

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

## 오류 처리

DLL 미발견, 종속성 해결 실패, API 생성 실패 세 가지 경우를 처리합니다. 종속성 해결 실패는 `null`을 반환하여 치명적이지 않은 오류로 처리하고, DLL 미발견과 API 생성 실패는 프로세스를 종료합니다.

```csharp
if (!File.Exists(dllPath))
{
    Console.Error.WriteLine($"Error: DLL not found: {dllPath}");
    Environment.Exit(1);
}
```

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

## 실제 사용 예시

명령줄에서 직접 실행하여 API를 확인할 수도 있습니다.

```bash
# Functorium.dll의 API 추출
dotnet ApiGenerator.cs bin/Release/net10.0/Functorium.dll api-output.cs

# 콘솔로 출력하여 확인
dotnet ApiGenerator.cs bin/Release/net10.0/Functorium.dll - | head -50
```

일반적으로는 ExtractApiChanges.cs가 자동으로 호출하므로 직접 실행할 필요는 없습니다.

```bash
# ExtractApiChanges.cs 실행 시 내부적으로 ApiGenerator.cs 호출
dotnet ExtractApiChanges.cs

# 결과 확인
cat Src/Functorium/.api/Functorium.cs
```

지금까지 Phase 2 데이터 수집에 사용되는 세 가지 스크립트를 모두 살펴보았습니다. AnalyzeAllComponents.cs가 Git 변경사항을 수집하고, ExtractApiChanges.cs가 API 추출을 오케스트레이션하며, ApiGenerator.cs가 DLL에서 실제 API를 읽어냅니다. 이 데이터들이 준비되면 다음 단계는 릴리스 노트의 구조를 결정하는 템플릿과 설정 파일입니다.

## 다음 단계

- [TEMPLATE.md 구조](07-template-structure.md)
