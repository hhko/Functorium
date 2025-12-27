# 비교 가능한 값 객체
> `ComparableValueObject`

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에 보는 정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 여러 기본 타입을 조합하면서도 비교 기능을 제공하는 `ComparableValueObject` 패턴을 이해하고 실습하는 것을 목표로 합니다. 복합적인 도메인 개념을 표현하면서도 정렬과 비교 연산을 자연스럽게 사용할 수 있습니다.

## 학습 목표

### **핵심 학습 목표**
1. **비교 가능한 복합 값 객체 이해**: `ComparableValueObject`의 특징과 장점을 학습합니다.
2. **GetComparableEqualityComponents() 구현**: 비교 가능한 구성 요소 반환 방법을 실습합니다.
3. **복합 데이터 정렬**: 여러 값으로 구성된 객체의 정렬 방법을 체험합니다.
4. **LINQ 정렬 활용**: `OrderBy()`와 같은 LINQ 메서드에서 값 객체를 사용하는 방법을 학습합니다.

### **실습을 통해 확인할 내용**
- `ComparableValueObject`의 상속과 구현
- `GetComparableEqualityComponents()` 메서드의 역할
- 값 객체의 자동 정렬 기능
- LINQ에서의 자연스러운 사용

## 왜 필요한가?

이전 단계인 `03-ValueObject-Primitive`에서는 여러 primitive 타입을 조합하여 복합적인 도메인 개념을 표현할 수 있었습니다. 하지만 이러한 복합 값 객체들을 정렬하거나 비교할 때 문제가 발생했습니다.

**첫 번째 문제는 정렬의 어려움입니다.** 날짜 범위나 좌표와 같은 복합 데이터를 정렬할 때, 기준이 되는 값이 무엇인지 명확하지 않아 수동으로 비교 로직을 구현해야 했습니다. 이는 마치 데이터베이스에서 복합 키로 정렬할 때 순서를 명시적으로 지정해야 하는 것처럼 복잡했습니다.

**두 번째 문제는 LINQ 활용의 제한입니다.** `OrderBy()`, `Min()`, `Max()`와 같은 LINQ 메서드에서 복합 값 객체를 자연스럽게 사용할 수 없었습니다. 이는 마치 컬렉션에서 기본 타입이 아닌 객체를 정렬할 때마다 비교 함수를 제공해야 하는 것처럼 불편했습니다.

**세 번째 문제는 비교 연산자의 부재입니다.** 날짜 범위가 다른 범위보다 "더 이전"인지 "더 이후"인지 비교할 수 없었습니다. 이는 마치 숫자를 비교할 수 없게 되는 것처럼 직관적이지 않은 사용 경험을 제공했습니다.

이러한 문제들을 해결하기 위해 `ComparableValueObject`를 도입했습니다. `ComparableValueObject`는 복합 데이터의 자연스러운 순서를 정의할 수 있게 해줍니다. 이는 마치 데이터베이스에서 복합 인덱스를 정의하는 것처럼, 복합 데이터의 정렬 기준을 명시적으로 지정할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 여러 primitive 타입을 조합하면서도 비교 기능을 제공하는 `ComparableValueObject`입니다. 크게 세 가지 개념으로 나눌 수 있습니다.

### 첫 번째 개념: GetComparableEqualityComponents() 구현

`ComparableValueObject`는 `GetComparableEqualityComponents()` 메서드를 구현하여 비교 순서를 정의해야 합니다. 이 메서드는 정렬과 비교에 사용될 구성 요소들을 순서대로 반환합니다.

**핵심 아이디어는 "구성 요소 기반 비교 순서"입니다.** 복합 값 객체의 비교는 구성 요소들의 순서와 값에 따라 결정됩니다.

예를 들어, 날짜 범위의 경우 시작 날짜부터 비교하고, 시작 날짜가 같으면 종료 날짜로 비교합니다. 이는 마치 사전식 순서에서 먼저 첫 번째 글자를 비교하고 같으면 두 번째 글자를 비교하는 것과 유사합니다.

```csharp
protected override IEnumerable<IComparable> GetComparableEqualityComponents()
{
    yield return StartDate;  // 먼저 시작 날짜로 비교
    yield return EndDate;    // 시작 날짜가 같으면 종료 날짜로 비교
}
```

이러한 순서 정의는 복합 객체의 자연스러운 정렬을 가능하게 합니다. 이는 마치 파일 이름을 정렬할 때 먼저 디렉터리 순으로 정렬하고 같은 디렉터리 내에서는 파일명으로 정렬하는 것처럼 계층적 비교를 구현합니다.

