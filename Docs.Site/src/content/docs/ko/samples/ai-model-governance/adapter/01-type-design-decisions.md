---
title: "어댑터 타입 설계 의사결정"
description: "LanguageExt IO 고급 기능(Retry, Timeout, Fork, Bracket) 선택 근거"
---

## 개요

[기술 요구사항](../00-business-requirements/)에서 정의한 4가지 외부 서비스 시나리오에 대해, 어떤 LanguageExt IO 고급 기능을 적용할지와 그 근거를 정리합니다.

## 외부 서비스 요구사항 -> IO 패턴 매핑

| 외부 서비스 | 문제 상황 | 필요한 보장 | 선택된 IO 패턴 |
|-----------|----------|-----------|--------------|
| 모델 헬스 체크 | 간헐적 느린 응답(>10s) | 최대 대기 시간 제한, 타임아웃 시 폴백 | **Timeout + Catch** |
| 모델 모니터링 | 간헐적 503 오류 | 일시적 실패에서 자동 복구, 재시도 간격 조절 | **Retry + Schedule** |
| 병렬 컴플라이언스 | 5개 독립 체크, 순차 실행 시 느림 | 병렬 실행, 모든 결과 수집 | **Fork + awaitAll** |
| 모델 레지스트리 | 세션 기반 리소스 관리 | 예외 발생해도 세션 해제 보장 | **Bracket** |

## 패턴별 설계 의사결정

### 1. Timeout + Catch -- 모델 헬스 체크

**문제:** 헬스 체크 서비스가 간헐적으로 12초 이상 응답 지연. 무한 대기하면 시스템 전체가 느려짐.

**왜 Timeout인가?** 외부 서비스의 응답 시간을 통제할 수 없을 때, 시스템이 허용하는 최대 대기 시간을 선언적으로 설정합니다. LanguageExt의 `Timeout`은 IO 연산에 시간 제한을 걸어 `Errors.TimedOut`을 발생시킵니다.

**왜 Catch 체이닝인가?** 타임아웃을 "오류"가 아닌 "폴백 결과"로 변환해야 합니다. 헬스 체크 타임아웃은 모델이 "건강하지 않음"을 의미하지, 시스템 오류를 의미하지 않습니다.

| Catch 순서 | 조건 | 결과 |
|-----------|------|------|
| 1번째 | `e.Is(Errors.TimedOut)` | TimedOut 폴백 결과 (오류 아님) |
| 2번째 | `e.IsExceptional` | AdapterError로 변환 |

### 2. Retry + Schedule -- 모델 모니터링

**문제:** 모니터링 서비스가 일시적으로 503을 반환. 첫 시도는 60% 확률로 실패하지만 재시도하면 대부분 성공.

**왜 Retry인가?** 일시적 네트워크 오류(503, timeout)는 재시도로 해결되는 경우가 많습니다. LanguageExt의 `Retry`는 IO 연산을 Schedule에 따라 자동으로 재시도합니다.

**Schedule 설계:**

```
exponential(100ms) | jitter(0.3) | recurs(3) | maxDelay(5s)
```

| 구성 요소 | 역할 | 값 |
|----------|------|-----|
| `exponential` | 기본 지연: 100ms -> 200ms -> 400ms | 100ms 기반 |
| `jitter` | 동시 재시도 분산 (thundering herd 방지) | 30% 변동 |
| `recurs` | 최대 재시도 횟수 | 3회 |
| `maxDelay` | 지연 상한 | 5초 |

**왜 이 Schedule인가?**
- `exponential`: 서버 부하를 점진적으로 줄임
- `jitter`: 여러 클라이언트가 동시에 재시도하는 thundering herd 문제 방지
- `recurs(3)`: 3회면 일시적 오류 대부분 복구, 그 이상은 영구 오류
- `maxDelay(5s)`: 사용자 대기 시간 상한 제한

### 3. Fork + awaitAll -- 병렬 컴플라이언스 체크

**문제:** 5개 컴플라이언스 기준을 순차 실행하면 100~500ms x 5 = 최대 2.5초. 각 체크는 독립적이므로 병렬 실행 가능.

**왜 Fork인가?** LanguageExt의 `Fork`는 IO 연산을 별도 파이버(경량 스레드)에서 실행하여 병렬성을 달성합니다. 각 체크가 독립적이므로 결과 간 의존성이 없어 안전하게 Fork할 수 있습니다.

