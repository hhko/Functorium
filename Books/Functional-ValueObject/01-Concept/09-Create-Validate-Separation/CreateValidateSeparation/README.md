# Create와 Validate 책임 분리하기

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 **단일 책임 원칙(Single Responsibility Principle)**을 적용하여 값 객체의 **생성(Create) 책임**과 **검증(Validate) 책임**을 명시적으로 분리하는 방법을 학습합니다. 함수형 프로그래밍의 **관심사 분리(Separation of Concerns)** 원칙을 통해 각 메서드가 하나의 명확한 책임만을 가지도록 설계합니다.

## 학습 목표

### **핵심 학습 목표**
1. **단일 책임 원칙 적용**: Create와 Validate 메서드를 분리하여 각각 하나의 책임만 담당하도록 구현
2. **검증 로직의 재사용성**: Validate 메서드를 독립적으로 사용하여 검증 로직의 재사용성과 테스트 용이성 확보
3. **함수형 합성 활용**: LanguageExt의 Validation과 Fin을 활용한 함수형 파이프라인으로 안전한 값 생성 구현

### **실습을 통해 확인할 내용**
- **검증 책임 분리**: `Denominator.Validate(5)` → 검증 결과만 반환
- **생성 책임 분리**: `Denominator.Create(5)` → 검증 후 객체 생성
- **독립적 검증 사용**: 검증만 필요한 상황에서 Validate 메서드만 호출하여 성능 최적화

## 왜 필요한가?

이전 단계인 `ValueComparability`에서는 값 객체의 비교 가능성을 구현하여 동등성과 정렬 기능을 제공했습니다. 하지만 실제로 복잡한 비즈니스 로직을 구현하려고 할 때 몇 가지 문제가 발생했습니다.

**첫 번째 문제는 검증 로직의 재사용성 부족입니다.** 마치 함수형 프로그래밍에서 순수 함수를 조합하듯이, 검증 로직도 다른 곳에서 재사용할 수 있어야 합니다. 하지만 Create 메서드에 검증 로직이 묶여있으면, 검증만 필요한 상황에서도 불필요한 객체 생성 비용을 지불해야 합니다.

**두 번째 문제는 단일 책임 원칙 위반입니다.** 마치 객체지향 설계에서 각 클래스가 하나의 책임만 가져야 하듯이, 각 메서드도 하나의 명확한 책임만 가져야 합니다. Create 메서드가 검증과 생성을 동시에 담당하면, 메서드의 복잡도가 증가하고 테스트하기 어려워집니다.

**세 번째 문제는 테스트 용이성의 저하입니다.** 마치 단위 테스트에서 각 테스트가 하나의 동작만 검증해야 하듯이, 검증 로직과 생성 로직을 분리해야 각각을 독립적으로 테스트할 수 있습니다. 현재 구조에서는 검증 로직만 테스트하기 위해 불필요한 객체 생성 과정을 거쳐야 합니다.

이러한 문제들을 해결하기 위해 **Create와 Validate 분리 패턴**을 도입했습니다. 이 패턴을 사용하면 검증 로직의 재사용성 향상, 단일 책임 원칙 준수, 그리고 테스트 용이성 확보를 얻을 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 2가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 첫 번째 개념: 검증 책임 분리 (Validation Responsibility Separation)

**핵심 아이디어는 "검증 로직을 독립적인 메서드로 분리"입니다.** 마치 함수형 프로그래밍에서 순수 함수를 조합하듯이, 검증 로직을 별도의 메서드로 분리하여 재사용 가능하게 만듭니다.

예를 들어, 사용자 입력을 검증만 하고 싶은 상황을 생각해보세요. 이전 방식에서는 Create 메서드를 호출해야 했지만, 이는 불필요한 객체 생성 비용을 발생시킵니다. 마치 데이터베이스에서 SELECT만 필요한데 INSERT까지 수행하는 것과 같은 비효율성입니다.

