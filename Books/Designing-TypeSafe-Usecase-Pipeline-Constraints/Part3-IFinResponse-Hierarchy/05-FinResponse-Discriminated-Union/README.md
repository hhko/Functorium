# 13장: FinResponse<A> Discriminated Union

## 개요

9장부터 12장까지 설계한 모든 인터페이스를 **하나의 타입**으로 통합합니다. `FinResponse<A>`는 `Succ`/`Fail` sealed record로 구성된 **Discriminated Union**이며, `IFinResponse<A>`, `IFinResponseFactory<FinResponse<A>>`를 모두 구현합니다. Match, Map, Bind 메서드와 암시적 변환, LINQ 지원까지 포함한 완전한 응답 타입입니다.

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

`FinResponse<A>`는 `abstract record`이며, `Succ`와 `Fail` 두 가지 `sealed record`만 존재합니다. 새로운 케이스를 추가할 수 없으므로, 패턴 매칭이 **완전(exhaustive)**합니다.

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
FinResponse<int> response = FinResponseFactory.Succ(42);

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
    v > 0 ? FinResponseFactory.Succ(v * 2) : FinResponseFactory.Fail<int>(Error.New("negative")));
```

### 4. LINQ 지원

`Select`와 `SelectMany`를 구현하여 LINQ `from ... select` 구문을 지원합니다.

```csharp
var result = from x in FinResponseFactory.Succ(3)
             from y in FinResponseFactory.Succ(4)
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

`FinResponse<A>`는 9장~12장의 모든 인터페이스를 구현합니다:

| 인터페이스 | 역할 | 구현 |
|-----------|------|------|
| `IFinResponse` | 성공/실패 읽기 | `IsSucc`, `IsFail` |
| `IFinResponse<out A>` | 공변 접근 | 상속 |
| `IFinResponseFactory<TSelf>` | 실패 생성 | `CreateFail` |
| `IFinResponseWithError` | 에러 접근 | `Fail`에서만 구현 |

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Discriminated Union을 abstract record + sealed record로 구현할 수 있다
2. Match, Map, Bind 메서드의 동작 원리를 설명할 수 있다
3. LINQ 지원을 위한 Select, SelectMany를 구현할 수 있다
4. 암시적 변환으로 간결한 API를 제공하는 방법을 이해할 수 있다
5. 9장~12장의 모든 인터페이스가 하나의 타입으로 통합되는 과정을 설명할 수 있다

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

---

[← 이전: 12장 IFinResponseWithError 에러 접근](../04-IFinResponseWithError/README.md) | [다음: Part 4 Pipeline 제약 패턴 적용 →](../../Part4-Pipeline-Constraint-Patterns/01-Create-Only-Constraint/README.md)