### 두 번째 개념: 자동 비교 기능 상속

`ComparableValueObject`는 부모 클래스에서 `IComparable<T>` 인터페이스를 자동으로 구현합니다. 이는 복합 값 객체가 .NET의 모든 정렬 알고리즘과 컬렉션에서 자연스럽게 작동할 수 있게 합니다.

**핵심 아이디어는 ".NET 생태계 통합"입니다.** 값 객체가 마치 기본 타입처럼 정렬되고 비교될 수 있습니다.

예를 들어, 날짜 범위들을 `List<T>.Sort()`로 정렬하거나 LINQ의 `OrderBy()`로 정렬할 때 별도의 비교 함수를 제공하지 않아도 됩니다. 값 객체가 스스로 자신의 비교 방법을 알고 있습니다.

```csharp
// 자동 정렬 가능
List<DateRange> ranges = new List<DateRange>
{
    new DateRange(start1, end1),
    new DateRange(start2, end2),
    new DateRange(start3, end3)
};

ranges.Sort(); // 별도의 비교 함수 불필요

// LINQ에서도 자연스럽게 사용
var sorted = ranges.OrderBy(r => r); // IComparable<T> 덕분에 가능
```

이러한 자동 통합은 코드의 재사용성과 호환성을 크게 향상시킵니다. 이는 마치 플러그 앤 플레이처럼 값 객체를 기존 코드에 쉽게 통합할 수 있게 합니다.

### 세 번째 개념: 모든 비교 연산자 지원

`ComparableValueObject`는 `<`, `<=`, `>`, `>=` 연산자를 모두 지원합니다. 이는 복합 값 객체에 대한 직관적인 비교 표현을 가능하게 합니다.

**핵심 아이디어는 "수학적 표현의 자연스러움"입니다.** 복합 데이터도 마치 숫자처럼 비교할 수 있습니다.

예를 들어, 날짜 범위의 포함 관계나 순서를 직관적으로 표현할 수 있습니다. 이는 조건문이나 계산식에서 자연스러운 비교 표현을 사용할 수 있게 합니다.

```csharp
// 자연스러운 비교 표현
DateRange range1 = DateRange.Create(start1, end1);
DateRange range2 = DateRange.Create(start2, end2);

// 직관적인 비교
bool isEarlier = range1 < range2;    // range1이 range2보다 이전인가?
bool isLater = range1 > range2;      // range1이 range2보다 이후인가?
bool overlaps = range1 <= range2;    // range1이 range2와 겹치는가?
```

이러한 연산자 지원은 코드의 가독성을 크게 향상시킵니다. 복잡한 비교 로직도 수학처럼 표현할 수 있습니다.

## 실전 지침

### 예상 출력
```
=== 4. 비교 가능한 복합 primitive 값 객체 - ComparableValueObject ===
부모 클래스: ComparableValueObject
예시: DateRange (날짜 범위)

📋 특징:
   ✅ 여러 primitive 값을 조합
   ✅ 비교 기능 자동 제공
   ✅ 날짜 범위의 유효성 검증

🔍 성공 케이스:
   ✅ DateRange: 2024-01-01 ~ 2024-06-30
     - StartDate: 2024-01-01
     - EndDate: 2024-06-30

   ✅ DateRange: 2024-07-01 ~ 2024-12-31
     - StartDate: 2024-07-01
     - EndDate: 2024-12-31

   ✅ DateRange: 2024-01-01 ~ 2024-06-30
     - StartDate: 2024-01-01
     - EndDate: 2024-06-30

📊 동등성 비교:
   2024-01-01 ~ 2024-06-30 == 2024-07-01 ~ 2024-12-31 = False
   2024-01-01 ~ 2024-06-30 == 2024-01-01 ~ 2024-06-30 = True

📊 비교 기능 (IComparable<T>):
   2024-01-01 ~ 2024-06-30 < 2024-07-01 ~ 2024-12-31 = True
   2024-01-01 ~ 2024-06-30 <= 2024-07-01 ~ 2024-12-31 = True
   2024-01-01 ~ 2024-06-30 > 2024-07-01 ~ 2024-12-31 = False
   2024-01-01 ~ 2024-06-30 >= 2024-07-01 ~ 2024-12-31 = False

🔢 해시코드:
   2024-01-01 ~ 2024-06-30.GetHashCode() = -1711187277
   2024-01-01 ~ 2024-06-30.GetHashCode() = -1711187277
   동일한 값의 해시코드가 같은가? True

❌ 실패 케이스:
   DateRange(2024-12-31, 2024-01-01): StartAfterEnd

📈 정렬 데모:
   정렬된 DateRange 목록:
     2024-01-01 ~ 2024-03-31
     2024-04-01 ~ 2024-05-31
     2024-06-01 ~ 2024-06-30
     2024-09-01 ~ 2024-12-31

💡 비교 가능한 primitive 조합 값 객체의 특징:
   - 여러 primitive 타입(DateTime 등)을 조합
   - 각 primitive 값에 대한 개별 검증 로직
   - 동등성 비교와 비교 기능 모두 제공
   - 정렬과 크기 비교가 가능한 복잡한 도메인 개념 표현

✅ 데모가 성공적으로 완료되었습니다!
```

