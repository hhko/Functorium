---
name: adapter-develop
description: "Functorium 프레임워크 기반 어댑터 레이어(Repository, Query Adapter, Endpoint, DI 등록)를 구현합니다. 'Repository 구현', '어댑터 추가', '엔드포인트 만들어줘', 'DI 등록', 'EF Core 설정', 'Dapper 쿼리' 등의 요청에 반응합니다."
---

## 선행 조건

`application-develop` 스킬에서 생성한 `application/03-implementation-results.md`를 읽어 Port 목록(IRepository, IQueryPort, External Service)을 확인합니다.
`architecture-design` 스킬에서 생성한 `01-architecture-design.md`가 있으면 읽어 폴더 구조와 영속성 전략을 확인합니다.

## 후속 스킬

```
project-spec → architecture-design → domain-develop → application-develop → **adapter-develop** → observability-develop → test-develop
```

어댑터 구현 완료 후 `observability-develop` 스킬에서 관측성 전략(KPI→메트릭 매핑, 대시보드, 알림, ctx.* 전파)을 설계합니다.

# Adapter Develop Skill

Functorium 프레임워크 기반 어댑터 레이어를 구현하는 스킬입니다.

## 워크플로우

### Phase 1: 포트 → 어댑터 매핑

사용자에게 질문:
- 어떤 포트를 구현할까요? (IRepository, IQueryPort, External API, Endpoint)
- 영속성 전략은? (EF Core, InMemory, Dapper)
- Soft Delete가 필요한가?

### Phase 2: 어댑터 구현

**Repository (Write Side):**
- `[GenerateObservablePort]` 어트리뷰트 적용
- `EfCoreRepositoryBase<TAgg, TId, TModel>` 상속
  - `ToDomain` / `ToModel` 매퍼 구현
  - `DbContext`, `DbSet` 오버라이드
  - `PropertyMap`으로 Specification 매핑
  - Include 선언 (N+1 방지)
- `InMemoryRepositoryBase<TAgg, TId>` 상속 (테스트용)
- 모든 메서드 `virtual` (Source Generator 파이프라인 필수)

**Query Adapter (Read Side / CQRS):**
- `DapperQueryBase<TEntity, TDto>` 상속
  - `SelectSql`, `CountSql`, `DefaultOrderBy`, `AllowedSortColumns` 오버라이드
  - `DapperSpecTranslator` 구현
- `InMemoryQueryBase<TEntity, TDto>` 상속 (테스트용)

**Endpoint (Presentation):**
- FastEndpoints의 `Endpoint<TRequest, TResponse>` 상속
- `IMediator.Send()` → `FinResponse` 매핑
- `SendCreatedFinResponseAsync` / `SendFinResponseAsync`

**External API:**
- `IObservablePort` 구현
- `IO.liftAsync` + try/catch
- `AdapterError.For` / `AdapterError.FromException`

상세 패턴은 references 파일을 읽으세요.

### ObservableSignal — Adapter 내부 개발자 로깅

`ObservableSignal`은 Adapter 구현 코드 내부에서 개발자가 직접 운영 목적의 로그를 출력하는 정적 API입니다.
Observable 래퍼가 설정한 공통 컨텍스트(`request.layer`, `request.category.name`, `request.handler.name`, `request.handler.method`)를 자동으로 포함합니다.

**Pillar 범위:**
- Debug → Logging만 (고빈도 → Tracing 노이즈 방지)
- Warning/Error → Logging + Activity Event

**로그 수준 가이드:**
| Level | 용도 | 예시 |
|-------|------|------|
| Debug | 정상 흐름 상세 (프로덕션 비활성) | 캐시 미스, 쿼리 상세 |
| Warning | 자동 복구 가능한 열화 | Polly 재시도, rate limit, 느린 쿼리 |
| Error | 복구 불가 실패 | 재시도 소진, 서킷 오픈, DLQ 이동 |

**사용 예시:**
```csharp
// Polly 재시도 시
ObservableSignal.Warning("Retry attempt {Attempt}/{MaxRetry}",
    ("adapter.retry.attempt", attempt),
    ("adapter.retry.delay_ms", delay.TotalMilliseconds));

// 캐시 미스 시
ObservableSignal.Debug("Cache miss", ("adapter.cache.key", cacheKey));

// 재시도 소진 시
ObservableSignal.Error(ex, "Database operation failed after exhausting retries",
    ("adapter.db.retry.attempt", maxRetries));
```

**부가 필드 프리픽스**: `adapter.*` (adapter.retry.*, adapter.http.*, adapter.db.*, adapter.cache.*, adapter.message.*)

### CtxEnricherPipeline
`CtxEnricherPipeline`은 파이프라인 최선두에서 `IUsecaseCtxEnricher`를 호출하여
`ctx.*` 필드를 Logging/Tracing/MetricsTag에 동시 전파합니다.

Adapter 초기화에서 `CtxEnricherContext.SetPushFactory`를 등록하여
Serilog LogContext + Activity.SetTag + MetricsTagContext를 통합합니다.

Observable Port의 `RequestCategory` 예시:
- Repository → `"repository"`
- QueryAdapter → `"query"`
- ExternalApi → `"external_api"`
- UnitOfWork → `"unit_of_work"`

### Phase 3: DI 등록

- `RegisterScopedObservablePort<IPort, AdapterObservable>()`
- `RegisterConfigureOptions<TOptions, TValidator>()`
- Provider 분기 (InMemory vs Sqlite)

### Phase 4: 구현

.cs 파일 생성 + EF Core Configuration

**출력:** `{context}/adapter/` 폴더에 4단계 문서 + 소스 코드

## References

- Repository 패턴: `references/repository-patterns.md`
- Endpoint 패턴: `references/endpoint-patterns.md`
- DI 등록 패턴: `references/di-registration.md`
