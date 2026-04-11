---
title: "What is Source Generator"
---

## 개요

소프트웨어가 복잡해질수록 반복적인 코드 작성은 피할 수 없는 현실이 됩니다. 로깅, 직렬화, 유효성 검사처럼 패턴이 동일한 코드를 매번 손으로 작성하면 실수가 생기고 일관성이 무너집니다. C# 컴파일러에 내장된 **소스 생성기(Source Generator)는** 이 문제를 컴파일 타임에 해결합니다. 기존 코드를 분석하고 필요한 코드를 자동으로 만들어 주기 때문에, 개발자는 핵심 로직에만 집중할 수 있습니다.

## 학습 목표

### 핵심 학습 목표
1. **소스 생성기의 정의와 동작 원리 이해**
   - 컴파일 파이프라인에서 소스 생성기가 실행되는 시점 파악
   - 구문 분석과 의미 분석 이후, IL 생성 이전이라는 위치의 의미 이해
2. **컴파일 타임 코드 생성의 개념 파악**
   - 런타임 코드 생성과의 근본적 차이 이해
   - 추가 전용(Additive Only)과 결정적 출력(Deterministic)의 설계 원칙
3. **소스 생성기의 기본 구조 학습**
   - `IIncrementalGenerator` 인터페이스와 파이프라인 구성 방식

---

## 소스 생성기 정의

**소스 생성기(Source Generator)는** C# 컴파일러의 확장 기능으로, 컴파일 과정에서 소스 코드를 분석하고 새로운 C# 코드를 자동으로 생성합니다.

```
컴파일 과정에서의 소스 생성기 위치
==================================

    소스 코드 (.cs)
          │
          ▼
    ┌─────────────────┐
    │   구문 분석      │  Syntax Analysis
    │   (Parsing)     │
    └────────┬────────┘
             │
             ▼
    ┌─────────────────┐
    │   의미 분석      │  Semantic Analysis
    │   (Binding)     │
    └────────┬────────┘
             │
             ▼
    ┌─────────────────┐
    │  소스 생성기     │  ← 여기서 실행!
    │  (Generator)    │
    └────────┬────────┘
             │
             ▼
    ┌─────────────────┐
    │   코드 생성      │  IL Code Generation
    │   (Emit)        │
    └────────┬────────┘
             │
             ▼
      어셈블리 (.dll)
```

소스 생성기는 컴파일러가 소스 코드를 분석한 후, IL 코드를 생성하기 전에 실행됩니다. 이 시점에서 기존 코드의 구조를 분석하고, 필요한 코드를 추가로 생성할 수 있습니다. 이 튜토리얼에서 구현할 **ObservablePortGenerator**도 바로 이 시점에 어댑터 클래스를 분석하여 Observability 코드를 자동으로 만들어 냅니다.

이러한 동작 원리를 이해했으니, 이제 소스 생성기가 가진 세 가지 핵심 특징을 살펴보겠습니다.

---

## 핵심 특징

소스 생성기의 설계는 세 가지 원칙 위에 서 있습니다. 컴파일 타임에만 실행되고, 기존 코드를 절대 수정하지 않으며, 동일한 입력에 대해 항상 같은 결과를 보장합니다. 이 원칙들이 왜 중요한지 하나씩 살펴보겠습니다.

### 1. 컴파일 타임 실행

소스 생성기는 런타임이 아닌 **컴파일 타임**에 실행됩니다:

```csharp
// 개발자가 작성한 코드
[GenerateObservablePort]
public class UserRepository : IObservablePort
{
    public FinT<IO, User> GetUserAsync(int id) => ...;
}

// 컴파일러가 자동 생성한 코드 (소스 생성기에 의해)
public partial class UserRepositoryObservable
{
    public FinT<IO, User> GetUserAsync(int id)
    {
        // 로깅, 메트릭 등 자동 생성된 코드
    }
}
```

### 2. 추가 전용 (Additive Only)

컴파일 타임에 실행된다면 기존 코드를 마음대로 바꿀 수도 있을까요? 그렇지 않습니다. 소스 생성기는 기존 코드를 **수정하거나 삭제할 수 없습니다**. 오직 새로운 코드만 추가할 수 있습니다:

```
소스 생성기의 제약
================

  ✓ 새 파일 추가        → 가능
  ✓ 새 클래스 추가      → 가능
  ✓ partial 클래스 확장 → 가능

  ✗ 기존 코드 수정      → 불가능
  ✗ 기존 코드 삭제      → 불가능
  ✗ 기존 파일 덮어쓰기  → 불가능
```

### 3. 결정적 출력 (Deterministic)

동일한 입력에 대해 항상 동일한 출력을 생성해야 합니다. 이는 증분 빌드와 캐싱을 위해 필수적입니다. ObservablePortGenerator도 이 원칙을 따르기 때문에, 같은 어댑터 클래스에 대해서는 항상 동일한 Observable 래퍼 코드가 생성됩니다.

이 세 가지 특징이 소스 생성기의 안전망 역할을 합니다. 이제 실제로 소스 생성기를 어떤 구조로 작성하는지 살펴보겠습니다.

---

## 소스 생성기의 기본 구조

모든 소스 생성기는 `IIncrementalGenerator` 인터페이스를 구현합니다:

