# DomainEvent 중복 로그 문제 수정

## 문제 현상

상품 생성/수정 시 동일한 DomainEvent 로그가 2회 출력됨:
```
[09:29:38 INF] [DomainEvent] Product created: 01KGGEFA2EA5765TP35Q4C8W1Y, Name: 노트북, Price: 1500000
[09:29:38 INF] [DomainEvent] Product created: 01KGGEFA2EA5765TP35Q4C8W1Y, Name: 노트북, Price: 1500000
```

## 근본 원인 분석

### 원인: DomainEvent Handler 중복 등록

**등록 경로 1 - Mediator.SourceGenerator 자동 등록:**
```csharp
// AdapterInfrastructureRegistration.cs:18
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
```
- `AddMediator()`는 `LayeredArch.Adapters.Infrastructure` 어셈블리 및 **참조된 어셈블리**를 스캔
- `LayeredArch.Application`이 의존성에 포함되어 있으므로 `OnProductCreated`, `OnProductUpdated`가 자동 등록됨

**등록 경로 2 - Scrutor 명시적 등록:**
```csharp
// AdapterInfrastructureRegistration.cs:30-31
services.RegisterDomainEventHandlersFromAssembly(
    LayeredArch.Application.AssemblyReference.Assembly);
```
- Scrutor를 사용하여 `LayeredArch.Application` 어셈블리의 핸들러를 다시 등록

**결과:** 핸들러가 DI 컨테이너에 2번 등록 → 이벤트 발행 시 2번 호출 → 로그 2회 출력

### 추가 발견: ObservableDomainEventPublisher/NotificationPublisher 로그 미출력

**ObservableDomainEventPublisher:**
- `RegisterDomainEventPublisher(enableObservability: false)` - 기본값이 `false`
- `enableObservability=true`를 전달해야 로깅 데코레이터가 활성화됨

**ObservableDomainEventNotificationPublisher:**
- `RegisterObservableDomainEventNotificationPublisher()` 메서드가 호출되지 않음
- Handler 관점 로깅이 비활성화 상태

## 해결 방안

**LayeredArch에서 `RegisterDomainEventHandlersFromAssembly()` 호출 제거**
- `AddMediator()`가 이미 참조된 어셈블리(LayeredArch.Application 포함)의 핸들러를 자동 등록함
- 명시적 등록을 제거하여 중복 방지
- `RegisterDomainEventHandlersFromAssembly()` 메서드 자체는 Functorium에 유지 (다른 시나리오에서 필요할 수 있음)

## 수정 대상 파일

| 파일 | 수정 내용 |
|------|----------|
| `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Infrastructure/Abstractions/Registrations/AdapterInfrastructureRegistration.cs` | `RegisterDomainEventHandlersFromAssembly()` 호출 제거 |

## 구현 계획

### Step 1: 중복 등록 제거

**파일:** `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Infrastructure/Abstractions/Registrations/AdapterInfrastructureRegistration.cs`

**수정 전:**
```csharp
// =================================================================
// Application 레이어의 도메인 이벤트 핸들러 등록
// Mediator.SourceGenerator는 해당 패키지가 참조된 프로젝트 내의
// 핸들러만 자동 등록하므로, 다른 어셈블리의 핸들러는 명시적 등록 필요
// =================================================================
services.RegisterDomainEventHandlersFromAssembly(
    LayeredArch.Application.AssemblyReference.Assembly);
```

**수정 후:**
```csharp
// =================================================================
// Application 레이어의 도메인 이벤트 핸들러 등록
// Mediator.SourceGenerator가 AddMediator() 호출 시 참조된 어셈블리
// (LayeredArch.Application 포함)의 핸들러를 자동으로 등록합니다.
// 명시적 등록은 중복 호출을 유발하므로 제거합니다.
// =================================================================
// services.RegisterDomainEventHandlersFromAssembly() 제거됨
```

## 검증 방법

1. LayeredArch 프로젝트 빌드 및 실행
2. POST `/api/products` API 호출
3. 로그 확인:
   - **수정 전:** `[DomainEvent] Product created: ...` 2회 출력
   - **수정 후:** `[DomainEvent] Product created: ...` 1회만 출력
