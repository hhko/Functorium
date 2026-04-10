---
title: "Usecase CQRS Specification"
---

This specification defines the public API for Functorium's CQRS request interfaces, `FinResponse<A>` discriminated union, FinT LINQ extensions, and caching/persistence contracts.

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `ICommandRequest<TSuccess>` | `Functorium.Applications.Usecases` | Command 요청 마커 인터페이스 |
| `ICommandUsecase<TCommand, TSuccess>` | `Functorium.Applications.Usecases` | Command Handler 인터페이스 |
| `IQueryRequest<TSuccess>` | `Functorium.Applications.Usecases` | Query 요청 마커 인터페이스 |
| `IQueryUsecase<TQuery, TSuccess>` | `Functorium.Applications.Usecases` | Query Handler 인터페이스 |
| `FinResponse<A>` | `Functorium.Applications.Usecases` | Succ/Fail 판별 공용체 (Match, Map, Bind, LINQ 지원) |
| `FinResponse` | `Functorium.Applications.Usecases` | 정적 팩토리 클래스 (`Succ`, `Fail`) |
| `IFinResponse` | `Functorium.Applications.Usecases` | 비제네릭 기본 인터페이스 (`IsSucc`/`IsFail`) |
| `IFinResponse<out A>` | `Functorium.Applications.Usecases` | 공변성 지원 제네릭 인터페이스 |
| `IFinResponseFactory<TSelf>` | `Functorium.Applications.Usecases` | CRTP 기반 Fail 생성 인터페이스 |
| `IFinResponseWithError` | `Functorium.Applications.Usecases` | Error 접근 인터페이스 (Pipeline용) |
| `FinToFinResponse` | `Functorium.Applications.Usecases` | `Fin<A>` → `FinResponse<A>` 변환 확장 메서드 |
| `FinTLinqExtensions` | `Functorium.Applications.Linq` | FinT 모나드 트랜스포머 LINQ 확장 메서드 |
| `ICacheable` | `Functorium.Applications.Usecases` | 캐싱 계약 인터페이스 |
| `IUnitOfWork` | `Functorium.Applications.Persistence` | 영속성 트랜잭션 계약 |
| `IUnitOfWorkTransaction` | `Functorium.Applications.Persistence` | 명시적 트랜잭션 스코프 |
| `CtxIgnoreAttribute` | `Functorium.Applications.Usecases` | CtxEnricher 자동 생성 제외 속성 |

### Core Rules

- **`FinResponse<A>`는** `IsSucc`/`IsFail`을 사용합니다 (`IsSuccess`가 아님)
- 성공 값 접근은 `ThrowIfFail()` 또는 `Match()`를 통해서만 가능합니다
- **정적 팩토리는** `FinResponse.Succ(value)`와 `FinResponse.Fail<T>(error)`입니다 (`FinResponse<T>.Succ()`가 아님)
- Command/Query 모두 `FinResponse<TSuccess>`를 반환 타입으로 사용합니다

Having reviewed the key types and core rules in the summary, the following sections detail each type's API.

---

## CQRS Request Interfaces

Functorium's CQRS interfaces are based on [Mediator](https://github.com/martinothamar/Mediator) the [Mediator](https://github.com/martinothamar/Mediator) library's `ICommand<T>`/`IQuery<T>`, fixing the return type to `FinResponse<TSuccess>` to enforce the Result pattern.

### Command 인터페이스

```csharp
// 요청 인터페이스 — record로 구현
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>>
{
}

// Handler 인터페이스 — Usecase 클래스로 구현
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess>
{
}
```

| Type Parameter | Constraint | Description |
|--------------|------|------|
| `TSuccess` | 없음 | 성공 시 반환할 데이터 타입 |
| `TCommand` | `ICommandRequest<TSuccess>` | Command 요청 타입 (반공변) |

### Query 인터페이스

```csharp
// 요청 인터페이스 — record로 구현
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>>
{
}

// Handler 인터페이스 — Usecase 클래스로 구현
public interface IQueryUsecase<in TQuery, TSuccess>
    : IQueryHandler<TQuery, FinResponse<TSuccess>>
    where TQuery : IQueryRequest<TSuccess>
{
}
```

