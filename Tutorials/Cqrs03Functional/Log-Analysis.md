# Cqrs03Functional 로그 형식 분석

이 문서는 `Cqrs03Functional.Demo` 애플리케이션 실행 시 출력되는 로그를 분석하여, **Usecase**와 **IAdapter** 인터페이스 중심의 두 가지 로그 범주로 구분하고 각각의 로그 필드(JSON)와 메시지 템플릿을 정리합니다.

## 목차

1. [로그 범주 개요](#로그-범주-개요)
2. [Usecase 로그 형식](#usecase-로그-형식)
3. [IAdapter 로그 형식](#iadapter-로그-형식)
4. [로그 필드 매핑](#로그-필드-매핑)

---

## 로그 범주 개요

Cqrs03Functional 애플리케이션에서 출력되는 로그는 크게 두 가지 범주로 구분됩니다:

### 1. Usecase 로그
- **생성 위치**: `UsecaseLoggerPipeline<TRequest, TResponse>`
- **대상**: CQRS Command/Query Handler (Usecase)
- **로그 레벨**: Information, Warning, Error
- **특징**: Request/Response 전체 객체를 JSON으로 직렬화하여 로깅

### 2. IAdapter 로그
- **생성 위치**: 소스 생성기로 자동 생성된 `*Pipeline` 클래스
- **대상**: `IAdapter` 인터페이스를 구현한 Repository 등의 Adapter
- **로그 레벨**: Debug, Information, Warning, Error
- **특징**: 메서드 파라미터를 개별 필드로 로깅 (파라미터가 6개 이하일 때)

---

## Usecase 로그 형식

### 1. Request 로그 (요청)

#### 메시지 템플릿
```
{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod} {@Request:Request} requesting
```

#### 실제 로그 예시
```
[00:22:36 INF] Application Usecase.Command CreateProductCommand.Handle {"Name": "노트북", "Description": "고성능 개발용 노트북", "Price": 1500000, "StockQuantity": 10, "$type": "Request"} requesting
```

#### 로그 필드 (JSON)

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 (고정값: "Application") | "Application" |
| `RequestCategory` | string | 요청 카테고리 (고정값: "Usecase") | "Usecase" |
| `RequestHandlerCqrs` | string | CQRS 타입 ("Command", "Query", "Unknown") | "Command" |
| `RequestHandler` | string | Handler 클래스 이름 | "CreateProductCommand" |
| `RequestHandlerMethod` | string | Handler 메서드 이름 (고정값: "Handle") | "Handle" |
| `Request` | object | 요청 객체 전체 (JSON 직렬화) | `{"Name": "노트북", "Description": "...", "Price": 1500000, "StockQuantity": 10, "$type": "Request"}` |

#### Scope 필드 (로그 컨텍스트)

다음 필드들은 `BeginScope`를 통해 로그 컨텍스트에 추가됩니다:

| 필드명 | 설명 |
|--------|------|
| `RequestLayer` | 요청 계층 |
| `RequestCategory` | 요청 카테고리 |
| `RequestHandler` | Handler 클래스 이름 |
| `RequestHandlerCqrs` | CQRS 타입 |
| `RequestHandlerMethod` | Handler 메서드 이름 |
| `Request` | 요청 객체 전체 |

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Application.ApplicationRequest` (1001)

---

### 2. Response 로그 - 성공 (Success)

#### 메시지 템플릿
```
{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod} {@Response:Response} responded {Status} in {Elapsed:0.0000} ms
```

#### 실제 로그 예시
```
[00:22:36 INF] Application Usecase.Command CreateProductCommand.Handle {"Value": {"ProductId": "dff8dfb4-db20-443b-9a72-03ceee7d9ba0", "Name": "노트북", "Description": "고성능 개발용 노트북", "Price": 1500000, "StockQuantity": 10, "CreatedAt": "2025-12-30T15:22:36.7938128Z", "$type": "Response"}, "IsSucc": true, "IsFail": false, "$type": "Succ"} responded Success in 200.4511 ms
```

#### 로그 필드 (JSON)

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 (고정값: "Application") | "Application" |
| `RequestCategory` | string | 요청 카테고리 (고정값: "Usecase") | "Usecase" |
| `RequestHandlerCqrs` | string | CQRS 타입 | "Command" |
| `RequestHandler` | string | Handler 클래스 이름 | "CreateProductCommand" |
| `RequestHandlerMethod` | string | Handler 메서드 이름 | "Handle" |
| `Response` | object | 응답 객체 전체 (FinResponse의 Value 포함) | `{"Value": {...}, "IsSucc": true, "IsFail": false, "$type": "Succ"}` |
| `Status` | string | 응답 상태 (고정값: "Success") | "Success" |
| `Elapsed` | double | 경과 시간 (밀리초) | 200.4511 |

#### Scope 필드

| 필드명 | 설명 |
|--------|------|
| `RequestLayer` | 요청 계층 |
| `RequestCategory` | 요청 카테고리 |
| `RequestHandler` | Handler 클래스 이름 |
| `RequestHandlerCqrs` | CQRS 타입 |
| `RequestHandlerMethod` | Handler 메서드 이름 |
| `Response` | 응답 객체의 Value (FinResponse에서 추출한 실제 값) |
| `Status` | 응답 상태 |
| `Elapsed` | 경과 시간 |

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Application.ApplicationResponseSuccess` (1002)

---

### 3. Response 로그 - 실패 (Warning: ErrorCodeExpected)

#### 메시지 템플릿
```
{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod} responded {Status} in {Elapsed:0.0000} ms with {@Error:Error}
```

#### 실제 로그 예시
```
[00:22:36 WRN] Application Usecase.Command CreateProductCommand.Handle responded Failure in 27.7202 ms with {"ErrorType": "ManyErrors", "ErrorCodeId": -2000000006, "Count": 3, "Errors": [{"ErrorType": "ErrorCodeExpected`1", "ErrorCodeId": -1000, "ErrorCode": "ApplicationErrors.UsecaseValidationPipeline.Validator", "Message": "Name: 상품명은 필수입니다", "ErrorCurrentValue": {"PropertyName": "Name", "PropertyValue": "", "PropertyPath": "Name"}}, ...]}
```

#### 로그 필드 (JSON)

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 | "Application" |
| `RequestCategory` | string | 요청 카테고리 | "Usecase" |
| `RequestHandlerCqrs` | string | CQRS 타입 | "Command" |
| `RequestHandler` | string | Handler 클래스 이름 | "CreateProductCommand" |
| `RequestHandlerMethod` | string | Handler 메서드 이름 | "Handle" |
| `Status` | string | 응답 상태 (고정값: "Failure") | "Failure" |
| `Elapsed` | double | 경과 시간 (밀리초) | 27.7202 |
| `Error` | object | 에러 객체 전체 (JSON 직렬화) | `{"ErrorType": "ManyErrors", "ErrorCodeId": -2000000006, "Count": 3, "Errors": [...]}` |

#### Error 객체 구조

**ManyErrors**:
```json
{
  "ErrorType": "ManyErrors",
  "ErrorCodeId": -2000000006,
  "Count": 3,
  "Errors": [
    {
      "ErrorType": "ErrorCodeExpected`1",
      "ErrorCodeId": -1000,
      "ErrorCode": "ApplicationErrors.UsecaseValidationPipeline.Validator",
      "Message": "Name: 상품명은 필수입니다",
      "ErrorCurrentValue": {
        "PropertyName": "Name",
        "PropertyValue": "",
        "PropertyPath": "Name"
      }
    }
  ]
}
```

#### Scope 필드

| 필드명 | 설명 |
|--------|------|
| `RequestLayer` | 요청 계층 |
| `RequestCategory` | 요청 카테고리 |
| `RequestHandler` | Handler 클래스 이름 |
| `RequestHandlerCqrs` | CQRS 타입 |
| `RequestHandlerMethod` | Handler 메서드 이름 |
| `Status` | 응답 상태 |
| `Elapsed` | 경과 시간 |
| `Error` | 에러 객체 |

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Application.ApplicationResponseWarning` (1003)

---

### 4. Response 로그 - 실패 (Error: ErrorCodeExceptional)

#### 메시지 템플릿
```
{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod} responded {Status} in {Elapsed:0.0000} ms with {@Error:Error}
```

#### 실제 로그 예시
```
[00:22:36 ERR] Application Usecase.Command UpdateProductCommand.Handle responded Failure in 16.5624 ms with {"ErrorType": "ErrorCodeExceptional", "ErrorCode": "ApplicationErrors.UsecaseExceptionPipeline.Exception", "ErrorCodeId": -2146233079, "Message": "시뮬레이션된 예외: 데모 목적으로 발생한 예외입니다", "ExceptionDetails": {"TargetSite": "Void MoveNext()", "Message": "...", "Data": [], "InnerException": null, "HelpLink": null, "Source": "Cqrs03Functional.Demo", "HResult": -2146233079, "StackTrace": "..."}}
```

#### 로그 필드 (JSON)

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 | "Application" |
| `RequestCategory` | string | 요청 카테고리 | "Usecase" |
| `RequestHandlerCqrs` | string | CQRS 타입 | "Command" |
| `RequestHandler` | string | Handler 클래스 이름 | "UpdateProductCommand" |
| `RequestHandlerMethod` | string | Handler 메서드 이름 | "Handle" |
| `Status` | string | 응답 상태 (고정값: "Failure") | "Failure" |
| `Elapsed` | double | 경과 시간 (밀리초) | 16.5624 |
| `Error` | object | 에러 객체 전체 (예외 정보 포함) | `{"ErrorType": "ErrorCodeExceptional", "ErrorCode": "...", "ErrorCodeId": -2146233079, "Message": "...", "ExceptionDetails": {...}}` |

#### Error 객체 구조

**ErrorCodeExceptional**:
```json
{
  "ErrorType": "ErrorCodeExceptional",
  "ErrorCode": "ApplicationErrors.UsecaseExceptionPipeline.Exception",
  "ErrorCodeId": -2146233079,
  "Message": "시뮬레이션된 예외: 데모 목적으로 발생한 예외입니다",
  "ExceptionDetails": {
    "TargetSite": "Void MoveNext()",
    "Message": "...",
    "Data": [],
    "InnerException": null,
    "HelpLink": null,
    "Source": "Cqrs03Functional.Demo",
    "HResult": -2146233079,
    "StackTrace": "..."
  }
}
```

#### Scope 필드

| 필드명 | 설명 |
|--------|------|
| `RequestLayer` | 요청 계층 |
| `RequestCategory` | 요청 카테고리 |
| `RequestHandler` | Handler 클래스 이름 |
| `RequestHandlerCqrs` | CQRS 타입 |
| `RequestHandlerMethod` | Handler 메서드 이름 |
| `Status` | 응답 상태 |
| `Elapsed` | 경과 시간 |
| `Error` | 에러 객체 |

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Application.ApplicationResponseError` (1004)

---

## IAdapter 로그 형식

IAdapter 로그는 소스 생성기(`AdapterPipelineGenerator`)에 의해 자동 생성된 Pipeline 클래스에서 출력됩니다. `[GeneratePipeline]` 애트리뷰트가 적용된 클래스에 대해 자동으로 Pipeline이 생성됩니다.

**중요**: IAdapter 로그는 **MinimumLevel 설정에 따라 출력 형식이 달라집니다**:
- **Debug 레벨**: 파라미터와 반환값을 포함한 상세 로그 출력
- **Information 레벨**: 파라미터와 반환값 없이 간단한 메시지만 출력

### 1. Request 로그

#### 1-1. Debug 레벨 (파라미터 포함)

#### 메시지 템플릿 (파라미터 개수에 따라 동적 생성)

**파라미터가 6개 이하인 경우** (LoggerMessage.Define 사용):
```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod} {Param1} {Param2} ... requesting
```

**파라미터가 6개 초과인 경우** (logger.LogDebug 직접 사용):
```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod} {Param1} {Param2} ... requesting
```

#### 실제 로그 예시

**단일 파라미터**:
```
[00:22:36 DBG] Adapter Repository InMemoryProductRepository.ExistsByName 노트북 requesting
```

**복합 파라미터**:
```
[00:22:36 DBG] Adapter Repository InMemoryProductRepository.Create Product { Id = dff8dfb4-db20-443b-9a72-03ceee7d9ba0, Name = 노트북, Description = 고성능 개발용 노트북, Price = 1500000, StockQuantity = 10, CreatedAt = 2025-12-30 오후 3:22:36, UpdatedAt =  } requesting
```

**파라미터 없음**:
```
[00:22:36 DBG] Adapter Repository InMemoryProductRepository.GetAll requesting
```

#### 로그 필드

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 (고정값: "Adapter") | "Adapter" |
| `RequestCategory` | string | 요청 카테고리 (IAdapter.RequestCategory 속성 값) | "Repository" |
| `RequestHandler` | string | Handler 클래스 이름 | "InMemoryProductRepository" |
| `RequestHandlerMethod` | string | 호출된 메서드 이름 | "ExistsByName", "Create", "GetAll" |
| `{ParamName}` | dynamic | 메서드 파라미터 (각 파라미터가 개별 필드로 로깅) | "노트북", `Product {...}` |
| `{ParamName}Count` | int | 컬렉션 타입 파라미터의 경우 Count 필드 (선택적) | - |

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Adapter.AdapterRequest` (1001)

---

#### 1-2. Information 레벨 (파라미터 제외)

**MinimumLevel이 Information으로 설정된 경우**, Request 로그는 파라미터 정보 없이 간단한 메시지만 출력됩니다.

#### 메시지 템플릿
```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod} requesting
```

#### 실제 로그 예시
```
[00:22:36 INF] Adapter Repository InMemoryProductRepository.ExistsByName requesting
```

```
[00:22:36 INF] Adapter Repository InMemoryProductRepository.Create requesting
```

```
[00:22:36 INF] Adapter Repository InMemoryProductRepository.GetAll requesting
```

#### 로그 필드

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 (고정값: "Adapter") | "Adapter" |
| `RequestCategory` | string | 요청 카테고리 (IAdapter.RequestCategory 속성 값) | "Repository" |
| `RequestHandler` | string | Handler 클래스 이름 | "InMemoryProductRepository" |
| `RequestHandlerMethod` | string | 호출된 메서드 이름 | "ExistsByName", "Create", "GetAll" |

**참고**: Information 레벨에서는 메서드 파라미터 정보가 출력되지 않습니다.

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Adapter.AdapterRequest` (1001)

---

### 2. Response 로그 - 성공 (Success)

#### 2-1. Debug 레벨 (반환값 포함)

#### 메시지 템플릿

**일반 반환 타입**:
```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod} {Response} responded {Status} in {Elapsed:0.0000} ms
```

**컬렉션 반환 타입**:
```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod} {Response} {ResponseCount} responded {Status} in {Elapsed:0.0000} ms
```

#### 실제 로그 예시

**단일 반환값**:
```
[00:22:36 DBG] Adapter Repository InMemoryProductRepository.ExistsByName False responded Success in 96.0635 ms
```

**복합 반환값**:
```
[00:22:36 DBG] Adapter Repository InMemoryProductRepository.Create Product { Id = dff8dfb4-db20-443b-9a72-03ceee7d9ba0, Name = 노트북, Description = 고성능 개발용 노트북, Price = 1500000, StockQuantity = 10, CreatedAt = 2025-12-30 오후 3:22:36, UpdatedAt =  } responded Success in 29.9305 ms
```

**컬렉션 반환값**:
```
[00:22:37 DBG] Adapter Repository InMemoryProductRepository.GetAll ["Product {...}", "Product {...}"] responded Success in 36.8517 ms
```

#### 로그 필드

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 | "Adapter" |
| `RequestCategory` | string | 요청 카테고리 | "Repository" |
| `RequestHandler` | string | Handler 클래스 이름 | "InMemoryProductRepository" |
| `RequestHandlerMethod` | string | 호출된 메서드 이름 | "ExistsByName" |
| `Response` | dynamic | 반환값 (ToString() 결과) | "False", `Product {...}`, `["Product {...}", ...]` |
| `ResponseCount` | int | 컬렉션 반환 타입의 경우 Count (선택적) | 2 |
| `Status` | string | 응답 상태 (고정값: "Success") | "Success" |
| `Elapsed` | double | 경과 시간 (밀리초) | 96.0635 |

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Adapter.AdapterResponseSuccess` (1002)

