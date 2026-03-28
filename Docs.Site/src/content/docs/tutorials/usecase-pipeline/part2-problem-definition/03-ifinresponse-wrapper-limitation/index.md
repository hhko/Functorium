---
title: "IFinResponse 래퍼 한계"
---

## 개요

래퍼 인터페이스를 도입하면 리플렉션 3곳을 1곳으로 줄일 수 있습니다. 그렇다면 나머지 1곳도 제거할 수 있을까요? 이 장에서는 래퍼 접근 방식이 어디까지 유효하고, 어디서 한계에 부딪히는지 분석합니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. 래퍼 인터페이스가 리플렉션을 3곳에서 1곳으로 줄이는 원리를 설명할 수 있습니다
2. `CreateFail`이 래퍼 접근 방식에서도 해결되지 않는 이유를 이해할 수 있습니다
3. 이중 인터페이스 문제(비즈니스 + 래퍼)의 설계 복잡성을 설명할 수 있습니다
4. 완전한 해결을 위해 필요한 요구사항(4장)을 도출할 수 있습니다

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

두 접근 방식을 나란히 놓으면, 래퍼가 해결한 부분과 여전히 남아 있는 문제가 명확해집니다.

| 항목 | 직접 사용 (2장) | 래퍼 사용 (3장) |
|------|:---------------:|:---------------:|
| IsSucc 접근 | 리플렉션 | is 캐스팅 |
| Error 추출 | 리플렉션 | 인터페이스 멤버 |
| CreateFail | 리플렉션 | **여전히 불가** |
| 리플렉션 횟수 | 3곳 | 1곳 |

## FAQ

### Q1: 래퍼 접근 방식에서 `is` 캐스팅이 리플렉션보다 나은 이유는 무엇인가요?
**A**: `is` 캐스팅은 CLR의 타입 시스템이 직접 수행하는 연산으로, 리플렉션(`GetType().GetProperty()`)보다 **수십 배 빠릅니다**. 또한 `IFinResponseWrapper` 인터페이스의 멤버는 컴파일 타임에 확인되므로, 프로퍼티 이름 오타 같은 실수도 방지됩니다.

### Q2: `CreateFail`을 래퍼에 추가하면 해결되지 않나요?
**A**: `CreateFail`은 **정적 팩토리 메서드**이므로 인스턴스 인터페이스에 추가할 수 없습니다. `IFinResponseWrapper`에 `CreateFail` 인스턴스 메서드를 추가할 수는 있지만, Pipeline에서 아직 응답 인스턴스가 없는 상태에서 실패 응답을 생성해야 하는 경우(Validation 실패)에는 호출할 인스턴스 자체가 없습니다. 이것이 `static abstract`와 CRTP가 필요한 이유입니다.

### Q3: 이중 인터페이스 문제란 구체적으로 무엇인가요?
**A**: 래퍼 방식에서는 응답 타입이 비즈니스 마커(`IResponse`)와 Fin 상태 노출(`IFinResponseWrapper`)을 **별도로** 구현해야 합니다. 이는 설계 복잡성을 높이고, 두 인터페이스를 모두 구현하지 않은 타입은 Pipeline을 통과할 수 없는 문제를 만듭니다. Part 3의 `FinResponse<A>`는 이를 **하나의 타입**으로 통합합니다.

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

지금까지의 시도를 종합하여, 응답 타입 시스템이 충족해야 할 4가지 요구사항과 Pipeline별 필요 능력 매트릭스를 정리합니다.

→ [2.4장: 요구사항 정리](../04-pipeline-requirements-summary.md)

