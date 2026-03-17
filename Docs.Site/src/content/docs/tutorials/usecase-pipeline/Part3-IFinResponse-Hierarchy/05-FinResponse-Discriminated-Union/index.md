---
title: "FinResponse DU"
---

## 개요

마지막 **요구사항 R4**를 포함하여, 1장부터 4장까지 설계한 모든 인터페이스를 **하나의 타입**으로 통합합니다. `FinResponse<A>`는 `Succ`/`Fail` sealed record로 구성된 **Discriminated Union**이며, `IFinResponse<A>`, `IFinResponseFactory<FinResponse<A>>`를 모두 구현합니다. Match, Map, Bind 메서드와 값 추출(ThrowIfFail, IfFail), 에러 트랙 연산(MapFail, BiMap, BiBind, BindFail), Boolean/Choice 연산자, 암시적 변환, LINQ 지원까지 포함한 완전한 응답 타입입니다.

```
FinResponse<A>                            Discriminated Union
├── : IFinResponse<A>                     공변 인터페이스 구현
├── : IFinResponseFactory<FinResponse<A>> CRTP 팩토리 구현
│
├── sealed record Succ(A Value)           성공 케이스
│
└── sealed record Fail(Error Error)       실패 케이스
    └── : IFinResponseWithError           Fail에서만 에러 접근
```

## 핵심 개념

### 1. Discriminated Union

`FinResponse<A>`는 `abstract record`이며, `Succ`와 `Fail` 두 가지 `sealed record`만 존재합니다. 새로운 케이스를 추가할 수 없으므로, 패턴 매칭이 **완전(exhaustive)합니다**.

```csharp
public abstract record FinResponse<A> : IFinResponse<A>, IFinResponseFactory<FinResponse<A>>
{
    public sealed record Succ(A Value) : FinResponse<A> { ... }
    public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError { ... }
}
```

### 2. Match 메서드

`Match`는 Succ/Fail 각 케이스에 대한 함수를 받아 결과를 반환합니다. 모든 케이스를 처리해야 하므로 **컴파일 타임에 안전성**이 보장됩니다.

```csharp
FinResponse<int> response = FinResponse.Succ(42);

var result = response.Match(
    Succ: value => $"값: {value}",
    Fail: error => $"에러: {error}");
// result = "값: 42"
```

### 3. Map과 Bind

`Map`은 Succ의 값을 변환하고, `Bind`는 값을 변환하면서 새로운 FinResponse를 반환합니다. Fail인 경우 두 메서드 모두 에러를 전파합니다.

```csharp
// Map: A → B (값만 변환)
FinResponse<string> mapped = response.Map(v => v.ToString());

// Bind: A → FinResponse<B> (체이닝)
FinResponse<int> bound = response.Bind(v =>
    v > 0 ? FinResponse.Succ(v * 2) : FinResponse.Fail<int>(Error.New("negative")));
```

### 4. LINQ 지원

`Select`와 `SelectMany`를 구현하여 LINQ `from ... select` 구문을 지원합니다.

```csharp
var result = from x in FinResponse.Succ(3)
             from y in FinResponse.Succ(4)
             select x + y;
// result = Succ(7)
```

### 5. 암시적 변환

값이나 에러를 `FinResponse<A>`에 직접 대입할 수 있습니다.

```csharp
FinResponse<string> succ = "Hello";               // 암시적 변환: string → Succ
FinResponse<string> fail = Error.New("error");     // 암시적 변환: Error → Fail
```

### 6. 모든 인터페이스 통합

다음 표는 1장~4장의 각 요구사항이 `FinResponse<A>` 하나로 어떻게 통합되었는지 보여줍니다.

`FinResponse<A>`는 1장~4장의 모든 인터페이스를 구현합니다:

| 인터페이스 | 역할 | 구현 |
|-----------|------|------|
| `IFinResponse` | 성공/실패 읽기 | `IsSucc`, `IsFail` |
| `IFinResponse<out A>` | 공변 접근 | 상속 |
| `IFinResponse<TSelf>` | 실패 생성 | `CreateFail` |
| `IFinResponseWithError` | 에러 접근 | `Fail`에서만 구현 |

