---
title: "Create/Validate 분리"
---

## 개요

사용자 입력이 유효한지 확인만 하고 싶은데, 기존 `Create` 메서드는 검증과 객체 생성을 동시에 수행한다. 검증만 필요한 상황에서도 불필요한 객체 생성 비용을 지불해야 하는 셈이다. 이 장에서는 Create와 Validate의 책임을 명시적으로 분리하여 단일 책임 원칙을 적용하고, 검증 로직의 재사용성과 테스트 용이성을 확보한다.

## 학습 목표

1. Create와 Validate 메서드를 분리하여 각각 하나의 책임만 담당하도록 구현할 수 있습니다
2. Validate 메서드를 독립적으로 호출하여 객체 생성 없이 검증만 수행할 수 있습니다
3. LanguageExt의 Validation과 Fin을 활용한 함수형 파이프라인으로 안전한 값 생성을 구현할 수 있습니다

## 왜 필요한가?

이전 단계인 `ValueComparability`에서는 값 객체의 비교 가능성을 구현하여 동등성과 정렬 기능을 제공했다. 하지만 복잡한 비즈니스 로직에서는 검증과 생성이 한 메서드에 묶여 있으면 문제가 된다.

Create 메서드에 검증 로직이 포함되어 있으면, 검증만 필요한 상황(예: 폼 유효성 확인, API 요청 검증, 배치 데이터 필터링)에서도 불필요한 객체 생성 비용이 발생한다. 또한 검증 로직과 생성 로직이 혼재되어 각각을 독립적으로 테스트하기 어렵다.

Create와 Validate를 분리하면 검증 로직의 재사용성, 단일 책임 원칙 준수, 테스트 용이성을 모두 확보할 수 있다.

## 핵심 개념

### 검증 책임 분리 (Validation Responsibility Separation)

검증 로직을 독립적인 메서드로 분리하여 재사용 가능하게 만든다. Validate 메서드는 검증 결과만 반환하고, Create 메서드는 Validate에 검증을 위임한 뒤 함수형 합성으로 객체를 생성한다.

다음 코드는 이전 방식과 개선된 방식의 차이를 보여준다.

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

### 함수형 합성을 통한 안전한 생성 (Functional Composition for Safe Creation)

LanguageExt의 Validation과 Fin을 활용하면 검증 실패 시 예외가 아닌 타입 안전한 에러 처리를 구현할 수 있다. 검증 결과를 `Map`으로 변환하고 `ToFin()`으로 최종 타입을 맞추는 파이프라인이다.

```csharp
// 함수형 합성을 통한 안전한 생성
public static Fin<Denominator> Create(int value) =>
    Validate(value)                    // 1단계: 검증
        .Map(validNumber => new Denominator(validNumber))  // 2단계: 변환
        .ToFin();                      // 3단계: 타입 변환
```

각 단계가 명확히 분리되어 디버깅이 쉽고, 실패 시 자동으로 에러가 전파된다.

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

다음 표는 이전 방식과 현재 방식의 차이를 비교한다.

| 구분 | 이전 방식 | 현재 방식 |
|------|-----------|-----------|
| **검증 로직** | Create 메서드 내부에 포함 | Validate 메서드로 분리 |
| **재사용성** | 검증만 필요한 경우에도 객체 생성 필요 | Validate 메서드만 호출 가능 |
| **책임 분리** | Create가 검증과 생성을 동시 담당 | 각 메서드가 하나의 책임만 담당 |
| **테스트 용이성** | 검증 로직 테스트 시 불필요한 객체 생성 | 검증 로직을 독립적으로 테스트 가능 |
| **성능** | 검증만 필요한 경우에도 객체 생성 비용 | 검증만 필요한 경우 비용 최적화 |

## FAQ

### Q1: Validate 메서드만 사용하는 경우가 실제로 있나요?
**A**: 네, 웹 API에서 요청 데이터 검증, 폼 유효성 확인, 배치 처리에서 데이터 필터링 등 객체 생성 없이 유효성만 확인하는 상황이 자주 있습니다.

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

### Q2: Validation<Error, int>와 `Fin<Denominator>`의 차이점이 무엇인가요?
**A**: `Validation<Error, int>`는 검증 결과를 나타내며 성공 시 원본 값(int)을 반환합니다. `Fin<Denominator>`는 객체 생성 결과를 나타내며 성공 시 생성된 객체(Denominator)를 반환합니다. Validate는 검증 레벨, Create는 생성 레벨에서 작동합니다.

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

### Q3: Map과 ToFin() 메서드는 무엇을 하나요?
**A**: `Map`은 성공 케이스의 값을 다른 타입으로 변환합니다(int -> Denominator). 실패 케이스는 그대로 전파됩니다. `ToFin()`은 Validation 타입을 Fin 타입으로 변환합니다. 두 타입 모두 성공/실패를 나타내지만 서로 다른 API를 제공하므로 상황에 맞게 변환합니다.

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

Create와 Validate를 분리하여 단일 값 객체의 검증 재사용성을 확보했다. 다음 장에서는 여러 값 객체를 조합한 복합 객체에서 검증된 값으로 직접 생성하는 CreateFromValidated 패턴을 다룬다.
