# IIncrementalGenerator 인터페이스

## 학습 목표

- IIncrementalGenerator 인터페이스 구조 이해
- Initialize 메서드의 역할 파악
- IncrementalGeneratorInitializationContext 활용법 학습

---

## IIncrementalGenerator란?

**IIncrementalGenerator**는 .NET 6부터 도입된 **증분 소스 생성기**의 핵심 인터페이스입니다. 기존 ISourceGenerator보다 성능이 크게 향상되었습니다.

```
ISourceGenerator (레거시)
========================
- 모든 소스 파일을 매번 처리
- 캐싱 없음
- 느린 빌드 성능

IIncrementalGenerator (현재 표준)
================================
- 변경된 파일만 처리
- 자동 캐싱
- 빠른 빌드 성능 (증분 빌드 지원)
```

---

## 인터페이스 정의

```csharp
namespace Microsoft.CodeAnalysis;

public interface IIncrementalGenerator
{
    void Initialize(IncrementalGeneratorInitializationContext context);
}
```

매우 단순합니다. **Initialize** 메서드 하나만 구현하면 됩니다.

---

## 최소 구현 예제

```csharp
using Microsoft.CodeAnalysis;

[Generator(LanguageNames.CSharp)]  // ← 필수 속성
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 여기서 소스 생성 파이프라인을 구성합니다
    }
}
```

### [Generator] 속성

```csharp
[Generator(LanguageNames.CSharp)]  // C# 전용
[Generator(LanguageNames.VisualBasic)]  // VB 전용
[Generator]  // 모든 언어 (비권장)
```

---

## IncrementalGeneratorInitializationContext

Initialize 메서드의 파라미터로, 소스 생성기를 구성하는 데 필요한 모든 것을 제공합니다.

### 주요 멤버

```csharp
public readonly struct IncrementalGeneratorInitializationContext
{
    // 1. 고정 코드 등록 (Post-initialization)
    public void RegisterPostInitializationOutput(
        Action<IncrementalGeneratorPostInitializationContext> callback);

    // 2. 소스 코드 분석 (Syntax Provider)
    public SyntaxValueProvider SyntaxProvider { get; }

    // 3. 추가 텍스트 파일 접근
    public IncrementalValuesProvider<AdditionalText> AdditionalTextsProvider { get; }

    // 4. 컴파일 옵션 접근
    public IncrementalValueProvider<CompilationOptions> CompilationOptionsProvider { get; }

    // 5. 분석기 옵션 접근
    public IncrementalValueProvider<AnalyzerConfigOptionsProvider> AnalyzerConfigOptionsProvider { get; }

    // 6. 컴파일 전체 접근
    public IncrementalValueProvider<Compilation> CompilationProvider { get; }

    // 7. 소스 출력 등록
    public void RegisterSourceOutput<TSource>(
        IncrementalValueProvider<TSource> source,
        Action<SourceProductionContext, TSource> action);

    public void RegisterSourceOutput<TSource>(
        IncrementalValuesProvider<TSource> source,
        Action<SourceProductionContext, TSource> action);
}
```

---

## 소스 생성 파이프라인 구조

```
Initialize 메서드에서 하는 일
============================

1. RegisterPostInitializationOutput
   │  고정 코드 등록 (예: Attribute 정의)
   │
   ▼
2. SyntaxProvider로 필터링
   │  관심 있는 노드만 선택
   │
   ▼
3. 데이터 변환
   │  Syntax → 코드 생성에 필요한 정보
   │
   ▼
4. RegisterSourceOutput
   │  실제 코드 생성 및 출력
   │
   ▼
5. 컴파일러가 생성된 코드를 포함하여 빌드
```

---

## 기본 패턴

### 패턴 1: 고정 코드만 생성

```csharp
[Generator(LanguageNames.CSharp)]
public class FixedCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 항상 동일한 코드 생성
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("Constants.g.cs", """
                namespace MyApp;

                public static class GeneratedConstants
                {
                    public const string Version = "1.0.0";
                }
                """);
        });
    }
}
```

### 패턴 2: 속성 기반 코드 생성

```csharp
[Generator(LanguageNames.CSharp)]
public class AttributeBasedGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Attribute 정의 생성
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("MyAttribute.g.cs", """
                namespace MyApp;

                [System.AttributeUsage(System.AttributeTargets.Class)]
                public class GenerateAttribute : System.Attribute { }
                """);
        });

        // 2. [Generate] 속성이 붙은 클래스 찾기
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "MyApp.GenerateAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => ctx.TargetSymbol.Name);

        // 3. 코드 생성
        context.RegisterSourceOutput(provider, (ctx, className) =>
        {
            ctx.AddSource($"{className}.g.cs", $"""
                namespace MyApp;

                public partial class {className}
                {{
                    public void GeneratedMethod() {{ }}
                }}
                """);
        });
    }
}
```

---

## Functorium의 IncrementalGeneratorBase

Functorium 프로젝트는 **템플릿 메서드 패턴**을 적용한 기본 클래스를 제공합니다:

```csharp
// IncrementalGeneratorBase.cs
public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext,
         IncrementalValuesProvider<TValue>> registerSourceProvider,
    Action<SourceProductionContext, ImmutableArray<TValue>> generate,
    bool AttachDebugger = false)
    : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 디버거 연결 (옵션)
        if (AttachDebugger)
        {
            Debugger.Launch();
        }

        // 1단계: 소스 제공자 등록 (구현체에서 정의)
        IncrementalValuesProvider<TValue> provider = registerSourceProvider(context);

        // 2단계: 코드 생성 등록 (구현체에서 정의)
        context.RegisterSourceOutput(provider.Collect(), generate);
    }
}
```

### 사용 예

```csharp
[Generator(LanguageNames.CSharp)]
public sealed class AdapterPipelineGenerator()
    : IncrementalGeneratorBase<PipelineClassInfo>(
        RegisterSourceProvider,    // 1단계 구현
        Generate,                  // 2단계 구현
        AttachDebugger: false)
{
    private static IncrementalValuesProvider<PipelineClassInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        // 속성 정의 생성 + 클래스 필터링
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<PipelineClassInfo> pipelineClasses)
    {
        // 각 클래스에 대해 Pipeline 코드 생성
    }
}
```

---

## SourceProductionContext

코드를 출력할 때 사용하는 컨텍스트입니다:

```csharp
public readonly struct SourceProductionContext
{
    // 소스 코드 추가
    public void AddSource(string hintName, string source);
    public void AddSource(string hintName, SourceText sourceText);

    // 진단 보고
    public void ReportDiagnostic(Diagnostic diagnostic);

    // 취소 토큰
    public CancellationToken CancellationToken { get; }
}
```

### 소스 추가 시 주의사항

```csharp
// hintName: 파일 이름 (확장자 포함, 고유해야 함)
ctx.AddSource("UserRepository.g.cs", code);

// 네임스페이스 충돌 방지를 위한 접두사 추가
ctx.AddSource("Repositories.UserRepositoryPipeline.g.cs", code);
```

---

## 요약

| 구성 요소 | 역할 |
|-----------|------|
| `IIncrementalGenerator` | 소스 생성기 인터페이스 |
| `[Generator]` | 컴파일러에게 생성기임을 알림 |
| `Initialize` | 파이프라인 구성 |
| `RegisterPostInitializationOutput` | 고정 코드 생성 |
| `SyntaxProvider` | 소스 코드 분석 |
| `RegisterSourceOutput` | 동적 코드 생성 |

---

## 다음 단계

다음 섹션에서는 Provider 패턴을 자세히 학습합니다.

➡️ [02. Provider 패턴](02-provider-pattern.md)