---

#### 2-2. Information 레벨 (반환값 제외)

**MinimumLevel이 Information으로 설정된 경우**, Response Success 로그는 반환값 정보 없이 간단한 메시지만 출력됩니다.

#### 메시지 템플릿
```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod} responded {Status} in {Elapsed:0.0000} ms
```

#### 실제 로그 예시

**단일 반환값 메서드**:
```
[00:22:36 INF] Adapter Repository InMemoryProductRepository.ExistsByName responded Success in 96.0635 ms
```

**복합 반환값 메서드**:
```
[00:22:36 INF] Adapter Repository InMemoryProductRepository.Create responded Success in 29.9305 ms
```

**컬렉션 반환값 메서드**:
```
[00:22:37 INF] Adapter Repository InMemoryProductRepository.GetAll responded Success in 36.8517 ms
```

#### 로그 필드

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 | "Adapter" |
| `RequestCategory` | string | 요청 카테고리 | "Repository" |
| `RequestHandler` | string | Handler 클래스 이름 | "InMemoryProductRepository" |
| `RequestHandlerMethod` | string | 호출된 메서드 이름 | "ExistsByName" |
| `Status` | string | 응답 상태 (고정값: "Success") | "Success" |
| `Elapsed` | double | 경과 시간 (밀리초) | 96.0635 |