| Type Parameter | Constraint | Description |
|--------------|------|------|
| `TSuccess` | 없음 | 성공 시 반환할 데이터 타입 |
| `TQuery` | `IQueryRequest<TSuccess>` | Query 요청 타입 (반공변) |

### Inheritance Hierarchy

```
Mediator.ICommand<FinResponse<TSuccess>>
  └─ ICommandRequest<TSuccess>

Mediator.ICommandHandler<TCommand, FinResponse<TSuccess>>
  └─ ICommandUsecase<TCommand, TSuccess>

Mediator.IQuery<FinResponse<TSuccess>>
  └─ IQueryRequest<TSuccess>

Mediator.IQueryHandler<TQuery, FinResponse<TSuccess>>
  └─ IQueryUsecase<TQuery, TSuccess>
```

---

## FinResponse\<A\> API

`FinResponse<A>`는 is a discriminated union representing success (`Succ`) and failure (`Fail`). `Match`, `Map`, `Bind`, LINQ 쿼리 표현식을 지원합니다.

### 타입 정의

```csharp
public abstract record FinResponse<A> : IFinResponse<A>, IFinResponseFactory<FinResponse<A>>
{
    public sealed record Succ(A Value) : FinResponse<A>;
    public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError;
}
```

### Nested Types

| 타입 | 프로퍼티 | `IsSucc` | `IsFail` | 설명 |
|------|---------|----------|----------|------|
| `FinResponse<A>.Succ` | `A Value` | `true` | `false` | 성공 케이스 |
| `FinResponse<A>.Fail` | `Error Error` | `false` | `true` | 실패 케이스 |

### Properties

| Property | Type | Description |
|---------|------|------|
| `IsSucc` | `bool` | Whether in success state |
| `IsFail` | `bool` | Whether in failure state |

### Methods

| Method | Signature | Description |
|--------|---------|------|
| `Match<B>` | `B Match<B>(Func<A, B> Succ, Func<Error, B> Fail)` | Calls function based on state (returns value) |
| `Match` | `void Match(Action<A> Succ, Action<Error> Fail)` | Calls action based on state |
| `Map<B>` | `FinResponse<B> Map<B>(Func<A, B> f)` | Transforms success value |
| `MapFail` | `FinResponse<A> MapFail(Func<Error, Error> f)` | Transforms failure value |
| `BiMap<B>` | `FinResponse<B> BiMap<B>(Func<A, B> Succ, Func<Error, Error> Fail)` | Transforms both success/failure simultaneously |
| `Bind<B>` | `FinResponse<B> Bind<B>(Func<A, FinResponse<B>> f)` | Monadic bind |
| `BiBind<B>` | `FinResponse<B> BiBind<B>(Func<A, FinResponse<B>> Succ, Func<Error, FinResponse<B>> Fail)` | Binds both success/failure simultaneously |
| `BindFail` | `FinResponse<A> BindFail(Func<Error, FinResponse<A>> Fail)` | Binds failure state |
| `IfFail` | `A IfFail(Func<Error, A> Fail)` | Alternative value function on failure |
| `IfFail` | `A IfFail(A alternative)` | Alternative value on failure |
| `IfFail` | `void IfFail(Action<Error> Fail)` | Executes action on failure |
| `IfSucc` | `void IfSucc(Action<A> Succ)` | Executes action on success |
| `ThrowIfFail` | `A ThrowIfFail()` | Throws on failure, returns value on success |
| `Select<B>` | `FinResponse<B> Select<B>(Func<A, B> f)` | LINQ `select` 지원 (`Map`과 동일) |
| `SelectMany<B,C>` | `FinResponse<C> SelectMany<B, C>(Func<A, FinResponse<B>> bind, Func<A, B, C> project)` | LINQ `from ... from ... select` 지원 |

### Static Factory Methods (`FinResponse` class)

```csharp
public static class FinResponse
{
    public static FinResponse<A> Succ<A>(A value);
    public static FinResponse<A> Succ<A>() where A : new();
    public static FinResponse<A> Fail<A>(Error error);
}
```

| Method | Description |
|--------|------|
| `Succ<A>(A value)` | Creates `FinResponse` from success value |
| `Succ<A>()` | `new A()`로 Creates default success `FinResponse` (`A : new()` 제약) |
| `Fail<A>(Error error)` | Creates failure `FinResponse` |

