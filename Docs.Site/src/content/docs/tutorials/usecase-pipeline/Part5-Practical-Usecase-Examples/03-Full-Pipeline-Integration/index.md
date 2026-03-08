---
title: "전체 흐름 통합"
---

## 개요

이 튜토리얼의 최종 목표는 7개 Pipeline이 리플렉션 없이 타입 안전하게 동작하는 것이었습니다. 이 장에서는 Part 4에서 학습한 **7개의 Pipeline을 모두 연결**하여 전체 요청 처리 흐름을 시뮬레이션합니다. 실제 Mediator Pipeline은 DI(의존성 주입)를 통해 자동으로 등록되지만, 여기서는 학습 목적으로 수동 호출하여 각 Pipeline의 역할과 실행 순서를 명확하게 이해합니다.

```
Pipeline 실행 순서 (Command 성공 시):

1. Exception Pipeline     ─ try { ... } catch
2. Validation Pipeline     ─ 유효성 검사
3. Logging Pipeline        ─ 요청 로그 기록
4. Tracing Pipeline        ─ Activity 시작
5. Metrics Pipeline        ─ 요청 카운트 증가
6. Transaction Pipeline    ─ BEGIN (Command만)
7. Handler                 ─ 비즈니스 로직 실행
6. Transaction Pipeline    ─ COMMIT/ROLLBACK
5. Metrics Pipeline        ─ 응답 카운트 증가
4. Tracing Pipeline        ─ Activity 종료
3. Logging Pipeline        ─ 결과 로그 기록
```

## 핵심 개념

### 1. Pipeline 실행 순서

Mediator Pipeline은 **러시안 인형(Matryoshka)처럼** 중첩됩니다. 가장 바깥쪽 Pipeline이 먼저 실행되고, 안쪽으로 들어간 후, 다시 바깥으로 나오면서 후처리합니다.

```
Exception → Validation → Logging → Tracing → Metrics → Transaction → Handler
                                                                        ↓
Exception ← Validation ← Logging ← Tracing ← Metrics ← Transaction ← 결과
```

### 2. Pipeline별 제약 조건 요약

Part 4에서 개별적으로 학습한 제약 조건을 한눈에 정리하면 다음과 같습니다.

| Pipeline | 제약 조건 | 필요 능력 |
|----------|-----------|-----------|
| Exception | `IFinResponseFactory<TResponse>` | CreateFail |
| Validation | `IFinResponseFactory<TResponse>` | CreateFail |
| Logging | `IFinResponse, IFinResponseFactory<TResponse>` | Read + Create |
| Tracing | `IFinResponse, IFinResponseFactory<TResponse>` | Read + Create |
| Metrics | `IFinResponse, IFinResponseFactory<TResponse>` | Read + Create |
| Transaction | `IFinResponse, IFinResponseFactory<TResponse>` | Read + Create |
| Caching | `IFinResponse, IFinResponseFactory<TResponse>` | Read + Create |

### 3. Command vs Query 분기

Transaction Pipeline은 `ICommandRequest`인 경우에만 활성화됩니다. Query 요청은 Transaction을 건너뜁니다.

```csharp
// Transaction Pipeline (Command only)
if (isCommand)
    ExecutionLog.Add("Transaction: BEGIN");

// Handler 실행 후
if (isCommand)
{
    if (response.IsSucc)
        ExecutionLog.Add("Transaction: COMMIT");
    else
        ExecutionLog.Add("Transaction: ROLLBACK");
}
```

### 4. 에러 전파 패턴

각 Pipeline은 실패 시 **즉시 반환**합니다. Validation이 실패하면 Handler까지 도달하지 않습니다.

```
Validation 실패 시:
  Exception → Validation → FAIL → 즉시 반환
                                    ↓
  Exception ← Metrics ← Tracing ← Logging ← 실패 응답
```

### 5. ExecutionLog를 통한 흐름 추적

`PipelineOrchestrator`는 `ExecutionLog`에 각 단계의 실행 기록을 남깁니다. 이를 통해 테스트에서 실행 순서와 조건부 분기를 검증할 수 있습니다.

```csharp
sut.ExecutionLog.ShouldContain("Validation: PASS");
sut.ExecutionLog.ShouldContain("Transaction: COMMIT");
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. 7개 Pipeline의 실행 순서와 중첩 구조를 설명할 수 있다
2. Command와 Query에 따른 Pipeline 분기를 이해할 수 있다
3. 각 Pipeline의 제약 조건이 어떤 능력을 제공하는지 설명할 수 있다
4. 실패 시 Pipeline 흐름이 어떻게 단축(short-circuit)되는지 설명할 수 있다

## 프로젝트 구조

```
03-Full-Pipeline-Integration/
├── FullPipelineIntegration/
│   ├── FullPipelineIntegration.csproj
│   ├── PipelineOrchestrator.cs
│   └── Program.cs
├── FullPipelineIntegration.Tests.Unit/
│   ├── FullPipelineIntegration.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── PipelineOrchestratorTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FullPipelineIntegration

# 테스트 실행
dotnet test --project FullPipelineIntegration.Tests.Unit
```

## FAQ

### Q1: Pipeline 실행 순서를 변경하면 동작에 영향이 있나요?
**A**: 네. 실행 순서는 중요합니다. Exception Pipeline이 가장 바깥에 있어야 모든 예외를 포착하고, Validation Pipeline이 Handler보다 앞에 있어야 유효하지 않은 요청이 Handler에 도달하지 않습니다. 순서를 변경하면 트랜잭션이 시작되기 전에 예외가 발생하거나, 캐시된 응답에 로깅이 누락되는 등의 문제가 발생할 수 있습니다.

### Q2: 7개 Pipeline이 모두 `FinResponse<T>`를 통해 동작하는데, Pipeline마다 다른 제약을 사용하는 이유는 무엇인가요?
**A**: 인터페이스 분리 원칙(ISP)에 따라 각 Pipeline은 **자신이 필요한 최소한의 능력만** 제약합니다. Validation은 `IFinResponseFactory`만, Logging은 `IFinResponse` + `IFinResponseFactory`를 제약합니다. `FinResponse<T>`가 모든 인터페이스를 구현하므로 실제로는 모든 Pipeline을 통과할 수 있지만, **코드의 의도가 제약으로 명확히 표현**됩니다.

### Q3: Validation 실패 시 Transaction Pipeline은 어떻게 되나요?
**A**: Validation Pipeline이 실패하면 `next()`를 호출하지 않고 즉시 실패 응답을 반환합니다(**단축 평가**). 이후의 Transaction Pipeline과 Handler는 실행되지 않으므로, **불필요한 트랜잭션 시작이 방지**됩니다. 응답은 외부 Pipeline(Logging, Tracing 등)을 거치며 결과가 기록됩니다.

### Q4: 실전에서 이 수동 오케스트레이션 대신 어떻게 Pipeline을 등록하나요?
**A**: Mediator 프레임워크(Mediator, MediatR 등)의 **DI(의존성 주입) 등록**을 사용합니다. `services.AddMediator()` 호출 시 Pipeline을 등록 순서대로 체인에 연결합니다. 이 장의 수동 호출은 학습 목적으로 각 Pipeline의 역할과 실행 순서를 명확히 이해하기 위한 것입니다.

---

인터페이스 계층 설계부터 Pipeline 제약, 실전 Usecase 적용, 그리고 전체 통합까지 — `FinResponse<T>`가 리플렉션 없이 타입 안전한 Pipeline을 가능하게 하는 여정을 마칩니다.