**참고**: Information 레벨에서는 반환값(Response)과 ResponseCount 정보가 출력되지 않습니다.

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Adapter.AdapterResponseSuccess` (1002)

---

### 3. Response 로그 - 실패 (Warning: Expected Error)

#### 메시지 템플릿
```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod} responded failure in {Elapsed:0.0000} ms with {@Error}
```

#### 실제 로그 예시
```
[00:22:36 WRN] Adapter Repository InMemoryProductRepository.GetById responded failure in 3.6537 ms with {"ErrorType": "Expected", "ErrorCodeId": 0, "Message": "상품 ID 'c5046bfd-3d02-41a2-b90e-e50e86463656'을(를) 찾을 수 없습니다"}
```

#### 로그 필드

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 | "Adapter" |
| `RequestCategory` | string | 요청 카테고리 | "Repository" |
| `RequestHandler` | string | Handler 클래스 이름 | "InMemoryProductRepository" |
| `RequestHandlerMethod` | string | 호출된 메서드 이름 | "GetById" |
| `Elapsed` | double | 경과 시간 (밀리초) | 3.6537 |
| `Error` | object | 에러 객체 (JSON 직렬화) | `{"ErrorType": "Expected", "ErrorCodeId": 0, "Message": "..."}` |

#### Error 객체 구조

**Expected Error**:
```json
{
  "ErrorType": "Expected",
  "ErrorCodeId": 0,
  "Message": "상품 ID 'c5046bfd-3d02-41a2-b90e-e50e86463656'을(를) 찾을 수 없습니다"
}
```

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Adapter.AdapterResponseWarning` (1003)

