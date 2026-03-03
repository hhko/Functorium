---
title: "파이프라인 요구사항 정리"
---

## 개요

5장에서 7장까지 Mediator Pipeline의 구조, `Fin<T>` 직접 사용의 한계, 래퍼 접근 방식의 한계를 분석했습니다. 이 장에서는 분석 결과를 바탕으로 **응답 타입 시스템의 4가지 요구사항**을 정리하고, 각 접근 방식이 이를 얼마나 충족하는지 비교합니다.

---

## 1. 응답 타입 시스템의 4가지 요구사항

Pipeline에서 응답 타입을 안전하게 처리하려면 다음 4가지 요구사항이 충족되어야 합니다.

### R1: Pipeline에서 성공/실패 상태를 직접 읽을 수 있어야 함 (리플렉션 없이)

Logging, Tracing, Metrics Pipeline은 응답의 성공/실패 상태를 확인해야 합니다. 이 정보는 **컴파일 타임에 보장된 인터페이스 멤버**로 접근 가능해야 합니다.

```csharp
// 요구사항: 리플렉션 없이 직접 접근
if (response.IsSucc)
    LogSuccess();
else
    LogFailure();
```

### R2: Pipeline에서 실패 응답을 직접 생성할 수 있어야 함 (static abstract)

Validation, Exception Pipeline은 실패 응답을 **직접 생성**해야 합니다. 이를 위해 `static abstract` 팩토리 메서드가 필요합니다.

```csharp
// 요구사항: 타입 안전한 실패 응답 생성
return TResponse.CreateFail(Error.New("Validation failed"));
```

### R3: Pipeline에서 에러 정보에 접근할 수 있어야 함

Logging, Tracing Pipeline은 실패 시 **에러 정보**(에러 메시지, 에러 코드 등)에 접근해야 합니다.

```csharp
// 요구사항: 에러 정보 직접 접근
if (response is IFinResponseWithError fail)
    RecordError(fail.Error);
```

### R4: sealed struct(Fin<T>)의 성공/실패를 래핑하지 않고 직접 표현할 수 있어야 함

`Fin<T>`를 별도의 래퍼로 감싸면 이중 인터페이스 문제가 발생합니다. 응답 타입 자체가 성공/실패를 **직접 표현**하는 Discriminated Union이어야 합니다.

```csharp
// 요구사항: 래퍼 없이 직접 표현
FinResponse<string> success = FinResponse.Succ("OK");
FinResponse<string> fail = FinResponse.Fail<string>(Error.New("error"));
```

---

## 2. Pipeline별 필요 능력 매트릭스

각 Pipeline이 응답 타입에 대해 필요로 하는 능력은 다릅니다:

| Pipeline | 읽기 (R1) | 생성 (R2) | 에러 접근 (R3) | 직접 표현 (R4) |
|----------|:---------:|:---------:|:--------------:|:--------------:|
| Validation | | O | | |
| Exception | | O | | |
| Logging | O | | O | |
| Tracing | O | | O | |
| Metrics | O | | O | |
| Transaction | O | | | |
| Caching | O | | | |

**핵심 관찰:**
- **Validation/Exception**: 실패 응답 **생성**만 필요 (Create-Only)
- **Logging/Tracing/Metrics**: 성공/실패 **읽기** + 에러 정보 접근 필요
- **Transaction/Caching**: 성공/실패 **읽기**만 필요

이 차이가 Part 3에서 설계할 인터페이스 계층의 근거가 됩니다.

---

## 3. 접근 방식 비교

| 접근 방식 | R1 | R2 | R3 | R4 | 리플렉션 |
|-----------|:--:|:--:|:--:|:--:|:--------:|
| Fin\<T\> 직접 (6장) | X | X | X | O | 3곳 |
| IFinResponse 래퍼 (7장) | △ | X | O | X | 1곳 |
| IFinResponse 계층 (Part 3) | O | O | O | O | 0곳 |

### Fin<T> 직접 사용 (6장)

- **장점**: `Fin<T>` 자체가 성공/실패를 직접 표현 (R4 충족)
- **단점**: sealed struct라 제약 불가, 리플렉션 3곳 필요

### IFinResponse 래퍼 (7장)

- **장점**: 리플렉션을 1곳(is 캐스팅)으로 감소, 에러 접근 가능 (R3 충족)
- **단점**: `CreateFail` 불가 (R2 미충족), 이중 인터페이스 (R4 미충족)
- R1이 △인 이유: `is` 캐스팅으로 접근 가능하지만, 컴파일 타임 보장이 아님

### IFinResponse 계층 (Part 3에서 설계)

- **장점**: 4가지 요구사항 모두 충족, 리플렉션 0곳
- 핵심 아이디어: **인터페이스 분리 원칙(ISP)** + **static abstract 멤버** + **CRTP 패턴**

---

## 4. Part 3에서의 해결 방향

Part 3에서는 다음 인터페이스 계층을 하나씩 설계합니다:

| 인터페이스 | 충족 요구사항 | 핵심 멤버 |
|-----------|:------------:|----------|
| `IFinResponse` | R1 | `IsSucc`, `IsFail` |
| `IFinResponse<out A>` | R1 + 공변성 | 값 접근 |
| `IFinResponseFactory<TSelf>` | R2 | `static abstract CreateFail(Error)` |
| `IFinResponseWithError` | R3 | `Error` 속성 |
| `FinResponse<A>` | R4 | Succ/Fail Discriminated Union |

각 인터페이스는 **하나의 요구사항**을 해결하며, Pipeline은 **필요한 인터페이스만** 제약 조건으로 사용합니다.

```csharp
// Create-Only: Validation, Exception
where TResponse : IFinResponseFactory<TResponse>

// Read + Create: Logging, Tracing, Metrics, Transaction, Caching
where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

---

[← 이전: 7장 IFinResponse 래퍼의 한계](03-IFinResponse-Wrapper-Limitation/) | [다음: Part 3 →](../../Part3-IFinResponse-Hierarchy/01-IFinResponse-Marker/)
