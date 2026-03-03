---
title: "IFinResponse 비제네릭 마커"
---

## 개요

**비제네릭 마커 인터페이스(Non-Generic Marker Interface)는** 제네릭 타입 파라미터 없이도 공통 속성에 접근할 수 있게 하는 패턴입니다. `IFinResponse`에 `IsSucc`과 `IsFail` 속성을 정의하면, Pipeline에서 응답의 제네릭 타입 `T`를 몰라도 성공/실패를 확인할 수 있습니다.

```
IFinResponse          ← 비제네릭 마커
├── IsSucc: bool
└── IsFail: bool
```

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

마커 인터페이스를 사용하면 **직접 접근**이 가능합니다:

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

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. 비제네릭 마커 인터페이스의 역할을 설명할 수 있다
2. 마커 인터페이스가 리플렉션을 제거하는 원리를 이해할 수 있다
3. Pipeline에서 `where TResponse : IFinResponse` 제약의 의미를 설명할 수 있다
4. 성공/실패 읽기가 IFinResponse 계층의 첫 번째 요구사항임을 이해할 수 있다

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

[← 이전: 8장 요구사항 정리](../../Part2-Problem-Definition/04-pipeline-requirements-summary.md) | [다음: 10장 IFinResponse<out A> 공변 인터페이스 →](../02-IFinResponse-Covariant/)
