---
title: "FinT/FinResponse 참조"
---
## 개요

Functorium CQRS에서 사용하는 함수형 타입의 참조 문서입니다. Repository 계층의 `FinT<IO, T>`와 Usecase 계층의 `FinResponse<T>`, 그리고 이를 연결하는 `ToFinResponse()` 변환을 설명합니다.

---

## 타입 계층

Functorium의 함수형 타입은 LanguageExt 위에 구축됩니다. 아래 계층도에서 각 타입의 소속을 확인하세요.

```
LanguageExt (라이브러리)
├── Fin<T>              성공(T) 또는 실패(Error)를 표현
├── FinT<M, T>          모나드 변환자: M<Fin<T>>를 래핑
└── IO                  순수 함수형 IO 효과

Functorium (프레임워크)
├── FinResponse<T>      Usecase 반환 타입: Fin<T> + IFinResponseFactory
└── ToFinResponse()     Fin<T> -> FinResponse<T> 변환 확장 메서드
```

---

## Fin\<T\>

LanguageExt 라이브러리의 Result 타입입니다. 성공 또는 실패를 표현합니다.

```csharp
// 생성
Fin<int> success = Fin.Succ(42);
Fin<int> failure = Fin.Fail<int>(Error.New("오류 발생"));

// 패턴 매칭
var result = fin.Match(
    Succ: value => $"성공: {value}",
    Fail: error => $"실패: {error.Message}");

// 상태 확인
if (fin.IsSucc) { /* 성공 */ }
if (fin.IsFail) { /* 실패 */ }

// 값 접근 (성공이 아니면 예외)
var value = fin.ThrowIfFail();
```

---

## FinT\<IO, T\>

FinT는 **모나드 변환자**로, `IO<Fin<T>>`를 래핑합니다. Repository 메서드의 반환 타입입니다.

```csharp
// Repository 메서드는 FinT<IO, T>를 반환
FinT<IO, Order> result = repository.GetById(orderId);

// 실행 (IO 효과를 실행하여 Fin<T>를 얻음)
Fin<Order> fin = await result.RunAsync();
```

### LINQ 모나딕 합성

FinT는 LINQ의 `from...select` 구문으로 합성할 수 있습니다.

```csharp
// 여러 Repository 작업을 순차적으로 합성
var pipeline =
    from order    in repository.GetById(orderId)
    from _        in guard(order.CanCancel(), Error.New("취소 불가"))
    from updated  in repository.Update(order.Cancel())
    select updated.Id;

// 하나라도 실패하면 전체 파이프라인이 실패
Fin<OrderId> fin = await pipeline.RunAsync();
```

### guard 함수

조건이 충족되지 않으면 파이프라인을 실패시킵니다.

```csharp
// guard(조건, 실패 시 에러)
from _ in guard(order.CanCancel(), Error.New("취소 불가 상태"))
```

### map / bind

```csharp
// map: 성공 값을 변환
FinT<IO, OrderId> orderId = repository.Create(order).Map(o => o.Id);

// bind (SelectMany): 다른 FinT로 체이닝
FinT<IO, Order> result = repository.GetById(orderId)
    .Bind(order => repository.Update(order.Cancel()));
```

---

## FinResponse\<T\>

Usecase 계층의 반환 타입입니다. Mediator 파이프라인(검증, 트랜잭션, 로깅 등)과 호환됩니다.

```csharp
// 생성
FinResponse<OrderId> success = FinResponse.Succ(orderId);
FinResponse<OrderId> failure = FinResponse.Fail<OrderId>(Error.New("실패"));

// 상태 확인
if (response.IsSucc) { /* 성공 */ }
if (response.IsFail) { /* 실패 */ }
```

### FinResponse vs Fin 차이점

두 타입이 사용되는 계층과 용도가 다릅니다.

| 특성 | Fin\<T\> | FinResponse\<T\> |
|------|---------|-----------------|
| **계층** | Repository/도메인 | Usecase/Application |
| **용도** | 함수형 합성 (FinT) | Mediator 응답 |
| **팩토리** | Fin.Succ / Fin.Fail | FinResponse.Succ / FinResponse.Fail |
| **파이프라인** | LINQ from...select | Mediator Pipeline |

---

## ToFinResponse() 변환

Repository 계층(Fin)에서 Usecase 계층(FinResponse)으로 변환하는 확장 메서드입니다. 용도에 따라 네 가지 오버로드를 제공합니다.

