---
title: "Test Scenario Catalog"
---

This is the complete test list for ObservablePortGenerator. For detailed descriptions of each scenario, refer to [Part 3-07. Test Scenario](../Part3-Advanced/07-Test-Scenarios/).

---

## Test Organization

ObservablePortGenerator tests are organized along two axes:

- **Generator snapshot tests** — Verify code generation results by comparing them with `.verified.txt` files
- **Runtime Observability structure verification** — Verify that the generated code outputs the correct tag/field structure at actual runtime

---

## Generator Snapshot Tests

The `ObservablePortGeneratorTests` class verifies 8 categories x 31 scenarios.

### 1. Basic Generation (1)

| Test Method | Verification |
|-------------|-------------|
| `ObservablePortGenerator_ShouldGenerate_GenerateObservablePortAttribute` | Auto-generation of `[GenerateObservablePort]` Attribute |

### 2. Basic Adapter (3)

| Test Method | Verification |
|-------------|-------------|
| `Should_Generate_PipelineClass_WithSingleMethod` | Observable class generation for single-method adapter |
| `Should_Generate_PipelineClass_WithMultipleMethods` | All method overrides for multi-method adapter |
| `Should_NotGenerate_PipelineClass_WhenNoMethods` | Adapter with no methods — no pipeline generated |

### 3. Parameters (8)

| Test Method | Verification |
|-------------|-------------|
| `Should_Generate_LoggerMessageDefine_WithZeroParameters` | 0 parameters → 4 total fields → LoggerMessage.Define |
| `Should_Generate_LoggerMessageDefine_WithOneParameter` | 1 parameter → 5 total fields → LoggerMessage.Define |
| `Should_Generate_LoggerMessageDefine_WithTwoParameters` | 2 parameters → 6 total fields → LoggerMessage.Define (boundary) |
| `Should_Generate_LogDebugFallback_WithThreeParameters` | 3 parameters → 7 total fields → logger.LogDebug() fallback |
| `Should_Generate_LogDebugFallback_WithManyParameters` | Many parameters → logger.LogDebug() fallback |
| `Should_Generate_CollectionCountFields` | Collection parameter → Count field added |
| `Should_NotGenerate_Count_ForTupleParameter` | Tuple parameter → Count not generated |
| `Should_Generate_CollectionCountFields_WithArrayParameter` | Array parameter → Length field added |

### 4. Return Types (6)

| Test Method | Verification |
|-------------|-------------|
| `Should_Generate_PipelineClass_WithSimpleReturnType` | Simple type extraction from `FinT<IO, int>` etc. |
| `Should_Generate_PipelineClass_WithCollectionReturnType` | `List<T>`, `T[]` → Count/Length field generation |
| `Should_Generate_PipelineClass_WithComplexGenericReturnType` | Nested generic `Dictionary<K, List<V>>` |
| `Should_Generate_PipelineClass_WithTupleReturnType` | Tuple return → Count not generated |
| `Should_Generate_PipelineClass_WithUnitReturnType` | `FinT<IO, Unit>` return type handling |
| `Should_Generate_PipelineClass_WithNestedCollectionReturnType` | Nested collection return type |

### 5. Constructors (4)

| Test Method | Verification |
|-------------|-------------|
| `Should_Generate_PipelineClass_WithPrimaryConstructor` | C# 12+ Primary Constructor handling |
| `Should_Generate_PipelineClass_WithMultipleConstructors` | Selecting the constructor with most parameters among multiple |
| `Should_Generate_PipelineClass_WithParameterNameConflict` | `logger` → `baseLogger` name conflict resolution |
| `Should_Generate_PipelineClass_WithNoConstructorParameters` | Case with no constructor parameters |

### 6. Interfaces (3)

| Test Method | Verification |
|-------------|-------------|
| `Should_Generate_PipelineClass_WithDirectIPortImplementation` | Direct `IObservablePort` implementation |
| `Should_Generate_PipelineClass_WithInheritedIPortInterface` | Inherited interface `IUserRepository : IObservablePort` |
| `Should_Generate_PipelineClass_WithMultipleInterfaces` | Multiple interface implementation |

### 7. Namespaces (2)

| Test Method | Verification |
|-------------|-------------|
| `Should_Generate_PipelineClass_WithSimpleNamespace` | Simple namespace `namespace MyApp;` |
| `Should_Generate_PipelineClass_WithDeepNamespace` | Deep namespace `namespace Company.Domain.Adapters...;` |

### 8. Diagnostics (4)

| Test Method | Verification |
|-------------|-------------|
| `Should_ReportDiagnostic_WhenDuplicateParameterTypes` | `ActivitySource` duplicate parameter → diagnostic warning |
| `Should_ReportDiagnostic_WhenDuplicateMeterFactoryParameter` | `IMeterFactory` duplicate parameter → diagnostic warning |
| `Should_ReportDiagnostic_WithCorrectLocation` | Verify diagnostic location points to the class declaration |
| `Should_NotReportDiagnostic_WhenNoParameterDuplication` | Normal case with no duplication → 0 diagnostics |

