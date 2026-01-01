# 프로젝트 개요

## 학습 목표

- AdapterPipelineGenerator의 목적과 기대 효과 이해
- 소스 생성기로 구현한 이유 파악
- 전체 프로젝트 구조 파악

---

## AdapterPipelineGenerator란?

**AdapterPipelineGenerator**는 어댑터 클래스에 **Observability(관측 가능성)** 기능을 자동으로 추가하는 소스 생성기입니다.

### 해결하려는 문제

어댑터 계층(데이터베이스, 외부 API 호출 등)에서는 다음과 같은 **횡단 관심사(Cross-Cutting Concerns)**를 처리해야 합니다:

```
횡단 관심사 목록
===============

1. 로깅 (Logging)
   - 요청/응답 기록
   - 파라미터 값 추적
   - 오류 정보 기록

2. 추적 (Tracing)
   - 분산 추적 컨텍스트 전파
   - Activity 생성 및 관리
   - 요청 간 상관관계 추적

3. 메트릭 (Metrics)
   - 응답 시간 측정
   - 성공/실패 카운터
   - 리소스 사용량 측정
```

### 수동 구현의 문제점

이러한 기능을 **수동으로 구현**하면 다음과 같은 문제가 발생합니다:

```csharp
// 수동 구현 - 모든 메서드에 반복되는 보일러플레이트 코드
public class UserRepository : IAdapter
{
    private readonly ILogger<UserRepository> _logger;
    private readonly IAdapterTrace _trace;
    private readonly IAdapterMetric _metric;

    public FinT<IO, User> GetUserAsync(int userId)
    {
        // 시작 시간 기록
        var startTimestamp = Stopwatch.GetTimestamp();

        // 요청 로깅
        _logger.LogInformation("GetUserAsync 요청: userId={UserId}", userId);

        // 추적 Activity 생성
        using var activity = _trace.StartActivity("GetUserAsync");

        try
        {
            // 실제 비즈니스 로직
            var result = await _dbContext.Users.FindAsync(userId);

            // 성공 로깅
            var elapsed = CalculateElapsed(startTimestamp);
            _logger.LogInformation("GetUserAsync 성공: {Elapsed}ms", elapsed);

            // 메트릭 기록
            _metric.RecordSuccess("GetUserAsync", elapsed);

            return result;
        }
        catch (Exception ex)
        {
            // 실패 로깅
            var elapsed = CalculateElapsed(startTimestamp);
            _logger.LogError(ex, "GetUserAsync 실패: {Elapsed}ms", elapsed);

            // 메트릭 기록
            _metric.RecordFailure("GetUserAsync", elapsed);

            throw;
        }
    }

    // 다른 메서드에도 동일한 패턴 반복...
    public FinT<IO, IEnumerable<User>> GetUsersAsync() { /* 동일한 보일러플레이트 */ }
    public FinT<IO, Unit> UpdateUserAsync(User user) { /* 동일한 보일러플레이트 */ }
    public FinT<IO, Unit> DeleteUserAsync(int userId) { /* 동일한 보일러플레이트 */ }
}
```

**문제점:**
- 메서드당 30-50줄의 **보일러플레이트 코드** 추가
- 실수로 로깅을 빠뜨릴 가능성
- 로깅 포맷 **일관성 유지 어려움**
- 코드 리뷰 시 핵심 로직 파악 어려움

---

## 소스 생성기로 구현한 이유

### 1. 자동화된 일관성

소스 생성기는 **모든 메서드에 동일한 패턴**을 적용합니다. 개발자가 실수로 빠뜨리는 것이 불가능합니다.

```csharp
// 개발자가 작성하는 코드 - 핵심 로직에만 집중
[GeneratePipeline]
public class UserRepository(ILogger<UserRepository> logger) : IAdapter
{
    public FinT<IO, User> GetUserAsync(int userId) =>
        // 순수한 비즈니스 로직만 작성
        from user in _dbContext.Users.FindAsync(userId)
        select user;

    public FinT<IO, IEnumerable<User>> GetUsersAsync() =>
        from users in _dbContext.Users.ToListAsync()
        select users;
}

// 소스 생성기가 자동으로 생성하는 코드
public class UserRepositoryPipeline : UserRepository
{
    // 로깅, 추적, 메트릭이 모든 메서드에 자동 적용
}
```

