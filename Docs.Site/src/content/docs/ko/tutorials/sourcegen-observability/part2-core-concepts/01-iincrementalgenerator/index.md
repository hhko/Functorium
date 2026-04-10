---
title: "IIncrementalGenerator"
---

## 개요

소스 생성기를 만들 때 가장 먼저 마주치는 질문은 "어떤 인터페이스를 구현해야 하는가"입니다. .NET 6 이전에는 `ISourceGenerator`가 유일한 선택이었지만, 매 키 입력마다 모든 소스 파일을 다시 처리하는 구조 탓에 IDE 성능이 심각하게 저하되었습니다. **IIncrementalGenerator는** 이 문제를 해결하기 위해 도입된 현재 표준 인터페이스로, 변경된 파일만 처리하는 증분 파이프라인을 선언적으로 구성할 수 있게 해줍니다.

## 학습 목표

### 핵심 학습 목표
1. **IIncrementalGenerator 인터페이스 구조를** 이해한다
   - Initialize 메서드 하나로 전체 파이프라인을 구성하는 방식
2. **IncrementalGeneratorInitializationContext의** 주요 멤버를 파악한다
   - 고정 코드 등록, 소스 분석, 출력 등록의 역할 구분
3. **ObservablePortGenerator에서의** 실제 적용 패턴을 학습한다
   - IncrementalGeneratorBase를 통한 템플릿 메서드 패턴

---

## IIncrementalGenerator란?

**IIncrementalGenerator는** .NET 6부터 도입된 **증분 소스 생성기의** 핵심 인터페이스입니다. 기존 `ISourceGenerator`가 모든 파일을 매번 처리했다면, `IIncrementalGenerator`는 변경된 파일만 선별적으로 처리하여 빌드 성능을 크게 향상시킵니다.

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

Initialize 메서드의 파라미터인 `IncrementalGeneratorInitializationContext`는 소스 생성기 파이프라인을 구성하는 데 필요한 모든 것을 제공합니다. 이 구조체의 멤버들은 크게 세 가지 역할로 나뉩니다. **고정 코드 등록**(Attribute 정의 등 항상 동일한 코드), **소스 분석**(SyntaxProvider를 통한 데이터 추출), **출력 등록**(분석 결과를 바탕으로 실제 코드 생성)입니다.

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
    //Action<IncrementalGeneratorPostInitializationContext>? registerPostInitializationSourceOutput = null,
    bool AttachDebugger = false)
    : IIncrementalGenerator
{
    protected const string ClassEntityName = "class";

    private readonly bool _attachDebugger = AttachDebugger;
    private readonly Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> _registerSourceProvider = registerSourceProvider;
    private readonly Action<SourceProductionContext, ImmutableArray<TValue>> _generate = generate;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        // DEBUG 빌드에서만 디버거 연결 지원
        if (_attachDebugger && Debugger.IsAttached is false)
        {
            Debugger.Launch();
        }
#endif

        // 1단계: 소스 제공자 등록 (구현체에서 정의) + null 필터링
        IncrementalValuesProvider<TValue> provider = _registerSourceProvider(context)
            .Where(static m => m is not null);

        // 2단계: 코드 생성 등록 (구현체에서 정의)
        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<TValue> displayValues)
    {
        _generate(context, displayValues);
    }
}
```

### 사용 예

```csharp
[Generator(LanguageNames.CSharp)]
public sealed class ObservablePortGenerator()
    : IncrementalGeneratorBase<ObservableClassInfo>(
        RegisterSourceProvider,    // 1단계 구현
        Generate,                  // 2단계 구현
        AttachDebugger: false)
{
    private static IncrementalValuesProvider<ObservableClassInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        // 속성 정의 생성 + 클래스 필터링
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<ObservableClassInfo> observableClasses)
    {
        // 각 클래스에 대해 Observable 코드 생성
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
ctx.AddSource("Repositories.UserRepositoryObservable.g.cs", code);
```

---

## 한눈에 보는 정리

`IIncrementalGenerator`는 `Initialize` 메서드 하나만 구현하면 되는 단순한 인터페이스이지만, 그 안에서 선언적 파이프라인을 통해 강력한 증분 빌드를 지원합니다. `RegisterPostInitializationOutput`으로 Attribute 같은 고정 코드를 등록하고, `SyntaxProvider`로 관심 있는 노드를 필터링한 뒤, `RegisterSourceOutput`으로 실제 코드를 생성하는 세 단계 구조를 기억하면 됩니다.

| 구성 요소 | 역할 |
|-----------|------|
| `IIncrementalGenerator` | 소스 생성기 인터페이스 |
| `[Generator]` | 컴파일러에게 생성기임을 알림 |
| `Initialize` | 파이프라인 구성 |
| `RegisterPostInitializationOutput` | 고정 코드 생성 |
| `SyntaxProvider` | 소스 코드 분석 |
| `RegisterSourceOutput` | 동적 코드 생성 |

---

## FAQ

### Q1: `RegisterPostInitializationOutput`으로 생성한 속성 코드는 언제 컴파일에 추가되나요?
**A**: 파이프라인 실행 이전의 Post-Initialization 단계에서 즉시 추가됩니다. 이 코드는 사용자 소스 코드와 함께 컴파일되므로, `ForAttributeWithMetadataName`에서 해당 속성을 참조할 수 있습니다. 소스 변경과 무관하게 항상 동일한 결과를 생성하는 고정 코드에 적합합니다.

### Q2: `IncrementalGeneratorBase<TValue>`의 `Collect()`는 왜 필요한가요?
**A**: `Collect()`는 여러 개의 `IncrementalValuesProvider<T>` 항목을 하나의 `ImmutableArray<T>`로 모읍니다. Functorium에서는 모든 대상 클래스를 한 번에 받아 `StringBuilder`를 재사용하며 순차적으로 코드를 생성하기 위해 이 패턴을 사용합니다. 다만 개별 항목 캐싱이 필요한 경우에는 `Collect` 없이 개별 처리가 더 효율적입니다.

### Q3: `AddSource`의 `hintName`은 어떤 규칙으로 정해야 하나요?
**A**: `hintName`은 프로젝트 내에서 고유해야 하며, 확장자를 `.g.cs`로 지정하는 것이 관례입니다. 서로 다른 네임스페이스에 같은 이름의 클래스가 있을 수 있으므로, 네임스페이스 접미사를 포함하여 `Repositories.UserRepositoryObservable.g.cs` 형태로 지정하면 충돌을 방지할 수 있습니다.

---

`IIncrementalGenerator`의 전체 구조를 이해했으니, 다음으로는 파이프라인의 핵심 구성 요소인 Provider 패턴을 살펴봅니다. LINQ와 유사한 선언적 연산자들이 어떻게 데이터를 변환하고 필터링하는지 학습합니다.

→ [02. Provider 패턴](../02-Provider-Pattern/)