```csharp
// 이전 방식 (문제가 있는 방식) - 검증만 필요한데도 객체 생성
public static Fin<Denominator> Create(int value)
{
    if (value == 0)  // 검증 로직이 Create에 묶여있음
        return Error.New("0은 허용되지 않습니다");
    
    return new Denominator(value);  // 불필요한 객체 생성
}

// 개선된 방식 (현재 방식) - 검증 책임 분리
public static Validation<Error, int> Validate(int value) =>
    value == 0
        ? Error.New("0은 허용되지 않습니다")
        : value;

public static Fin<Denominator> Create(int value) =>
    Validate(value)  // 검증 책임을 Validate에 위임
        .Map(validNumber => new Denominator(validNumber))
        .ToFin();
```

이 방식의 장점은 검증 로직을 독립적으로 사용할 수 있어 성능 최적화와 코드 재사용성을 동시에 확보할 수 있다는 점입니다.

### 두 번째 개념: 함수형 합성을 통한 안전한 생성 (Functional Composition for Safe Creation)

**핵심 아이디어는 "함수형 파이프라인을 통한 안전한 값 변환"입니다.** 마치 함수형 프로그래밍의 모나드 체이닝처럼, 검증 결과를 안전하게 객체 생성으로 변환합니다.

LanguageExt의 Validation과 Fin을 활용하여 검증 실패 시 예외가 아닌 타입 안전한 에러 처리를 구현합니다. 이는 마치 Maybe 모나드나 Either 모나드처럼 실패 가능성을 타입 시스템에 명시하는 방식입니다.

```csharp
// 함수형 합성을 통한 안전한 생성
public static Fin<Denominator> Create(int value) =>
    Validate(value)                    // 1단계: 검증
        .Map(validNumber => new Denominator(validNumber))  // 2단계: 변환
        .ToFin();                      // 3단계: 타입 변환
```

이 방식의 장점은 각 단계가 명확히 분리되어 있어 디버깅이 쉽고, 함수형 프로그래밍의 합성 원칙을 활용하여 안전한 코드를 작성할 수 있다는 점입니다.

## 실전 지침

### 예상 출력
```
=== 단일 책임 원칙을 통한 Create와 Validate 분리 ===

=== 1. 핵심 개선사항: Create와 Validate 책임 분리 ===
검증 책임 분리: Validate 메서드만 호출
  검증 성공: 5
생성 책임 분리: Create 메서드 호출
  생성 성공: 5

=== 2. Validate 메서드 독립적 사용 예제 ===
검증 책임만 분리하여 사용:
  1 -> 검증 통과: 1
  5 -> 검증 통과: 5
  10 -> 검증 통과: 10
  0 -> 검증 실패: 0은 허용되지 않습니다
  -3 -> 검증 통과: -3
```

### 핵심 구현 포인트
1. **Validate 메서드 구현**: 검증 로직만 담당하는 순수 함수로 구현하여 재사용성 확보
2. **Create 메서드 리팩토링**: Validate 메서드를 호출하여 검증 책임을 위임하고, 함수형 합성으로 안전한 객체 생성
3. **독립적 검증 활용**: 검증만 필요한 상황에서 Validate 메서드만 호출하여 성능 최적화

## 프로젝트 설명

### 프로젝트 구조
```
CreateValidateSeparation/                 # 메인 프로젝트
├── Program.cs                           # 메인 실행 파일
├── MathOperations.cs                    # 수학 연산 클래스
├── ValueObjects/
│   └── Denominator.cs                   # 분모 값 객체 (Create/Validate 분리)
├── CreateValidateSeparation.csproj      # 프로젝트 파일
└── README.md                           # 메인 문서
```

### 핵심 코드

#### Denominator.cs - 검증과 생성 책임 분리
```csharp
/// <summary>
/// 검증 책임 - 단일 책임 원칙
/// 검증 로직만 담당하는 별도 메서드
/// </summary>
public static Validation<Error, int> Validate(int value) =>
    value == 0
        ? Error.New("0은 허용되지 않습니다")
        : value;

/// <summary>
/// Denominator 인스턴스를 생성하는 팩토리 메서드
/// 검증 책임을 분리하여 단일 책임 원칙 준수
/// </summary>
public static Fin<Denominator> Create(int value) =>
    Validate(value)
        .Map(validNumber => new Denominator(validNumber))
        .ToFin();
```

