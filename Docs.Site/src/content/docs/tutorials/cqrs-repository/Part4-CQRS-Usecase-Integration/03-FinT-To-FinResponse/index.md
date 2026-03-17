---
title: "FinT에서 FinResponse로"
---
## 개요

여러 Repository 호출을 순차 연결하면서 중간에 조건 검증도 끼워야 한다면 어떻게 할까요? 단일 `from...select`로는 부족합니다. 조회 → 검증 → 수정 → 응답처럼 여러 단계를 파이프라인으로 엮어야 하는데, 매번 `RunAsync()` → `IsSucc` 체크 → 다음 호출을 반복하면 보일러플레이트가 급증합니다. 이 장에서는 FinT의 LINQ 모나딕 합성으로 이 문제를 깔끔하게 해결하는 방법을 학습합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. **단일 연산** `from...select` 패턴으로 하나의 IO 호출을 변환할 수 있습니다
2. **순차 연산** `from...from...select` 패턴으로 여러 IO를 체이닝할 수 있습니다
3. **조건부 중단** `guard()`로 비즈니스 규칙 위반 시 파이프라인을 중단시킬 수 있습니다
4. **중간 값** `let`으로 순수 계산 결과를 바인딩할 수 있습니다
5. **ToFinResponse()로** `Fin<T>`를 `FinResponse<T>`로 변환할 수 있습니다

---

## "왜 필요한가?"

여러 Repository 호출을 순차로 엮을 때, 모나딕 합성 없이는 이런 코드가 됩니다.

```csharp
// 모나딕 합성 없이 매번 결과를 꺼내서 검사하는 보일러플레이트
var existingFin = await repository.GetById(productId).RunAsync();
if (existingFin.IsFail) return existingFin.ToFinResponse<Response>();

var existing = existingFin.ThrowIfFail();
if (!existing.IsActive)
    return FinResponse.Fail<Response>(Error.New("Product is not active"));

var updatedFin = await repository.Update(existing.UpdatePrice(newPrice)).RunAsync();
if (updatedFin.IsFail) return updatedFin.ToFinResponse<Response>();

var updated = updatedFin.ThrowIfFail();
return FinResponse.Succ(new Response(updated.Id.ToString(), updated.Price));
```

매 단계마다 `RunAsync()` → `IsFail` 체크 → 값 추출을 반복합니다. 단계가 늘어날수록 중첩이 깊어지고, 핵심 로직이 에러 처리 코드에 묻힙니다. LINQ 모나딕 합성은 이 반복을 제거합니다.

---

## 핵심 개념

### 패턴 1: 단일 연산 (from...select)

가장 단순한 경우입니다. 하나의 IO 연산을 실행하고 결과를 변환합니다.

```csharp
FinT<IO, Response> usecase =
    from created in repository.Create(product)
    select new Response(created.Id.ToString(), created.Name, created.Price);
```

Repository 호출이 성공하면 `select`가 결과를 Response로 변환하고, 실패하면 이후 연산 없이 Fail이 전파됩니다.

### 패턴 2: 순차 연산 (from...from...select)

조회 후 수정처럼 여러 IO 연산을 순차적으로 합성해야 할 때 사용합니다. 앞 단계가 실패하면 이후 단계는 자동으로 건너뜁니다 (Railway-oriented programming).

```csharp
FinT<IO, Response> usecase =
    from existing in repository.GetById(productId)
    let oldPrice = existing.Price
    from updated in repository.Update(existing.UpdatePrice(newPrice))
    select new Response(updated.Id.ToString(), oldPrice, updated.Price);
```

`let`은 IO 효과 없이 순수 값을 바인딩합니다. 여기서는 변경 전 가격을 기억해두는 데 사용합니다.

### 패턴 3: guard로 조건부 중단

비즈니스 규칙을 검증하고 위반 시 파이프라인을 중단시킵니다. `guard(condition, error)`는 조건이 false이면 `Fin.Fail`을 생성합니다.

