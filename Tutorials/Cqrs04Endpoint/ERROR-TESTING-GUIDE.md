# 에러 시나리오 테스트 가이드

## 개요

이 문서는 `UsecaseMetricsPipeline`의 에러 태그 기능을 검증하기 위한 테스트 가이드입니다.

## 목적

- `error.type` 태그 검증 (expected, exceptional, aggregate)
- `error.code` 태그 검증 (에러 코드 문자열)
- `response.status` 태그 검증 (success, failure)
- `IHasErrorCode` 인터페이스 기반 에러 코드 추출 검증

## 테스트 파일

### 1. TestErrorCommand.cs

에러 시나리오 테스트를 위한 Command 유스케이스입니다.

**위치**: `Src/Cqrs04Endpoint.WebApi/Usecases/TestErrorCommand.cs`

**지원 시나리오**:

| 시나리오 | 설명 | 메트릭 태그 |
|---------|------|-----------|
| `Success` | 성공 케이스 (에러 없음) | `response.status=success` |
| `SingleExpected` | 단일 Expected 에러 (비즈니스 에러) | `response.status=failure`, `error.type=expected`, `error.code=TestErrors.TestErrorCommand.BusinessRuleViolation` |
| `SingleExceptional` | 단일 Exceptional 에러 (시스템 에러) | `response.status=failure`, `error.type=exceptional`, `error.code=TestErrors.TestErrorCommand.SystemFailure` |
| `ManyExpected` | 복합 Expected 에러 (여러 비즈니스 에러) | `response.status=failure`, `error.type=aggregate`, `error.code=TestErrors.TestErrorCommand.BusinessRuleViolation` (첫 번째) |
| `ManyMixed` | 복합 Mixed 에러 (Expected + Exceptional) | `response.status=failure`, `error.type=aggregate`, `error.code=TestErrors.TestErrorCommand.SystemFailure` (Exceptional 우선) |
| `GenericExpected` | 제네릭 Expected 에러 (`ErrorCodeExpected<T>`) | `response.status=failure`, `error.type=expected`, `error.code=TestErrors.TestErrorCommand.GenericError` |

### 2. TestErrorEndpoint.cs

FastEndpoints 기반 HTTP 엔드포인트입니다.

**위치**: `Src/Cqrs04Endpoint.WebApi/Endpoints/TestErrorEndpoint.cs`

**엔드포인트**: `POST /api/test-error`

**Request Body**:
```json
{
  "Scenario": "SingleExpected",
  "TestMessage": "Test error message"
}
```

### 3. test-error-scenarios.http

HTTP 요청 테스트 파일입니다.

**위치**: `Tutorials/Cqrs04Endpoint/test-error-scenarios.http`

**포함 내용**:
- 각 시나리오별 HTTP 요청 예제
- 예상 메트릭 태그 설명
- 전체 시나리오 순차 실행 스크립트

## 실행 방법

### 1. 애플리케이션 실행

```bash
cd Tutorials/Cqrs04Endpoint/Src/Cqrs04Endpoint.WebApi
dotnet run
```

기본 포트: `http://localhost:5000`

### 2. HTTP 요청 실행

#### Visual Studio Code 사용

1. `test-error-scenarios.http` 파일 열기
2. 각 요청 위의 "Send Request" 버튼 클릭
3. 또는 "Send All Requests" 로 전체 실행

#### curl 사용

```bash
# 1. Success
curl -X POST http://localhost:5000/api/test-error \
  -H "Content-Type: application/json" \
  -d '{"Scenario": "Success", "TestMessage": "Test 1"}'

# 2. SingleExpected
curl -X POST http://localhost:5000/api/test-error \
  -H "Content-Type: application/json" \
  -d '{"Scenario": "SingleExpected", "TestMessage": "Test 2"}'

# 3. SingleExceptional
curl -X POST http://localhost:5000/api/test-error \
  -H "Content-Type: application/json" \
  -d '{"Scenario": "SingleExceptional", "TestMessage": "Test 3"}'

# 4. ManyExpected
curl -X POST http://localhost:5000/api/test-error \
  -H "Content-Type: application/json" \
  -d '{"Scenario": "ManyExpected", "TestMessage": "Test 4"}'

# 5. ManyMixed
curl -X POST http://localhost:5000/api/test-error \
  -H "Content-Type: application/json" \
  -d '{"Scenario": "ManyMixed", "TestMessage": "Test 5"}'

# 6. GenericExpected
curl -X POST http://localhost:5000/api/test-error \
  -H "Content-Type: application/json" \
  -d '{"Scenario": "GenericExpected", "TestMessage": "Test 6"}'
```

### 3. 메트릭 확인

애플리케이션 콘솔 출력에서 메트릭을 확인합니다.

**출력 예시**:

```
Export functorium.demo.application.usecase.command.response, Number of datapoints: 1
Data point attributes:
 - request.layer: application
 - request.category: usecase
 - request.handler.cqrs: Command
 - request.handler: TestErrorCommand
 - request.handler.method: Handle
 - response.status: failure
 - error.type: expected
 - error.code: TestErrors.TestErrorCommand.BusinessRuleViolation
Value: 1
```

## 검증 포인트