#### Program.cs - 사용 예제
```csharp
// 검증 책임 분리: Validate 메서드만 호출
var validationResult = Denominator.Validate(5);
validationResult.Match(
    Succ: value => Console.WriteLine($"  검증 성공: {value}"),
    Fail: error => Console.WriteLine($"  검증 실패: {error}")
);

// 생성 책임 분리: Create 메서드 호출
var creationResult = Denominator.Create(5);
creationResult.Match(
    Succ: denominator => Console.WriteLine($"  생성 성공: {denominator}"),
    Fail: error => Console.WriteLine($"  생성 실패: {error}")
);
```

## 한눈에 보는 정리

### 비교 표
| 구분 | 이전 방식 | 현재 방식 |
|------|-----------|-----------|
| **검증 로직** | Create 메서드 내부에 포함 | Validate 메서드로 분리 |
| **재사용성** | 검증만 필요한 경우에도 객체 생성 필요 | Validate 메서드만 호출 가능 |
| **책임 분리** | Create가 검증과 생성을 동시 담당 | 각 메서드가 하나의 책임만 담당 |
| **테스트 용이성** | 검증 로직 테스트 시 불필요한 객체 생성 | 검증 로직을 독립적으로 테스트 가능 |
| **성능** | 검증만 필요한 경우에도 객체 생성 비용 | 검증만 필요한 경우 비용 최적화 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **검증 로직 재사용성** | **코드 복잡도 증가** |
| **단일 책임 원칙 준수** | **메서드 수 증가** |
| **테스트 용이성 향상** | **초기 학습 곡선** |
| **성능 최적화 가능** | **함수형 개념 이해 필요** |

## FAQ

### Q1: Validate 메서드만 사용하는 경우가 실제로 있나요?
**A**: 네, 실제로 매우 자주 있습니다. 특히 웹 애플리케이션과 API 개발에서 검증만 필요한 상황이 많습니다.

**세부 설명:**
- **사용자 입력 검증**: 폼 데이터 검증 시 객체 생성 없이 유효성만 확인
- **API 요청 검증**: REST API에서 요청 데이터 검증 시 즉시 응답 반환
- **배치 처리**: 대량 데이터 처리 시 유효하지 않은 데이터를 미리 필터링

**실제 예시:**
```csharp
// 웹 API에서 사용자 입력 검증만 필요한 경우
[HttpPost]
public IActionResult Divide([FromBody] DivisionRequest request)
{
    // 객체 생성 없이 검증만 수행하여 성능 최적화
    var denominatorValidation = Denominator.Validate(request.Denominator);
    if (denominatorValidation.IsFail)
    {
        return BadRequest("분모는 0이 될 수 없습니다");
    }
    
    // 검증 통과 시에만 실제 연산 수행
    var result = request.Numerator / request.Denominator;
    return Ok(result);
}
```

### Q2: Create와 Validate를 분리하면 코드가 복잡해지지 않나요?
**A**: 초기에는 복잡해 보일 수 있지만, 장기적으로는 유지보수성이 크게 향상됩니다. 이는 마치 Repository 패턴에서 데이터 접근과 비즈니스 로직을 분리하는 것처럼, 각 메서드가 하나의 명확한 책임만 가져 코드 이해가 쉬워집니다.

**단일 책임 원칙**은 각 메서드가 하나의 명확한 책임만 가져 코드 이해가 쉬워집니다. 이는 마치 SOLID 원칙의 단일 책임 원칙처럼, 각 메서드가 변경되어야 하는 이유가 하나만 있도록 설계하는 것입니다.

**테스트 용이성**은 검증 로직과 생성 로직을 독립적으로 테스트할 수 있게 합니다. 이는 마치 단위 테스트에서 각 테스트가 하나의 동작만 검증해야 하는 것처럼, 검증과 생성을 별도로 테스트할 수 있어 테스트의 명확성이 향상됩니다.

**재사용성**은 Validate 메서드를 다른 곳에서도 활용할 수 있게 합니다. 이는 마치 공통 유틸리티 클래스를 여러 곳에서 재사용하는 것처럼, 한 번 작성한 검증 로직을 다양한 상황에서 활용할 수 있습니다.

