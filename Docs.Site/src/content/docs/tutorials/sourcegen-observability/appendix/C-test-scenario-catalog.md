---
title: "Test Scenario Catalog"
---

ObservablePortGenerator의 전체 테스트 목록입니다. 각 시나리오의 상세 설명은 [Part 3-07. Test Scenario](../Part3-Advanced/07-Test-Scenarios/)를 참고하십시오.

---

## 테스트 구성

ObservablePortGenerator의 테스트는 두 가지 축으로 구성됩니다:

- **생성기 스냅샷 테스트** — 소스 생성기의 코드 생성 결과를 `.verified.txt`와 비교하여 검증
- **런타임 Observability 구조 검증** — 생성된 코드가 실제 실행 시 올바른 태그/필드 구조를 출력하는지 검증

---

## 생성기 스냅샷 테스트

`ObservablePortGeneratorTests` 클래스에서 8개 카테고리 × 31개 시나리오를 검증합니다.

### 1. 기본 생성 (1개)

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `ObservablePortGenerator_ShouldGenerate_GenerateObservablePortAttribute` | `[GenerateObservablePort]` Attribute 자동 생성 |

### 2. 기본 어댑터 (3개)

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Should_Generate_PipelineClass_WithSingleMethod` | 단일 메서드 어댑터에 대한 Observable 클래스 생성 |
| `Should_Generate_PipelineClass_WithMultipleMethods` | 다중 메서드 어댑터에 대한 모든 메서드 오버라이드 |
| `Should_NotGenerate_PipelineClass_WhenNoMethods` | 메서드 없는 어댑터 — 파이프라인 미생성 |

### 3. 파라미터 (8개)

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Should_Generate_LoggerMessageDefine_WithZeroParameters` | 0개 파라미터 → 총 4필드 → LoggerMessage.Define |
| `Should_Generate_LoggerMessageDefine_WithOneParameter` | 1개 파라미터 → 총 5필드 → LoggerMessage.Define |
| `Should_Generate_LoggerMessageDefine_WithTwoParameters` | 2개 파라미터 → 총 6필드 → LoggerMessage.Define (경계값) |
| `Should_Generate_LogDebugFallback_WithThreeParameters` | 3개 파라미터 → 총 7필드 → logger.LogDebug() 폴백 |
| `Should_Generate_LogDebugFallback_WithManyParameters` | 다수 파라미터 → logger.LogDebug() 폴백 |
| `Should_Generate_CollectionCountFields` | 컬렉션 파라미터 → Count 필드 추가 |
| `Should_NotGenerate_Count_ForTupleParameter` | 튜플 파라미터 → Count 미생성 |
| `Should_Generate_CollectionCountFields_WithArrayParameter` | 배열 파라미터 → Length 필드 추가 |

### 4. 반환 타입 (6개)

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Should_Generate_PipelineClass_WithSimpleReturnType` | `FinT<IO, int>` 등 단순 타입 추출 |
| `Should_Generate_PipelineClass_WithCollectionReturnType` | `List<T>`, `T[]` → Count/Length 필드 생성 |
| `Should_Generate_PipelineClass_WithComplexGenericReturnType` | `Dictionary<K, List<V>>` 중첩 제네릭 |
| `Should_Generate_PipelineClass_WithTupleReturnType` | 튜플 반환 → Count 미생성 |
| `Should_Generate_PipelineClass_WithUnitReturnType` | `FinT<IO, Unit>` 반환 타입 처리 |
| `Should_Generate_PipelineClass_WithNestedCollectionReturnType` | 중첩 컬렉션 반환 타입 |

### 5. 생성자 (4개)

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Should_Generate_PipelineClass_WithPrimaryConstructor` | C# 12+ Primary Constructor 처리 |
| `Should_Generate_PipelineClass_WithMultipleConstructors` | 다중 생성자 중 최다 파라미터 선택 |
| `Should_Generate_PipelineClass_WithParameterNameConflict` | `logger` → `baseLogger` 이름 충돌 해결 |
| `Should_Generate_PipelineClass_WithNoConstructorParameters` | 생성자 파라미터 없는 경우 |

### 6. 인터페이스 (3개)

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Should_Generate_PipelineClass_WithDirectIPortImplementation` | `IObservablePort` 직접 구현 |
| `Should_Generate_PipelineClass_WithInheritedIPortInterface` | `IUserRepository : IObservablePort` 상속 인터페이스 |
| `Should_Generate_PipelineClass_WithMultipleInterfaces` | 다중 인터페이스 구현 |

### 7. 네임스페이스 (2개)

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Should_Generate_PipelineClass_WithSimpleNamespace` | `namespace MyApp;` 단순 네임스페이스 |
| `Should_Generate_PipelineClass_WithDeepNamespace` | `namespace Company.Domain.Adapters...;` 깊은 네임스페이스 |

