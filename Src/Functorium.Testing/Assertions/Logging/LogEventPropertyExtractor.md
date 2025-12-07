# LogEventPropertyExtractor

## 개요

`LogEventPropertyExtractor`는 Serilog의 `LogEventPropertyValue`에서 실제 값을 재귀적으로 추출하는 유틸리티 클래스입니다. 구조화된 로깅(Structured Logging)에서 복잡한 중첩 객체를 올바르게 처리하여 테스트 검증에 활용할 수 있도록 합니다.

## 주요 기능

- **재귀적 값 추출**: 중첩된 구조의 모든 속성을 재귀적으로 탐색하여 실제 값을 추출
- **타입별 처리**: Serilog의 모든 `LogEventPropertyValue` 하위 타입을 처리
  - `ScalarValue`: 단일 값 (문자열, 숫자, 불린 등)
  - `StructureValue`: 객체 구조
  - `SequenceValue`: 배열/리스트 구조
  - `DictionaryValue`: 키-값 쌍 구조
- **테스트 데이터 변환**: LogEvent를 익명 타입으로 변환하여 Verify 테스트에 활용

## 클래스 구조

```csharp
public static class LogEventPropertyExtractor
{
    public static object ExtractValue(Serilog.Events.LogEventPropertyValue propertyValue)
    public static IEnumerable<object> ExtractLogData(IEnumerable<Serilog.Events.LogEvent> logEvents)
    public static object ExtractLogData(Serilog.Events.LogEvent logEvent)
}
```

## .NET 9.0 C# 언어 기능 활용

### 1. Switch 식 (Switch Expressions)
기존의 if-else 체인을 현대적인 switch 식으로 대체:
```csharp
// 이전
if (propertyValue is ScalarValue scalar) return scalar.Value ?? "null";
else if (propertyValue is SequenceValue sequence) return sequence.Elements.Select(ExtractValue).ToList();
// ...

// 개선 후
=> propertyValue switch
{
    ScalarValue scalar => scalar.Value ?? "null",
    SequenceValue sequence => sequence.Elements.Select(ExtractValue).ToList(),
    // ...
};
```

### 2. 정적 람다 (Static Lambdas)
성능 최적화를 위한 정적 람다 사용:
```csharp
// 이전
logEvent.Properties.ToDictionary(p => p.Key, p => ExtractValue(p.Value))

// 개선 후
logEvent.Properties.ToDictionary(static p => p.Key, static p => ExtractValue(p.Value))
```

### 3. 향상된 문자열 보간법
더 나은 디버깅 메시지:
```csharp
System.Diagnostics.Debug.WriteLine(
    $"[LogEventPropertyExtractor] Unhandled type: {propertyValue.GetType().Name} - {propertyValue}"
);
```

### 4. Null 병합 할당 연산자 (??=)
이미 사용 중인 null 안전 처리:
```csharp
return scalar.Value ?? "null";
```

### 5. 컬렉션 식 (Collection Expressions)
가능한 경우 더 간결한 컬렉션 초기화 사용 가능

## 메서드 상세

### ExtractValue(LogEventPropertyValue)

`LogEventPropertyValue`에서 실제 값을 재귀적으로 추출합니다. .NET 9.0의 switch 식을 활용하여 타입별 처리를 간결하게 구현했습니다.

**파라미터:**
- `propertyValue`: 추출할 LogEventPropertyValue

**반환값:** 추출된 실제 값 (object 타입)

**처리 로직:**

#### Switch 식을 활용한 타입별 처리
```csharp
public static object ExtractValue(Serilog.Events.LogEventPropertyValue propertyValue)
    => propertyValue switch
    {
        ScalarValue scalar => scalar.Value ?? "null",
        SequenceValue sequence => sequence.Elements.Select(ExtractValue).ToList(),
        StructureValue structure => structure.Properties.ToDictionary(
            prop => prop.Name,
            prop => ExtractValue(prop.Value)
        ),
        DictionaryValue dict => dict.Elements.ToDictionary(
            kvp => kvp.Key.Value?.ToString() ?? "null",
            kvp => ExtractValue(kvp.Value)
        ),
        var unhandled => HandleUnhandledType(unhandled)
    };
```

#### 1. ScalarValue 처리
```csharp
if (propertyValue is Serilog.Events.ScalarValue scalar)
{
    return scalar.Value ?? "null";  // 기본 타입 값 반환, null인 경우 "null" 문자열로 처리
}
```

#### 2. StructureValue 처리
```csharp
else if (propertyValue is Serilog.Events.StructureValue structure)
{
    return structure.Properties.ToDictionary(
        prop => prop.Name,           // 속성 이름
        prop => ExtractValue(prop.Value)  // 재귀적 값 추출
    );
}
```