**왜 awaitAll인가?** `awaitAll`은 모든 Fork의 결과를 수집합니다. 하나의 체크가 느려도 나머지는 이미 완료되어 있으므로, 전체 소요 시간은 가장 느린 체크의 시간에 수렴합니다.

**성능 비교:**

| 실행 방식 | 최악 소요 시간 | 기대 소요 시간 |
|----------|-------------|-------------|
| 순차 | 500ms x 5 = 2,500ms | ~1,500ms |
| 병렬 (Fork) | max(500ms) = 500ms | ~350ms |

### 4. Bracket -- 모델 레지스트리

**문제:** 레지스트리 조회는 세션을 획득하고 사용한 뒤 반드시 해제해야 합니다. 조회 중 예외가 발생해도 세션이 누수되면 안 됩니다.

**왜 Bracket인가?** Bracket 패턴은 리소스의 수명 주기를 Acquire -> Use -> Release 세 단계로 보장합니다. Release(Fin 매개변수)는 Use 단계의 성공/실패 무관하게 항상 실행됩니다. C#의 `try-finally`와 유사하지만, IO 컨텍스트 안에서 합성 가능합니다.

```
Acquire: 세션 획득 (50~150ms 지연, 5% 실패)
    |
    v
Use: 레지스트리 조회 (100~400ms 지연, 5% 실패)
    |
    v
Fin(Release): 세션 해제 (성공/실패 무관 보장)
```

**왜 try-finally가 아닌 Bracket인가?**
- IO 합성 체인 안에서 자연스럽게 사용 가능
- Release가 IO 효과를 가질 수 있음 (비동기 해제)
- FinT LINQ 체인에 투명하게 합성 가능

## 네이밍 규칙: `{Subject}{Role}{Variant}`

Adapter 레이어의 파일명은 3차원 네이밍 규칙을 따릅니다:

| 차원 | 표현 수단 | 예시 |
|------|-----------|------|
| Subject (무엇) | Aggregate 이름 | `AIModel`, `Deployment`, `Assessment`, `Incident` |
| Role (역할) | CQRS 역할 | `Repository`, `Query`, `DetailQuery` |
| Variant (어떻게) | 기술 접미사 | `InMemory`, `EfCore`, `Dapper` |

적용 예:

| 파일명 | Subject | Role | Variant |
|--------|---------|------|---------|
| `AIModelRepositoryInMemory.cs` | AIModel | Repository | InMemory |
| `AIModelRepositoryEfCore.cs` | AIModel | Repository | EfCore |
| `AIModelQueryInMemory.cs` | AIModel | Query | InMemory |
| `DeploymentDetailQueryInMemory.cs` | Deployment | DetailQuery | InMemory |
| `UnitOfWorkInMemory.cs` | (공통) | UnitOfWork | InMemory |

이 규칙은 Observable 래퍼에도 동일하게 적용됩니다: `{Subject}{Role}{Variant}Observable` (예: `AIModelRepositoryInMemoryObservable`).

## 관측성 설계

### GenerateObservablePort

모든 외부 서비스와 Repository는 `[GenerateObservablePort]` Source Generator를 적용합니다. 이 속성은 원본 클래스를 래핑하는 Observable 클래스를 자동 생성하여, 각 메서드 호출에 대해 로깅, 메트릭, 트레이싱을 추가합니다.

```
IModelHealthCheckService
    |
    [GenerateObservablePort]
    |
    v
ModelHealthCheckServiceObservable  (Source Generator 자동 생성)
    |-- 메서드 진입/종료 로깅
    |-- 실행 시간 메트릭
    |-- 분산 트레이싱 스팬
    |
    v
ModelHealthCheckService  (실제 구현)
```

### DI 등록 패턴

```csharp
// Observable 래퍼를 인터페이스에 등록
services.AddScoped<IModelHealthCheckService, ModelHealthCheckService>();
services.RegisterScopedObservablePort<IAIModelRepository, InMemoryAIModelRepositoryObservable>();
```

외부 서비스는 직접 등록, Repository는 Observable 래퍼를 통해 등록합니다.

다음 단계에서는 이 설계를 C# 코드로 구현하여 [코드 설계](../02-code-design/)를 진행합니다.
