# 소스 생성기란 무엇인가

## 학습 목표

- 소스 생성기의 정의와 동작 원리 이해
- 컴파일 타임 코드 생성의 개념 파악
- 소스 생성기의 기본 구조 학습

---

## 소스 생성기 정의

**소스 생성기(Source Generator)**는 C# 컴파일러의 확장 기능으로, 컴파일 과정에서 소스 코드를 분석하고 새로운 C# 코드를 자동으로 생성합니다.

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

소스 생성기는 컴파일러가 소스 코드를 분석한 후, IL 코드를 생성하기 전에 실행됩니다. 이 시점에서 기존 코드의 구조를 분석하고, 필요한 코드를 추가로 생성할 수 있습니다.

---

## 핵심 특징

### 1. 컴파일 타임 실행

소스 생성기는 런타임이 아닌 **컴파일 타임**에 실행됩니다:

```csharp
// 개발자가 작성한 코드
[GeneratePipeline]
public class UserRepository : IAdapter
{
    public FinT<IO, User> GetUserAsync(int id) => ...;
}

// 컴파일러가 자동 생성한 코드 (소스 생성기에 의해)
public partial class UserRepositoryPipeline
{
    public FinT<IO, User> GetUserAsync(int id)
    {
        // 로깅, 메트릭 등 자동 생성된 코드
    }
}
```

### 2. 추가 전용 (Additive Only)

소스 생성기는 기존 코드를 **수정하거나 삭제할 수 없습니다**. 오직 새로운 코드만 추가할 수 있습니다:

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

동일한 입력에 대해 항상 동일한 출력을 생성해야 합니다. 이는 증분 빌드와 캐싱을 위해 필수적입니다.

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

---

## 소스 생성기 vs 다른 기술

### T4 템플릿과의 비교

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

## 요약

| 항목 | 설명 |
|------|------|
| 정의 | 컴파일 타임에 C# 코드를 자동 생성하는 컴파일러 확장 |
| 실행 시점 | 컴파일 타임 (구문/의미 분석 후, IL 생성 전) |
| 주요 제약 | 추가 전용 (기존 코드 수정 불가) |
| 핵심 인터페이스 | `IIncrementalGenerator` |
| 장점 | 성능, 타입 안전성, 디버깅 용이성, AOT 지원 |

---

## 다음 단계

다음 섹션에서는 소스 생성기를 사용해야 하는 이유와 장점을 더 자세히 살펴봅니다.

➡️ [02. 왜 소스 생성기인가](02-why-source-generator.md)
