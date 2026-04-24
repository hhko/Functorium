---
title: "비교 가능한 값 객체"
---

> `ComparableValueObject`

## 개요

`ValueObject`로 날짜 범위를 표현했는데, 여러 범위를 시간순으로 정렬해야 한다면 어떻게 할까요? `ComparableValueObject`는 여러 primitive 타입을 조합하면서도 `GetComparableEqualityComponents()`를 통해 비교 순서를 정의하여, 정렬과 비교 연산을 자연스럽게 지원합니다.

## 학습 목표

- `ComparableValueObject`가 `ValueObject`와 어떻게 다른지 설명할 수 있습니다
- `GetComparableEqualityComponents()`로 비교 순서를 정의할 수 있습니다
- 여러 값으로 구성된 객체를 컬렉션에서 정렬할 수 있습니다
- LINQ의 `OrderBy()`에서 복합 값 객체를 직접 사용할 수 있습니다

## 왜 필요한가?

`ValueObject`는 동등성 비교만 지원합니다. 날짜 범위나 좌표 같은 복합 데이터를 정렬하려면 기준이 되는 값이 무엇인지 직접 지정해야 하고, `OrderBy()`나 `Min()` 같은 LINQ 메서드에서도 별도의 비교 함수를 매번 제공해야 합니다. `<`, `>` 같은 비교 연산자도 사용할 수 없어, 조건문이 직관적이지 않습니다.

`ComparableValueObject`는 `GetComparableEqualityComponents()`에서 구성 요소의 반환 순서를 정의하는 것만으로 자연스러운 정렬과 비교를 지원합니다.

## 핵심 개념

### GetComparableEqualityComponents() 구현

`ComparableValueObject`는 `GetComparableEqualityComponents()` 메서드를 구현하여 비교에 사용될 구성 요소들을 순서대로 반환합니다. 첫 번째 요소부터 비교하고, 값이 같으면 다음 요소로 진행하는 사전식 순서를 따릅니다.

날짜 범위의 경우, 시작 날짜부터 비교하고 시작 날짜가 같으면 종료 날짜로 비교합니다.

```csharp
protected override IEnumerable<IComparable> GetComparableEqualityComponents()
{
    yield return StartDate;  // 먼저 시작 날짜로 비교
    yield return EndDate;    // 시작 날짜가 같으면 종료 날짜로 비교
}
```

### 자동 비교 기능 상속

부모 클래스가 `IComparable<T>` 인터페이스를 자동으로 구현하므로, `List<T>.Sort()`나 LINQ의 `OrderBy()`에서 별도의 비교 함수 없이 바로 사용할 수 있습니다.

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

### 모든 비교 연산자 지원

`<`, `<=`, `>`, `>=` 연산자가 모두 자동으로 오버로딩됩니다. 복합 데이터에 대해서도 직관적인 비교 표현이 가능합니다.

```csharp
// 자연스러운 비교 표현
DateRange range1 = DateRange.Create(start1, end1);
DateRange range2 = DateRange.Create(start2, end2);

// 직관적인 비교
bool isEarlier = range1 < range2;    // range1이 range2보다 이전인가?
bool isLater = range1 > range2;      // range1이 range2보다 이후인가?
bool overlaps = range1 <= range2;    // range1이 range2와 겹치는가?
```

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

`ValueObject`와 비교했을 때 추가되는 핵심 요소를 정리합니다.

| 포인트 | 설명 |
|--------|------|
| **ComparableValueObject 상속** | 자동 비교 기능 상속 |
| **GetComparableEqualityComponents() 구현** | 비교 순서 정의 |
| **`IComparable<T>` 자동 구현** | 부모 클래스에서 제공 |
| **비교 연산자 자동 지원** | `<`, `<=`, `>`, `>=` 사용 가능 |

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

`DateRange`는 `ComparableValueObject`를 상속하여 시작일과 종료일을 하나의 비교 가능한 날짜 범위로 표현합니다.

**DateRange.cs - 비교 가능한 복합 primitive 값 객체 구현**
```csharp
public sealed class DateRange : ComparableValueObject
{
    public sealed record StartAfterEnd : DomainErrorKind.Custom;
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Fin<DateRange> Create(DateTime startDate, DateTime endDate) =>
        CreateFromValidation(Validate(startDate, endDate), v => new DateRange(v.startDate, v.endDate));

    public static DateRange CreateFromValidated((DateTime startDate, DateTime endDate) validatedValues) =>
        new(validatedValues.startDate, validatedValues.endDate);

    // 날짜 범위 검증
    public static Validation<Error, (DateTime startDate, DateTime endDate)> Validate(
        DateTime startDate, DateTime endDate) =>
        startDate <= endDate
            ? (startDate, endDate)
            : DomainError.For<DateRange, DateTime, DateTime>(new StartAfterEnd(), startDate, endDate,
                $"Start date cannot be after end date. Start: '{startDate:yyyy-MM-dd}', End: '{endDate:yyyy-MM-dd}'");

    // 비교 순서 정의
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;  // 시작 날짜 우선 비교
        yield return EndDate;    // 시작 날짜 같으면 종료 날짜 비교
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}";
}
```

비교 연산자와 자동 정렬을 확인하는 데모 코드입니다.

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

`ValueObject`와 `ComparableValueObject`의 차이를 비교합니다.

### 비교 표
| 구분 | ValueObject | ComparableValueObject |
|------|-------------|----------------------|
| **비교 기능** | 미지원 | 자동 지원 |
| **GetComparableEqualityComponents()** | 해당 없음 | 필수 구현 |
| **`IComparable<T>`** | 미구현 | 자동 구현 |
| **LINQ 정렬** | 수동 구현 | 자동 지원 |
| **연산자 오버로딩** | 미지원 | 자동 지원 |

## FAQ

### Q1: GetComparableEqualityComponents()와 GetEqualityComponents()의 차이점은 무엇인가요?
**A**: `GetEqualityComponents()`는 동등성 비교에만 사용되고, `GetComparableEqualityComponents()`는 동등성과 정렬 비교 모두에 사용됩니다. 후자는 `IComparable` 타입의 요소를 반환해야 합니다.

### Q2: 비교 순서는 어떻게 결정되나요?
**A**: `GetComparableEqualityComponents()`에서 요소를 반환하는 순서에 따라 결정됩니다. 첫 번째 요소가 다르면 그것으로 결과가 확정되고, 같으면 다음 요소로 진행합니다. 날짜 범위에서는 시작일 우선, 좌표에서는 X축 우선 등 도메인에 맞게 순서를 지정합니다.

### Q3: 모든 경우에 ComparableValueObject를 사용해야 하나요?
**A**: 아닙니다. 정렬이나 크기 비교가 필요 없는 복합 데이터에는 `ValueObject`가 적합합니다. 불필요한 비교 기능은 `GetComparableEqualityComponents()` 구현 부담만 추가하므로, 정렬이 필요한 경우에만 `ComparableValueObject`를 선택합니다.

다음 장에서는 primitive 타입이 아닌 다른 값 객체를 조합하는 복합 값 객체 패턴을 학습합니다. 값 객체 안에 값 객체를 포함하여 더 풍부한 도메인 모델을 구성하는 방법을 살펴봅니다.

---

→ [5장: ValueObject (Composite)](../05-ValueObject-Composite/)