```csharp
FinT<IO, Response> usecase =
    from existing in repository.GetById(productId)
    from _ in guard(existing.IsActive, Error.New("Product is not active"))
    from updated in repository.Update(existing.UpdatePrice(newPrice))
    select new Response(...);
```

예외를 던지지 않고 모나딕 합성 안에서 실패를 처리하므로, 파이프라인의 흐름이 일관됩니다.

### 실행과 변환

LINQ로 합성한 파이프라인은 아직 실행되지 않은 lazy 상태입니다. `RunAsync()`로 IO를 실행하고, `ToFinResponse()`로 API 레이어에 전달할 수 있는 형태로 변환합니다.

```csharp
Fin<Response> result = await usecase.Run().RunAsync();  // IO 실행
return result.ToFinResponse();                           // FinResponse로 변환
```

---

## 프로젝트 설명

아래 파일들에서 세 가지 합성 패턴을 직접 실행해볼 수 있습니다.

| 파일 | 설명 |
|------|------|
| `ProductId.cs` | Ulid 기반 Product 식별자 |
| `Product.cs` | AggregateRoot 기반 상품 (UpdatePrice, Deactivate 지원) |
| `IProductRepository.cs` | Repository 인터페이스 |
| `InMemoryProductRepository.cs` | InMemory 구현 |
| `CompositionExamples.cs` | 3가지 LINQ 합성 패턴 예제 |
| `Program.cs` | 실행 데모 |

---

## 한눈에 보는 정리

각 패턴의 구문과 용도를 한눈에 비교합니다.

| 패턴 | 구문 | 용도 |
|------|------|------|
| 단일 연산 | `from x in op select ...` | 하나의 IO 호출 후 변환 |
| 순차 연산 | `from x in op1 from y in op2 select ...` | 여러 IO를 순차 합성 |
| 중간 값 | `let v = expr` | 순수 계산 결과를 바인딩 |
| 조건 검증 | `from _ in guard(cond, error)` | false면 Fail로 중단 |
| 실행 | `.Run().RunAsync()` | lazy IO를 실행하여 Fin 획득 |
| 변환 | `.ToFinResponse()` | Fin -> FinResponse 변환 |

---

## 구조화된 에러 타입

`Error.New("message")` 대신 Functorium의 구조화된 에러 타입을 사용하면 에러의 맥락을 명확히 전달할 수 있습니다.

```csharp
// ❌ 문자열 에러 — 호출자가 에러 종류를 판별할 수 없음
from _ in guard(order.CanCancel(), Error.New("취소 불가"))

// ✅ 구조화된 에러 — 타입으로 판별 가능
from _ in guard(order.CanCancel(),
    DomainError.ForContext<Order>("주문 상태가 취소 가능하지 않습니다"))
```

`DomainError`, `ApplicationError` 등의 구조화된 에러 타입은 Pipeline 레이어에서 에러 종류에 따라 HTTP 상태 코드를 자동 매핑하는 데 사용됩니다.

---

## FAQ

### Q1: guard와 if-throw의 차이는?
**A**: `guard`는 모나딕 합성 안에서 Fail을 생성하므로 예외 없이 실패를 처리합니다. if-throw는 예외를 발생시키므로 Pipeline에서 catch해야 합니다.

### Q2: let과 from의 차이는?
**A**: `from`은 `FinT<IO, T>`를 바인딩 (IO 효과가 있는 연산), `let`은 순수 값을 바인딩 (IO 효과 없음)합니다.

### Q3: 실패 시 어디에서 중단되나요?
**A**: `from`의 대상이 `Fin.Fail`을 반환하면, 이후의 모든 `from`, `let`, `select`는 실행되지 않고 Fail이 그대로 전파됩니다.

---

FinT 합성으로 깔끔한 파이프라인을 만들었습니다. 그런데 도메인 이벤트는 어디서 수집하고 발행할까요? 다음 장에서는 Aggregate 내부에서 이벤트가 생성되어 외부로 전파되는 흐름을 살펴봅니다.