**실제 예시:**
```csharp
// 검증 로직만 테스트하는 경우 - 객체 생성 없이 빠른 테스트
[Test]
public void Validate_ZeroValue_ReturnsError()
{
    var result = Denominator.Validate(0);
    result.IsFail.ShouldBeTrue();
    result.Match(
        Succ: _ => throw new Exception("Expected failure"),
        Fail: error => error.Message.ShouldBe("0은 허용되지 않습니다")
    );
}

// 생성 로직만 테스트하는 경우 - 검증과 생성을 함께 테스트
[Test]
public void Create_ValidValue_ReturnsDenominator()
{
    var result = Denominator.Create(5);
    result.IsSucc.ShouldBeTrue();
    result.Match(
        Succ: denominator => denominator.Value.ShouldBe(5),
        Fail: _ => throw new Exception("Expected success")
    );
}
```

### Q3: Validation<Error, int>와 `Fin<Denominator>`의 차이점이 무엇인가요?
**A**: 두 타입은 서로 다른 목적을 가지고 있습니다. 이는 마치 함수형 프로그래밍의 Maybe와 Either 모나드처럼, 각각 다른 추상화 레벨을 제공합니다.

**Validation<Error, int>**는 검증 결과를 나타내며, 성공 시 원본 값(int)을 반환합니다. 이는 마치 데이터베이스의 제약 조건 검사처럼, 입력값의 유효성만 확인하고 원본 데이터를 그대로 반환하는 역할을 합니다.

**`Fin<Denominator>`**는 객체 생성 결과를 나타내며, 성공 시 생성된 객체(Denominator)를 반환합니다. 이는 마치 Factory 패턴의 Create 메서드처럼, 검증을 통과한 데이터로 실제 도메인 객체를 생성하는 역할을 합니다.

**비교 예시:**
```csharp
// Validate: 검증만 수행, 원본 값 반환 (검증 레벨)
var validation = Denominator.Validate(5);
validation.Match(
    Succ: value => Console.WriteLine($"검증된 값: {value}"), // int 타입
    Fail: error => Console.WriteLine($"검증 실패: {error}")
);

// Create: 검증 후 객체 생성, 생성된 객체 반환 (생성 레벨)
var creation = Denominator.Create(5);
creation.Match(
    Succ: denominator => Console.WriteLine($"생성된 객체: {denominator}"), // Denominator 타입
    Fail: error => Console.WriteLine($"생성 실패: {error}")
);
```

### Q4: 왜 예외(Exception) 대신 Validation과 Fin을 사용하나요?
**A**: 함수형 프로그래밍에서는 예외보다 타입 안전한 에러 처리를 선호합니다. 이는 마치 함수형 프로그래밍의 모나드 체이닝처럼, Validation과 Fin은 안전하게 합성 가능한 에러 처리 방식을 제공합니다.

**예외의 문제점**은 예외가 제어 흐름을 방해하고, 컴파일 타임에 에러 가능성을 알 수 없다는 점입니다. 이는 마치 goto 문처럼 예측하기 어려운 제어 흐름을 만들어 코드의 가독성과 유지보수성을 저하시킵니다.

**타입 안전성**은 Validation과 Fin이 에러 가능성을 타입 시스템에 명시하여 컴파일 타임에 체크할 수 있게 합니다. 이는 마치 제네릭을 통해 타입 안전성을 보장하는 것처럼, 에러 가능성을 타입으로 표현하여 컴파일러가 미리 오류를 잡아줍니다.

**함수형 합성**은 예외가 함수 체이닝을 방해하지만, Validation과 Fin은 안전하게 합성할 수 있게 합니다. 이는 마치 LINQ의 메서드 체이닝처럼, 각 단계가 실패해도 안전하게 에러를 전파하면서 전체 파이프라인을 유지할 수 있습니다.

**비교 예시:**
```csharp
// 예외 방식 (문제가 있는 방식) - 제어 흐름 방해
public static Denominator CreateWithException(int value)
{
    if (value == 0)
        throw new ArgumentException("0은 허용되지 않습니다"); // 예외 발생으로 제어 흐름 중단
    return new Denominator(value);
}

// 함수형 방식 (현재 방식) - 타입 안전한 에러 처리
public static Fin<Denominator> Create(int value) =>
    Validate(value)
        .Map(validNumber => new Denominator(validNumber))
        .ToFin(); // 함수형 합성을 통한 안전한 에러 처리
```

