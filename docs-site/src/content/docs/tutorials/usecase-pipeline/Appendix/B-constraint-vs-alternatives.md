---
title: "제약 조건 vs 대안"
---

## 개요

이 부록에서는 Functorium이 선택한 **인터페이스 제약 조건 방식**과 다른 대안들을 비교합니다. 각 접근 방식의 장단점을 분석하여, 왜 인터페이스 계층 + 제네릭 제약이 최선의 선택인지 이해합니다.

---

## 접근 방식 비교

### 1. 인터페이스 제약 조건 (Functorium 방식)

```csharp
where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| 항목 | 평가 |
|------|------|
| **타입 안전성** | 컴파일 타임에 보장 |
| **리플렉션** | 불필요 (0곳) |
| **성능** | 최적 (static dispatch) |
| **코드 복잡도** | 인터페이스 계층 설계 필요 |
| **확장성** | 새 인터페이스 추가로 확장 가능 |
| **IDE 지원** | 자동 완성, 리팩토링 완전 지원 |

### 2. 리플렉션 기반

```csharp
// Pipeline 내부에서 런타임 타입 검사
var isSuccProp = typeof(TResponse).GetProperty("IsSucc");
var isSucc = (bool)isSuccProp!.GetValue(response)!;

// CreateFail 호출도 리플렉션 필요
var createFail = typeof(TResponse).GetMethod("CreateFail", BindingFlags.Static | BindingFlags.Public);
var failResponse = (TResponse)createFail!.Invoke(null, [error])!;
```

| 항목 | 평가 |
|------|------|
| **타입 안전성** | 런타임에만 검증 (컴파일 타임 보장 없음) |
| **리플렉션** | 다수 필요 (3곳 이상) |
| **성능** | 리플렉션 오버헤드 (매 요청마다) |
| **코드 복잡도** | Pipeline 내부가 복잡해짐 |
| **확장성** | 새 속성/메서드 추가 시 리플렉션 코드도 변경 필요 |
| **IDE 지원** | 문자열 기반이라 리팩토링 시 누락 위험 |

### 3. dynamic 사용

```csharp
public TResponse Handle(dynamic request, Func<TResponse> next)
{
    dynamic response = next();
    if (response.IsSucc) { ... }
    return response;
}
```

| 항목 | 평가 |
|------|------|
| **타입 안전성** | 없음 (모든 검사가 런타임) |
| **리플렉션** | 내부적으로 리플렉션 사용 |
| **성능** | 리플렉션 + DLR 오버헤드 |
| **코드 복잡도** | 간단하지만 안전하지 않음 |
| **확장성** | 오타 발견 불가, 런타임 에러 |
| **IDE 지원** | 자동 완성 불가 |

### 4. Source Generator 기반

```csharp
// Source Generator가 Pipeline 코드를 자동 생성
[GeneratePipeline]
public partial class ValidationPipeline<TResponse> { }
```

| 항목 | 평가 |
|------|------|
| **타입 안전성** | 생성된 코드는 타입 안전 |
| **리플렉션** | 불필요 |
| **성능** | 최적 (컴파일 타임 생성) |
| **코드 복잡도** | Generator 자체가 복잡 |
| **확장성** | Generator 수정 필요 (학습 곡선 높음) |
| **IDE 지원** | Generator에 따라 다름 |

### 5. object + 캐스팅

```csharp
public object Handle(object request, Func<object> next)
{
    var response = next();
    if (response is IFinResponse fin && fin.IsSucc) { ... }
    return response;
}
```

| 항목 | 평가 |
|------|------|
| **타입 안전성** | 부분적 (캐스팅 실패 가능) |
| **리플렉션** | 불필요하지만 박싱 발생 |
| **성능** | 박싱/언박싱 오버헤드 |
| **코드 복잡도** | 캐스팅 코드가 산재 |
| **확장성** | 새 타입 추가 시 캐스팅 코드 변경 필요 |
| **IDE 지원** | 제한적 |

---

## 종합 비교표

| 기준 | 인터페이스 제약 | 리플렉션 | dynamic | Source Gen | object 캐스팅 |
|------|:--------------:|:--------:|:-------:|:----------:|:------------:|
| 컴파일 타임 안전성 | O | X | X | O | 부분 |
| 리플렉션 없음 | O | X | X | O | O |
| 성능 최적 | O | X | X | O | 부분 |
| 설계 비용 | 중 | 낮 | 낮 | 높 | 낮 |
| 유지보수 | O | X | X | 중 | X |
| IDE 지원 | O | X | X | 중 | 부분 |

---

## 왜 인터페이스 제약을 선택했는가?

### 1. Pipeline은 핫 경로

모든 요청이 Pipeline을 거치므로, 리플렉션이나 dynamic의 성능 오버헤드는 누적됩니다.

### 2. 실수를 컴파일러가 잡아야 한다

Pipeline 제약이 잘못되면 런타임 예외가 발생합니다. 인터페이스 제약은 이를 컴파일 타임에 방지합니다.

### 3. CRTP로 static abstract 호출이 가능

C# 11의 `static abstract` 멤버와 CRTP 패턴을 결합하면, 인터페이스에서 정적 팩토리 메서드를 호출할 수 있습니다. 이것이 리플렉션 없는 `CreateFail` 호출의 핵심입니다.

### 4. 최소 제약 원칙

각 Pipeline이 필요한 능력만 제약으로 요구하므로, 불필요한 의존성이 없습니다. Validation Pipeline은 `CreateFail`만 필요하므로 `IFinResponseFactory<TResponse>`만 제약합니다.

