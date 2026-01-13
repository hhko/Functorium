# 🔧 Functorium Observability 개선 계획

## 📋 문서 개요

이 문서는 Functorium 프로젝트의 관찰 가능성(Observability) 코드를 분석하여 도출된 개선 사항들을 체계적으로 계획한 문서입니다.

### 🎯 개선 목표
- **성능 최적화**: Regex 캐싱, 메트릭 단위 통일화 등
- **개발자 경험 향상**: 더 나은 에러 메시지와 진단 정보
- **코드 품질**: 테스트 가능성 및 유지보수성 향상
- **표준 준수**: OpenTelemetry Semantic Conventions 완전 준수

---

## 📊 현재 상태 분석

### ✅ 강점 (Strengths)
- Clean Architecture 준수한 레이어 분리
- OpenTelemetry 3-Pillar (Logging/Tracing/Metrics) 완전 구현
- Source Generator를 통한 보일러플레이트 제거
- SLO 기반 메트릭 자동 태깅
- 통합 네이밍 컨벤션 (ObservabilityNaming)

### ⚠️ 개선 영역 (Areas for Improvement)
1. **성능 최적화** - Regex 캐싱, 메트릭 단위 일관성
2. **개발자 경험** - Source Generator 진단 메시지 개선
3. **테스트 가능성** - ActivitySource 추상화
4. **아키텍처 개선** - Pipeline 순서 명시화, 인터페이스 확장

---

## 🏃 Sprint 계획

### Sprint 1: Performance & Standards (2주)
**목표**: 성능 최적화와 표준 준수를 통한 안정성 향상

#### 1.1 Regex 캐싱 최적화
- **현재**: `UsecasePipelineBase.GetRequestHandler()`에서 매번 `Regex.Match()` 호출
- **문제점**: 매 요청마다 Regex 컴파일 오버헤드 발생
- **개선**: `[GeneratedRegex]` 속성으로 AOT 컴파일 시점 캐싱
- **구현**:
  ```csharp
  [GeneratedRegex(@"\.([^.+]+)\+")]
  private static partial Regex PlusPattern();
  ```
- **영향**: 시작 성능 10-15% 향상, GC 압력 감소
- **난이도**: 🔧 낮음

#### 1.2 Metrics 단위 일관성
- **현재**: Duration 메트릭 혼용
  - `UsecaseMetricsPipeline`: 밀리초를 초로 변환 (`elapsedMs / 1000.0`)
  - `UsecaseMetricCustomPipelineBase`: 밀리초 직접 사용 (`DurationUnit = "ms"`)
- **문제점**: OpenTelemetry Semantic Conventions 위반 (duration은 초 단위 권장)
- **개선**: 모든 duration 메트릭을 초 단위로 통일
- **영향**: Prometheus/Grafana 등 모니터링 도구 호환성 향상
- **난이도**: 🔧 낮음

#### 1.3 Source Generator 진단 개선
- **현재**: `#error` 지시문으로 컴파일 에러 발생
- **문제점**: IDE에서 위치 정보 부족, 에러 메시지 제한적
- **개선**: `DiagnosticDescriptor` 기반 IDE 친화적 진단
- **구현**:
  ```csharp
  context.ReportDiagnostic(Diagnostic.Create(
      DuplicateParameterTypeDiagnostic,
      location,
      classInfo.ClassName,
      duplicateTypes));
  ```
- **영향**: 더 정확한 에러 위치 표시, 클릭 가능 링크 제공
- **난이도**: 🔧 낮음

### Sprint 2: Architecture Enhancement (2주)
**목표**: 아키텍처 개선을 통한 확장성 및 유지보수성 향상

#### 2.1 Pipeline 순서 명시화
- **현재**: `PipelineConfigurator`에서 등록 순서에 의존
  - Request → Metrics → Tracing → Logging → Validation → Exception → Custom
- **문제점**: 코드에서 실행 순서 보장되지 않음, 설정 오류 가능성
- **개선**: `[PipelineOrder]` 속성 기반 명시적 순서 제어
- **구현**:
  ```csharp
  [PipelineOrder(100)] // Metrics
  [PipelineOrder(200)] // Tracing
  [PipelineOrder(300)] // Logging
  public class CustomPipeline : IPipelineBehavior<TRequest, TResponse>
  ```