### Q5: Map과 ToFin() 메서드는 무엇을 하나요?
**A**: 함수형 프로그래밍의 변환(Transform)과 타입 변환을 담당합니다. 이는 마치 함수형 프로그래밍의 펑터(Functor)와 모나드(Monad) 변환처럼, 각 단계가 실패하면 자동으로 에러가 전파됩니다.

**Map**은 성공 케이스의 값을 다른 타입으로 변환합니다 (int → Denominator). 이는 마치 LINQ의 Select 메서드처럼, 컬렉션의 각 요소를 다른 타입으로 변환하는 것과 같은 원리입니다. Validation의 성공 케이스에만 변환 함수를 적용하고, 실패 케이스는 그대로 전파합니다.

**ToFin()**은 Validation 타입을 Fin 타입으로 변환합니다. 이는 마치 타입 캐스팅처럼, 동일한 의미를 가진 다른 타입으로 변환하는 역할을 합니다. Validation과 Fin은 모두 성공/실패를 나타내지만, 서로 다른 API를 제공하므로 상황에 맞는 타입으로 변환할 때 사용합니다.

**함수형 합성**은 각 단계가 실패하면 자동으로 에러가 전파됩니다. 이는 마치 파이프라인에서 한 단계가 실패하면 전체 파이프라인이 중단되는 것처럼, 중간 단계에서 실패가 발생하면 최종 결과까지 자동으로 실패가 전파됩니다.

**단계별 설명:**
```csharp
public static Fin<Denominator> Create(int value) =>
    Validate(value)                                         // Validation<Error, int>
        .Map(validNumber => new Denominator(validNumber))   // Validation<Error, Denominator>
        .ToFin();                                           // Fin<Denominator>

// 각 단계별 결과 (성공 케이스):
// 1단계: Validate(5) → Success(5)
// 2단계: Map(...) → Success(new Denominator(5))
// 3단계: ToFin() → Succ(new Denominator(5))

// 각 단계별 결과 (실패 케이스):
// 1단계: Validate(0) → Failure(Error("0은 허용되지 않습니다"))
// 2단계: Map(...) → Failure(Error("0은 허용되지 않습니다")) (자동 전파)
// 3단계: ToFin() → Fail(Error("0은 허용되지 않습니다")) (자동 전파)
```

### Q6: 이전 방식과 현재 방식의 성능 차이가 있나요?
**A**: 검증만 필요한 경우 현재 방식이 더 효율적입니다. 이는 마치 데이터베이스에서 SELECT만 필요한데 INSERT까지 수행하는 것과 같은 비효율성을 해결합니다.

**이전 방식**은 검증만 해도 객체 생성 비용이 발생합니다. 이는 마치 캐시에서 데이터를 조회할 때 불필요한 데이터베이스 쓰기 작업까지 수행하는 것처럼, 필요한 작업 외에 추가적인 비용을 지불해야 합니다.

**현재 방식**은 Validate 메서드만 호출하면 객체 생성 없이 검증만 수행합니다. 이는 마치 데이터베이스의 EXISTS 쿼리처럼, 존재 여부만 확인하고 실제 데이터는 조회하지 않는 것과 같은 원리입니다.

**실제 성능**은 대량 데이터 처리 시 상당한 성능 차이가 발생합니다. 이는 마치 배치 처리에서 불필요한 객체 생성이 누적되어 메모리 사용량과 처리 시간이 크게 증가하는 것처럼, 대규모 데이터 처리에서는 성능 차이가 더욱 두드러집니다.

**성능 비교 예시:**
```csharp
// 이전 방식: 1000개 검증 시 1000개 객체 생성 (불필요한 비용)
var validCount = 0;
for (int i = 0; i < 1000; i++)
{
    try
    {
        var result = Denominator.CreateWithException(i); // 항상 객체 생성
        if (result != null) validCount++; // 검증만 필요한 로직
    }
    catch { /* 에러 처리 */ }
}

// 현재 방식: 1000개 검증 시 0개 객체 생성 (비용 최적화)
var validCount = 0;
for (int i = 0; i < 1000; i++)
{
    var validation = Denominator.Validate(i); // 객체 생성 없음
    if (validation.IsSucc) validCount++; // 검증만 필요한 로직
}
```