### IFinResponseFactory Static Methods

```csharp
// FinResponse<A>에 구현된 정적 팩토리 (Pipeline 내부 사용)
public static FinResponse<A> CreateFail(Error error);
```

> Pipeline에서 `TResponse.CreateFail(error)`를 호출할 때 사용됩니다. `static abstract` 메서드이므로 구체 타입에서만 호출 가능합니다.

### Implicit Conversion Operators

| Operator | Description |
|--------|------|
| `implicit operator FinResponse<A>(A value)` | Value to `Succ` auto-conversion |
| `implicit operator FinResponse<A>(Error error)` | `Error` → `Fail` 자동 변환 |
| `operator true` | `IsSucc`이면 `true` |
| `operator false` | `IsFail`이면 `true` |
| `operator \|` | Choice 연산자: 좌항이 `Succ`이면 좌항, 아니면 우항 반환 |

---

## IFinResponse Interface Hierarchy

is an interface hierarchy for handling `FinResponse<A>` in a type-safe manner in Pipeline and Observability.

### Hierarchy

```
IFinResponse                          비제네릭 (IsSucc/IsFail 접근)
  └─ IFinResponse<out A>             공변성 지원 제네릭

IFinResponseFactory<TSelf>            CRTP 기반 Fail 생성 (Pipeline용)

IFinResponseWithError                 Error 접근 (Logger/Trace Pipeline용)

FinResponse<A>
  ├─ implements IFinResponse<A>
  ├─ implements IFinResponseFactory<FinResponse<A>>
  └─ Fail : implements IFinResponseWithError
```

### IFinResponse

```csharp
public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}
```

is a non-generic interface for accessing `IsSucc`/`IsFail` properties without generic types in Pipeline.

### IFinResponse\<out A\>

```csharp
public interface IFinResponse<out A> : IFinResponse
{
}
```

Supports covariance (`out`) for read-only use in pipelines.

### IFinResponseFactory\<TSelf\>

```csharp
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}
```

Uses CRTP (Curiously Recurring Template Pattern) to support type-safe `Fail` creation. Pipeline의 `UsecaseValidationPipeline`과 `UsecaseExceptionPipeline`에서 `TResponse.CreateFail(error)`를 호출합니다.

### IFinResponseWithError

```csharp
public interface IFinResponseWithError
{
    Error Error { get; }
}
```

is an interface for accessing `Error` information on failure. Logger Pipeline과 Trace Pipeline에서 사용됩니다. `FinResponse<A>.Fail`만 이 인터페이스를 구현합니다.

---

## Fin to FinResponse Conversion

The `FinToFinResponse` extension method class is used for cross-layer conversion from Repository (`Fin<A>`) to Usecase (`FinResponse<A>`).

### Extension Methods

| Method | Signature | Description |
|--------|---------|------|
| `ToFinResponse<A>` | `Fin<A> → FinResponse<A>` | Same type conversion |
| `ToFinResponse<A,B>` (mapper) | `Fin<A> → Func<A, B> → FinResponse<B>` | Success value mapping conversion |
| `ToFinResponse<A,B>` (factory) | `Fin<A> → Func<B> → FinResponse<B>` | Calls factory on success (ignores original value) |
| `ToFinResponse<A,B>` (onSucc/onFail) | `Fin<A> → Func<A, FinResponse<B>> → Func<Error, FinResponse<B>> → FinResponse<B>` | Custom handling for both success/failure |

```csharp
// 기본 변환
FinResponse<Product> response = fin.ToFinResponse();

// 매핑 변환
FinResponse<ProductDto> response = fin.ToFinResponse(product => new ProductDto(product));

// Factory 변환 (Delete 등 원본 값이 필요 없는 경우)
Fin<Unit> result = await repository.DeleteAsync(id);
return result.ToFinResponse(() => new DeleteResponse(id));
```

---

## FinT LINQ Extensions

`FinTLinqExtensions`는 `Fin<A>`, `IO<A>`, `Validation<Error, A>` 타입을 `FinT<M, A>` 모나드 트랜스포머로 통합하여 LINQ 쿼리 표현식을 지원하는 partial 클래스입니다.

