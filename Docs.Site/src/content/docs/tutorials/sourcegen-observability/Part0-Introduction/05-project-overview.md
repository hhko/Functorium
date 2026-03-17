---
title: "프로젝트 개요"
---

## 개요

앞의 두 장에서 소스 생성기의 개념과 선택 이유를 다루었습니다. 이제 이론을 실제 프로젝트에 연결할 차례입니다. 이 장에서는 튜토리얼 전체에 걸쳐 구현할 **ObservablePortGenerator**의 설계 목표, 해결하려는 문제, 그리고 프로젝트 구조를 소개합니다. 여기서 그리는 전체 그림이 이후 각 장의 학습 맥락이 됩니다.

## 학습 목표

### 핵심 학습 목표
1. **ObservablePortGenerator의 목적과 기대 효과 이해**
   - 어댑터 계층의 횡단 관심사 문제와 자동화 필요성 파악
2. **소스 생성기로 구현한 이유 파악**
   - 런타임 AOP 대비 컴파일 타임 생성의 구체적 이점
3. **전체 프로젝트 구조 파악**
   - 소스 생성기, 핵심 라이브러리, 테스트 프로젝트의 관계

---

## ObservablePortGenerator란?

**ObservablePortGenerator**는 어댑터 클래스에 **Observability(관측 가능성)** 기능을 자동으로 추가하는 소스 생성기입니다.

### 해결하려는 문제

어댑터 계층(데이터베이스, 외부 API 호출 등)에서는 다음과 같은 **횡단 관심사(Cross-Cutting Concerns)를** 처리해야 합니다:

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
public class UserRepository : IObservablePort
{
    private readonly ILogger<UserRepository> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _durationHistogram;