---

### 4. Response 로그 - 실패 (Error: Exceptional Error)

#### 메시지 템플릿
```
{RequestLayer} {RequestCategory} {RequestHandler}.{RequestHandlerMethod} responded failure in {Elapsed:0.0000} ms with {@Error}
```

#### 실제 로그 예시
```
[00:22:36 ERR] Adapter Repository InMemoryProductRepository.SomeMethod responded failure in 10.1234 ms with {"ErrorType": "Exceptional", "ErrorCodeId": -2146233079, "Message": "...", "ExceptionDetails": {...}}
```

#### 로그 필드

| 필드명 | 타입 | 설명 | 예시 |
|--------|------|------|------|
| `RequestLayer` | string | 요청 계층 | "Adapter" |
| `RequestCategory` | string | 요청 카테고리 | "Repository" |
| `RequestHandler` | string | Handler 클래스 이름 | "InMemoryProductRepository" |
| `RequestHandlerMethod` | string | 호출된 메서드 이름 | "SomeMethod" |
| `Elapsed` | double | 경과 시간 (밀리초) | 10.1234 |
| `Error` | object | 에러 객체 (예외 정보 포함) | `{"ErrorType": "Exceptional", "ErrorCodeId": -2146233079, "Message": "...", "ExceptionDetails": {...}}` |