### SelectMany Extension Methods

| 파일 | Source 타입 | Selector 반환 타입 | 결과 타입 | 설명 |
|------|-----------|-------------------|----------|------|
| `.Fin.cs` | `Fin<A>` | `FinT<M, B>` | `FinT<M, C>` | Fin → FinT 승격 후 체이닝 |
| `.Fin.cs` | `FinT<M, A>` | `Fin<B>` | `FinT<M, C>` | FinT 체인 중간에 Fin 사용 |
| `.IO.cs` | `IO<A>` | `B` (Map) | `FinT<IO, B>` | IO → FinT 단순 변환 |
| `.IO.cs` | `IO<A>` | `FinT<IO, B>` | `FinT<IO, C>` | IO → FinT 승격 후 체이닝 |
| `.IO.cs` | `FinT<IO, A>` | `IO<B>` | `FinT<IO, C>` | FinT 체인 중간에 IO 사용 |
| `.Validation.cs` | `Validation<Error, A>` | `FinT<M, B>` | `FinT<M, C>` | Validation → FinT (제네릭) |
| `.Validation.cs` | `Validation<Error, A>` | `B` (Map) | `FinT<M, B>` | Validation → FinT 단순 변환 |
| `.Validation.cs` | `Validation<Error, A>` | `FinT<IO, B>` | `FinT<IO, C>` | Validation → FinT (IO 특화) |
| `.Validation.cs` | `FinT<M, A>` | `Validation<Error, B>` | `FinT<M, C>` | FinT 체인 중간에 Validation 사용 |
| `.Validation.cs` | `FinT<IO, A>` | `Validation<Error, B>` | `FinT<IO, C>` | FinT 체인 중간에 Validation (IO) |

### Filter Extension Methods

| 파일 | 대상 타입 | 반환 타입 | 설명 |
|------|----------|----------|------|
| `.Fin.cs` | `Fin<A>` | `Fin<A>` | 조건 불만족 시 `Fail` 반환 |
| `.FinT.cs` | `FinT<M, A>` | `FinT<M, A>` | 조건 불만족 시 `Fail` 반환 |

```csharp
// Fin Filter
Fin<int> result = FinTest(25).Filter(x => x > 20);

// FinT Filter
FinT<IO, int> result = FinT<IO, int>.Succ(42).Filter(x => x > 20);
```

### TraverseSerial Extension Method

```csharp
public static FinT<M, Seq<B>> TraverseSerial<M, A, B>(
    this Seq<A> seq,
    Func<A, FinT<M, B>> f)
    where M : Monad<M>;
```

`Seq<A>`를 순차적으로 순회하며 각 요소를 `FinT<M, B>`로 변환합니다. `fold`를 사용하여 각 작업이 완전히 끝난 후 다음 작업을 시작하도록 보장합니다.

| Parameter | Type | Description |
|---------|------|------|
| `seq` | `Seq<A>` | 처리할 시퀀스 |
| `f` | `Func<A, FinT<M, B>>` | 각 요소를 FinT로 변환하는 함수 |

> **사용 시나리오:** DbContext 등 동시성을 지원하지 않는 리소스를 순차적으로 안전하게 사용해야 하는 경우에 적합합니다. 각 항목이 독립적이고 리소스 공유가 없다면 `Traverse`를 사용하십시오.

```csharp
// LINQ 쿼리 표현식에서 사용
FinT<IO, Response> response =
    from infos in GetFtpInfos()
    from results in infos.TraverseSerial(info => Process(info))
    select new Response(results);
```

### Difference in IO and Validation Method Counts

| 방향 | IO.cs | Validation.cs |
|------|-------|---------------|
| Source → FinT (Map) | IO 전용 | 제네릭 M |
| Source → FinT\<M, B\> | IO 전용 | 제네릭 M + IO |
| FinT\<M, A\> → Source | IO 전용 | 제네릭 M + IO |

`IO`는 그 자체가 특정 모나드이므로 제네릭 `M` 버전이 불필요합니다. `Validation`은 모나드가 아닌 데이터 타입이므로 어떤 모나드 `M`과도 조합 가능하여 제네릭 `M` 버전과 IO 특화 버전 모두 제공합니다.