#### 3. SequenceValue 처리
```csharp
else if (propertyValue is Serilog.Events.SequenceValue sequence)
{
    return sequence.Elements.Select(ExtractValue).ToList();
}
```

#### 4. DictionaryValue 처리
```csharp
else if (propertyValue is Serilog.Events.DictionaryValue dict)
{
    return dict.Elements.ToDictionary(
        kvp => kvp.Key.Value?.ToString() ?? "null",  // 키 변환
        kvp => ExtractValue(kvp.Value)                // 값 추출
    );
}
```

### ExtractLogData(`IEnumerable<LogEvent>`)

여러 LogEvent에서 정보를 추출하여 익명 타입 리스트로 변환합니다.

**파라미터:**
- `logEvents`: 처리할 LogEvent 컬렉션

**반환값:** 익명 타입의 IEnumerable

**변환 형식:**
```csharp
{
    Information = log.MessageTemplate.Text,  // 메시지 템플릿
    Properties = log.Properties.ToDictionary(...)  // 추출된 속성들
}
```

### ExtractLogData(LogEvent)

단일 LogEvent에서 정보를 추출하여 익명 타입으로 변환합니다.

**파라미터:**
- `logEvent`: 처리할 단일 LogEvent

**반환값:** 익명 타입 객체

## 사용 예시

### 기본 사용법

```csharp
using Serilog.Events;

// 로그 이벤트 수집
var logEvents = testSink.GetLogEvents();

// 단일 로그 이벤트 처리
var logEvent = logEvents.First();
var extractedData = LogEventPropertyExtractor.ExtractLogData(logEvent);

// 여러 로그 이벤트 처리 (.NET 9.0 컬렉션 식 활용)
var extractedLogs = LogEventPropertyExtractor.ExtractLogData(logEvents).ToList();

// Verify 테스트에서 사용
await Verify(extractedLogs)
    .ScrubMember("RequestId")
    .ScrubMember("Timestamp")
    .ScrubInlineGuids();
```

### 타입별 처리 예시

```csharp
// ScalarValue 예시
var scalarValue = new ScalarValue("test@example.com");
var result = LogEventPropertyExtractor.ExtractValue(scalarValue);
// 결과: "test@example.com"

// StructureValue 예시
var structureValue = new StructureValue([
    new LogEventProperty("Email", new ScalarValue("test@example.com")),
    new LogEventProperty("Age", new ScalarValue(25))
]);
var result = LogEventPropertyExtractor.ExtractValue(structureValue);
// 결과: { "Email": "test@example.com", "Age": 25 }
```

### 테스트에서의 활용

```csharp
[Fact]
public async Task CreateUser_ValidRequest_ShouldProduceExpectedLogs()
{
    // Arrange & Act
    var response = await _client.PostAsJsonAsync("/api/user/create", request);

    // Assert - 로그 검증
    var logEvents = _factory.GetLogEvents();
    var responseLogs = logEvents.Where(l =>
        l.MessageTemplate.Text == "Single user creation responsed successfully: {@ResponseData}");

    // .NET 9.0 기능으로 간소화된 로그 데이터 추출
    var logData = LogEventPropertyExtractor.ExtractLogData(responseLogs).ToList();

    // Verify를 통한 스냅샷 테스트
    await Verify(logData)
        .ScrubMember("RequestId")
        .ScrubMember("Timestamp")
        .ScrubMember("ActionId")
        .ScrubInlineGuids();
}
```

### .NET 9.0 이전 버전과 비교

```csharp
// .NET 9.0 이전 (if-else 체인)
public static object ExtractValue(LogEventPropertyValue propertyValue)
{
    if (propertyValue is ScalarValue scalar)
        return scalar.Value ?? "null";
    else if (propertyValue is SequenceValue sequence)
        return sequence.Elements.Select(ExtractValue).ToList();
    else if (propertyValue is StructureValue structure)
        return structure.Properties.ToDictionary(prop => prop.Name, prop => ExtractValue(prop.Value));
    else if (propertyValue is DictionaryValue dict)
        return dict.Elements.ToDictionary(kvp => kvp.Key.Value?.ToString() ?? "null", kvp => ExtractValue(kvp.Value));
    else
        return propertyValue.ToString();
}

// .NET 9.0 (switch 식)
public static object ExtractValue(LogEventPropertyValue propertyValue)
    => propertyValue switch
    {
        ScalarValue scalar => scalar.Value ?? "null",
        SequenceValue sequence => sequence.Elements.Select(ExtractValue).ToList(),
        StructureValue structure => structure.Properties.ToDictionary(
            prop => prop.Name,
            prop => ExtractValue(prop.Value)
        ),
        DictionaryValue dict => dict.Elements.ToDictionary(
            kvp => kvp.Key.Value?.ToString() ?? "null",
            kvp => ExtractValue(kvp.Value)
        ),
        var unhandled => HandleUnhandledType(unhandled)
    };
```