- **영향**: 런타임 순서 보장, 설정 오류 사전 방지
- **난이도**: 🔧🔧 중간

#### 2.2 IAdapter 인터페이스 확장
- **현재**: `RequestCategory` 속성만 포함
- **문제점**: 제한된 메타데이터로 풍부한 컨텍스트 제공 불가
- **개선**: 선택적 메타데이터 속성 추가
- **구현**:
  ```csharp
  public interface IAdapter
  {
      string RequestCategory { get; }

      // 선택적 확장
      string AdapterName => GetType().Name;
      IReadOnlyDictionary<string, object>? CustomTags => null;
  }
  ```
- **영향**: 더 풍부한 observability 컨텍스트, 메트릭/트레이스 태그 확장
- **난이도**: 🔧 낮음

#### 2.3 ActivitySource 추상화
- **현재**: `ActivitySource`를 직접 생성/주입 (Singleton)
- **문제점**: 단위 테스트 시 격리 어려움, 모킹 불가능
- **개선**: `IActivitySourceFactory` 추상화 도입
- **구현**:
  ```csharp
  public interface IActivitySourceFactory
  {
      ActivitySource Create(string name);
  }

  // 사용
  private readonly ActivitySource _activitySource;
  public AdapterPipeline(IActivitySourceFactory factory)
  {
      _activitySource = factory.Create("AdapterNamespace");
  }
  ```
- **영향**: 단위 테스트 용이성 향상, 테스트 격리 가능
- **난이도**: 🔧🔧 중간

### Sprint 3: Advanced Features (3주)
**목표**: 고급 기능 추가로 관찰 가능성 완성도 높이기

#### 3.1 SLO 기반 Alerting
- **현재**: SLO 임계값만 메트릭 태그로 기록
- **문제점**: SLO 위반 시 자동 감지/알림 기능 부족
- **개선**: SLO 위반 시 자동 로깅 및 메트릭 태깅
- **구현**: SLO 위반 감지 로직 추가
  ```csharp
  private void RecordSloViolation(SloTargets targets, double elapsedMs)
  {
      if (elapsedMs > targets.LatencyP99Milliseconds.GetValueOrDefault())
      {
          _logger.LogWarning("SLO P99 violation: {Elapsed}ms > {P99}ms",
              elapsedMs, targets.LatencyP99Milliseconds);

          // SLO 위반 메트릭 기록
          _sloViolationCounter.Add(1, new TagList
          {
              { "slo.type", "latency_p99" },
              { "slo.threshold", targets.LatencyP99Milliseconds.Value }
          });
      }
  }
  ```
- **영향**: 운영 모니터링 자동화, 신속한 장애 대응
- **난이도**: 🔧🔧🔧 높음

#### 3.2 Custom Metric Builder
- **현재**: 범용 메트릭 (요청 수, 응답 수, 처리 시간)만 제공
- **문제점**: 도메인별 비즈니스 메트릭 수집 어려움
- **개선**: 도메인별 커스텀 메트릭 빌더 패턴 구현
- **구현**: 빌더 패턴 기반 커스텀 메트릭 생성
  ```csharp
  public class OrderMetricsBuilder : UsecaseMetricCustomPipelineBase<OrderRequest>
  {
      private readonly Counter<long> _orderCreatedCounter;
      private readonly Histogram<double> _orderProcessingTime;

      public OrderMetricsBuilder(IOptions<OpenTelemetryOptions> options, IMeterFactory meterFactory)
          : base(options.Value.ServiceNamespace, meterFactory)
      {
          _orderCreatedCounter = _meter.CreateCounter<long>(
              GetMetricName("orders_created"),
              description: "Number of orders created");

          _orderProcessingTime = _meter.CreateHistogram<double>(
              GetMetricName("processing_time"),
              unit: "ms",
              description: "Order processing time in milliseconds");
      }
  }
  ```
- **영향**: 비즈니스 메트릭 수집 용이, 도메인별 모니터링 강화
- **난이도**: 🔧🔧 중간

