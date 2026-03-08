---
title: "Query Usecase 예제"
---

## 개요

앞 장에서 Command Usecase를 구현했습니다. 이번에는 Query Usecase를 다룹니다. 이 장에서는 Functorium의 `IQueryRequest<TSuccess>` 인터페이스를 활용하여 **Query Usecase의 완전한 구현 예제**를 작성합니다. Query는 Command와 달리 **데이터를 읽기만** 하므로 Transaction Pipeline이 적용되지 않으며, `ICacheable`을 구현하여 캐싱 최적화를 적용할 수 있습니다.

```
Query Usecase 구조:

GetProductQuery (최상위 클래스)
├── Request   : IQueryRequest<Response>   ← 읽기 전용 요청
├── Response                              ← 조회 결과
└── Handler                               ← 조회 로직
```

## 핵심 개념

### 1. IQueryRequest 인터페이스

`IQueryRequest<TSuccess>`는 Mediator의 `IQuery<FinResponse<TSuccess>>`를 상속합니다. Pipeline은 이 인터페이스를 통해 요청이 Query임을 인식합니다.

```csharp
// Functorium 정의
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>> { }
```

Command와 Query를 인터페이스로 구분하면, Pipeline의 `where` 제약 조건을 통해 **컴파일 타임에** 적용 대상이 결정됩니다:
- `ICommandRequest` → `ICommand<TResponse>`를 상속 → Transaction Pipeline(`where TRequest : ICommand<TResponse>`) 적용
- `IQueryRequest` → `IQuery<TResponse>`를 상속 → Caching Pipeline(`where TRequest : IQuery<TResponse>`) 적용 가능

### 2. Command vs Query 차이점

두 패턴의 핵심 차이를 정리하면 다음과 같습니다.

| 항목 | Command | Query |
|------|---------|-------|
| 인터페이스 | `ICommandRequest<T>` | `IQueryRequest<T>` |
| 데이터 변경 | O (생성/수정/삭제) | X (읽기만) |
| Transaction | 적용 | 미적용 |
| Caching | 일반적으로 미적용 | `ICacheable` 구현 시 적용 |
| 반환 타입 | `FinResponse<TSuccess>` | `FinResponse<TSuccess>` |

### 3. ICacheable을 통한 캐싱 최적화

Query Request가 `ICacheable`을 구현하면 Caching Pipeline이 자동으로 캐시를 적용합니다.

```csharp
// ICacheable 인터페이스 (Functorium 정의)
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Duration { get; }
}
```

```csharp
// ICacheable 적용 예시
public sealed record Request(string ProductId)
    : IQueryRequest<Response>, ICacheable
{
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan? Duration => TimeSpan.FromMinutes(5);
}
```

Caching Pipeline은 `request is ICacheable`로 조건부 캐싱을 수행하므로, ICacheable을 구현하지 않은 Query는 캐싱을 건너뜁니다.

### 4. 읽기 전용 Handler 패턴

Query Handler는 상태를 변경하지 않으므로, 의존성이 읽기 전용 저장소(Repository의 Query 부분)에만 의존합니다.

```csharp
public sealed class Handler
{
    private readonly Dictionary<string, Response> _products = new() { ... };

    public FinResponse<Response> Handle(Request request)
    {
        if (_products.TryGetValue(request.ProductId, out var product))
            return product;  // 암시적 변환

        return Error.New($"Product not found: {request.ProductId}");
    }
}
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `IQueryRequest<TSuccess>` 인터페이스의 역할과 Command와의 차이를 설명할 수 있다
2. `ICacheable` 인터페이스를 구현하여 Query에 캐싱 최적화를 적용할 수 있다
3. Query Handler가 읽기 전용으로 동작하는 패턴을 이해할 수 있다
4. Pipeline이 Command/Query를 타입 수준에서 구분하는 방식을 설명할 수 있다

## FAQ

### Q1: Query Usecase에 Validator가 없는 이유는 무엇인가요?
**A**: 이 예제에서는 간결성을 위해 Validator를 생략했습니다. 실제 프로젝트에서는 Query에도 Validator를 추가할 수 있습니다. 예를 들어 `ProductId`가 빈 문자열인지 검사하는 것은 유효한 검증입니다. Validator 추가 여부는 비즈니스 요구사항에 따라 결정합니다.

### Q2: `IQueryRequest`와 `ICommandRequest`를 분리하면 Pipeline에서 어떤 이점이 있나요?
**A**: Pipeline의 `where` 제약 조건을 통해 **컴파일 타임에** 적용 대상이 결정됩니다. Transaction Pipeline은 `where TRequest : ICommand<TResponse>` 제약으로 Command에만, Caching Pipeline은 `where TRequest : IQuery<TResponse>` 제약으로 Query에만 등록됩니다. Mediator 소스 제너레이터가 이 제약을 확인하여 해당 타입에만 Pipeline을 적용하므로, 런타임 타입 검사 없이 **인터페이스 제약만으로** 분기가 결정됩니다.

### Q3: `ICacheable`의 `Duration`이 `null`이면 어떻게 되나요?
**A**: `Duration`이 `null`이면 Caching Pipeline이 **기본 캐시 만료 시간**을 적용합니다. 이를 통해 대부분의 Query에는 기본값을 사용하고, 특정 Query에만 커스텀 만료 시간을 설정할 수 있습니다.

### Q4: Query Handler가 `Dictionary`를 사용하는 것은 실전에서도 동일한가요?
**A**: 아닙니다. 예제에서는 학습 목적으로 `Dictionary`를 인메모리 저장소로 사용했습니다. 실전에서는 Repository 인터페이스를 DI로 주입받아 데이터베이스에서 조회하며, Repository가 반환하는 `Fin<T>`를 `ToFinResponse()`로 변환하여 `FinResponse<T>`를 반환합니다.

Command와 Query Usecase를 각각 구현했으니, 다음 장에서는 7개 Pipeline을 모두 연결하여 전체 흐름을 통합합니다.

## 프로젝트 구조

```
02-Query-Usecase-Example/
├── QueryUsecaseExample/
│   ├── QueryUsecaseExample.csproj
│   ├── GetProductQuery.cs
│   └── Program.cs
├── QueryUsecaseExample.Tests.Unit/
│   ├── QueryUsecaseExample.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── GetProductQueryTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project QueryUsecaseExample

# 테스트 실행
dotnet test --project QueryUsecaseExample.Tests.Unit
```