### 기본 변환

성공 값을 그대로 전달합니다.

```csharp
// Fin<A> -> FinResponse<A>
Fin<Order> fin = await repository.Create(order).RunAsync();
FinResponse<Order> response = fin.ToFinResponse();
```

### 매핑 변환

성공 값을 다른 타입으로 변환합니다.

```csharp
// Fin<A> -> FinResponse<B> (성공 값 변환)
Fin<Order> fin = await repository.Create(order).RunAsync();
FinResponse<OrderId> response = fin.ToFinResponse(order => order.Id);
```

### 팩토리 변환

성공 값을 무시하고 새 인스턴스를 생성합니다.

```csharp
// Fin<A> -> FinResponse<B> (성공 값 무시, 새 인스턴스 생성)
Fin<int> fin = await repository.Delete(orderId).RunAsync();
FinResponse<DeleteResult> response = fin.ToFinResponse(() => new DeleteResult(orderId));
```

### 커스텀 변환

성공과 실패 모두 커스텀 로직으로 처리합니다.

```csharp
// Fin<A> -> FinResponse<B> (성공/실패 모두 커스텀 처리)
Fin<Order> fin = await repository.GetById(orderId).RunAsync();
FinResponse<OrderDto> response = fin.ToFinResponse(
    onSucc: order => FinResponse.Succ(order.ToDto()),
    onFail: error => FinResponse.Fail<OrderDto>(error));
```

---

## 일반적인 사용 패턴

### 패턴 1: 단순 변환

```csharp
public async ValueTask<FinResponse<OrderId>> Handle(
    CreateOrderCommand command, CancellationToken ct)
{
    var order = Order.Create(OrderId.New(), command.CustomerId);
    var fin = await repository.Create(order).RunAsync();
    return fin.ToFinResponse(o => o.Id);
}
```

### 패턴 2: 모나딕 합성 후 변환

```csharp
public async ValueTask<FinResponse<OrderId>> Handle(
    CancelOrderCommand command, CancellationToken ct)
{
    var pipeline =
        from order in repository.GetById(command.OrderId)
        from _     in guard(order.CanCancel(), Error.New("취소 불가"))
        from __    in repository.Update(order.Cancel())
        select order.Id;

    var fin = await pipeline.RunAsync();
    return fin.ToFinResponse();
}
```

### 패턴 3: Query 어댑터 사용

```csharp
public async ValueTask<FinResponse<PagedResult<OrderDto>>> Handle(
    SearchOrdersQuery request, CancellationToken ct)
{
    var fin = await query.Search(spec, request.Page, request.Sort).RunAsync();
    return fin.ToFinResponse();
}
```

---

## 에러 처리

### Error 생성

```csharp
// 단순 에러
Error.New("주문을 찾을 수 없습니다")

// 코드 포함
Error.New(404, "주문을 찾을 수 없습니다")

// 예외 래핑
Error.New(exception)
```

### 파이프라인에서 에러 전파

FinT 파이프라인에서 어느 단계든 실패하면 이후 단계는 실행되지 않고 에러가 전파됩니다.

```csharp
var pipeline =
    from order in repository.GetById(orderId)     // 실패 시 아래 단계 건너뜀
    from _     in guard(order.CanCancel(), ...)    // 실패 시 아래 단계 건너뜀
    from __    in repository.Update(order.Cancel())
    select order.Id;
```

---

## 구조화된 에러 타입

Functorium은 `Error.New("message")` 외에 구조화된 에러 타입을 제공합니다. Pipeline 레이어에서 에러 종류에 따라 HTTP 상태 코드를 자동 매핑합니다.

| 에러 타입 | 용도 | HTTP 매핑 |
|-----------|------|-----------|
| `DomainError` | 도메인 규칙 위반 | 422 Unprocessable Entity |
| `ApplicationError` | 애플리케이션 레벨 오류 | 400 Bad Request |
| `NotFoundError` | 리소스 미존재 | 404 Not Found |

```csharp
// DomainError 생성 예시
DomainError.ForContext<Order>("주문 상태가 취소 가능하지 않습니다")

// Guard와 조합
from _ in guard(order.CanCancel(),
    DomainError.ForContext<Order>("취소 불가 상태입니다"))
```

---

CQRS 적용 시 흔히 발생하는 설계 실수와 올바른 대안을 확인합니다.

→ [부록 D: CQRS 안티패턴](../D-anti-patterns/)
