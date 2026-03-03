---
title: "IFinResponse 래퍼의 한계"
---

## 개요

6장에서 `Fin<T>`를 직접 사용하면 리플렉션이 3곳에서 필요하다는 것을 확인했습니다. 이 장에서는 **래퍼 인터페이스**를 도입하여 리플렉션을 1곳으로 줄이는 접근 방식과, 이 접근 방식이 여전히 가진 한계를 분석합니다.

## 핵심 개념

### 1. 래퍼 인터페이스로 리플렉션 줄이기

`Fin<T>`의 상태를 노출하는 래퍼 인터페이스를 정의합니다:

```csharp
public interface IFinResponseWrapper
{
    bool IsSucc { get; }
    bool IsFail { get; }
    Error GetError();
}
```

이렇게 하면 Pipeline에서 `is IFinResponseWrapper`로 **캐스팅**하여 접근할 수 있습니다:

```csharp
if (response is IFinResponseWrapper wrapper)
{
    if (wrapper.IsSucc)
        LogSuccess();
    else
        LogError(wrapper.GetError());
}
```

리플렉션이 `IsSucc` 프로퍼티 조회와 `Error` 추출에서 사라지고, `is` 캐스팅 1곳만 남습니다.

### 2. CreateFail은 여전히 해결 불가

래퍼 접근 방식의 핵심 한계는 **실패 응답 생성**입니다. Pipeline에서 Validation 실패 시 실패 응답을 생성해야 하는데:

```csharp
// Pipeline은 TResponse를 반환해야 함
// 하지만 TResponse의 구체 타입을 모르므로 생성할 수 없음
public static TResponse CreateFail<TResponse>(Error error) => ???
```

`IFinResponseWrapper`에는 팩토리 메서드가 없으므로, 실패 응답 생성에는 여전히 리플렉션이나 다른 우회 방법이 필요합니다.

### 3. 이중 인터페이스 문제

래퍼 접근 방식은 두 개의 인터페이스를 요구합니다:

| 인터페이스 | 역할 | 사용처 |
|-----------|------|--------|
| `IResponse` | 비즈니스 응답 마커 | Handler, Usecase |
| `IFinResponseWrapper` | Fin 상태 노출 | Pipeline |

응답 타입은 두 인터페이스를 모두 구현해야 하며, 이는 설계 복잡성을 높입니다:

```csharp
public record ResponseWrapper<T>(T? Value, Error? Error)
    : IResponse, IFinResponseWrapper  // 두 개의 인터페이스 필요
    where T : IResponse
```

### 4. 래퍼의 한계 요약

| 항목 | 직접 사용 (6장) | 래퍼 사용 (7장) |
|------|:---------------:|:---------------:|
| IsSucc 접근 | 리플렉션 | is 캐스팅 |
| Error 추출 | 리플렉션 | 인터페이스 멤버 |
| CreateFail | 리플렉션 | **여전히 불가** |
| 리플렉션 횟수 | 3곳 | 1곳 |

래퍼는 리플렉션을 줄이지만, **완전히 제거하지는 못합니다**.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. 래퍼 인터페이스가 리플렉션을 3곳에서 1곳으로 줄이는 원리를 설명할 수 있다
2. `CreateFail`이 래퍼 접근 방식에서도 해결되지 않는 이유를 이해할 수 있다
3. 이중 인터페이스 문제(비즈니스 + 래퍼)의 설계 복잡성을 설명할 수 있다
4. 완전한 해결을 위해 필요한 요구사항(8장)을 도출할 수 있다

## 프로젝트 구조

```
03-IFinResponse-Wrapper-Limitation/
├── FinResponseWrapperLimitation/
│   ├── FinResponseWrapperLimitation.csproj
│   ├── IFinResponseWrapper.cs
│   └── Program.cs
├── FinResponseWrapperLimitation.Tests.Unit/
│   ├── FinResponseWrapperLimitation.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseWrapperTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FinResponseWrapperLimitation

# 테스트 실행
dotnet test --project FinResponseWrapperLimitation.Tests.Unit
```

---

[← 이전: 6장 Fin<T> 직접 사용의 한계](../02-Fin-Direct-Limitation/) | [다음: 8장 Pipeline 요구사항 정리 →](../04-pipeline-requirements-summary.md)