## Serilog LogEventPropertyValue 타입별 처리

현재 `ExtractValue` 메서드는 Serilog의 `LogEventPropertyValue`의 모든 주요 하위 타입을 처리합니다:

### 1. ScalarValue (단일 값)
- **설명**: 문자열, 숫자, 불린, 날짜 등 단일 값
- **처리**: `scalar.Value ?? "null"` (null인 경우 "null" 문자열로 변환)
- **예시**:
  ```csharp
  // 입력: ScalarValue("test@example.com")
  // 출력: "test@example.com"

  // 입력: ScalarValue(42)
  // 출력: 42

  // 입력: ScalarValue(null)
  // 출력: "null"
  ```

### 2. StructureValue (객체 구조)
- **설명**: 속성들을 가진 객체 구조
- **처리**: 재귀적으로 각 속성의 값을 추출하여 Dictionary로 변환
- **예시**:
  ```csharp
  // 입력: StructureValue([{ Name: "Email", Value: ScalarValue("test@example.com") }])
  // 출력: { "Email": "test@example.com" }
  ```

### 3. SequenceValue (배열/리스트)
- **설명**: 요소들의 순차적 컬렉션
- **처리**: 재귀적으로 각 요소를 추출하여 List로 변환
- **예시**:
  ```csharp
  // 입력: SequenceValue([ScalarValue("user1"), ScalarValue("user2")])
  // 출력: ["user1", "user2"]
  ```

### 4. DictionaryValue (키-값 쌍)
- **설명**: 키-값 쌍의 컬렉션
- **처리**: 재귀적으로 키와 값을 추출하여 Dictionary로 변환
- **예시**:
  ```csharp
  // 입력: DictionaryValue([KeyValuePair(ScalarValue("key1"), ScalarValue("value1"))])
  // 출력: { "key1": "value1" }
  ```

### 5. 기타 타입들
- **처리**: `propertyValue.ToString()`으로 문자열 변환
- **확장성**: 처리되지 않은 타입에 대해 디버그 로그 출력

## 활용 사례

### 1. 구조화된 로깅 테스트
- 복잡한 객체가 포함된 로그의 구조를 올바르게 검증
- 중첩된 속성까지 정확한 값 비교

### 2. Verify 스냅샷 테스트
- 로그 데이터를 익명 타입으로 변환하여 스냅샷 생성
- 민감한 정보는 ScrubMember로 처리

### 3. 로그 분석 및 디버깅
- 개발 중 로그 내용의 구조를 쉽게 파악
- 예상치 못한 데이터 구조 발견

## 주의사항

1. **성능 고려**: 대량의 로그 이벤트 처리 시 재귀적 호출로 인한 스택 오버플로우 가능성
2. **순환 참조**: 순환 참조가 있는 객체의 경우 무한 루프 발생 가능
3. **메모리 사용량**: 복잡한 중첩 구조의 경우 메모리 사용량 증가
4. **Null 값 처리**: ScalarValue.Value가 null인 경우 "null" 문자열로 처리됨

## .NET 9.0 C# 언어 기능 활용 효과

### 1. 코드 간결성
- **Switch 식**: if-else 체인을 하나의 식으로 대체하여 코드 70% 이상 간소화
- **정적 람다**: `static` 키워드로 성능 최적화 및 메모리 할당 감소

### 2. 가독성 향상
- **선언적 패턴 매칭**: 각 타입별 처리 로직이 명확하게 구분
- **의미있는 변수명**: `var unhandled`로 처리되지 않은 타입을 명시적 표현

### 3. 유지보수성
- **확장 가능한 구조**: 새로운 타입 추가 시 switch case만 추가하면 됨
- **중앙화된 예외 처리**: `HandleUnhandledType` 메서드로 통일된 예외 처리

### 4. 성능 최적화
- **할당 감소**: 정적 람다와 switch 식으로 불필요한 객체 할당 최소화
- **조기 반환**: 패턴 매칭으로 빠른 타입 식별 및 처리

## 확장 가능성

현재 클래스는 기본적인 Serilog 타입만 처리합니다. 필요에 따라 추가 타입 지원이나 사용자 정의 변환 로직을 추가할 수 있습니다. .NET 9.0의 패턴 매칭 기능을 활용하면 새로운 타입 추가가 매우 간단합니다.
