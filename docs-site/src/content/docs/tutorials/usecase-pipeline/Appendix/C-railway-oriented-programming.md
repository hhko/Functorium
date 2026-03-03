---
title: "Railway Oriented Programming 참조"
---

## 개요

**Railway Oriented Programming(ROP)은** Scott Wlaschin이 제안한 함수형 에러 처리 패턴입니다. 모든 함수가 성공(Success) 또는 실패(Failure) 트랙을 반환하며, 실패가 발생하면 이후 단계를 건너뛰고 실패 트랙으로 전파됩니다. Functorium의 `FinResponse<A>`는 이 ROP 패턴을 C#에서 구현한 것입니다.

---

## Railway 모델

### 두 개의 트랙

```
성공 트랙:  ──── f1 ────── f2 ────── f3 ────── 결과
                  │           │           │
실패 트랙:  ──────────────────────────────────── 에러
```

모든 함수는 두 개의 출력을 가집니다:
- **성공(Succ)**: 다음 함수로 값을 전달
- **실패(Fail)**: 나머지 함수를 건너뛰고 에러를 전파

### 스위치 함수

각 함수는 "스위치(분기기)"처럼 동작합니다:

```
입력 → [함수] → 성공 출력 (다음 단계로)
              └→ 실패 출력 (실패 트랙으로)
```

---

## FinResponse\<A\>와 ROP

### 1. FinResponse는 2트랙 타입

```csharp
// 성공 트랙
FinResponse<A>.Succ(value)    // value를 담고 있음

// 실패 트랙
FinResponse<A>.Fail(error)    // error를 담고 있음
```

`FinResponse<A>`는 항상 둘 중 하나의 상태입니다. C#의 Discriminated Union으로 두 트랙을 표현합니다.

### 2. Match는 트랙 분기

```csharp
result.Match(
    Succ: value => $"성공: {value}",     // 성공 트랙 처리
    Fail: error => $"실패: {error}");    // 실패 트랙 처리
```

`Match`는 현재 어떤 트랙에 있는지에 따라 다른 함수를 실행합니다.

### 3. Map은 성공 트랙 변환

```csharp
FinResponse<int> length = name.Map(s => s.Length);
```

```
성공 트랙:  ──── "hello" ──── [Map: s.Length] ──── 5
                                    │
실패 트랙:  ──── error ──────────────────────────── error (그대로 통과)
```

`Map`은 성공 트랙의 값만 변환합니다. 실패 트랙에 있으면 함수를 실행하지 않고 에러를 그대로 전달합니다.

### 4. Bind는 스위치 연결

```csharp
FinResponse<User> user = userId
    .Bind(id => FindUser(id))      // FindUser가 실패할 수 있음
    .Bind(u => ValidateUser(u));   // ValidateUser가 실패할 수 있음
```

```
성공 트랙:  ──── id ──── [FindUser] ──── user ──── [ValidateUser] ──── validUser
                              │                          │
실패 트랙:  ──────────────── error ──────────────────── error
```

`Bind`는 스위치 함수를 연결합니다. 각 단계에서 실패하면 이후 단계를 건너뜁니다.

### 5. LINQ 구문으로 Railway 구성

C#의 LINQ query syntax를 사용하면 Bind 체인을 읽기 쉽게 작성할 수 있습니다:

```csharp
// LINQ 구문 (읽기 쉬움)
var result =
    from request in Validate(input)
    from product in CreateProduct(request)
    from saved in SaveProduct(product)
    select saved;

// 위는 아래와 동일
var result = Validate(input)
    .Bind(request => CreateProduct(request))
    .Bind(product => SaveProduct(product));
```

이것이 가능한 이유는 `FinResponse<A>`가 `Select`와 `SelectMany`를 구현하기 때문입니다:

```csharp
public FinResponse<B> Select<B>(Func<A, B> f) => Map(f);

public FinResponse<C> SelectMany<B, C>(
    Func<A, FinResponse<B>> bind,
    Func<A, B, C> project) =>
    Bind(a => bind(a).Map(b => project(a, b)));
```

---

## Pipeline과 ROP의 관계

Mediator Pipeline도 ROP와 유사한 구조를 가집니다:

```
요청 → [Validation] → [Logging] → [Tracing] → [Handler] → 응답
              │            │           │            │
              └──── Fail ──┴─── Fail ──┴─── Fail ──┘
```

각 Pipeline은 스위치처럼 동작합니다:
- **성공**: 다음 Pipeline(`next()`)을 호출
- **실패**: `TResponse.CreateFail(error)`로 실패 응답을 반환하고, 이후 Pipeline을 건너뜀

---

## Fin\<T\> vs FinResponse\<A\>

| 항목 | Fin\<T\> (LanguageExt) | FinResponse\<A\> (Functorium) |
|------|----------------------|-------------------------------|
| **타입** | sealed struct | abstract record |
| **Pipeline 제약** | 불가 (sealed struct) | 가능 (인터페이스 구현) |
| **ROP 메서드** | Match, Map, Bind | Match, Map, Bind |
| **LINQ 지원** | O | O |
| **변환** | - | `Fin<A>.ToFinResponse()` |
| **팩토리** | `Fin.Succ`, `Fin.Fail` | `FinResponse.Succ`, `FinResponse.Fail` |

`Fin<T>`는 Repository 계층에서, `FinResponse<A>`는 Usecase/Pipeline 계층에서 사용합니다. `ToFinResponse()` 확장 메서드로 두 계층을 연결합니다.

---

## 참고

- [Railway Oriented Programming - Scott Wlaschin](https://fsharpforfunandprofit.com/rop/)
- [LanguageExt - Fin\<T\>](https://github.com/louthy/language-ext)

---

[← 이전: B. Pipeline 제약 조건 vs 대안 비교](B-constraint-vs-alternatives.md) | [다음: D. 용어집 →](D-glossary.md)
