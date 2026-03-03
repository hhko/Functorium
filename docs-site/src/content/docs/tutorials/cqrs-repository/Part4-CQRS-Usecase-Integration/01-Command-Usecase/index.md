---
title: "Part 4 - Chapter 14: Command Usecase"
---

> **Part 4: CQRS Usecase 통합** | [← 이전: 13장 Dapper Query Adapter](../../Part3-Query-Patterns/05-Dapper-Query-Adapter/) | [다음: 15장 Query Usecase →](../02-Query-Usecase/)

---

## 개요

Command Usecase는 CQRS의 Command 측면에서 비즈니스 로직을 실행하는 핵심 패턴입니다. `ICommandRequest<TSuccess>`로 요청을 정의하고, `ICommandUsecase<TCommand, TSuccess>`로 처리 로직을 구현합니다. 결과는 `FinResponse<T>`로 감싸서 성공/실패를 명확하게 전달합니다.

---

## 학습 목표

- **ICommandRequest / ICommandUsecase** 인터페이스의 역할 이해
- **FinT<IO, T> LINQ 구문**으로 Repository 호출을 합성하는 방법
- **FinResponse<T>**로 Usecase 결과를 HTTP-friendly하게 변환하는 패턴
- **Nested class 패턴**: Request, Response, Usecase를 하나의 Command 클래스에 응집

---

## 핵심 개념

### Command Usecase 구조

```
CreateProductCommand (봉투)
├── Request   - 입력 데이터 (ICommandRequest<Response>)
├── Response  - 출력 데이터
└── Usecase   - 비즈니스 로직 (ICommandUsecase<Request, Response>)
```

### 실행 흐름

```
Request → Usecase.Handle()
           ├── Product.Create()        (도메인 객체 생성)
           ├── repository.Create()     (FinT<IO, Product>)
           ├── LINQ select             (Product → Response 변환)
           ├── .Run().RunAsync()       (IO 실행 → Fin<Response>)
           └── .ToFinResponse()        (Fin → FinResponse 변환)
```

### FinT LINQ 합성

```csharp
FinT<IO, Response> usecase =
    from created in productRepository.Create(product)
    select new Response(created.Id.ToString(), created.Name, ...);
```

`from ... in`은 `FinT<IO, T>`의 monadic bind이며, `select`는 결과를 변환합니다. Repository가 `Fin.Fail`을 반환하면 이후 연산은 자동으로 건너뜁니다 (Railway-oriented programming).

---

## 프로젝트 설명

| 파일 | 설명 |
|------|------|
| `ProductId.cs` | Ulid 기반 Product 식별자 |
| `Product.cs` | AggregateRoot를 상속한 상품 엔티티 |
| `IProductRepository.cs` | IRepository<Product, ProductId> 확장 인터페이스 |
| `InMemoryProductRepository.cs` | InMemoryRepositoryBase 기반 구현 |
| `CreateProductCommand.cs` | Command Usecase 패턴 (Request, Response, Usecase) |
| `Program.cs` | 실행 데모 |

---

## 한눈에 보는 정리

| 개념 | 설명 |
|------|------|
| `ICommandRequest<T>` | Command 요청 마커 (Mediator ICommand 확장) |
| `ICommandUsecase<TCmd, T>` | Command 핸들러 (Mediator ICommandHandler 확장) |
| `FinT<IO, T>` | IO 효과 + 성공/실패를 감싼 모나딕 타입 |
| `FinResponse<T>` | HTTP 응답에 적합한 성공/실패 래퍼 |
| `.ToFinResponse()` | `Fin<T>` → `FinResponse<T>` 변환 확장 메서드 |

---

## FAQ

**Q: 왜 Usecase를 nested class로 구성하나요?**
A: Request, Response, Usecase가 하나의 Command에 응집되어 코드 네비게이션이 쉽고, 파일 하나에 Command의 전체 계약을 파악할 수 있습니다.

**Q: FinT와 Fin의 차이는 무엇인가요?**
A: `FinT<IO, T>`는 IO 효과를 포함한 lazy 연산입니다. `.Run().RunAsync()`로 실행하면 `Fin<T>`(즉시 값)가 됩니다.

**Q: ToFinResponse()는 왜 필요한가요?**
A: `Fin<T>`는 LanguageExt의 내부 타입이고, `FinResponse<T>`는 Functorium이 Pipeline/API 레이어에서 사용하는 HTTP-friendly 래퍼입니다. 계층 간 변환을 명시적으로 수행합니다.