### File Structure

| File | Content |
|------|------|
| `FinTLinqExtensions.cs` | 클래스 정의 및 문서 |
| `FinTLinqExtensions.Fin.cs` | `Fin<A>` 확장 (SelectMany, Filter) |
| `FinTLinqExtensions.IO.cs` | `IO<A>` 확장 (SelectMany) |
| `FinTLinqExtensions.Validation.cs` | `Validation<Error, A>` 확장 (SelectMany) |
| `FinTLinqExtensions.FinT.cs` | `FinT<M, A>` 확장 (Filter, TraverseSerial) |

---

## ICacheable Caching Contract

Interface for applying caching to Query requests. `IQueryRequest<TSuccess>`를 구현하는 record가 `ICacheable`도 함께 구현하면 Pipeline이 자동으로 캐싱을 처리합니다.

### Interface Definition

```csharp
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Duration { get; }
}
```

### Properties

| Property | Type | Description |
|---------|------|------|
| `CacheKey` | `string` | 캐시 항목의 고유 키 |
| `Duration` | `TimeSpan?` | 캐시 유효 기간 (`null`이면 기본 정책 적용) |

```csharp
// 사용 예: Query 요청에 캐싱 적용
public sealed record GetProductByIdQuery(ProductId Id)
    : IQueryRequest<ProductDto>, ICacheable
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? Duration => TimeSpan.FromMinutes(5);
}
```

---

## IUnitOfWork Persistence Contract

Contract for persisting changes in Command Usecases. `UsecaseTransactionPipeline`이 Handler 실행 후 자동으로 `SaveChanges`를 호출합니다.

### Interface Definition

```csharp
public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

### Methods

| Method | Return Type | Description |
|--------|----------|------|
| `SaveChanges` | `FinT<IO, Unit>` | Persists changes |
| `BeginTransactionAsync` | `Task<IUnitOfWorkTransaction>` | Starts an explicit transaction |

> `IUnitOfWork`는 `IObservablePort`를 상속합니다. `IObservablePort`는 `string RequestCategory { get; }` 프로퍼티를 정의하여, 관측성 Pipeline에서 레이어와 카테고리를 자동으로 식별합니다.

### IUnitOfWorkTransaction

```csharp
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

| Method | Return Type | Description |
|--------|----------|------|
| `CommitAsync` | `Task` | Commits the transaction |
| `DisposeAsync` | `ValueTask` | Uncommitted transactions are automatically rolled back (IAsyncDisposable) |

> **명시적 트랜잭션은** `ExecuteDeleteAsync`/`ExecuteUpdateAsync` 등 즉시 실행 SQL과 `SaveChanges`를 동일 트랜잭션으로 묶어야 할 때 사용합니다.

```csharp
// 명시적 트랜잭션 사용 예
await using var tx = await unitOfWork.BeginTransactionAsync(ct);
// ... ExecuteDeleteAsync, SaveChanges 등
await tx.CommitAsync(ct);
// Dispose 시 미커밋이면 자동 롤백
```

---

## CtxIgnoreAttribute

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class CtxIgnoreAttribute : Attribute;
```

Request records, properties, or record constructor parameters with this attribute are excluded from CtxEnricher source generator auto-generation targets.

| Target | Effect |
|------|------|
| `Class` | Excludes the entire record from CtxEnricher generation |
| `Property` | Excludes only this property from CtxEnricher |
| `Parameter` | Excludes only this record constructor parameter from CtxEnricher |

---

## Related Documents

| Document | Description |
|------|------|
| [Use Case와 CQRS 가이드](../guides/application/11-usecases-and-cqrs) | CQRS 패턴 설계 의도와 구현 가이드 |
| [검증 시스템 사양](./03-validation) | `TypedValidation`, FluentValidation 통합 |
| [에러 시스템 사양](./04-error-system) | `DomainErrorType`, `ApplicationErrorType` 등 |
| [포트와 어댑터 사양](./06-port-adapter) | `IRepository`, `IQueryPort` 등 |
| [파이프라인 사양](./07-pipeline) | Pipeline 동작, `UsecaseTransactionPipeline` 등 |
| [관측 가능성 사양](./08-observability) | Field/Tag 사양, Meter 정의 |