### 2. 컴파일 타임 성능

런타임 AOP(Aspect-Oriented Programming)나 Interceptor와 달리, **컴파일 타임에 코드가 생성**되므로:

| 접근 방식 | 런타임 오버헤드 | AOT 지원 |
|-----------|----------------|----------|
| Castle DynamicProxy | 높음 | 제한적 |
| DispatchProxy | 중간 | 제한적 |
| 소스 생성기 | **없음** | **완벽 지원** |

### 3. 디버깅 용이성

생성된 코드는 일반 C# 코드이므로 **디버거로 스텝 인**하여 로깅 로직을 확인할 수 있습니다.

### 4. 고성능 로깅

`LoggerMessage.Define`을 사용한 **고성능 로깅 코드**를 자동 생성합니다:

```csharp
// 소스 생성기가 생성하는 고성능 로깅 코드
internal static class UserRepositoryPipelineLoggers
{
    private static readonly Action<ILogger, int, Exception?> s_getUserAsyncRequest =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(1001, "AdapterRequest"),
            "GetUserAsync Request: userId={userId}");

    public static void GetUserAsyncRequest(ILogger logger, int userId)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            s_getUserAsyncRequest(logger, userId, null);
        }
    }
}
```

### 5. 타입 안전성

파라미터 타입이 변경되면 **컴파일 오류**가 발생하여 즉시 알 수 있습니다.

---

## 핵심 설계 패턴

### 템플릿 메서드 패턴 (Template Method Pattern)

`IncrementalGeneratorBase`는 **템플릿 메서드 패턴**을 적용하여 소스 생성기의 공통 흐름을 정의합니다:

```csharp
// 템플릿 메서드 패턴 - 공통 흐름 정의
public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext,
         IncrementalValuesProvider<TValue>> registerSourceProvider,  // 1단계: 소스 제공자 등록
    Action<SourceProductionContext, ImmutableArray<TValue>> generate, // 2단계: 코드 생성
    bool AttachDebugger = false) : IIncrementalGenerator
{
    // 템플릿 메서드 - 고정된 알고리즘 흐름
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        if (AttachDebugger)
            Debugger.Launch();

        // 1단계: 소스 코드에서 관심 대상 추출
        var provider = registerSourceProvider(context);

        // 2단계: 추출된 정보로 코드 생성
        context.RegisterSourceOutput(provider.Collect(), generate);
    }
}
```

```
템플릿 메서드 패턴 구조
=====================

IncrementalGeneratorBase (추상 클래스)
│
├── Initialize()           # 템플릿 메서드 (고정)
│   ├── registerSourceProvider()  # 추상 단계 1
│   └── generate()                # 추상 단계 2
│
└── AdapterPipelineGenerator (구체 클래스)
    ├── RegisterSourceProvider()  # 구현: [GeneratePipeline] 클래스 필터링
    └── Generate()                # 구현: Pipeline 코드 생성
```

**장점:**
- 소스 생성기의 **공통 구조 재사용**
- 새로운 생성기 추가 시 핵심 로직만 구현
- 디버깅 플래그 등 **공통 기능 중앙 관리**

### 전략 패턴 (Strategy Pattern) with IAdapter

`IAdapter` 인터페이스를 통해 **전략 패턴**을 구현합니다. 각 어댑터는 특정 외부 시스템과의 통신 전략을 캡슐화합니다:

```csharp
// IAdapter 인터페이스 - 전략의 공통 계약
public interface IAdapter
{
    // 마커 인터페이스로 사용
    // 실제 메서드는 각 도메인별 인터페이스에서 정의
}

// 구체적인 전략 정의 - 사용자 저장소
public interface IUserRepository : IAdapter
{
    FinT<IO, User> GetUserAsync(int id);
    FinT<IO, IEnumerable<User>> GetUsersAsync();
}

// 구체적인 전략 정의 - 주문 저장소
public interface IOrderRepository : IAdapter
{
    FinT<IO, Order> GetOrderAsync(int id);
    FinT<IO, Unit> CreateOrderAsync(Order order);
}
```