#### EventId
- **EventId**: `ObservabilityFields.EventIds.Adapter.AdapterResponseError` (1004)

---

## 로그 필드 매핑

### Usecase 로그 필드 매핑

| 로그 필드 | Scope 키 | 설명 |
|-----------|----------|------|
| `RequestLayer` | `RequestLayer` | 요청 계층 ("Application") |
| `RequestCategory` | `RequestCategory` | 요청 카테고리 ("Usecase") |
| `RequestHandlerCqrs` | `RequestHandlerCqrs` | CQRS 타입 ("Command", "Query") |
| `RequestHandler` | `RequestHandler` | Handler 클래스 이름 |
| `RequestHandlerMethod` | `RequestHandlerMethod` | Handler 메서드 이름 ("Handle") |
| `Request` | `Request` | 요청 객체 전체 |
| `Response` | `Response` | 응답 객체의 Value (성공 시) |
| `Status` | `Status` | 응답 상태 ("Success", "Failure") |
| `Elapsed` | `Elapsed` | 경과 시간 (밀리초) |
| `Error` | `Error` | 에러 객체 (실패 시) |

### IAdapter 로그 필드 매핑

| 로그 필드 | 설명 |
|-----------|------|
| `RequestLayer` | 요청 계층 ("Adapter") |
| `RequestCategory` | 요청 카테고리 (IAdapter.RequestCategory 속성 값) |
| `RequestHandler` | Handler 클래스 이름 |
| `RequestHandlerMethod` | 호출된 메서드 이름 |
| `{ParamName}` | 메서드 파라미터 (동적) |
| `{ParamName}Count` | 컬렉션 파라미터의 Count (선택적) |
| `Response` | 반환값 (ToString() 결과) |
| `ResponseCount` | 컬렉션 반환값의 Count (선택적) |
| `Status` | 응답 상태 ("Success", "failure") |
| `Elapsed` | 경과 시간 (밀리초) |
| `Error` | 에러 객체 (실패 시) |

