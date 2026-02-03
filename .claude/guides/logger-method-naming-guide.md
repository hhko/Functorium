# Logger Method Naming Guide

LoggerExtensions 클래스에서 로그 메서드 이름을 작성할 때 따라야 하는 규칙입니다.

## 네이밍 패턴

```
Log{Context}{Phase}{Status}
```

## 구성 요소

| 요소 | 설명 | 값 |
|------|------|-----|
| `Context` | 로깅 대상 컨텍스트 | `Usecase`, `DomainEventHandler`, `DomainEventPublisher`, `DomainEventsPublisher` |
| `Phase` | 요청/응답 단계 | `Request`, `Response` |
| `Status` | 결과 상태 (Response에만 사용) | (없음), `Success`, `Warning`, `Error`, `PartialFailure` |

## 규칙

### 1. Phase 규칙
- `Request`: 작업 시작 시점 로그 (Status 없음)
- `Response`: 작업 완료 시점 로그 (Status 필수)

### 2. Status 규칙
| Status | 로그 레벨 | 용도 |
|--------|-----------|------|
| `Success` | Information | 정상 완료 |
| `Warning` | Warning | 예상된 에러 (Expected Error) |
| `Error` | Error | 예외적 에러 (Exceptional Error) |
| `PartialFailure` | Warning | 부분 실패 (일부만 성공) |

### 3. Context 규칙
- 단수/복수 구분: 단일 항목은 단수, 다중 항목은 복수 사용
  - `DomainEventPublisher`: 단일 이벤트 발행
  - `DomainEventsPublisher`: 다중 이벤트 발행 (Aggregate의 모든 이벤트)

## 예시

### UsecaseLoggerExtensions
```csharp
// Request
LogUsecaseRequest<T>(...)

// Response
LogUsecaseResponseSuccess<T>(...)
LogUsecaseResponseWarning(...)
LogUsecaseResponseError(...)
```

### DomainEventHandlerLoggerExtensions
```csharp
// Request
LogDomainEventHandlerRequest<TEvent>(...)

// Response
LogDomainEventHandlerResponseSuccess(...)
LogDomainEventHandlerResponseWarning(...)
LogDomainEventHandlerResponseError(...)   // Error 파라미터
LogDomainEventHandlerResponseError(...)   // Exception 파라미터 (오버로드)
```

### DomainEventPublisherLoggerExtensions
```csharp
// 단일 이벤트
LogDomainEventPublisherRequest<TEvent>(...)
LogDomainEventPublisherResponseSuccess<TEvent>(...)
LogDomainEventPublisherResponseWarning<TEvent>(...)
LogDomainEventPublisherResponseError<TEvent>(...)

// 다중 이벤트 (Aggregate)
LogDomainEventsPublisherRequest(...)
LogDomainEventsPublisherResponseSuccess(...)
LogDomainEventsPublisherResponseWarning(...)
LogDomainEventsPublisherResponseError(...)
LogDomainEventsPublisherResponsePartialFailure(...)
```

## 안티패턴

| 잘못된 예 | 올바른 예 | 이유 |
|-----------|-----------|------|
| `LogRequestMessage` | `LogUsecaseRequest` | Context 누락 |
| `LogDomainEventHandlerSuccess` | `LogDomainEventHandlerResponseSuccess` | Phase 누락 |
| `LogDomainEventPublish` | `LogDomainEventPublisherRequest` | 동작 대신 Phase 사용 |
| `LogResponseMessageSuccess` | `LogUsecaseResponseSuccess` | "Message" 접미사 불필요 |

## 새 LoggerExtensions 클래스 추가 시

1. Context 이름 결정 (예: `Repository`, `ExternalApi`)
2. 필요한 Phase 결정 (`Request`, `Response` 또는 둘 다)
3. 필요한 Status 결정 (`Success`, `Warning`, `Error`)
4. 패턴에 따라 메서드 이름 작성
