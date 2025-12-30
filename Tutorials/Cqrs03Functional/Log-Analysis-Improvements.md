# Log-Analysis.md 데이터 형식 개선 사항

이 문서는 `Log-Analysis.md`에서 발견된 **데이터 형식, 데이터 구조, 데이터 품질** 관점의 개선 사항을 정리합니다.

## 데이터 형식 일관성 문제

### 1. 필드명 명명 규칙 불일치

**문제점**:
- **TelemetryKeys** (Trace/Metrics용): 소문자 + 점 구분 (`request.layer`, `request.category`)
- **TelemetryLogKeys** (Log용): PascalCase (`RequestLayer`, `RequestCategory`)

**현재 상태**:
```csharp
// ObservabilityFields.cs
public static class TelemetryKeys
{
    public const string Layer = "request.layer";      // 소문자 + 점
    public const string Category = "request.category";
}

public static class TelemetryLogKeys
{
    public const string Layer = "RequestLayer";      // PascalCase
    public const string Category = "RequestCategory";
}
```

**개선 방안**:
- Log 필드명도 소문자 + 점 구분으로 통일하여 Log/Trace/Metrics 간 일관성 확보
- 또는 모든 필드명을 PascalCase로 통일 (기존 로그 수집 도구와의 호환성 고려)

**영향도**: 높음 - Log/Trace/Metrics 간 필드명 불일치로 인한 쿼리 복잡도 증가

---

### 2. Elapsed 필드 데이터 타입 및 단위 명확성

**문제점**:
- `Elapsed` 필드가 `double` 타입으로 명시되어 있으나 단위가 명확하지 않음
- 실제로는 밀리초 단위이지만 문서에 명시되어 있지 않음

**현재 상태**:
- Usecase Log: `Elapsed: 200.4511` (밀리초)
- IAdapter Log: `Elapsed: 96.0635` (밀리초)
- Trace: `response.elapsed: 200.4511` (밀리초)

**개선 방안**:
- 필드명에 단위 명시: `ElapsedMs` 또는 `ElapsedMilliseconds`
- 또는 표준 단위(초)로 통일: `ElapsedSeconds: 0.2004511`

**영향도**: 중간 - 단위 불명확으로 인한 데이터 해석 오류 가능성

---

### 3. JSON 직렬화 형식의 $type 필드

**문제점**:
- Request/Response 객체 직렬화 시 `$type` 필드가 포함됨
- 이 필드가 표준인지, 제거 가능한지 명확하지 않음

**현재 상태**:
```json
{
  "Name": "노트북",
  "Description": "...",
  "$type": "Request"  // 타입 정보 포함
}
```

**개선 방안**:
- `$type` 필드의 필요성 검토
- 필요시 명시적으로 문서화
- 불필요시 제거 또는 선택적 포함 옵션 제공

**영향도**: 낮음 - 기능에는 영향 없으나 로그 크기 증가

---

### 4. Scope 필드와 메시지 필드의 중복

**문제점**:
- 동일한 필드가 Scope와 메시지 템플릿 양쪽에 포함됨
- 데이터 중복으로 인한 로그 크기 증가

**현재 상태**:
- Scope: `RequestLayer`, `RequestCategory`, `RequestHandler`, `RequestHandlerCqrs`, `RequestHandlerMethod`, `Request`
- 메시지: 동일한 필드들이 메시지 템플릿에도 포함

**개선 방안**:
- Scope 필드만 사용하고 메시지 템플릿에서는 제거
- 또는 메시지 템플릿에서만 사용하고 Scope 제거 (구조화된 로깅 도구 활용)

**영향도**: 낮음 - 기능에는 영향 없으나 로그 크기 최적화 가능

---

## 데이터 구조 문제

### 5. IAdapter 파라미터 필드명의 동적 생성

**문제점**:
- IAdapter 로그의 파라미터 필드명이 메서드 파라미터명에 따라 동적으로 생성됨
- 일관된 필드명이 없어 쿼리 작성이 어려움

**현재 상태**:
- `ExistsByName(string name)` → 필드명: `name` (파라미터명 그대로)
- `Create(Product product)` → 필드명: `product` (파라미터명 그대로)
- 필드명이 메서드마다 달라짐

**개선 방안**:
- 파라미터 필드명을 표준화: `param1`, `param2`, ... 또는 `@param.name`, `@param.product`
- 또는 파라미터 인덱스 기반: `param[0]`, `param[1]`

**영향도**: 중간 - 동적 필드명으로 인한 쿼리 복잡도 증가

---

### 6. 컬렉션 파라미터의 Count 필드 일관성

**문제점**:
- 컬렉션 타입 파라미터의 경우 Count 필드가 추가되지만, 필드명 규칙이 일관되지 않음

**현재 상태**:
- 파라미터명이 `items`인 경우 → `itemsCount` 또는 `items.Count`?
- 필드명 생성 규칙이 명확하지 않음

**개선 방안**:
- Count 필드명 규칙 명시: `{paramName}Count` (예: `itemsCount`)
- 또는 표준화: `{paramName}.count`

**영향도**: 낮음 - 현재는 동작하나 일관성 확보 필요

---

### 7. Error 객체 구조의 불일치

**문제점**:
- Usecase와 IAdapter의 Error 객체 구조가 다름
- Usecase: 상세한 에러 정보 (ErrorType, ErrorCode, Message, ErrorCurrentValue 등)
- IAdapter: 간단한 에러 정보 (ErrorType, ErrorCodeId, Message)