### 핵심 구현 포인트
1. **ComparableValueObject 상속**: 자동 비교 기능 상속
2. **GetComparableEqualityComponents() 구현**: 비교 순서 정의
3. **`IComparable<T>` 자동 구현**: 부모 클래스에서 제공
4. **모든 비교 연산자 자동 지원**: `<`, `<=`, `>`, `>=` 사용 가능

## 프로젝트 설명

### 프로젝트 구조
```
04-ComparableValueObject-Primitive/
├── Program.cs                              # 메인 실행 파일
├── ComparableValueObjectPrimitive.csproj  # 프로젝트 파일
├── ValueObjects/
│   └── DateRange.cs                       # 날짜 범위 값 객체
└── README.md                              # 프로젝트 문서
```

### 핵심 코드

**DateRange.cs - 비교 가능한 복합 primitive 값 객체 구현**
```csharp
public sealed class DateRange : ComparableValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Fin<DateRange> Create(DateTime startDate, DateTime endDate) =>
        CreateFromValidation(
            Validate(startDate, endDate),
            validValues => new DateRange(validValues.startDate, validValues.endDate));

    internal static DateRange CreateFromValidated((DateTime startDate, DateTime endDate) validatedValues) =>
        new DateRange(validatedValues.startDate, validatedValues.endDate);

    // 날짜 범위 검증
    public static Validation<Error, (DateTime startDate, DateTime endDate)> Validate(
        DateTime startDate, DateTime endDate) =>
        startDate <= endDate
            ? (startDate, endDate)
            : DomainErrors.StartAfterEnd(startDate, endDate);

    // 비교 순서 정의
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;  // 시작 날짜 우선 비교
        yield return EndDate;    // 시작 날짜 같으면 종료 날짜 비교
    }

    internal static class DomainErrors
    {
        public static Error StartAfterEnd(DateTime startDate, DateTime endDate) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(StartAfterEnd)}",
                errorCurrentValue1: startDate,
                errorCurrentValue2: endDate);
    }
}
```

**Program.cs - 비교 가능한 복합 값 객체 데모**
```csharp
// 비교 연산자 사용
var r1 = range1.Match(Succ: x => x, Fail: _ => default!);
var r2 = range2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {r1} < {r2} = {r1 < r2}");
Console.WriteLine($"   {r1} <= {r2} = {r1 <= r2}");

// 자동 정렬
var ranges = new[] { ... }
    .Select(r => DateRange.Create(r.Item1, r.Item2))
    .Where(result => result.IsSucc)
    .Select(result => result.Match(Succ: x => x, Fail: _ => default!))
    .OrderBy(r => r)  // 자연스러운 정렬
    .ToArray();
```

## 한눈에 보는 정리

### 비교 표
| 구분 | ValueObject | ComparableValueObject |
|------|-------------|----------------------|
| **비교 기능** | ❌ 미지원 | ✅ 자동 지원 |
| **GetComparableEqualityComponents()** | ❌ 해당 없음 | ✅ 필수 구현 |
| **`IComparable<T>`** | ❌ 미구현 | ✅ 자동 구현 |
| **LINQ 정렬** | ❌ 수동 구현 | ✅ 자동 지원 |
| **연산자 오버로딩** | ❌ 미지원 | ✅ 자동 지원 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **자동 비교 기능** | 비교 순서 명시적 정의 필요 |
| **LINQ 완전 지원** | 구현 복잡도 약간 증가 |
| **직관적인 연산자** | GetComparableEqualityComponents() 구현 필수 |
| **표준 인터페이스 준수** | 비교 의미가 항상 명확하지 않음 |

