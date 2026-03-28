---
title: "IFinResponse 공변 인터페이스"
---

## 개요

1장에서 만든 비제네릭 `IFinResponse` 마커는 성공/실패를 확인할 수 있지만, **값의 타입 정보**는 제공하지 않습니다. Part 1에서 학습한 공변성을 적용하여, 이 장에서는 `out A` 키워드를 사용한 **공변 인터페이스** `IFinResponse<out A>`를 추가합니다. 파생 타입에서 기본 타입으로의 안전한 대입을 가능하게 합니다.

```
IFinResponse                    ← 비제네릭 마커 (1장)
└── IFinResponse<out A>         ← 공변 인터페이스 (이번 장)
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `out A` 키워드를 사용하여 공변 인터페이스를 선언할 수 있습니다
2. 공변성에 의해 `IFinResponse<string>`을 `IFinResponse<object>`에 대입할 수 있는 이유를 설명할 수 있습니다
3. 비제네릭 마커와 제네릭 공변 인터페이스의 상속 관계를 이해할 수 있습니다
4. Pipeline에서 공변성이 제공하는 유연성을 설명할 수 있습니다

## 핵심 개념

### 1. 공변 인터페이스: `out A`

`out` 키워드는 타입 파라미터 `A`가 **출력 위치에서만** 사용됨을 선언합니다. 이를 통해 `IFinResponse<string>`을 `IFinResponse<object>`에 대입할 수 있습니다.

```csharp
public interface IFinResponse<out A> : IFinResponse
{
}
```

`IFinResponse<out A>`의 본문이 비어 있는 것은 의도적입니다. 이 인터페이스의 역할은 값 멤버를 노출하는 것이 아니라, **제네릭 타입 파라미터 `A`의 공변성을 선언하는 것**입니다. Pipeline에서 필요한 `IsSucc`/`IsFail`은 부모 인터페이스 `IFinResponse`가 이미 제공하고, 값 접근 멤버(`Value`, `Match`, `Map` 등)는 구현체인 `FinResponse<A>`에서 제공됩니다. 인터페이스를 역할별로 분리하여 각 계층이 하나의 책임만 갖도록 설계한 결과입니다.

### 2. 공변 대입

`string`은 `object`의 하위 타입이므로, 공변성에 의해 다음 대입이 가능합니다:

```csharp
IFinResponse<string> stringResponse = CovariantResponse<string>.Succ("Hello");

// 공변성: IFinResponse<string> → IFinResponse<object> 대입 가능
IFinResponse<object> objectResponse = stringResponse;
```

### 3. 비제네릭 마커로도 대입 가능

`IFinResponse<out A>`는 `IFinResponse`를 상속하므로, 비제네릭 마커 타입으로도 대입할 수 있습니다:

```csharp
IFinResponse<string> stringResponse = CovariantResponse<string>.Succ("Hello");

// IFinResponse<string> → IFinResponse 대입 가능 (상속)
IFinResponse nonGeneric = stringResponse;
nonGeneric.IsSucc; // true
```

### 4. Pipeline에서의 활용

공변성을 활용하면 Pipeline에서 응답을 더 유연하게 처리할 수 있습니다. 예를 들어, `IFinResponse<object>`를 받는 로깅 Pipeline은 모든 `IFinResponse<T>` 응답을 처리할 수 있습니다.

```csharp
public void LogAnyResponse(IFinResponse<object> response)
{
    Console.WriteLine(response.IsSucc ? "Success" : "Fail");
}
```

## FAQ

### Q1: `IFinResponse<out A>`에 값을 반환하는 멤버가 없는데, 공변 인터페이스의 의미가 있나요?
**A**: 현재 `IFinResponse<out A>`에는 값 접근 멤버가 명시적으로 없지만, 타입 파라미터 `A`를 통해 **타입 정보를 전달**합니다. 공변 선언(`out`)은 `IFinResponse<string>`을 `IFinResponse<object>`에 대입할 수 있게 하여, Pipeline에서 다양한 응답 타입을 일반적으로 처리할 수 있는 기반을 제공합니다.

### Q2: `IFinResponse`(비제네릭)와 `IFinResponse<out A>`(공변)를 왜 분리하나요?
**A**: Pipeline에서 **성공/실패 상태만** 필요한 경우 비제네릭 `IFinResponse`로 충분합니다. 타입 파라미터 `A`를 도입하면 불필요한 제네릭 전파가 발생합니다. 분리함으로써 각 Pipeline이 자신에게 필요한 **최소한의 타입 정보만** 요구할 수 있습니다.

### Q3: 공변성이 실제 Pipeline 코드에서 어떤 차이를 만드나요?
**A**: `IFinResponse<out A>`의 공변성 덕분에, `IFinResponse<ProductDto>`를 반환하는 Handler의 응답을 `IFinResponse<object>`를 받는 로깅 유틸리티에서 그대로 사용할 수 있습니다. 공변성이 없으면 매번 명시적 캐스팅이 필요하여 코드가 복잡해집니다.

## 프로젝트 구조

```
02-IFinResponse-Covariant/
├── FinResponseCovariant/
│   ├── FinResponseCovariant.csproj
│   ├── IFinResponse.cs
│   ├── CovariantResponse.cs
│   └── Program.cs
├── FinResponseCovariant.Tests.Unit/
│   ├── FinResponseCovariant.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseCovariantTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FinResponseCovariant

# 테스트 실행
dotnet test --project FinResponseCovariant.Tests.Unit
```

---

Pipeline이 응답을 읽을 수 있게 되었으니, 이제 요구사항 R2에 도전합니다. CRTP와 `static abstract`를 사용하여 리플렉션 없이 실패 응답을 생성하는 팩토리 인터페이스를 설계합니다.

→ [3.3장: IFinResponseFactory CRTP 팩토리](../03-IFinResponseFactory-CRTP/)