**현재 상태**:
```json
// Usecase Error
{
  "ErrorType": "ErrorCodeExpected`1",
  "ErrorCodeId": -1000,
  "ErrorCode": "ApplicationErrors.UsecaseValidationPipeline.Validator",
  "Message": "Name: 상품명은 필수입니다",
  "ErrorCurrentValue": {...}
}

// IAdapter Error
{
  "ErrorType": "Expected",
  "ErrorCodeId": 0,
  "Message": "상품 ID '...'을(를) 찾을 수 없습니다"
}
```

**개선 방안**:
- Error 객체 구조를 통일하여 일관된 쿼리 작성 가능하도록 개선
- 또는 Usecase/IAdapter 구분을 명시적으로 문서화

**영향도**: 중간 - 에러 분석 시 구조 불일치로 인한 복잡도 증가

---

## 데이터 품질 문제

### 8. Status 필드 값의 대소문자 불일치

**문제점**:
- Usecase Log: `Status: "Success"` / `Status: "Failure"` (대문자 시작)
- IAdapter Log: `Status: "Success"` / `Status: "failure"` (소문자, 일관성 없음)

**현재 상태**:
- 문서에는 "Success", "failure"로 표기되어 있으나 실제 코드 확인 필요

**개선 방안**:
- Status 값의 대소문자를 통일 (권장: `Success` / `Failure`)
- 또는 소문자로 통일 (`success` / `failure`)

**영향도**: 중간 - 쿼리 시 대소문자 구분 필요

---

### 9. Response 필드의 타입 정보 누락

**문제점**:
- Response 필드가 `object` 타입으로만 명시되어 있음
- 실제 구조(FinResponse<T>)에 대한 설명 부족

**현재 상태**:
```json
{
  "Value": {...},
  "IsSucc": true,
  "IsFail": false,
  "$type": "Succ"
}
```

**개선 방안**:
- Response 객체의 정확한 스키마 정의
- FinResponse<T> 구조 명시
- Value 필드의 실제 타입 명시

**영향도**: 낮음 - 현재는 동작하나 데이터 구조 이해 어려움

---

### 10. IAdapter Information 레벨의 데이터 손실

**문제점**:
- MinimumLevel이 Information일 때 파라미터와 반환값 정보가 완전히 제거됨
- 디버깅 시 필요한 정보 부족

**현재 상태**:
- Debug 레벨: 파라미터와 반환값 포함
- Information 레벨: 파라미터와 반환값 제거

**개선 방안**:
- Information 레벨에서도 최소한의 정보 유지 (예: 파라미터 개수, 반환값 타입)
- 또는 선택적으로 파라미터 포함 옵션 제공

**영향도**: 낮음 - 성능 최적화를 위한 의도적 설계

---

## 데이터 수집 효율성 문제

### 11. 로그 메시지 템플릿의 중복 데이터

**문제점**:
- 메시지 템플릿에 포함된 필드와 Scope 필드가 중복
- 구조화된 로깅 도구에서는 메시지 템플릿의 필드가 불필요할 수 있음

**개선 방안**:
- 구조화된 로깅만 사용 시 메시지 템플릿 단순화
- 또는 메시지 템플릿과 Scope 필드의 역할 명확히 구분

**영향도**: 낮음 - 로그 크기 최적화 가능

---

### 12. 대용량 객체 직렬화 비용

**문제점**:
- Request/Response 전체 객체를 JSON으로 직렬화하여 로깅
- 큰 객체의 경우 직렬화 비용 및 로그 크기 증가

**개선 방안**:
- 선택적 필드만 로깅하는 옵션 제공
- 또는 객체 크기 제한 설정

**영향도**: 중간 - 성능 및 저장 비용에 영향

---

## 우선순위별 개선 사항 요약

| 우선순위 | 개선 사항 | 데이터 관점 | 예상 영향 |
|---------|----------|------------|----------|
| 높음 | 필드명 명명 규칙 통일 | 형식 일관성 | Log/Trace/Metrics 쿼리 통합 |
| 높음 | Status 값 대소문자 통일 | 값 일관성 | 쿼리 시 대소문자 구분 불필요 |
| 중간 | Elapsed 필드 단위 명시 | 타입 명확성 | 데이터 해석 오류 방지 |
| 중간 | Error 객체 구조 통일 | 구조 일관성 | 에러 분석 쿼리 단순화 |
| 중간 | 파라미터 필드명 표준화 | 구조 일관성 | 동적 쿼리 작성 용이 |
| 낮음 | $type 필드 필요성 검토 | 형식 최적화 | 로그 크기 감소 |
| 낮음 | Scope/메시지 필드 중복 제거 | 구조 최적화 | 로그 크기 감소 |
| 낮음 | Response 스키마 명시 | 타입 명확성 | 데이터 구조 이해 향상 |

## 권장 개선 방향

1. **필드명 통일**: Log/Trace/Metrics 간 필드명 규칙 통일 (소문자 + 점 구분 권장)
2. **값 표준화**: Status, ErrorType 등 고정값의 대소문자 통일
3. **타입 명시**: 모든 필드의 정확한 타입과 단위 명시
4. **구조 통일**: Usecase/IAdapter 간 데이터 구조 일관성 확보
5. **스키마 정의**: JSON 스키마 또는 OpenAPI 스펙으로 데이터 구조 명시