### Q7: 다음 단계에서는 어떤 내용을 학습하게 되나요?
**A**: 다음 단계인 `ValidatedValueCreation`에서는 복합 값 객체의 검증과 생성을 학습합니다. 이는 마치 함수형 프로그래밍의 모나드 체이닝처럼, 여러 값 객체를 조합한 복합 객체의 검증 방법을 배웁니다.

**복합 검증**은 여러 값 객체를 조합한 복합 객체의 검증 방법입니다. 이는 마치 데이터베이스의 복합 제약 조건처럼, 개별 필드뿐만 아니라 전체적인 무결성도 함께 검증하는 방식입니다.

**검증 조합**은 각 구성 요소의 검증 결과를 조합하는 함수형 기법입니다. 이는 마치 LINQ의 Join이나 GroupBy처럼, 여러 검증 결과를 하나의 의미 있는 결과로 조합하는 방법을 배웁니다.

**계층적 검증**은 부모-자식 관계의 값 객체들 간의 검증 순서와 의존성 관리입니다. 이는 마치 객체지향 프로그래밍의 상속 계층처럼, 상위 객체가 하위 객체들의 검증을 조합하여 전체 검증을 수행하는 방식을 학습합니다.

**실제 예시:**
```csharp
// Address는 Street, City, PostalCode를 조합한 복합 값 객체
public static Validation<Error, Address> Create(
    string street, string city, string postalCode)
{
    return from s in Street.Validate(street)
           from c in City.Validate(city)  
           from p in PostalCode.Validate(postalCode)
           select new Address(s, c, p);
}

// 사용 예시
var addressResult = Address.Create("123 Main St", "Seoul", "12345");
addressResult.Match(
    Succ: address => Console.WriteLine($"주소 생성 성공: {address}"),
    Fail: error => Console.WriteLine($"주소 생성 실패: {error}")
);
```

### Q8: 초보자가 이 패턴을 이해하기 어려운 부분은 무엇인가요?
**A**: 함수형 프로그래밍 개념과 타입 시스템에 대한 이해가 필요합니다. 이는 마치 객체지향 프로그래밍에서 상속과 다형성을 이해하는 것처럼, 함수형 프로그래밍의 핵심 개념들을 단계별로 학습해야 합니다.

**함수형 개념**은 Map, Match, Validation 등의 함수형 프로그래밍 개념을 이해하는 것입니다. 이는 마치 LINQ를 처음 배울 때 Select, Where, GroupBy 같은 메서드들을 익혀야 하는 것처럼, 함수형 프로그래밍의 기본 연산들을 단계별로 학습해야 합니다.

**타입 시스템**은 제네릭 타입과 타입 변환에 대한 이해가 필요합니다. 이는 마치 제네릭 컬렉션을 사용할 때 `List<T>`, `Dictionary<K,V>` 같은 타입을 이해해야 하는 것처럼, 함수형 타입들의 구조와 변환 방법을 익혀야 합니다.

**에러 처리**는 예외 대신 타입 안전한 에러 처리 방식을 이해하는 것입니다. 이는 마치 try-catch 대신 Result 패턴을 사용하는 것처럼, 에러를 값으로 처리하는 함수형 접근 방식을 학습해야 합니다.

**학습 팁:**
```csharp
// 1단계: 기본 개념부터 이해 (검증만 수행)
var validation = Denominator.Validate(5); // 검증만 수행

// 2단계: Match 패턴 이해 (결과 처리)
validation.Match(
    Succ: value => Console.WriteLine($"성공: {value}"),
    Fail: error => Console.WriteLine($"실패: {error}")
);

// 3단계: 함수형 합성 이해 (검증 + 생성)
var result = Denominator.Create(5); // 검증 + 생성

// 4단계: 고급 패턴 이해 (체이닝과 합성)
var complexResult = Denominator.Validate(5)
    .Map(value => value * 2)
    .Map(doubled => new Denominator(doubled))
    .ToFin();
```
