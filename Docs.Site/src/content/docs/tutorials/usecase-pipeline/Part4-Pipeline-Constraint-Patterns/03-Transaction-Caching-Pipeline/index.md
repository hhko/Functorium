---
title: "Transaction/Caching"
---

## 개요

앞 장에서 Read+Create 이중 제약을 적용했습니다. 이번에는 동일한 이중 제약을 사용하면서 요청 타입에 따라 조건부로 실행되는 Pipeline을 다룹니다. Transaction Pipeline은 **Command에만** 적용되고, Caching Pipeline은 **Query에만** 적용됩니다. 두 Pipeline 모두 응답의 성공/실패 상태를 읽어야 하므로 Read+Create 제약을 사용하며, 추가로 요청 타입에 따라 **조건부 실행**이 필요합니다.

```
Transaction Pipeline:
  isCommand? ──No──→ Skip (Query는 트랜잭션 불필요)
              │
              Yes──→ Begin → handler() → IsSucc? → Commit / Rollback

Caching Pipeline:
  isCacheable? ──No──→ handler() 직접 실행
                 │
                 Yes──→ cache hit? → 캐시 반환
                                 │
                                 No → handler() → IsSucc? → 캐시 저장
```

## 핵심 개념

### 1. Transaction Pipeline

Transaction Pipeline은 Command 요청에만 트랜잭션을 적용합니다:

```csharp
public sealed class SimpleTransactionPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public TResponse Execute(bool isCommand, Func<TResponse> handler)
    {
        if (!isCommand)
        {
            // Query는 트랜잭션 불필요
            return handler();
        }

        // Command: Begin → Execute → Commit/Rollback
        var response = handler();

        if (response.IsSucc)    // Read: IFinResponse
            Commit();
        else
            Rollback();

        return response;
    }
}
```

실제 Mediator Pipeline에서는 `TRequest is ICommandRequest` 같은 타입 검사로 Command/Query를 분기합니다.

### 2. Caching Pipeline

Caching Pipeline은 Query 요청 중 `ICacheable`을 구현한 요청에만 캐싱을 적용합니다:

```csharp
public sealed class SimpleCachingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public TResponse GetOrExecute(string cacheKey, bool isCacheable, Func<TResponse> handler)
    {
        if (!isCacheable)
            return handler();

        if (TryGetFromCache(cacheKey, out var cached))
            return cached;

        var response = handler();

        if (response.IsSucc)    // Read: 성공 응답만 캐싱
            SetCache(cacheKey, response);

        return response;
    }
}
```

### 3. 왜 Read+Create 제약인가?

두 Pipeline이 Read와 Create 능력을 각각 어떻게 사용하는지 정리하면 다음과 같습니다.

| Pipeline | Read (IsSucc/IsFail) | Create (CreateFail) |
|----------|:--------------------:|:-------------------:|
| Transaction | Commit/Rollback 결정 | 예외 시 실패 응답 생성 |
| Caching | 성공 응답만 캐싱 | 예외 시 실패 응답 생성 |

두 Pipeline 모두 응답 상태를 **읽어야** 하므로 `IFinResponse`가 필요하고, 예외 처리를 위해 `IFinResponseFactory<TResponse>`도 필요합니다.

### 4. Command/Query 분기

실제 Pipeline에서는 요청 타입으로 분기합니다:

```csharp
// Transaction Pipeline: Command에만 적용
if (request is ICommandRequest)
{
    BeginTransaction();
    // ...
}

// Caching Pipeline: ICacheable Query에만 적용
if (request is ICacheable cacheable)
{
    var cacheKey = cacheable.CacheKey;
    // ...
}
```

## FAQ

### Q1: Transaction Pipeline이 Query를 건너뛰는 것은 어떻게 구현되나요?
**A**: 실제 Mediator Pipeline에서는 `request is ICommandRequest` 타입 검사로 Command/Query를 분기합니다. `ICommandRequest`가 아닌 요청은 트랜잭션 시작/커밋/롤백 없이 `next()`를 직접 호출하여 통과시킵니다.

### Q2: Caching Pipeline이 실패 응답을 캐싱하지 않는 이유는 무엇인가요?
**A**: 실패 응답은 일시적 오류(네트워크 타임아웃, 일시적 DB 장애 등)인 경우가 많습니다. 실패를 캐싱하면 재시도 시에도 캐시된 실패가 반환되어 **복구 불가능한 상태**가 됩니다. 따라서 `response.IsSucc`으로 성공 응답만 캐싱합니다.

### Q3: Transaction과 Caching이 같은 이중 제약을 사용하지만 적용 대상이 다른 이유는 무엇인가요?
**A**: 두 Pipeline 모두 응답의 성공/실패를 읽는 능력(Read)과 예외 시 실패 응답을 생성하는 능력(Create)이 필요하므로 **제약 조건은 동일**합니다. 하지만 Transaction은 데이터 변경이 있는 **Command에만**, Caching은 읽기 전용인 **Query에만** 적용되는 것이 비즈니스 요구사항입니다.

### Q4: `ICacheable` 인터페이스를 구현하지 않은 Query는 어떻게 되나요?
**A**: Caching Pipeline은 `request is ICacheable`로 캐싱 가능 여부를 확인합니다. `ICacheable`을 구현하지 않은 Query는 캐싱을 건너뛰고 매번 Handler를 실행합니다. 모든 Query에 캐싱을 강제하지 않아 **선택적 최적화**가 가능합니다.

Pipeline별 타입 제약 패턴을 모두 확인했습니다. 다음 장에서는 Repository 계층의 `Fin<T>`와 Usecase 계층의 `FinResponse<T>`를 연결하는 브릿지 패턴을 살펴봅니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Transaction Pipeline이 Command에만 적용되는 이유를 설명할 수 있다
2. Caching Pipeline이 성공 응답만 캐싱하는 이유를 설명할 수 있다
3. 두 Pipeline 모두 Read+Create 제약이 필요한 이유를 이해할 수 있다
4. Command/Query 분기가 요청 타입 검사로 이루어지는 방식을 이해할 수 있다

## 프로젝트 구조

```
03-Transaction-Caching-Pipeline/
├── TransactionCachingPipeline/
│   ├── TransactionCachingPipeline.csproj
│   ├── SimpleTransactionPipeline.cs
│   └── Program.cs
├── TransactionCachingPipeline.Tests.Unit/
│   ├── TransactionCachingPipeline.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── TransactionCachingPipelineTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project TransactionCachingPipeline

# 테스트 실행
dotnet test --project TransactionCachingPipeline.Tests.Unit
```

