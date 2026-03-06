---
title: "IFinResponse 계층 참조"
---

## 개요

이 부록은 Functorium의 IFinResponse 인터페이스 계층 전체를 한눈에 볼 수 있도록 정리한 참조 문서입니다. 각 인터페이스의 역할, 제약 조건, 그리고 `FinResponse<A>`에서의 구현 방식을 상세하게 설명합니다.

---

## 인터페이스 계층 다이어그램

```
IFinResponse                              비제네릭 마커 (IsSucc/IsFail)
├── IFinResponse<out A>                   공변 인터페이스 (읽기 전용)
│
IFinResponseFactory<TSelf>                CRTP 팩토리 (CreateFail)
│
IFinResponseWithError                     에러 접근 (Error 속성)
│
FinResponse<A>                            Discriminated Union
├── : IFinResponse<A>                     공변 인터페이스 구현
├── : IFinResponseFactory<FinResponse<A>> CRTP 팩토리 구현
│
├── sealed record Succ(A Value)           성공 케이스
│
└── sealed record Fail(Error Error)       실패 케이스
    └── : IFinResponseWithError           Fail에서만 에러 접근
```

---

## 인터페이스 상세

### 1. IFinResponse (비제네릭 마커)

```csharp
public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}
```

| 항목 | 설명 |
|------|------|
| **역할** | Pipeline에서 성공/실패 상태를 읽기 위한 최소 인터페이스 |
| **변성** | 없음 (비제네릭) |
| **사용 Pipeline** | Logging, Tracing, Metrics, Transaction, Caching |
| **제약 조건으로 사용** | `where TResponse : IFinResponse` |

### 2. IFinResponse\<out A\> (공변 인터페이스)

```csharp
public interface IFinResponse<out A> : IFinResponse
{
}
```

| 항목 | 설명 |
|------|------|
| **역할** | 공변성을 지원하는 제네릭 확장 |
| **변성** | 공변 (`out A`) |
| **상속** | `IFinResponse`를 상속 |
| **의미** | `FinResponse<Dog>`을 `IFinResponse<Animal>`로 참조 가능 |

### 3. IFinResponseFactory\<TSelf\> (CRTP 팩토리)

```csharp
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}
```

| 항목 | 설명 |
|------|------|
| **역할** | Pipeline에서 실패 응답을 생성하기 위한 팩토리 |
| **패턴** | CRTP (Curiously Recurring Template Pattern) |
| **핵심 메서드** | `static abstract TSelf CreateFail(Error error)` |
| **사용 Pipeline** | Validation, Exception (Create-Only), 그리고 모든 Read+Create Pipeline |
| **제약 조건으로 사용** | `where TResponse : IFinResponseFactory<TResponse>` |

### 4. IFinResponseWithError (에러 접근)

```csharp
public interface IFinResponseWithError
{
    Error Error { get; }
}
```

| 항목 | 설명 |
|------|------|
| **역할** | 실패 시 Error 정보에 접근 |
| **구현** | `FinResponse<A>.Fail`에서만 구현 |
| **사용 방법** | `if (response is IFinResponseWithError fail) { ... fail.Error ... }` |
| **사용 Pipeline** | Logging Pipeline (에러 메시지 기록) |

---

## FinResponse\<A\> 구현 상세

### 추상 레코드

```csharp
public abstract record FinResponse<A> : IFinResponse<A>, IFinResponseFactory<FinResponse<A>>
```

### Succ 케이스

```csharp
public sealed record Succ(A Value) : FinResponse<A>
{
    public override bool IsSucc => true;
    public override bool IsFail => false;
}
```

### Fail 케이스

```csharp
public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError
{
    public override bool IsSucc => false;
    public override bool IsFail => true;
    Error IFinResponseWithError.Error => Error;
}
```

### 주요 메서드

| 메서드 | 시그니처 | 설명 |
|--------|----------|------|
| `Match` | `B Match<B>(Func<A, B> Succ, Func<Error, B> Fail)` | 성공/실패 분기 처리 |
| `Map` | `FinResponse<B> Map<B>(Func<A, B> f)` | 성공 값 변환 |
| `Bind` | `FinResponse<B> Bind<B>(Func<A, FinResponse<B>> f)` | 모나딕 바인드 |
| `Select` | `FinResponse<B> Select<B>(Func<A, B> f)` | LINQ select 지원 |
| `SelectMany` | `FinResponse<C> SelectMany<B, C>(...)` | LINQ from/select 지원 |
| `ThrowIfFail` | `A ThrowIfFail()` | 실패 시 예외, 성공 시 값 반환 |
| `IfFail` | `A IfFail(A alternative)` | 실패 시 대체 값 반환 |
| `CreateFail` | `static FinResponse<A> CreateFail(Error error)` | CRTP 팩토리 구현 |

### 암시적 변환 연산자

```csharp
// 값 → FinResponse (Succ)
public static implicit operator FinResponse<A>(A value) => new Succ(value);

// Error → FinResponse (Fail)
public static implicit operator FinResponse<A>(Error error) => new Fail(error);
```

### 정적 팩토리 (FinResponse 클래스)

```csharp
public static class FinResponse
{
    public static FinResponse<A> Succ<A>(A value) => new FinResponse<A>.Succ(value);
    public static FinResponse<A> Fail<A>(Error error) => new FinResponse<A>.Fail(error);
}
```

---

## Pipeline별 제약 조건 매트릭스

```
Pipeline                    TResponse 제약 조건                              능력
──────────────────────────  ───────────────────────────────────────────────  ────────────
Validation Pipeline         IFinResponseFactory<TResponse>                   CreateFail
Exception Pipeline          IFinResponseFactory<TResponse>                   CreateFail
Logging Pipeline            IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Tracing Pipeline            IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Metrics Pipeline            IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Transaction Pipeline        IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Caching Pipeline            IFinResponse, IFinResponseFactory<TResponse>     Read + Create
```

