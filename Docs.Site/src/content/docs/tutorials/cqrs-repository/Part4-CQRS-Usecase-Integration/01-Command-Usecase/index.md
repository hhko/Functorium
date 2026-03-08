---
title: "커맨드 유스케이스"
---
## 개요

Repository가 반환한 `FinT<IO, T>`를 어떻게 실행하고 API에 전달할까요? Part 3에서 Repository는 lazy한 `FinT`를 반환하도록 설계했습니다. 하지만 실제 Usecase에서는 이 `FinT`를 실행하고, 결과를 HTTP 응답에 적합한 `FinResponse<T>`로 변환해야 합니다. 이 장에서는 Command Usecase의 구조를 잡고, Repository 호출부터 응답 반환까지의 전체 흐름을 만들어봅시다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. **ICommandRequest / ICommandUsecase** 인터페이스로 Command 요청과 핸들러를 정의할 수 있습니다
2. **FinT\<IO, T\> LINQ 구문으로** Repository 호출을 합성할 수 있습니다
3. **FinResponse\<T\>로** Usecase 결과를 HTTP-friendly하게 변환할 수 있습니다
4. **Nested class 패턴으로** Request, Response, Usecase를 하나의 Command 클래스에 응집시킬 수 있습니다

---

## 핵심 개념

### Command Usecase 구조

Command Usecase는 Request(입력), Response(출력), Usecase(로직) 세 가지를 하나의 봉투 클래스에 묶습니다. 파일 하나만 열면 Command의 전체 계약을 파악할 수 있습니다.

```
CreateProductCommand (봉투)
├── Request   - 입력 데이터 (ICommandRequest<Response>)
├── Response  - 출력 데이터
└── Usecase   - 비즈니스 로직 (ICommandUsecase<Request, Response>)
```

### 실행 흐름

Request가 들어오면 Usecase는 도메인 객체를 생성하고, Repository에 저장한 뒤, 결과를 Response로 변환합니다. 각 단계가 어떤 타입을 다루는지 살펴보세요.

```
Request → Usecase.Handle()
           ├── Product.Create()        (도메인 객체 생성)
           ├── repository.Create()     (FinT<IO, Product>)
           ├── LINQ select             (Product → Response 변환)
           ├── .Run().RunAsync()       (IO 실행 → Fin<Response>)
           └── .ToFinResponse()        (Fin → FinResponse 변환)
```

### FinT LINQ 합성

Repository가 반환한 `FinT<IO, T>`는 LINQ 구문으로 자연스럽게 합성할 수 있습니다.

```csharp
FinT<IO, Response> usecase =
    from created in productRepository.Create(product)
    select new Response(created.Id.ToString(), created.Name, ...);
```

`from ... in`은 `FinT<IO, T>`의 monadic bind이며, `select`는 결과를 변환합니다. Repository가 `Fin.Fail`을 반환하면 이후 연산은 자동으로 건너뜁니다 (Railway-oriented programming).

---

## 프로젝트 설명

아래 파일들이 Command Usecase의 전체 구조를 구성합니다.

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

각 개념이 Command Usecase에서 어떤 역할을 하는지 정리합니다.

| 개념 | 설명 |
|------|------|
| `ICommandRequest<T>` | Command 요청 마커 (Mediator ICommand 확장) |
| `ICommandUsecase<TCmd, T>` | Command 핸들러 (Mediator ICommandHandler 확장) |
| `FinT<IO, T>` | IO 효과 + 성공/실패를 감싼 모나딕 타입 |
| `FinResponse<T>` | HTTP 응답에 적합한 성공/실패 래퍼 |
| `.ToFinResponse()` | `Fin<T>` → `FinResponse<T>` 변환 확장 메서드 |

---

## FAQ

### Q1: 왜 Usecase를 nested class로 구성하나요?
**A**: Request, Response, Usecase가 하나의 Command에 응집되어 코드 네비게이션이 쉽고, 파일 하나에 Command의 전체 계약을 파악할 수 있습니다.

### Q2: FinT와 Fin의 차이는 무엇인가요?
**A**: `FinT<IO, T>`는 IO 효과를 포함한 lazy 연산입니다. `.Run().RunAsync()`로 실행하면 `Fin<T>`(즉시 값)가 됩니다.

### Q3: ToFinResponse()는 왜 필요한가요?
**A**: `Fin<T>`는 LanguageExt의 내부 타입이고, `FinResponse<T>`는 Functorium이 Pipeline/API 레이어에서 사용하는 HTTP-friendly 래퍼입니다. 계층 간 변환을 명시적으로 수행합니다.

---

Command Usecase 구조를 만들었습니다. 그런데 목록 조회에는 Repository가 아닌 IQueryPort가 필요한데, Usecase 구조는 어떻게 달라질까요? 다음 장에서는 Query Usecase의 설계를 살펴봅니다.