### 8. 진단 (4개)

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Should_ReportDiagnostic_WhenDuplicateParameterTypes` | `ActivitySource` 중복 파라미터 → 진단 경고 |
| `Should_ReportDiagnostic_WhenDuplicateMeterFactoryParameter` | `IMeterFactory` 중복 파라미터 → 진단 경고 |
| `Should_ReportDiagnostic_WithCorrectLocation` | 진단 위치가 클래스 선언을 가리키는지 검증 |
| `Should_NotReportDiagnostic_WhenNoParameterDuplication` | 중복 없는 정상 케이스 → 진단 0개 |

---

## 런타임 Observability 구조 검증 테스트

생성된 코드가 실제 실행 환경에서 올바른 Observability 구조를 출력하는지 검증합니다.

### ObservablePortObservabilityTests

태그 구조 규격 준수를 검증합니다.

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `GeneratedCode_RequestMetrics_ShouldContainCorrectTagKeys` | 요청 메트릭 4개 태그 |
| `GeneratedCode_ResponseSuccessMetrics_ShouldContainCorrectTagKeys` | 성공 응답 메트릭 태그 |
| `GeneratedCode_ResponseFailureMetrics_ShouldContainCorrectTagKeys` | 실패 응답 메트릭 태그 (error.type, error.code 포함) |
| `GeneratedCode_MetricsNames_ShouldFollowCorrectPattern` | 메트릭 이름 패턴 검증 |
| `GeneratedCode_TracingRequestTags_ShouldContainCorrectKeys` | Tracing 요청 태그 |
| `GeneratedCode_TracingSuccessTags_ShouldContainCorrectKeys` | Tracing 성공 태그 |
| `GeneratedCode_TracingFailureTags_ShouldContainCorrectKeys` | Tracing 실패 태그 |
| `GeneratedCode_SpanName_ShouldFollowCorrectPattern` | Span 이름 패턴 검증 |
| `GeneratedCode_LoggingTags_ShouldContainCorrectKeys` | 로깅 태그 |
| `GeneratedCode_ErrorHandling_ShouldClassifyErrorTypes` | 에러 타입 분류 (Expected/Exceptional/Aggregate) |
| `GeneratedCode_ManyErrors_ShouldSelectPrimaryErrorCode` | ManyErrors 대표 에러 코드 선택 |

### ObservablePortLoggingStructureTests

실제 로깅 출력의 필드 구조를 스냅샷으로 검증합니다.

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Request_Should_Log_Expected_Fields` | 요청 로그 필드 |
| `SuccessResponse_Should_Log_Expected_Fields` | 성공 응답 로그 필드 |
| `WarningResponse_WithExpectedError_Should_Log_Expected_Fields` | Expected 에러 로그 필드 |
| `WarningResponse_WithExpectedErrorT_Should_Log_Expected_Fields` | 제네릭 Expected 에러 로그 필드 |
| `ErrorResponse_WithExceptionalError_Should_Log_Expected_Fields` | Exceptional 에러 로그 필드 |
| `ErrorResponse_WithAggregateError_Should_Log_Expected_Fields` | Aggregate 에러 로그 필드 |

### ObservablePortMetricsStructureTests

메트릭 태그 구조를 스냅샷으로 검증합니다.

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Handle_DurationTags_ShouldContainSameTagsAsRequestCounter` | Duration 태그 = Request 태그 |
| `Handle_SuccessAndFailureResponses_ShouldHaveDifferentTagCounts` | 성공 5개 vs 실패 7개 태그 |
| `Snapshot_RequestTags` | 요청 메트릭 태그 스냅샷 |
| `Snapshot_SuccessResponse_Tags` | 성공 응답 태그 스냅샷 |
| `Snapshot_FailureResponse_ExpectedError_Tags` | Expected 에러 태그 스냅샷 |
| `Snapshot_FailureResponse_ExceptionalError_Tags` | Exceptional 에러 태그 스냅샷 |
| `Snapshot_FailureResponse_AggregateError_Tags` | Aggregate 에러 태그 스냅샷 |
| `Snapshot_DurationHistogram_Tags` | Duration 히스토그램 태그 스냅샷 |

### ObservablePortTracingStructureTests

Tracing(Activity) 태그 구조를 스냅샷으로 검증합니다.

| 테스트 메서드 | 검증 내용 |
|---------------|-----------|
| `Handle_ShouldCreateActivityWithCorrectName` | Activity span 이름 패턴 |
| `Handle_Success_ShouldHaveSixTags` | 성공 Activity 6개 태그 |
| `Handle_Success_ShouldSetActivityStatusOk` | 성공 시 Activity 상태 = Ok |
| `Handle_Failure_ShouldHaveEightTags` | 실패 Activity 8개 태그 |
| `Handle_Failure_ShouldSetActivityStatusError` | 실패 시 Activity 상태 = Error |
| `Snapshot_SuccessTags` | 성공 Tracing 태그 스냅샷 |
| `Snapshot_FailureResponse_ExpectedError_Tags` | Expected 에러 태그 스냅샷 |
| `Snapshot_FailureResponse_ExceptionalError_Tags` | Exceptional 에러 태그 스냅샷 |
| `Snapshot_FailureResponse_AggregateError_Tags` | Aggregate 에러 태그 스냅샷 |
| `Snapshot_FailureResponse_GenericError_Tags` | Generic 에러 태그 스냅샷 |

---

## 상세 학습

- [Part 3-05. Unit Test 설정](../Part3-Advanced/05-Unit-Testing-Setup/)
- [Part 3-06. Verify Snapshot Test](../Part3-Advanced/06-Verify-Snapshot-Testing/)
- [Part 3-07. Test Scenario](../Part3-Advanced/07-Test-Scenarios/)
