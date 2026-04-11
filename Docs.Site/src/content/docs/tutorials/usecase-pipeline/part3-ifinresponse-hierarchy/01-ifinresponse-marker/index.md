---
title: "IFinResponse Non-Generic Marker"
---

## 개요

Part 2의 4장에서 정의한 **요구사항 R1**: Pipeline에서 성공/실패 상태를 리플렉션 없이 읽을 수 있어야 합니다. 이 요구사항을 해결하는 첫 번째 인터페이스가 비제네릭 마커 `IFinResponse`입니다. `IsSucc`과 `IsFail` 속성을 정의하면, Pipeline에서 응답의 제네릭 타입 `T`를 몰라도 성공/실패를 확인할 수 있습니다.

```
IFinResponse          ← 비제네릭 마커
├── IsSucc: bool
└── IsFail: bool
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. 비제네릭 마커 인터페이스의 역할을 설명할 수 있습니다
2. 마커 인터페이스가 리플렉션을 제거하는 원리를 이해할 수 있습니다
3. Pipeline에서 `where TResponse : IFinResponse` 제약의 의미를 설명할 수 있습니다
4. 성공/실패 읽기가 IFinResponse 계층의 첫 번째 요구사항임을 이해할 수 있습니다

## 핵심 개념

### 1. 비제네릭 마커 인터페이스

`IFinResponse`는 제네릭 타입 파라미터가 없는 **비제네릭** 인터페이스입니다. 성공/실패 여부만 노출하므로, Pipeline에서 `T`가 무엇인지 알 필요 없이 응답 상태를 확인할 수 있습니다.

```csharp
public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}
```

### 2. 리플렉션 없는 읽기 접근

마커 인터페이스가 없다면 Pipeline에서 응답의 성공/실패를 확인하려면 **리플렉션**이 필요합니다:

```csharp
// 리플렉션 필요 (마커 없을 때)
var isSuccProp = response.GetType().GetProperty("IsSucc");
var isSucc = (bool)isSuccProp!.GetValue(response)!;
```

주목할 점은 `where TResponse : IFinResponse` 제약 한 줄로 리플렉션이 사라진다는 것입니다. 마커 인터페이스를 사용하면 **직접 접근**이 가능합니다:

```csharp
// 직접 접근 (마커 있을 때)
public static string LogResponse<TResponse>(TResponse response)
    where TResponse : IFinResponse
{
    return response.IsSucc ? "Success" : "Fail";
}
```

### 3. Pipeline 제약에서의 역할

Pipeline의 `where TResponse : IFinResponse` 제약을 통해, 컴파일 타임에 `IsSucc`/`IsFail` 접근이 보장됩니다. 이것이 **타입 안전한 Pipeline**의 첫 번째 단계입니다.

```csharp
public class LoggingPipeline<TRequest, TResponse>
    where TResponse : IFinResponse    // IsSucc/IsFail 접근 보장
{
    public TResponse Handle(TRequest request, Func<TResponse> next)
    {
        var response = next();
        Console.WriteLine(response.IsSucc ? "Success" : "Fail");
        return response;
    }
}
```

## FAQ

### Q1: 비제네릭 마커 인터페이스가 제네릭 인터페이스보다 먼저 필요한 이유는 무엇인가요?
**A**: Pipeline에서 응답의 성공/실패를 확인할 때 **값의 타입 `T`를 알 필요가 없습니다**. `IsSucc`/`IsFail`은 타입 `T`와 무관한 정보이므로, 비제네릭 인터페이스로 충분합니다. 제네릭 파라미터가 없으면 Pipeline의 `where` 제약이 더 단순해지고, 모든 응답 타입에 일관되게 적용됩니다.

### Q2: `IFinResponse` 마커 인터페이스만으로 어떤 Pipeline을 구현할 수 있나요?
**A**: 응답의 성공/실패 상태만 확인하면 되는 Pipeline에 사용할 수 있습니다. 예를 들어 Transaction Pipeline은 `response.IsSucc`으로 커밋/롤백을 결정하고, Metrics Pipeline은 성공/실패 카운트를 수집합니다. 하지만 실패 응답을 **생성**해야 하는 Validation Pipeline에는 `IFinResponseFactory`가 추가로 필요합니다.

### Q3: `where TResponse : IFinResponse` 제약을 추가하면 기존 응답 타입에 영향이 있나요?
**A**: `IFinResponse`를 구현하지 않는 기존 응답 타입은 이 제약이 있는 Pipeline을 통과할 수 없습니다. 하지만 이는 의도된 동작입니다. 모든 응답 타입이 `IFinResponse`를 구현하도록 하거나, 최종적으로 `FinResponse<A>` Discriminated Union을 사용하면 자동으로 해결됩니다.

## 프로젝트 구조

```
01-IFinResponse-Marker/
├── FinResponseMarker/
│   ├── FinResponseMarker.csproj
│   ├── IFinResponse.cs
│   ├── SimpleResponse.cs
│   ├── PipelineExample.cs
│   └── Program.cs
├── FinResponseMarker.Tests.Unit/
│   ├── FinResponseMarker.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseMarkerTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FinResponseMarker

# 테스트 실행
dotnet test --project FinResponseMarker.Tests.Unit
```

---

성공/실패를 읽을 수 있지만, 값의 타입 정보는 아직 없습니다. `out A` 키워드를 사용한 공변 인터페이스로 타입 안전한 값 접근을 추가합니다.

→ [3.2장: IFinResponse\<out A\> 공변 인터페이스](../02-IFinResponse-Covariant/)

