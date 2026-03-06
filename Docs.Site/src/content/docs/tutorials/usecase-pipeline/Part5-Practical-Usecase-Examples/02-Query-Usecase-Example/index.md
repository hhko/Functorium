---
title: "Query Usecase 예제"
---

## 개요

이 장에서는 Functorium의 `IQueryRequest<TSuccess>` 인터페이스를 활용하여 **Query Usecase의 완전한 구현 예제**를 작성합니다. Query는 Command와 달리 **데이터를 읽기만** 하므로 Transaction Pipeline이 적용되지 않으며, `ICacheable`을 구현하여 캐싱 최적화를 적용할 수 있습니다.

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

Command와 Query를 인터페이스로 구분하면, Pipeline이 **타입 수준에서** 동작을 분기할 수 있습니다:
- `ICommandRequest` → Transaction Pipeline 적용
- `IQueryRequest` → Transaction Pipeline 미적용, Caching Pipeline 적용 가능

### 2. Command vs Query 차이점

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

