구체적으로 6가지입니다. 영향도 순으로 정리합니다.

---

## 1. ObservableDomainEventPublisher — 제거 가능 (350줄)

**문제:** 관찰 가능성 코드가 3곳에 중복됩니다.

| 파일 | 줄 수 | 역할 |
|------|-------|------|
| `DomainEventPublisher.cs` | 289 | 코어 발행 + 배치 핸들러 관찰 가능성 (Activity/Meter/Logger) |
| `ObservableDomainEventPublisher.cs` | 289 | **같은 관찰 가능성을 데코레이터로 다시 감싸기** |
| `ObservableDomainEventNotificationPublisher.cs` | 279 | 핸들러 레벨 관찰 가능성 |

`DomainEventPublisher`가 이미 Activity/Meter/Logger를 내장하고 있는데, `ObservableDomainEventPublisher`가 **같은 일을 데코레이터로 한 번 더 합니다.** TagList 빌딩, elapsed 계산, ErrorInfoExtractor 호출 — 전부 복붙입니다.

**빼면:** 350줄 제거. 테스트 5개 파일 단순화. 이벤트 발행 경로에서 불필요한 래핑 1단계 제거.

---

## 2. 파이프라인 7단계 — 기본값을 opt-in으로 전환

현재 모든 유스케이스가 7단계 파이프라인을 **무조건** 통과합니다:

```
Exception → Validation → Caching → Logging → Metrics → Tracing → Transaction
```

실제로 대부분의 Command에 필요한 것은 **Validation + Transaction** 뿐입니다. 나머지 5개는 환경에 따라 다릅니다.

**빼는 게 아니라 기본값을 바꾸는 것:**
```csharp
// 현재: 전부 자동 등록
services.ConfigurePipelines(p => p.UseAll());

// 변경: 필요한 것만
services.ConfigurePipelines(p => p
    .UseValidation()      // 항상
    .UseTransaction()     // 항상
    .UseLogging()         // opt-in
);
```

**빼면:** 런타임 오버헤드 감소. 개발자가 "왜 이 로그가 나오지?" 디버깅할 일 감소.

---

## 3. 에러 타입 3계층 — 단순화 가능 (~600줄)

19개 파일, 1,381줄의 에러 시스템:

```
Abstractions/Errors/ (5파일, 624줄)
  └─ ErrorType, ErrorCodeFactory, IHasErrorCode...
Domains/Errors/ (11파일, 380줄)
  └─ DomainError + 10개 DomainErrorType (Format, Length, Range...)
Applications/Errors/ (3파일, 377줄)
  └─ ApplicationError + ApplicationErrorType (거의 동일 구조)
```

`DomainError.For<T>(...)`와 `ApplicationError.For<T>(...)`는 **시그니처가 동일하고 레이어 접두사만 다릅니다.** 레이어 구분은 네임스페이스에서 이미 달성되고 있습니다.

**빼면:** `Error.For<T>(...)` 하나로 통합. 레이어 접두사는 `typeof(T).Namespace`에서 추론. ApplicationError 관련 ~600줄 제거.

---

## 4. FinResponse<T> 브릿지 — 제거 가능 (403줄)

4개 파일로 `Fin<T>` → `FinResponse<T>` 변환을 합니다:

```
IFinResponse.cs (62줄) — 인터페이스
IFinResponse.Impl.cs (227줄) — Succ/Fail 구현
IFinResponse.Factory.cs (23줄) — 팩토리
IFinResponse.FinConversions.cs (91줄) — 변환 확장 메서드
```

`FinResponse<T>`가 제공하는 것: `IsSucc`, `IsFail` 프로퍼티.
`Fin<T>`가 이미 제공하는 것: `IsSucc`, `IsFail` 프로퍼티.

**차이가 없습니다.** 파이프라인이 `Fin<T>`를 직접 사용하면 403줄이 사라집니다.

---

## 5. Repository 기반 클래스의 Expression 캐싱 — 과잉 (약 30줄)

`EfCoreRepositoryBase.cs` 74-102줄:

```csharp
private static readonly MethodInfo ListContainsMethod = ...
private static readonly ParameterExpression ModelParam = ...
private static readonly MemberExpression IdProperty = ...

protected virtual Expression<Func<TModel, bool>> ByIdPredicate(TId id)
{
    var body = Expression.Equal(IdProperty, Expression.Constant(id.ToString()));
    return Expression.Lambda<Func<TModel, bool>>(body, ModelParam);
}
```

EF Core가 이미 내부적으로 Expression 캐싱을 합니다. 수동으로 `Expression.Equal` + `Expression.Lambda`를 조립하는 것은 EF Core의 `FindAsync()`보다 나을 게 없습니다.

**빼면:** 30줄 단순화. 개발자가 Expression 트리를 이해할 필요 없어짐.

---

## 6. ObservablePort 소스 제너레이터 — 장기적 검토 대상 (1,845줄)

포트 하나당 수동 데코레이터 20-30줄이면 되는 일을 1,845줄 소스 제너레이터로 자동화합니다. `ObservablePortIgnore` 어트리뷰트의 존재가 엣지 케이스 미해결의 증거입니다.

다만 이것은 **포트가 많아지면 가치가 올라가므로** 현재 규모에서는 과잉이지만, 성장하면 정당화될 수 있습니다. 장기적 검토 대상.

---

## 요약

| 항목 | 제거/단순화 가능 줄 수 | 난이도 | 우선순위 |
|------|----------------------|--------|----------|
| ObservableDomainEventPublisher 중복 | ~350 | 낮음 | **즉시** |
| 파이프라인 기본값 opt-in 전환 | 구조 변경 | 중간 | **높음** |
| 에러 타입 3계층 → 단일화 | ~600 | 중간 | **중간** |
| FinResponse 브릿지 제거 | ~400 | 낮음 | **중간** |
| Repository Expression 캐싱 | ~30 | 낮음 | **낮음** |
| ObservablePort 제너레이터 | ~1,845 | 높음 | **장기** |
| **합계** | **~3,200줄+** | | |

이것들을 빼도 기능은 동일합니다. **빼야 A+입니다.**