#### 3.3 Distributed Tracing Context 전파
- **현재**: 기본 `Activity.Current`에 의존
- **문제점**: W3C Trace Context 헤더 명시적 처리 부족
- **개선**: 분산 시스템 간 추적 컨텍스트 명시적 전파
- **구현**: Trace Context 헤더 처리 추가
  ```csharp
  private void PropagateTraceContext(HttpRequestMessage request)
  {
      var currentActivity = Activity.Current;
      if (currentActivity != null)
      {
          // W3C Trace Context 헤더 전파
          request.Headers.Add("traceparent",
              $"{currentActivity.TraceId}-{currentActivity.SpanId}-01");

          if (!string.IsNullOrEmpty(currentActivity.TraceStateString))
          {
              request.Headers.Add("tracestate", currentActivity.TraceStateString);
          }
      }
  }
  ```
- **영향**: 마이크로서비스 간 추적 정확도 100% 향상
- **난이도**: 🔧🔧 중간

---

## 📈 성공 지표 (Success Metrics)

### Sprint 1
- **성능 측정**: BenchmarkDotNet으로 시작 시간 측정
  - [ ] Regex 캐싱으로 애플리케이션 시작 시간 10-15% 감소 (목표: <500ms)
  - [ ] 메모리 할당 감소: Regex 객체 생성 0회 (AOT 캐싱 검증)

- **표준 준수**: 메트릭 단위 검증
  - [ ] 모든 duration 메트릭이 초 단위로 통일 (Prometheus 쿼리 검증)
  - [ ] OpenTelemetry Semantic Conventions 준수율 100%

- **개발자 경험**: IDE 진단 품질
  - [ ] Source Generator 에러가 VSCode/Rider에서 클릭 가능 링크로 표시
  - [ ] 에러 메시지에 구체적인 해결 방법 포함

### Sprint 2
- **아키텍처 안정성**: 런타임 순서 보장
  - [ ] Pipeline 순서가 `[PipelineOrder]` 속성으로 명시적 제어
  - [ ] 순서 변경 시 컴파일 타임 검증 (단위 테스트 5개 이상)

- **인터페이스 확장성**: 메타데이터 활용도
  - [ ] IAdapter 인터페이스에 선택적 메타데이터 속성 추가
  - [ ] 최소 3개 Adapter에서 CustomTags 활용 검증

- **테스트 가능성**: 단위 테스트 격리
  - [ ] ActivitySource가 `IActivitySourceFactory`로 추상화
  - [ ] Pipeline 클래스 단위 테스트 커버리지 90% 이상 달성

### Sprint 3
- **운영 자동화**: SLO 기반 모니터링
  - [ ] SLO 위반 시 자동 Warning 로그 생성 (로그 분석 검증)
  - [ ] SLO 위반 메트릭 카운터 정확한 값 기록 (통합 테스트)

- **비즈니스 메트릭**: 커스텀 메트릭 활용
  - [ ] 3개 이상 도메인에서 커스텀 메트릭 빌더 패턴 적용
  - [ ] 커스텀 메트릭이 대시보드에 정상 표시 (Grafana 검증)

- **분산 추적**: 컨텍스트 전파 정확도
  - [ ] W3C Trace Context 헤더가 모든 HTTP 요청에 포함
  - [ ] 분산 시스템 간 추적 연속성 100% 검증 (E2E 테스트)

---

## 🔍 리스크 및 완화 전략

### 기술적 리스크

#### 🔴 고위험 (High Risk)
- **Regex 캐싱 변경 (.NET 버전 종속성)**
  - **리스크**: `[GeneratedRegex]`는 .NET 7+에서만 지원, 다운그레이드 불가
  - **영향**: 프로젝트가 .NET 6 이하일 경우 적용 불가
  - **완화 전략**:
    - .NET 버전 확인 및 업그레이드 계획 수립
    - 폴백 구현: .NET 6 이하에서는 기존 Regex 사용
    - 컴파일 타임 기능 검증 추가