---

## 참고사항

### Usecase 로그 특징

1. **전체 객체 직렬화**: Request/Response 객체 전체를 JSON으로 직렬화하여 로깅
2. **Structured Logging**: `BeginScope`를 사용하여 구조화된 로그 컨텍스트 제공
3. **CQRS 구분**: Command/Query를 명시적으로 구분하여 로깅
4. **FinResponse 지원**: `FinResponse<T>` 타입의 `IsSucc`/`IsFail` 패턴을 지원

### IAdapter 로그 특징

1. **로그 레벨별 출력 차이**: MinimumLevel 설정에 따라 출력 형식이 달라짐
   - **Debug 레벨**: 파라미터와 반환값을 포함한 상세 로그
   - **Information 레벨**: 파라미터와 반환값 없이 간단한 메시지만 출력
2. **파라미터 개별 로깅**: Debug 레벨에서 메서드 파라미터를 개별 필드로 로깅 (파라미터가 6개 이하일 때)
3. **소스 생성기 기반**: `[GeneratePipeline]` 애트리뷰트로 자동 생성
4. **LoggerMessage.Define 최적화**: 파라미터가 6개 이하일 때 `LoggerMessage.Define` 사용 (성능 최적화)
5. **폴백 메커니즘**: 파라미터가 6개 초과일 때 `logger.LogDebug()` 직접 사용
6. **컬렉션 지원**: Debug 레벨에서 컬렉션 타입 파라미터/반환값의 경우 Count 필드 자동 추가
7. **조건부 로깅**: `_isDebugEnabled`와 `_isInformationEnabled` 플래그를 확인하여 적절한 로그 메서드 호출

### 로그 레벨 정리

| 로그 타입 | Request | Response (Success) | Response (Warning) | Response (Error) |
|-----------|---------|-------------------|-------------------|------------------|
| **Usecase** | Information | Information | Warning | Error |
| **IAdapter** | Debug / Information* | Debug / Information* | Warning | Error |

\* **IAdapter 로그 레벨 동작**:
- **Debug 레벨**: 파라미터와 반환값을 포함한 상세 로그 출력
- **Information 레벨**: 파라미터와 반환값 없이 간단한 메시지만 출력 (MinimumLevel이 Information 이상일 때)
- MinimumLevel이 Information으로 설정되면 Debug 레벨 로그는 출력되지 않고, Information 레벨의 간소화된 로그만 출력됩니다.

---

## 관련 코드 위치

- **Usecase 로그**: `Src/Functorium/Applications/Pipelines/UsecaseLoggerPipeline.cs`
- **Usecase 로거 확장**: `Src/Functorium/Adapters/Observabilities/Loggers/UsecaseLoggerExtensions.cs`
- **IAdapter Pipeline 생성기**: `Src/Functorium.Adapters.SourceGenerator/AdapterPipelineGenerator.cs`
- **Observability 필드 정의**: `Src/Functorium/Adapters/Observabilities/ObservabilityFields.cs`