```csharp
using Microsoft.CodeAnalysis;

[Generator]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 소스 코드에서 관심 있는 부분 선택 (Provider 등록)
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "MyNamespace.MyAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => GetClassInfo(ctx));

        // 2. 코드 생성 로직 등록
        context.RegisterSourceOutput(provider, (spc, classInfo) =>
        {
            var code = GenerateCode(classInfo);
            spc.AddSource($"{classInfo.Name}.g.cs", code);
        });
    }
}
```

### 구성 요소

| 구성 요소 | 역할 |
|-----------|------|
| `[Generator]` 특성 | 컴파일러에게 이 클래스가 소스 생성기임을 알림 |
| `IIncrementalGenerator` | 증분 소스 생성기 인터페이스 |
| `Initialize` 메서드 | 생성기 초기화 및 파이프라인 구성 |
| `SyntaxProvider` | 소스 코드에서 관심 있는 노드 선택 |
| `RegisterSourceOutput` | 생성된 코드 출력 |

기본 구조를 이해했으니, 소스 생성기가 기존 코드 생성 기술과 어떻게 다른지 비교해 보겠습니다.

---

## 소스 생성기 vs 다른 기술

### T4 템플릿과의 비교

T4 템플릿은 오랫동안 .NET 생태계에서 코드 생성의 표준이었지만, 컴파일러와 분리되어 있다는 근본적인 한계가 있습니다.

```
T4 템플릿
=========
- 별도의 .tt 파일 필요
- 디자인 타임에 실행
- 생성된 코드를 소스 제어에 포함해야 함
- 입력 소스의 변경을 자동 감지하지 못함

소스 생성기
==========
- 컴파일러에 통합
- 컴파일 타임에 실행
- 생성된 코드가 소스 제어에 불필요
- 입력 변경 시 자동으로 재생성
```

### Reflection.Emit과의 비교

Reflection.Emit은 런타임에 IL 코드를 직접 생성하는 강력한 도구이지만, AOT 환경에서는 사용할 수 없고 디버깅도 어렵습니다.

```
Reflection.Emit
===============
- 런타임에 IL 코드 직접 생성
- 디버깅이 어려움
- AOT 컴파일과 호환성 문제
- 높은 학습 곡선

소스 생성기
==========
- 컴파일 타임에 C# 코드 생성
- 생성된 코드 디버깅 가능
- AOT 컴파일 완벽 지원
- C# 문법만 알면 작성 가능
```

이처럼 소스 생성기는 기존 기술들의 단점을 해결하면서도, 컴파일러와의 긴밀한 통합이라는 고유한 강점을 제공합니다. 실제로 .NET 생태계에서 이미 널리 활용되고 있습니다.

---

## 실제 사용 사례

소스 생성기는 다양한 시나리오에서 활용됩니다:

### 1. JSON 직렬화 (System.Text.Json)

```csharp
[JsonSerializable(typeof(User))]
public partial class MyJsonContext : JsonSerializerContext
{
}
// → 컴파일 타임에 직렬화 코드 생성
```

### 2. 로깅 (Microsoft.Extensions.Logging)

```csharp
[LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} logged in")]
public static partial void LogUserLogin(ILogger logger, int userId);
// → 고성능 로깅 코드 생성
```

### 3. 의존성 주입

```csharp
[RegisterService]
public class UserService : IUserService
{
}
// → DI 컨테이너 등록 코드 생성
```

---

## 한눈에 보는 정리

소스 생성기는 C# 컴파일러의 확장 기능으로, 구문/의미 분석 이후 IL 생성 이전 시점에 실행됩니다. 기존 코드를 수정할 수 없는 추가 전용 모델을 따르며, 동일 입력에 대해 항상 같은 출력을 보장합니다. `IIncrementalGenerator` 인터페이스를 구현하여 작성하며, 성능, 타입 안전성, 디버깅 용이성, AOT 지원이라는 장점을 제공합니다.

---

## FAQ

### Q1: 소스 생성기와 런타임 코드 생성은 어떤 차이가 있나요?
**A**: 소스 생성기는 컴파일 타임에 C# 소스 코드를 생성하므로 런타임 오버헤드가 없고, 생성된 코드를 디버거로 직접 확인할 수 있습니다. 반면 런타임 코드 생성(Reflection.Emit, Expression Trees 등)은 애플리케이션 실행 중에 동작하여 성능 비용이 발생하고, AOT 환경에서 제약이 있습니다.

### Q2: "추가 전용(Additive Only)"이라는 제약은 왜 필요한가요?
**A**: 기존 코드를 수정하거나 삭제할 수 있다면, 여러 소스 생성기가 동시에 동작할 때 서로의 변경이 충돌하여 예측 불가능한 결과를 초래합니다. 추가 전용 모델은 이러한 충돌을 원천적으로 방지하고, 개발자가 작성한 원본 코드의 무결성을 보장합니다.

### Q3: `IIncrementalGenerator`와 이전의 `ISourceGenerator`는 무엇이 다른가요?
**A**: `ISourceGenerator`는 소스 변경 시마다 전체 생성 로직을 재실행했지만, `IIncrementalGenerator`는 변경된 부분만 다시 처리하는 증분 파이프라인을 제공합니다. 이 덕분에 대규모 프로젝트에서도 빌드 성능이 크게 향상되며, 현재 공식적으로 권장되는 방식입니다.

---

소스 생성기가 무엇인지 이해했으니, 이제 왜 다른 기술 대신 소스 생성기를 선택해야 하는지 더 구체적인 근거를 살펴보겠습니다.

→ [02. 왜 소스 생성기인가](02-why-source-generator.md)