    public FinT<IO, User> GetUserAsync(int userId)
    {
        // 시작 시간 기록
        var startTimestamp = Stopwatch.GetTimestamp();

        // 요청 로깅
        _logger.LogInformation("GetUserAsync 요청: userId={UserId}", userId);

        // 추적 Activity 생성
        using var activity = _activitySource.StartActivity("GetUserAsync");

        try
        {
            // 실제 비즈니스 로직
            var result = await _dbContext.Users.FindAsync(userId);

            // 성공 로깅
            var elapsed = CalculateElapsed(startTimestamp);
            _logger.LogInformation("GetUserAsync 성공: {Elapsed}ms", elapsed);

            // 메트릭 기록
            _requestCounter.Add(1);

            return result;
        }
        catch (Exception ex)
        {
            // 실패 로깅
            var elapsed = CalculateElapsed(startTimestamp);
            _logger.LogError(ex, "GetUserAsync 실패: {Elapsed}ms", elapsed);

            // 메트릭 기록
            _durationHistogram.Record(elapsed);

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

이 문제를 해결하기 위해 런타임 AOP나 Interceptor 같은 기존 기법을 사용할 수도 있습니다. 하지만 ObservablePortGenerator는 소스 생성기를 선택했습니다. 그 이유를 구체적으로 살펴보겠습니다.

---

## 소스 생성기로 구현한 이유

### 1. 자동화된 일관성

소스 생성기는 **모든 메서드에 동일한 패턴**을 적용합니다. 개발자가 실수로 빠뜨리는 것이 불가능합니다.

```csharp
// 개발자가 작성하는 코드 - 핵심 로직에만 집중
[GenerateObservablePort]
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
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
public class UserRepositoryObservable : UserRepository
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
internal static class UserRepositoryObservableLoggers
{
    private static readonly Action<ILogger, string, string, string, string, int, Exception?> _logAdapterRequestDebug_UserRepository_GetUserAsync =
        LoggerMessage.Define<string, string, string, string, int>(
            LogLevel.Debug,
            ObservabilityNaming.EventIds.Adapter.AdapterRequest,
            "{request.layer} {request.category} {request.handler}.{request.handler.method} requesting with {request.params.userid}");

    public static void LogAdapterRequestDebug_UserRepository_GetUserAsync(
        this ILogger logger, string requestLayer, string requestCategory, string requestHandler, string requestHandlerMethod, int userId)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
            return;

        _logAdapterRequestDebug_UserRepository_GetUserAsync(logger, requestLayer, requestCategory, requestHandler, requestHandlerMethod, userId, null);
    }
}
```

### 5. 타입 안전성

파라미터 타입이 변경되면 **컴파일 오류**가 발생하여 즉시 알 수 있습니다.

소스 생성기를 선택한 이유를 확인했으니, 이제 ObservablePortGenerator가 어떤 설계 패턴 위에 구축되었는지 살펴보겠습니다.

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
    //Action<IncrementalGeneratorPostInitializationContext>? registerPostInitializationSourceOutput = null,
    bool AttachDebugger = false) : IIncrementalGenerator
{
    protected const string ClassEntityName = "class";

    private readonly bool _attachDebugger = AttachDebugger;
    private readonly Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> _registerSourceProvider = registerSourceProvider;
    private readonly Action<SourceProductionContext, ImmutableArray<TValue>> _generate = generate;

    // 템플릿 메서드 - 고정된 알고리즘 흐름
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (_attachDebugger && Debugger.IsAttached is false)
        {
            Debugger.Launch();
        }
#endif

        // 1단계: 소스 코드에서 관심 대상 추출
        IncrementalValuesProvider<TValue> provider = _registerSourceProvider(context)
            .Where(static m => m is not null);

        // 2단계: 추출된 정보로 코드 생성
        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<TValue> displayValues)
    {
        _generate(context, displayValues);
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
│   ├── .Where(not null)          # null 필터링
│   └── Execute() → generate()   # 추상 단계 2
│
└── ObservablePortGenerator (구체 클래스)
    ├── RegisterSourceProvider()  # 구현: [GenerateObservablePort] 클래스 필터링
    └── Generate()                # 구현: Observable 코드 생성
```

**장점:**
- 소스 생성기의 **공통 구조 재사용**
- 새로운 생성기 추가 시 핵심 로직만 구현
- 디버깅 플래그 등 **공통 기능 중앙 관리**

### 전략 패턴 (Strategy Pattern) with IObservablePort

`IObservablePort` 인터페이스를 통해 **전략 패턴**을 구현합니다. 각 어댑터는 특정 외부 시스템과의 통신 전략을 캡슐화합니다:

```csharp
// IObservablePort 인터페이스 - 전략의 공통 계약
public interface IObservablePort
{
    string RequestCategory { get; }
}

// 구체적인 전략 정의 - 사용자 저장소
public interface IUserRepository : IObservablePort
{
    FinT<IO, User> GetUserAsync(int id);
    FinT<IO, IEnumerable<User>> GetUsersAsync();
}

// 구체적인 전략 정의 - 주문 저장소
public interface IOrderRepository : IObservablePort
{
    FinT<IO, Order> GetOrderAsync(int id);
    FinT<IO, Unit> CreateOrderAsync(Order order);
}
```

```
전략 패턴 구조
=============

IObservablePort (전략 인터페이스)
│
├── IUserRepository        # 사용자 관련 전략
│   └── UserRepository     # 구체적 구현
│       └── UserRepositoryObservable  ← 소스 생성기가 생성
│
├── IOrderRepository       # 주문 관련 전략
│   └── OrderRepository    # 구체적 구현
│       └── OrderRepositoryObservable ← 소스 생성기가 생성
│
└── IProductRepository     # 상품 관련 전략
    └── ProductRepository  # 구체적 구현
        └── ProductRepositoryObservable ← 소스 생성기가 생성
```

**소스 생성기의 역할:**

```csharp
// 개발자가 작성 - 전략 구현
[GenerateObservablePort]
public class UserRepository(ILogger<UserRepository> logger) : IUserRepository
{
    public FinT<IO, User> GetUserAsync(int id) =>
        // 순수한 데이터 접근 로직
        from user in _dbContext.Users.FindAsync(id)
        select user;
}

// 소스 생성기가 자동 생성 - 전략 데코레이터
public class UserRepositoryObservable : UserRepository
{
    // 원본 전략을 상속받아 관찰 가능성 기능 추가
    // 로깅, 추적, 메트릭이 자동으로 적용됨
}
```

**장점:**
- 각 어댑터(전략)의 **비즈니스 로직 격리**
- 관찰 가능성 코드의 **일관된 자동 적용**
- DI 컨테이너에서 **Observable 클래스로 교체** 용이

```csharp
// DI 등록 시 Observable 클래스 사용
services.AddScoped<IUserRepository, UserRepositoryObservable>();
services.AddScoped<IOrderRepository, OrderRepositoryObservable>();
```

템플릿 메서드 패턴과 전략 패턴의 조합으로, 소스 생성기의 공통 흐름을 재사용하면서 각 어댑터의 Observability 코드를 일관되게 생성합니다. 이 설계가 실제로 어떤 효과를 가져오는지 수치로 확인해 보겠습니다.

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

+ 자동 생성되는 Observable 클래스
  UserRepositoryObservable.g.cs    : 자동 생성
  OrderRepositoryObservable.g.cs   : 자동 생성
  ProductRepositoryObservable.g.cs : 자동 생성

장점
====
- 비즈니스 로직만 집중
- 100% 일관된 Observability
- 코드 리뷰 효율성 향상
```

이러한 효과를 달성하는 프로젝트가 실제로 어떤 디렉터리 구조로 구성되어 있는지 살펴보겠습니다.

---

## 프로젝트 구조

```
Functorium/
├── Src/
│   ├── Functorium.SourceGenerators/            # 소스 생성기
│   │   ├── Abstractions/
│   │   │   ├── Constants.cs                           # 공통 상수 (헤더 등)
│   │   │   └── Selectors.cs                           # 공통 선택자
│   │   │
│   │   └── Generators/
│   │       ├── IncrementalGeneratorBase.cs             # 템플릿 메서드 패턴 기반 클래스
│   │       │
│   │       ├── ObservablePortGenerator/               # Observability 코드 생성기
│   │       │   ├── ObservablePortGenerator.cs          # 메인 소스 생성기
│   │       │   ├── ObservableGeneratorConstants.cs     # 생성기 전용 상수
│   │       │   ├── ObservableClassInfo.cs              # 클래스 정보 레코드
│   │       │   ├── MethodInfo.cs                       # 메서드 정보
│   │       │   ├── ParameterInfo.cs                    # 파라미터 정보
│   │       │   ├── TypeExtractor.cs                    # 타입 추출 유틸리티
│   │       │   ├── CollectionTypeHelper.cs             # 컬렉션 타입 판별
│   │       │   ├── SymbolDisplayFormats.cs             # 타입 문자열 포맷
│   │       │   ├── ConstructorParameterExtractor.cs    # 생성자 분석
│   │       │   └── ParameterNameResolver.cs            # 이름 충돌 해결
│   │       │
│   │       ├── EntityIdGenerator/                     # Entity ID 자동 생성기
│   │       │   ├── EntityIdGenerator.cs                # Ulid 기반 ID 구조체 생성
│   │       │   └── EntityIdInfo.cs                     # Entity 정보 레코드
│   │       │
│   │       └── UnionTypeGenerator/                    # Union Type 생성기
│   │           ├── UnionTypeGenerator.cs               # Match/Switch 메서드 생성
│   │           └── UnionTypeInfo.cs                    # Union 정보 레코드
│   │
│   ├── Functorium/                                    # 핵심 도메인 라이브러리
│   │   └── Domains/
│   │       └── Observabilities/
│   │           └── IObservablePort.cs                  # 관측 가능성 마커 인터페이스
│   │
│   ├── Functorium.Adapters/                           # 어댑터 라이브러리
│   │   ├── SourceGenerators/
│   │   │   └── GenerateObservablePortAttribute.cs     # [GenerateObservablePort] 속성
│   │   └── Observabilities/
│   │       └── Naming/
│   │           ├── ObservabilityNaming.cs              # 관측 가능성 네이밍 규칙
│   │           ├── ObservabilityNaming.Events.cs       # 이벤트 ID 정의
│   │           └── ObservabilityNaming.Attributes.cs   # 속성 키 정의
│   │
│   └── Functorium.Testing/                            # 테스트 유틸리티
│       └── Actions/
│           └── SourceGenerators/
│               └── SourceGeneratorTestRunner.cs        # 테스트 러너
│
└── Tests/
    └── Functorium.Tests.Unit/
        └── AdaptersTests/
            └── SourceGenerators/
                ├── ObservablePortGeneratorTests.cs     # 31개 테스트
                └── *.verified.txt                      # 스냅샷 파일
```

---

## 핵심 컴포넌트

### 1. ObservablePortGenerator

메인 소스 생성기 클래스입니다. 2단계 파이프라인으로 동작합니다:

```
1단계: 대상 클래스 필터링
========================
Functorium 라이브러리에 미리 정의된
[GenerateObservablePort] 속성이 붙고
IObservablePort를 구현한 클래스만 선택

2단계: Observable 클래스 생성
===========================
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
    //Action<IncrementalGeneratorPostInitializationContext>? registerPostInitializationSourceOutput = null,
    bool AttachDebugger = false) : IIncrementalGenerator
{
    protected const string ClassEntityName = "class";

    private readonly bool _attachDebugger = AttachDebugger;
    private readonly Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> _registerSourceProvider = registerSourceProvider;
    private readonly Action<SourceProductionContext, ImmutableArray<TValue>> _generate = generate;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (_attachDebugger && Debugger.IsAttached is false)
        {
            Debugger.Launch();
        }
#endif

        IncrementalValuesProvider<TValue> provider = _registerSourceProvider(context)
            .Where(static m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<TValue> displayValues)
    {
        _generate(context, displayValues);
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
- 31개 테스트 시나리오 작성
```

---

## 요약

**ObservablePortGenerator**는 어댑터 계층에 반복되는 로깅, 추적, 메트릭 코드를 자동으로 생성하여 제거합니다. 소스 생성기를 선택한 이유는 일관성, 성능, 타입 안전성, AOT 지원이라는 네 가지 요구사항을 동시에 충족하기 때문입니다. 템플릿 메서드 패턴과 전략 패턴을 결합한 설계로, 개발자가 작성하는 코드량을 약 75% 줄이면서 100% 일관된 관측 가능성을 보장합니다.

---

## FAQ

### Q1: `IncrementalGeneratorBase<TValue>`를 사용하면 어떤 이점이 있나요?
**A**: 디버거 연결, null 필터링, `Collect()`를 통한 배치 처리 같은 공통 로직을 한 곳에서 관리합니다. 새로운 소스 생성기를 추가할 때 `registerSourceProvider`와 `generate` 두 함수만 구현하면 되므로, 파이프라인 구성 코드의 중복을 제거할 수 있습니다.

### Q2: `IObservablePort` 인터페이스가 전략 패턴에서 수행하는 역할은 무엇인가요?
**A**: `IObservablePort`는 소스 생성기가 코드를 생성할 대상을 식별하는 마커 역할을 합니다. 이 인터페이스를 구현한 클래스만 `[GenerateObservablePort]` 속성의 대상이 되며, 각 어댑터는 `RequestCategory` 프로퍼티로 자신의 관측 가능성 카테고리를 정의합니다.

### Q3: 소스 생성기 도입 전후로 코드량이 75% 감소한다는 수치의 근거는 무엇인가요?
**A**: 수동 구현 시 각 메서드에 로깅, 추적, 메트릭을 위한 30-50줄의 보일러플레이트가 추가됩니다. 소스 생성기 도입 후에는 순수 비즈니스 로직만 남고 횡단 관심사 코드가 전부 자동 생성되므로, 실제 프로젝트 측정치 기반으로 약 75%의 코드 감소 효과를 보입니다.

### Q4: `TypeExtractor`, `CollectionTypeHelper` 같은 헬퍼 클래스들은 왜 별도로 분리되어 있나요?
**A**: 각 헬퍼는 독립적인 책임(타입 추출, 컬렉션 감지, 이름 충돌 해결 등)을 담당합니다. 단일 책임 원칙에 따라 분리하면 개별 로직을 독립적으로 테스트할 수 있고, 다른 소스 생성기에서도 재사용할 수 있습니다.

---

## 다음 단계

프로젝트의 전체 그림을 파악했으니, 이제 실제로 소스 생성기를 개발하기 위한 환경을 설정할 차례입니다.

→ [Part 1의 1장. 개발 환경](../../Part1-Fundamentals/01-development-environment.md)
