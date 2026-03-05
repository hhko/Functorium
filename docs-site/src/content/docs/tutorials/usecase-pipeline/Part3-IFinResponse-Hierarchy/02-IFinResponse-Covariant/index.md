---
title: "IFinResponse 공변 인터페이스"
---

## 개요

9장에서 만든 비제네릭 `IFinResponse` 마커는 성공/실패를 확인할 수 있지만, **값의 타입 정보**는 제공하지 않습니다. 이 장에서는 `out A` 키워드를 사용한 **공변 인터페이스** `IFinResponse<out A>`를 추가하여, 파생 타입에서 기본 타입으로의 안전한 대입을 가능하게 합니다.

```
IFinResponse                    ← 비제네릭 마커 (9장)
└── IFinResponse<out A>         ← 공변 인터페이스 (이번 장)
```

## 핵심 개념

### 1. 공변 인터페이스: `out A`

`out` 키워드는 타입 파라미터 `A`가 **출력 위치에서만** 사용됨을 선언합니다. 이를 통해 `IFinResponse<string>`을 `IFinResponse<object>`에 대입할 수 있습니다.

```csharp
public interface IFinResponse<out A> : IFinResponse
{
}
```

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

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `out A` 키워드를 사용하여 공변 인터페이스를 선언할 수 있다
2. 공변성에 의해 `IFinResponse<string>`을 `IFinResponse<object>`에 대입할 수 있는 이유를 설명할 수 있다
3. 비제네릭 마커와 제네릭 공변 인터페이스의 상속 관계를 이해할 수 있다
4. Pipeline에서 공변성이 제공하는 유연성을 설명할 수 있다

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

