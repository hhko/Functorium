---
title: "FinT에서 FinResponse로"
---
## 개요

`FinT<IO, T>`는 LanguageExt의 모나딕 타입으로, IO 효과와 성공/실패를 LINQ 구문으로 합성할 수 있게 합니다. Usecase에서는 이 합성 결과를 `.Run().RunAsync()`로 실행한 후 `.ToFinResponse()`로 변환하여 API 레이어에 전달합니다. 이 장에서는 다양한 LINQ 합성 패턴을 학습합니다.

---

## 학습 목표

- **단일 연산**: `from...select` 패턴
- **순차 연산**: `from...from...select` 패턴 (chained monadic bind)
- **조건부 중단**: `guard()`를 사용한 early termination
- **중간 값**: `let`을 사용한 intermediate value binding
- **ToFinResponse()**: `Fin<T>` -> `FinResponse<T>` 변환

---

## 핵심 개념

### 패턴 1: 단일 연산 (from...select)

```csharp
FinT<IO, Response> usecase =
    from created in repository.Create(product)
    select new Response(created.Id.ToString(), created.Name, created.Price);
```

하나의 IO 연산을 실행하고 결과를 변환합니다.

### 패턴 2: 순차 연산 (from...from...select)

```csharp
FinT<IO, Response> usecase =
    from existing in repository.GetById(productId)
    let oldPrice = existing.Price
    from updated in repository.Update(existing.UpdatePrice(newPrice))
    select new Response(updated.Id.ToString(), oldPrice, updated.Price);
```

여러 IO 연산을 순차적으로 합성합니다. 앞 단계가 실패하면 이후 단계는 자동으로 건너뜁니다 (Railway-oriented programming).

### 패턴 3: guard로 조건부 중단

```csharp
FinT<IO, Response> usecase =
    from existing in repository.GetById(productId)
    from _ in guard(existing.IsActive, Error.New("Product is not active"))
    from updated in repository.Update(existing.UpdatePrice(newPrice))
    select new Response(...);
```

`guard(condition, error)`는 조건이 false이면 `Fin.Fail`을 생성하여 이후 연산을 중단합니다.

### 실행과 변환

```csharp
Fin<Response> result = await usecase.Run().RunAsync();  // IO 실행
return result.ToFinResponse();                           // FinResponse로 변환
```

---

## 프로젝트 설명

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

| 패턴 | 구문 | 용도 |
|------|------|------|
| 단일 연산 | `from x in op select ...` | 하나의 IO 호출 후 변환 |
| 순차 연산 | `from x in op1 from y in op2 select ...` | 여러 IO를 순차 합성 |
| 중간 값 | `let v = expr` | 순수 계산 결과를 바인딩 |
| 조건 검증 | `from _ in guard(cond, error)` | false면 Fail로 중단 |
| 실행 | `.Run().RunAsync()` | lazy IO를 실행하여 Fin 획득 |
| 변환 | `.ToFinResponse()` | Fin -> FinResponse 변환 |

---

## FAQ

**Q: guard와 if-throw의 차이는?**
A: `guard`는 모나딕 합성 안에서 Fail을 생성하므로 예외 없이 실패를 처리합니다. if-throw는 예외를 발생시키므로 Pipeline에서 catch해야 합니다.

**Q: let과 from의 차이는?**
A: `from`은 `FinT<IO, T>`를 바인딩 (IO 효과가 있는 연산), `let`은 순수 값을 바인딩 (IO 효과 없음)합니다.

**Q: 실패 시 어디에서 중단되나요?**
A: `from`의 대상이 `Fin.Fail`을 반환하면, 이후의 모든 `from`, `let`, `select`는 실행되지 않고 Fail이 그대로 전파됩니다.