`FinResponse<A>`의 전체 API를 그룹별로 정리하면 다음과 같습니다:

| 그룹 | 멤버 | 역할 |
|------|------|------|
| **패턴 매칭** | `Match<B>(Func, Func)` | 값/에러 → B 변환 |
| | `Match(Action, Action)` | 사이드 이펙트 실행 |
| **값 추출** | `ThrowIfFail()` | 성공 값 추출 (실패 시 throw) |
| | `IfFail(Func<Error, A>)` | 에러 → 폴백 값 |
| | `IfFail(A)` | 기본값 제공 |
| | `IfFail(Action<Error>)` | 실패 시 사이드 이펙트 |
| | `IfSucc(Action<A>)` | 성공 시 사이드 이펙트 |
| **성공 트랙** | `Map<B>(Func<A, B>)` | 값 변환 |
| | `Bind<B>(Func<A, FinResponse<B>>)` | 모나드 바인드 |
| **에러 트랙** | `MapFail(Func<Error, Error>)` | 에러 변환 |
| | `BindFail(Func<Error, FinResponse<A>>)` | 에러 복구 |
| **양방향** | `BiMap<B>(Func, Func)` | 성공/에러 동시 변환 |
| | `BiBind<B>(Func, Func)` | 성공/에러 동시 바인드 |
| **LINQ** | `Select`, `SelectMany` | `from ... select` 구문 |
| **연산자** | `implicit A →`, `implicit Error →` | 암시적 변환 |
| | `operator true/false` | `if (response)` 패턴 |
| | `operator \|` | choice (`fail \| fallback`) |

### 7. 값 추출 패턴 — ThrowIfFail, IfFail, IfSucc

`Match`는 항상 두 갈래를 다뤄야 합니다. 성공 값만 꺼내고 싶다면?

**ThrowIfFail** — 테스트 코드에서 가장 자주 사용하는 패턴입니다:

```csharp
var value = response.ThrowIfFail();  // 실패 시 ErrorException throw
```

**IfFail** — 안전한 폴백:

```csharp
var value = response.IfFail(-1);              // 기본값 제공
var value = response.IfFail(err => 0);        // 에러 기반 폴백
response.IfFail(err => logger.Error(err));    // 사이드 이펙트
```

**IfSucc** — 성공 시 사이드 이펙트:

```csharp
response.IfSucc(value => logger.Info($"Got: {value}"));
```

> **FAQ: `ThrowIfFail()`은 프로덕션에서 안전한가요?**
> 테스트와 최상위 API 바운더리(Controller 등)에서만 사용하세요. 비즈니스 로직 내부에서는 `Match`나 `Bind`로 에러를 전파하는 것이 안전합니다.

### 8. 에러 트랙 연산 — MapFail, BiMap, BiBind, BindFail

Railway의 에러 트랙에서도 변환이 필요할 때 사용합니다.

**MapFail** — 도메인 에러를 애플리케이션 에러로 변환:

```csharp
var result = response.MapFail(e => Error.New($"Application error: {e.Message}"));
```

**BindFail** — 에러 복구 시도 (fallback 조회):

```csharp
var result = response.BindFail(err => TryFallback());
// TryFallback()이 Succ를 반환하면 복구, Fail이면 새 에러 전파
```

**BiMap, BiBind** — 양방향 변환:

```csharp
// BiMap: 성공 값과 에러를 동시에 변환
var result = response.BiMap(
    value => value.ToString(),
    error => Error.New($"Wrapped: {error.Message}"));

// BiBind: 성공/에러 모두에서 새 FinResponse 반환 가능
var result = response.BiBind(
    value => FinResponse.Succ(value.ToString()),
    error => FinResponse.Succ("recovered"));
```

> 부록 C "Railway Oriented Programming"에서 에러 트랙 연산의 실전 활용 패턴을 더 자세히 다룹니다.