```
전략 패턴 구조
=============

IAdapter (전략 인터페이스)
│
├── IUserRepository        # 사용자 관련 전략
│   └── UserRepository     # 구체적 구현
│       └── UserRepositoryPipeline  ← 소스 생성기가 생성
│
├── IOrderRepository       # 주문 관련 전략
│   └── OrderRepository    # 구체적 구현
│       └── OrderRepositoryPipeline ← 소스 생성기가 생성
│
└── IProductRepository     # 상품 관련 전략
    └── ProductRepository  # 구체적 구현
        └── ProductRepositoryPipeline ← 소스 생성기가 생성
```

**소스 생성기의 역할:**

```csharp
// 개발자가 작성 - 전략 구현
[GeneratePipeline]
public class UserRepository(ILogger<UserRepository> logger) : IUserRepository
{
    public FinT<IO, User> GetUserAsync(int id) =>
        // 순수한 데이터 접근 로직
        from user in _dbContext.Users.FindAsync(id)
        select user;
}

// 소스 생성기가 자동 생성 - 전략 데코레이터
public class UserRepositoryPipeline : UserRepository
{
    // 원본 전략을 상속받아 관찰 가능성 기능 추가
    // 로깅, 추적, 메트릭이 자동으로 적용됨
}
```

**장점:**
- 각 어댑터(전략)의 **비즈니스 로직 격리**
- 관찰 가능성 코드의 **일관된 자동 적용**
- DI 컨테이너에서 **Pipeline 클래스로 교체** 용이

```csharp
// DI 등록 시 Pipeline 클래스 사용
services.AddScoped<IUserRepository, UserRepositoryPipeline>();
services.AddScoped<IOrderRepository, OrderRepositoryPipeline>();
```

---

## 기대 효과

### Before (수동 구현)

```
코드량
======
UserRepository.cs        : 200줄 (로깅 코드 포함)
OrderRepository.cs       : 180줄 (로깅 코드 포함)
ProductRepository.cs     : 220줄 (로깅 코드 포함)
-----------------------------------------
총합                      : 600줄

문제점
======
- 비즈니스 로직과 횡단 관심사 혼재
- 일관성 유지 어려움
- 코드 리뷰 복잡도 증가
```

### After (소스 생성기)

```
코드량
======
UserRepository.cs        : 50줄 (순수 비즈니스 로직)
OrderRepository.cs       : 40줄 (순수 비즈니스 로직)
ProductRepository.cs     : 60줄 (순수 비즈니스 로직)
-----------------------------------------
총합                      : 150줄 (75% 감소)

+ 자동 생성되는 Pipeline 클래스
  UserRepositoryPipeline.g.cs    : 자동 생성
  OrderRepositoryPipeline.g.cs   : 자동 생성
  ProductRepositoryPipeline.g.cs : 자동 생성

장점
====
- 비즈니스 로직만 집중
- 100% 일관된 Observability
- 코드 리뷰 효율성 향상
```

---

## 프로젝트 구조

```
Functorium/
├── Src/
│   ├── Functorium.Adapters.SourceGenerator/            # 소스 생성기
│   │   ├── AdapterPipelineGenerator.cs                 # 메인 생성기
│   │   └── Generators/
│   │       ├── IncrementalGeneratorBase.cs             # 기본 패턴
│   │       └── AdapterPipelineGenerator/
│   │           ├── PipelineClassInfo.cs                # 클래스 정보 모델
│   │           ├── MethodInfo.cs                       # 메서드 정보 모델
│   │           ├── ParameterInfo.cs                    # 파라미터 정보 모델
│   │           ├── TypeExtractor.cs                    # 타입 추출 유틸리티
│   │           ├── CollectionTypeHelper.cs             # 컬렉션 타입 처리
│   │           ├── SymbolDisplayFormats.cs             # 타입 포맷팅
│   │           ├── ConstructorParameterExtractor.cs    # 생성자 분석
│   │           └── ParameterNameResolver.cs            # 이름 충돌 해결
│   │
│   ├── Functorium/                                     # 핵심 라이브러리
│   │   └── Adapters/
│   │       └── Observabilities/
│   │           └── ObservabilityFields.cs              # 관측 가능성 상수
│   │
│   └── Functorium.Testing/                             # 테스트 유틸리티
│       └── SourceGenerators/
│           └── SourceGeneratorTestRunner.cs            # 테스트 러너
│
└── Tests/
    └── Functorium.Tests.Unit/
        └── AdaptersTests/
            └── SourceGenerators/
                ├── AdapterPipelineGeneratorTests.cs    # 27개 테스트
                └── *.verified.txt                      # 스냅샷 파일
```