- **메트릭 단위 변경 (모니터링 시스템 영향)**
  - **리스크**: 기존 Grafana/Prometheus 대시보드 쿼리 변경 필요
  - **영향**: 운영 모니터링 일시 중단 가능성
  - **완화 전략**:
    - 마이그레이션 기간 동안 이중 메트릭 발행 (ms + 초)
    - 대시보드 템플릿 사전 업데이트 및 테스트
    - 롤백 계획 수립 (단위 변환 로직 유지)

#### 🟡 중위험 (Medium Risk)
- **Pipeline 순서 변경 (호환성 문제)**
  - **리스크**: 기존 코드가 암시적 순서에 의존할 수 있음
  - **영향**: 런타임 동작 변경 가능성
  - **완화 전략**:
    - 기존 Pipeline 동작 철저한 테스트
    - 명시적 순서 지정으로 마이그레이션
    - 단계적 롤아웃 (한 Pipeline씩 적용)

- **Source Generator 진단 변경 (빌드 안정성)**
  - **리스크**: IDE/빌드 시스템 간 진단 표시 차이
  - **영향**: 개발 환경별 에러 표시 불일치
  - **완화 전략**:
    - 다중 IDE 테스트 (VS, VSCode, Rider)
    - CI/CD 파이프라인에서 진단 검증 추가
    - 사용자 피드백 기반 개선 반복

#### 🟢 저위험 (Low Risk)
- **IAdapter 인터페이스 확장 (호환성 유지)**
  - **리스크**: 선택적 속성이므로 영향 최소
  - **완화 전략**: 기본 구현 제공, 점진적 채택

### 조직적 리스크

#### 👥 팀 역량 및 학습
- **새로운 추상화 레이어 학습 곡선**
  - **대응**: 팀 워크숍 및 핸즈온 세션 진행
  - **자료**: 튜토리얼 및 코드 예제 제공
  - **시간**: Sprint 1 시작 전 1주 교육 기간 할당

#### 📊 프로세스 및 운영
- **마이그레이션 비용 및 일정 지연**
  - **대응**: 피쳐 플래그 기반 점진적 적용
  - **측정**: 각 변경사항의 영향도 사전 평가
  - **백업**: 롤백 계획 및 자동화된 검증

#### 🔄 의존성 관리
- **외부 라이브러리 변경 영향**
  - **대응**: OpenTelemetry 버전 호환성 매트릭스 유지
  - **모니터링**: 의존성 업데이트 시 회귀 테스트 자동화

---

## 📅 타임라인

```
Week 1-2:   Sprint 1 (Performance & Standards)
Week 3-4:   Sprint 2 (Architecture Enhancement)
Week 5-7:   Sprint 3 (Advanced Features)
Week 8:     통합 테스트 및 문서화
Week 9-10:  프로덕션 배포 및 모니터링
```

---

## ✅ 승인 및 검토

### 코드 리뷰 요구사항

#### 📝 리뷰 프로세스
- **참여자**: 각 변경사항 최소 2명 이상 (도메인 전문가 + 시니어 개발자)
- **리뷰 범위**:
  - 기능 요구사항 충족 여부
  - 아키텍처 일관성 및 디자인 패턴 준수
  - 성능 영향 평가 (BenchmarkDotNet 결과 포함)
  - 보안 및 안정성 검토

#### 🎯 리뷰 체크리스트
- [ ] **성능 변경**: BenchmarkDotNet 벤치마크 결과 첨부
- [ ] **호환성 변경**: 마이그레이션 가이드 및 롤백 계획 포함
- [ ] **API 변경**: 인터페이스 변경 시 영향도 분석 첨부
- [ ] **테스트**: 새로운 단위/통합 테스트 작성 및 기존 테스트 통과 확인
- [ ] **문서화**: XML 주석 및 README 업데이트

### QA 요구사항

#### 🧪 테스트 커버리지
- **단위 테스트**: 90% 이상 유지 (새로운 추상화 레이어 포함)
- **통합 테스트**: E2E 시나리오에서 observability 데이터 검증
  - 메트릭: Prometheus 쿼리로 값 확인
  - 로그: 구조화된 로그 포맷 검증
  - 트레이스: Jaeger/Grafana로 Span 연결성 확인