## FAQ

### Q1: GetComparableEqualityComponents()와 GetEqualityComponents()의 차이점은 무엇인가요?
**A**: `GetEqualityComponents()`는 동등성 비교에만 사용되는 반면, `GetComparableEqualityComponents()`는 동등성 비교와 정렬 비교 모두에 사용됩니다. `GetComparableEqualityComponents()`는 `IComparable` 타입의 요소들을 반환해야 합니다.

예를 들어, 날짜 범위의 경우 두 메서드 모두 StartDate와 EndDate를 반환하지만, `GetComparableEqualityComponents()`는 DateTime 타입을 유지하여 정렬에 사용할 수 있습니다. 이는 마치 데이터베이스에서 동등성 비교와 정렬에 모두 사용되는 복합 키와 유사합니다.

이러한 차이는 값 객체의 용도에 따라 적절한 메서드를 선택할 수 있게 합니다. 동등성 비교만 필요한 경우 `ValueObject`를, 정렬도 필요한 경우 `ComparableValueObject`를 사용합니다.

### Q2: 비교 순서는 어떻게 결정되나요?
**A**: 비교 순서는 `GetComparableEqualityComponents()`에서 요소를 반환하는 순서에 따라 결정됩니다. 첫 번째 요소부터 순차적으로 비교하며, 같으면 다음 요소로 진행합니다.

예를 들어, 날짜 범위의 경우 StartDate가 다르면 StartDate로 비교 결과가 결정되고, StartDate가 같으면 EndDate로 비교합니다. 이는 마치 문자열 비교에서 첫 번째 문자부터 순차적으로 비교하는 것과 유사합니다.

이러한 순서 정의는 도메인에 맞는 자연스러운 정렬을 가능하게 합니다. 날짜 범위에서는 시간순 정렬이 자연스럽고, 좌표에서는 X축 우선 정렬이 적절할 수 있습니다.

### Q3: 복합 값 객체의 정렬 성능은 어떠한가요?
**A**: `ComparableValueObject`의 정렬 성능은 기본 타입과 유사합니다. `GetComparableEqualityComponents()`는 LINQ를 사용하므로 약간의 오버헤드가 있지만, 대부분의 경우 무시할 만합니다.

실제 성능 bottleneck은 대개 데이터 양과 비교 횟수에 있습니다. 값 객체의 비교는 기본 타입 비교로 수행되므로 매우 효율적입니다. 이는 마치 기본 타입 배열을 정렬하는 것처럼 최적화된 성능을 제공합니다.

성능이 중요한 경우라도 `ComparableValueObject`의 오버헤드는 일반적으로 문제가 되지 않습니다. 정렬 알고리즘의 시간 복잡도가 더 큰 영향을 미치기 때문입니다.

### Q4: 모든 경우에 ComparableValueObject를 사용해야 하나요?
**A**: 아닙니다. 비교가 필요하지 않은 복합 데이터에는 `ValueObject`를 사용하는 것이 좋습니다. `ComparableValueObject`는 정렬이나 크기 비교가 필요한 경우에만 사용해야 합니다.

예를 들어, 사람의 이름과 주소로 구성된 복합 데이터가 있다고 할 때, 이름 순으로 정렬해야 한다면 `ComparableValueObject`를 사용하고, 그렇지 않다면 `ValueObject`로 충분합니다.

이는 마치 프로그래밍에서 필요한 기능만 구현하는 인터페이스 분리 원칙을 따르는 것과 유사합니다. 불필요한 비교 기능은 오히려 코드의 복잡도를 증가시킬 수 있습니다.

### Q5: LINQ에서 OrderBy()를 사용할 때 주의할 점은 무엇인가요?
**A**: `OrderBy()`를 사용할 때는 값 객체의 비교 순서가 의도한 대로 정의되어 있는지 확인해야 합니다. `GetComparableEqualityComponents()`의 구현이 도메인 요구사항과 일치하는지 검토해야 합니다.

예를 들어, 날짜 범위를 정렬할 때 시작 날짜 우선인지 종료 날짜 우선인지에 따라 구현이 달라질 수 있습니다. 이는 마치 SQL에서 ORDER BY 절에 컬럼 순서를 지정하는 것처럼 중요합니다.

또한 복합 비교에서 null 값이나 특수 케이스를 적절히 처리해야 합니다. 이는 마치 정렬 함수에서 null 처리를 명시적으로 하는 것처럼 중요한 고려사항입니다.



