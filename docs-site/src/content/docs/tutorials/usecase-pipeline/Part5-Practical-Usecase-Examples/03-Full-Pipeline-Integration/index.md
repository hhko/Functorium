---
title: "전체 흐름 통합"
---

## 개요

이 장에서는 Part 4에서 학습한 **7개의 Pipeline을 모두 연결**하여 전체 요청 처리 흐름을 시뮬레이션합니다. 실제 Mediator Pipeline은 DI(의존성 주입)를 통해 자동으로 등록되지만, 여기서는 학습 목적으로 수동 호출하여 각 Pipeline의 역할과 실행 순서를 명확하게 이해합니다.

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