### 9. Boolean 및 Choice 연산자

`if/else` 분기를 더 간결하게 표현할 수 있습니다.

**`operator true/false`** — `if (response)` 패턴:

```csharp
if (response)
    Console.WriteLine("성공!");
else
    Console.WriteLine("실패!");
```

**`operator |`** — choice 연산자:

```csharp
// 실패 시 대안 사용
var result = primaryLookup | fallbackLookup;
```

## FAQ

### Q1: `abstract record`로 Discriminated Union을 구현하면 `switch` 패턴 매칭의 완전성(exhaustiveness)이 보장되나요?
**A**: C# 컴파일러는 현재 sealed hierarchy에 대한 완전성 검사를 **경고 수준**에서 지원합니다. `Succ`와 `Fail`이 모두 `sealed record`이므로 새로운 케이스를 추가할 수 없고, `Match` 메서드를 사용하면 두 케이스를 모두 처리하도록 **컴파일 타임에 강제**됩니다.

### Q2: 암시적 변환(`implicit operator`)이 코드 가독성을 해칠 수 있지 않나요?
**A**: 타입이 명확한 경우에 한해 암시적 변환은 **보일러플레이트를 줄여** 오히려 가독성을 높입니다. `return new Response(...)` 대신 `return response`로 작성할 수 있습니다. 하지만 타입이 모호한 상황에서는 `FinResponse.Succ(value)` 같은 명시적 팩토리 메서드를 사용하는 것이 좋습니다.

### Q3: `Map`과 `Bind`의 차이는 무엇인가요?
**A**: `Map`은 값을 변환하되 결과가 항상 성공(`A → B`)입니다. `Bind`는 값을 변환하면서 새로운 `FinResponse`를 반환하므로(`A → FinResponse<B>`), 변환 중 실패가 발생할 수 있습니다. Railway-Oriented Programming에서 `Map`은 직선 경로, `Bind`는 분기 가능한 경로에 해당합니다.

### Q4: LINQ `from ... select` 구문은 실전에서 자주 사용되나요?
**A**: 여러 `FinResponse`를 **연쇄적으로 조합**할 때 유용합니다. 중첩된 `Bind` 호출보다 LINQ 구문이 읽기 쉬운 경우가 많습니다. 다만 단일 변환에는 `Map`이나 `Bind`를 직접 사용하는 것이 더 간결합니다.

Part 3에서 R1~R4 요구사항을 모두 충족하는 IFinResponse 계층을 완성했습니다. Part 4에서는 이 계층을 활용하여 실제 Pipeline에 타입 제약을 적용합니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Discriminated Union을 abstract record + sealed record로 구현할 수 있다
2. Match, Map, Bind 메서드의 동작 원리를 설명할 수 있다
3. LINQ 지원을 위한 Select, SelectMany를 구현할 수 있다
4. 암시적 변환으로 간결한 API를 제공하는 방법을 이해할 수 있다
5. 1장~4장의 모든 인터페이스가 하나의 타입으로 통합되는 과정을 설명할 수 있다
6. ThrowIfFail, IfFail, IfSucc로 값을 추출하고 사이드 이펙트를 실행할 수 있다
7. MapFail, BiMap, BiBind, BindFail로 에러 트랙을 조작할 수 있다
8. Boolean 및 Choice 연산자를 활용하여 간결한 조건 분기를 작성할 수 있다

## 프로젝트 구조

```
05-FinResponse-Discriminated-Union/
├── FinResponseDiscriminatedUnion/
│   ├── FinResponseDiscriminatedUnion.csproj
│   ├── IFinResponse.cs
│   ├── FinResponse.cs
│   └── Program.cs
├── FinResponseDiscriminatedUnion.Tests.Unit/
│   ├── FinResponseDiscriminatedUnion.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseDiscriminatedUnionTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FinResponseDiscriminatedUnion

# 테스트 실행
dotnet test --project FinResponseDiscriminatedUnion.Tests.Unit
```