### 1. Success 케이스

✅ **확인 사항**:
- `response.status=success`
- `error.type` 태그 **없음**
- `error.code` 태그 **없음**

### 2. SingleExpected 케이스

✅ **확인 사항**:
- `response.status=failure`
- `error.type=expected`
- `error.code=TestErrors.TestErrorCommand.BusinessRuleViolation`

**코드 참조**: [UsecaseMetricsPipeline.cs:166-169](../../Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs#L166-L169)

```csharp
// 3순위: IHasErrorCode - Expected 에러 (모든 ErrorCodeExpected<...> 변형 포함)
IHasErrorCode hasErrorCode => (
    ErrorType: ObservabilityNaming.ErrorTypes.Expected,
    ErrorCode: hasErrorCode.ErrorCode  // ✅ 인터페이스 사용
),
```

### 3. SingleExceptional 케이스

✅ **확인 사항**:
- `response.status=failure`
- `error.type=exceptional`
- `error.code=TestErrors.TestErrorCommand.SystemFailure`

**코드 참조**: [UsecaseMetricsPipeline.cs:161-164](../../Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs#L161-L164)

```csharp
// 2순위: ErrorCodeExceptional - Exceptional을 먼저 매칭
// (IHasErrorCode보다 먼저 와야 함!)
ErrorCodeExceptional exceptional => (
    ErrorType: ObservabilityNaming.ErrorTypes.Exceptional,
    ErrorCode: exceptional.ErrorCode
),
```

### 4. ManyExpected 케이스

✅ **확인 사항**:
- `response.status=failure`
- `error.type=aggregate`
- `error.code=TestErrors.TestErrorCommand.BusinessRuleViolation` (첫 번째 에러)

**코드 참조**: [UsecaseMetricsPipeline.cs:184-196](../../Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs#L184-L196)

```csharp
private static string GetPrimaryErrorCode(ManyErrors many)
{
    // 1순위: Exceptional 에러 (시스템 에러가 더 심각)
    foreach (var e in many.Errors)
    {
        if (e.IsExceptional)
            return GetErrorCode(e);
    }

    // 2순위: 첫 번째 에러
    return many.Errors.Head.Match(
        Some: GetErrorCode,
        None: () => nameof(ManyErrors));
}
```

### 5. ManyMixed 케이스

✅ **확인 사항**:
- `response.status=failure`
- `error.type=aggregate`
- `error.code=TestErrors.TestErrorCommand.SystemFailure` (**Exceptional 우선**)

**중요**: 복합 에러에서 Exceptional 에러가 있으면 우선적으로 선택됩니다.

### 6. GenericExpected 케이스

✅ **확인 사항**:
- `response.status=failure`
- `error.type=expected`
- `error.code=TestErrors.TestErrorCommand.GenericError`

**중요**: `ErrorCodeExpected<T>` 제네릭 타입도 `IHasErrorCode` 인터페이스를 통해 올바르게 처리됩니다.

## 패턴 매칭 순서의 중요성

**코드 참조**: [UsecaseMetricsPipeline.cs:143-149](../../Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs#L143-L149)

```csharp
/// <remarks>
/// 패턴 매칭 순서가 중요합니다:
/// 1. ManyErrors - 특수 처리 필요
/// 2. ErrorCodeExceptional - Exceptional 명시적 처리
/// 3. IHasErrorCode - Expected 에러 (ErrorCodeExceptional도 이 인터페이스를 구현하므로 순서 중요!)
/// 4. Fallback - 알 수 없는 에러 타입
/// </remarks>
```

⚠️ **주의**: `ErrorCodeExceptional`도 `IHasErrorCode`를 구현하므로, `IHasErrorCode` 패턴이 먼저 오면 Exceptional 에러가 Expected로 잘못 분류됩니다!

## 트러블슈팅

### 메트릭이 출력되지 않음

1. `Program.cs`에 `UsecaseMetricsPipeline` 등록 확인:
   ```csharp
   builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsecaseMetricsPipeline<,>));
   ```

2. OpenTelemetry ConsoleExporter 설정 확인:
   ```csharp
   .ConfigureMetrics(metrics => metrics.Configure(b => b.AddConsoleExporter()))
   ```

### 에러 태그가 없음

- `error.type`과 `error.code`는 **실패(failure) 응답에만** 추가됩니다
- 성공 응답에는 에러 태그가 없는 것이 정상입니다

### 잘못된 error.type

- 패턴 매칭 순서를 확인하세요
- `ErrorCodeExceptional`이 `IHasErrorCode`보다 먼저 매칭되어야 합니다

## 관련 문서

- [UsecaseMetricsPipeline.cs](../../Src/Functorium/Applications/Pipelines/UsecaseMetricsPipeline.cs)
- [Usecase Pipeline Review](.sprints/usecase-metrics-pipeline-review.md)
- [IHasErrorCode Interface](../../Src/Functorium/Abstractions/Errors/IHasErrorCode.cs)

## 변경 이력

| 날짜 | 변경 내용 |
|------|----------|
| 2026-01-05 | 에러 테스트 가이드 초안 작성 |
| 2026-01-05 | TestErrorCommand, TestErrorEndpoint, test-error-scenarios.http 추가 |