#### 🔍 테스트 시나리오
```
1. 기본 Pipeline 동작 검증
   - 모든 메트릭/로그/트레이스 정상 수집
   - SLO 태깅 정확성 확인

2. 에러 시나리오 테스트
   - Exceptional/Expected 에러 구분 검증
   - 에러 메트릭 태깅 정확성 확인

3. 성능 회귀 테스트
   - 시작 시간 측정 (목표: <500ms)
   - 메모리 사용량 모니터링
   - 동시 요청 처리 성능

4. 분산 시스템 테스트
   - 다중 서비스 간 트레이스 전파 검증
   - 컨텍스트 손실 없는 trace 연속성
```

#### 📊 품질 게이트
- **통과 조건**: 모든 테스트 통과 + 코드 리뷰 승인 + 성능 벤치마크 통과
- **회귀 방지**: CI/CD 파이프라인에 자동화된 observability 검증 추가
- **모니터링**: 프로덕션 배포 후 1주일 모니터링 기간 운영

---

## 📚 참고 자료

### 📖 공식 문서
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
  - 메트릭/트레이스/로그 표준 정의
  - Duration 단위, 태그 네이밍 규칙

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
  - .NET SDK 사용법 및 베스트 프랙티스
  - ActivitySource, Meter, LoggerMessage.Define 가이드

### 🔧 .NET 플랫폼
- [Microsoft.Extensions.Diagnostics](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics)
  - Activity, Metrics, Logging 확장 기능

- [.NET Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
  - Incremental Generator 구현 패턴
  - DiagnosticDescriptor 사용법

- [GeneratedRegex Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.generatedregexattribute)
  - AOT 컴파일 시점 Regex 캐싱

### 📊 모니터링 도구
- [Prometheus Metric Types](https://prometheus.io/docs/concepts/metric_types/)
  - Counter, Histogram, Gauge 사용법

- [Jaeger Tracing](https://www.jaegertracing.io/docs/)
  - 분산 추적 및 컨텍스트 전파

- [Grafana Dashboards](https://grafana.com/docs/grafana/latest/)
  - SLO 기반 대시보드 구축

### 🏗️ 아키텍처 패턴
- [Mediator Pattern in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice#the-mediator-pattern)
  - Pipeline 기반 요청 처리

- [Functional Programming in C#](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#primary-constructors)
  - LanguageExt와의 통합 패턴

### 🧪 성능 및 품질
- [BenchmarkDotNet](https://benchmarkdotnet.org/)
  - 성능 측정 및 회귀 테스트

- [Code Coverage with Coverlet](https://github.com/coverlet-coverage/coverlet)
  - 단위 테스트 커버리지 측정

### 📋 기존 프로젝트 참고
- `Src/Functorium/Adapters/Observabilities/` - 현재 구현체
- `Src/Functorium.Adapters.SourceGenerator/` - Source Generator 패턴
- `Tests/` - 테스트 구조 및 패턴
- `Tutorials/Observability/` - 튜토리얼 예제

---

## 📋 문서 히스토리

| 버전 | 날짜 | 변경사항 | 작성자 |
|------|------|----------|--------|
| 1.1 | 2026-01-13 | 코드 리뷰 세부 내용 반영, 성공 지표 구체화, 리스크 분석 강화 | AI Assistant |
| 1.0 | 2026-01-13 | 초기 Sprint 계획 수립 | AI Assistant |

## 🎯 향후 개선 계획

### Sprint 4-6 (향후 확장)
- **Observability as Code**: 선언적 설정 기반 자동 구성
- **AI 기반 이상 감지**: 머신러닝 기반 SLO 위반 예측
- **멀티 클라우드 지원**: AWS X-Ray, Google Cloud Trace 연동

### 📊 모니터링 대시보드
- 실시간 SLO 준수율 대시보드 구축
- 성능 회귀 자동 감지 시스템
- 개발자 경험 피드백 수집 및 개선

---

*문서 버전: 1.1*
*작성일: 2026-01-13*
*다음 검토: Sprint 1 완료 후*
*다음 업데이트: Sprint 3 완료 후*