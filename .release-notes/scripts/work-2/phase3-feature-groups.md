# Phase 3: 기능 그룹화 결과

Generated: 2025-12-16

## 그룹 1: 함수형 오류 처리 (Error Handling)

**관련 커밋:**
- feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가
- refactor(errors): Destructurer 필드명 일관성 개선

**관련 API:**
- ErrorCodeFactory (Create, CreateFromException)
- ErrorsDestructuringPolicy
- IErrorDestructurer
- ErrorCodeExceptionalDestructurer
- ErrorCodeExpectedDestructurer
- ErrorCodeExpectedTDestructurer
- ExceptionalDestructurer
- ExpectedDestructurer
- ManyErrorsDestructurer

**사용자 가치:**
LanguageExt의 Error 타입을 구조화하여 생성하고 Serilog에서 적절히 로깅할 수 있는 디스트럭처링 정책을 제공합니다.

---

## 그룹 2: OpenTelemetry 통합 (Observability)

**관련 커밋:**
- feat(observability): OpenTelemetry 및 Serilog 통합 구성 추가
- feat(observability): OpenTelemetry 의존성 등록 확장 메서드 추가
- refactor(observability): Builder 코드를 Configurator 패턴으로 재구성

**관련 API:**
- OpenTelemetryRegistration.RegisterObservability()
- OpenTelemetryBuilder
- OpenTelemetryOptions
- LoggingConfigurator
- MetricsConfigurator
- TracingConfigurator
- StartupLogger
- IStartupOptionsLogger

**사용자 가치:**
OpenTelemetry 분산 추적, 메트릭, 로깅을 통합하는 빌더 패턴 API를 제공합니다. Serilog와 함께 OTLP 프로토콜로 텔레메트리 데이터를 수집합니다.

---

## 그룹 3: 아키텍처 검증 (Architecture Validation)

**관련 커밋:**
- feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가

**관련 API:**
- ArchitectureValidationEntryPoint.ValidateAllClasses()
- ClassValidator
- MethodValidator
- ValidationResultSummary

**사용자 가치:**
ArchUnitNET을 활용한 아키텍처 테스트를 위한 유창한 검증 API를 제공합니다.

---

## 그룹 4: 테스트 픽스처 (Test Fixtures)

**관련 커밋:**
- feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가
- refactor(testing): ControllerTestFixture를 HostTestFixture로 이름 변경

**관련 API:**
- HostTestFixture<TProgram>
- QuartzTestFixture<TProgram>
- JobCompletionListener
- JobExecutionResult

**사용자 가치:**
ASP.NET Core 호스트 및 Quartz.NET 스케줄러 통합 테스트를 위한 픽스처를 제공합니다.

---

## 그룹 5: Serilog 테스트 유틸리티 (Logging Testing)

**관련 커밋:**
- feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가
- refactor(testing): 로깅 테스트 유틸리티 재구성

**관련 API:**
- StructuredTestLogger<T>
- TestSink
- LogEventPropertyExtractor
- LogEventPropertyValueConverter
- SerilogTestPropertyValueFactory

**사용자 가치:**
테스트에서 Serilog 로그 이벤트를 검증하기 위한 유틸리티를 제공합니다.

---

## 그룹 6: FinT 유틸리티 (LINQ Extensions)

**관련 커밋:**
- feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가

**관련 API:**
- FinTUtilites.Filter()
- FinTUtilites.SelectMany()

**사용자 가치:**
LanguageExt의 FinT 모나드와 IO 모나드를 LINQ 쿼리 구문에서 사용할 수 있는 확장 메서드를 제공합니다.

---

## 그룹 7: Options 패턴 (Configuration)

**관련 커밋:**
- refactor(options): OptionsUtilities를 OptionsConfigurator로 교체

**관련 API:**
- OptionsConfigurator.RegisterConfigureOptions<TOptions, TValidator>()
- OptionsConfigurator.GetOptions<TOptions>()

**사용자 가치:**
FluentValidation과 통합된 Options 패턴 등록을 간소화합니다.

---

## 그룹 8: 유틸리티 확장 메서드

**관련 커밋:**
- feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가

**관련 API:**
- DictionaryUtilities.AddOrUpdate()
- IEnumerableUtilities (Any, IsEmpty, Join)
- StringUtilities (ConvertToDouble, ConvertToInt, Empty, NotContains, NotEmpty, NotEquals, Replace, TryConvertToDouble)

**사용자 가치:**
일반적인 컬렉션 및 문자열 작업을 위한 확장 메서드를 제공합니다.