---

## Runtime Observability Structure Verification Tests

These verify that the generated code outputs the correct Observability structure in actual runtime environments.

### ObservablePortObservabilityTests

Verifies tag structure specification compliance.

| Test Method | Verification |
|-------------|-------------|
| `GeneratedCode_RequestMetrics_ShouldContainCorrectTagKeys` | Request metrics 4 tags |
| `GeneratedCode_ResponseSuccessMetrics_ShouldContainCorrectTagKeys` | Success response metrics tags |
| `GeneratedCode_ResponseFailureMetrics_ShouldContainCorrectTagKeys` | Failure response metrics tags (including error.type, error.code) |
| `GeneratedCode_MetricsNames_ShouldFollowCorrectPattern` | Metrics name pattern verification |
| `GeneratedCode_TracingRequestTags_ShouldContainCorrectKeys` | Tracing request tags |
| `GeneratedCode_TracingSuccessTags_ShouldContainCorrectKeys` | Tracing success tags |
| `GeneratedCode_TracingFailureTags_ShouldContainCorrectKeys` | Tracing failure tags |
| `GeneratedCode_SpanName_ShouldFollowCorrectPattern` | Span name pattern verification |
| `GeneratedCode_LoggingTags_ShouldContainCorrectKeys` | Logging tags |
| `GeneratedCode_ErrorHandling_ShouldClassifyErrorTypes` | Error type classification (Expected/Exceptional/Aggregate) |
| `GeneratedCode_ManyErrors_ShouldSelectPrimaryErrorCode` | ManyErrors primary error code selection |

### ObservablePortLoggingStructureTests

Verifies actual logging output field structure with snapshots.

| Test Method | Verification |
|-------------|-------------|
| `Request_Should_Log_Expected_Fields` | Request log fields |
| `SuccessResponse_Should_Log_Expected_Fields` | Success response log fields |
| `WarningResponse_WithExpectedError_Should_Log_Expected_Fields` | Expected error log fields |
| `WarningResponse_WithExpectedErrorT_Should_Log_Expected_Fields` | Generic Expected error log fields |
| `ErrorResponse_WithExceptionalError_Should_Log_Expected_Fields` | Exceptional error log fields |
| `ErrorResponse_WithAggregateError_Should_Log_Expected_Fields` | Aggregate error log fields |

### ObservablePortMetricsStructureTests

Verifies metrics tag structure with snapshots.

| Test Method | Verification |
|-------------|-------------|
| `Handle_DurationTags_ShouldContainSameTagsAsRequestCounter` | Duration tags = Request tags |
| `Handle_SuccessAndFailureResponses_ShouldHaveDifferentTagCounts` | Success 5 tags vs Failure 7 tags |
| `Snapshot_RequestTags` | Request metrics tags snapshot |
| `Snapshot_SuccessResponse_Tags` | Success response tags snapshot |
| `Snapshot_FailureResponse_ExpectedError_Tags` | Expected error tags snapshot |
| `Snapshot_FailureResponse_ExceptionalError_Tags` | Exceptional error tags snapshot |
| `Snapshot_FailureResponse_AggregateError_Tags` | Aggregate error tags snapshot |
| `Snapshot_DurationHistogram_Tags` | Duration histogram tags snapshot |

### ObservablePortTracingStructureTests

Verifies Tracing (Activity) tag structure with snapshots.

| Test Method | Verification |
|-------------|-------------|
| `Handle_ShouldCreateActivityWithCorrectName` | Activity span name pattern |
| `Handle_Success_ShouldHaveSixTags` | Success Activity 6 tags |
| `Handle_Success_ShouldSetActivityStatusOk` | Success Activity status = Ok |
| `Handle_Failure_ShouldHaveEightTags` | Failure Activity 8 tags |
| `Handle_Failure_ShouldSetActivityStatusError` | Failure Activity status = Error |
| `Snapshot_SuccessTags` | Success Tracing tags snapshot |
| `Snapshot_FailureResponse_ExpectedError_Tags` | Expected error tags snapshot |
| `Snapshot_FailureResponse_ExceptionalError_Tags` | Exceptional error tags snapshot |
| `Snapshot_FailureResponse_AggregateError_Tags` | Aggregate error tags snapshot |
| `Snapshot_FailureResponse_GenericError_Tags` | Generic error tags snapshot |

---

## Further Reading

- [Part 3-05. Unit Test Setup](../Part3-Advanced/05-Unit-Testing-Setup/)
- [Part 3-06. Verify Snapshot Test](../Part3-Advanced/06-Verify-Snapshot-Testing/)
- [Part 3-07. Test Scenario](../Part3-Advanced/07-Test-Scenarios/)