---

## 핵심 컴포넌트

### 1. AdapterPipelineGenerator

메인 소스 생성기 클래스입니다. 3단계 파이프라인으로 동작합니다:

```
1단계: 속성 정의 생성
=====================
[GeneratePipeline] 속성을 자동으로 정의하여
개발자가 별도로 선언할 필요 없음

2단계: 대상 클래스 필터링
========================
[GeneratePipeline] 속성이 붙고
IAdapter를 구현한 클래스만 선택

3단계: Pipeline 클래스 생성
=========================
각 메서드에 대해 로깅, 추적, 메트릭 코드를
포함한 래퍼 메서드 생성
```

### 2. IncrementalGeneratorBase

증분 소스 생성기의 **템플릿 패턴**을 제공합니다:

```csharp
public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext,
         IncrementalValuesProvider<TValue>> registerSourceProvider,
    Action<SourceProductionContext, ImmutableArray<TValue>> generate,
    bool AttachDebugger = false) : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        if (AttachDebugger)
            Debugger.Launch();

        var provider = registerSourceProvider(context);
        context.RegisterSourceOutput(provider.Collect(), generate);
    }
}
```

### 3. 헬퍼 클래스들

| 클래스 | 역할 |
|--------|------|
| `TypeExtractor` | `FinT<IO, User>` → `User` 타입 추출 |
| `CollectionTypeHelper` | `List<T>`, `IEnumerable<T>` 등 컬렉션 감지 |
| `SymbolDisplayFormats` | 결정적 타입 문자열 생성 |
| `ConstructorParameterExtractor` | 생성자 파라미터 분석 |
| `ParameterNameResolver` | `logger` → `baseLogger` 이름 충돌 해결 |

---

## 학습 로드맵

```
Chapter 01-02: 기초
==================
- 소스 생성기 개념 이해
- 개발 환경 설정
- Hello World 생성기 만들기

Chapter 03-04: 핵심 API
======================
- Roslyn 기초 (Syntax API, Semantic API)
- IIncrementalGenerator 패턴
- ForAttributeWithMetadataName 사용법

Chapter 05-06: 심볼 분석 & 코드 생성
===================================
- INamedTypeSymbol, IMethodSymbol 활용
- SymbolDisplayFormat으로 결정적 출력
- StringBuilder로 코드 생성

Chapter 07: 고급 시나리오
========================
- 제네릭 타입 처리 (FinT<IO, T>)
- 컬렉션 타입 감지
- LoggerMessage.Define 6개 파라미터 제한

Chapter 08: 테스트 전략
======================
- SourceGeneratorTestRunner 활용
- Verify 스냅샷 테스트
- 27개 테스트 시나리오 작성
```

---

## 요약

| 항목 | 설명 |
|------|------|
| 프로젝트 이름 | AdapterPipelineGenerator |
| 목적 | 어댑터에 Observability 자동 추가 |
| 해결 문제 | 반복적인 로깅/추적/메트릭 코드 제거 |
| 구현 이유 | 일관성, 성능, 타입 안전성, AOT 지원 |
| 기대 효과 | 코드량 75% 감소, 100% 일관된 관측 가능성 |

---

## 다음 단계

다음 장에서는 소스 생성기 개발을 위한 환경을 설정합니다.

➡️ [02장. 사전 준비](../02-prerequisites/)
