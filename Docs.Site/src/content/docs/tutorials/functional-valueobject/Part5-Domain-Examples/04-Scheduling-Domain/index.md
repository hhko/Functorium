---
title: "일정/예약 도메인"
---
## 개요

이 프로젝트는 일정/예약 도메인에서 자주 사용되는 4가지 핵심 값 객체를 구현합니다. 날짜 범위, 시간 슬롯, 기간, 반복 규칙 등 캘린더와 예약 시스템에 필요한 개념을 타입 안전하게 표현합니다.

구현되는 값 객체:
- **DateRange**: 시작일과 종료일을 관리하며 겹침 검사 기능 제공
- **TimeSlot**: 시작 시간과 종료 시간을 관리하며 충돌 검사 기능 제공
- **Duration**: 분/시간/일 단위의 기간을 표현하며 연산 기능 제공
- **RecurrenceRule**: 매일/매주/매월 반복 규칙을 표현하며 발생일 계산 기능 제공

## 학습 목표

### **핵심 학습 목표**
1. **범위 검증 패턴**: DateRange와 TimeSlot에서 시작 < 종료 검증을 구현합니다.
2. **겹침/충돌 검사**: 두 범위 간의 중복 여부를 판단하는 도메인 로직을 학습합니다.
3. **단위 변환**: Duration에서 분/시간/일 간의 변환을 캡슐화합니다.
4. **반복 패턴 계산**: RecurrenceRule에서 다음 발생일을 계산하는 알고리즘을 구현합니다.

### **실습을 통해 확인할 내용**
- DateRange의 Contains, Overlaps, Intersect 연산
- TimeSlot의 Duration 계산과 Conflicts 검사
- Duration의 단위 변환과 Add/Subtract 연산
- RecurrenceRule의 GetOccurrences로 다음 발생일 계산

## 왜 필요한가?

일정과 예약 시스템은 시간 관련 로직이 복잡합니다. 원시 타입으로 시간 데이터를 다루면 여러 문제가 발생합니다.

**첫 번째 문제는 범위 검증 누락입니다.** 종료일이 시작일보다 이른 잘못된 데이터가 저장될 수 있습니다. DateRange와 TimeSlot은 생성 시점에 범위 유효성을 검증합니다.

**두 번째 문제는 중복 예약입니다.** 두 예약이 겹치는지 판단하는 로직이 여러 곳에 흩어지면 버그가 발생하기 쉽습니다. 값 객체에 Overlaps/Conflicts 메서드를 캡슐화하면 일관된 중복 검사를 보장합니다.

**세 번째 문제는 반복 일정 계산 오류입니다.** "매월 31일"을 2월에 어떻게 처리할지, "매주 월/수/금"의 다음 발생일을 어떻게 계산할지 등 반복 로직은 복잡합니다. RecurrenceRule은 이러한 로직을 캡슐화합니다.

## 핵심 개념

### 첫 번째 개념: DateRange (날짜 범위)

DateRange는 시작일과 종료일을 관리합니다. 포함 여부, 겹침 검사, 교집합 계산 기능을 제공합니다.

```csharp
public sealed class DateRange : ValueObject
{
    public sealed record EndBeforeStart : DomainErrorType.Custom;

    public DateOnly Start { get; }
    public DateOnly End { get; }

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start; End = end;
    }

    public static Fin<DateRange> Create(DateOnly start, DateOnly end) =>
        CreateFromValidation(
            Validate(start, end),
            validValues => new DateRange(validValues.Start, validValues.End));

    public static Validation<Error, (DateOnly Start, DateOnly End)> Validate(DateOnly start, DateOnly end) =>
        ValidateEndNotBeforeStart(start, end).Map(_ => (start, end));

    private static Validation<Error, DateOnly> ValidateEndNotBeforeStart(DateOnly start, DateOnly end) =>
        end >= start
            ? end
            : DomainError.For<DateRange>(new EndBeforeStart(), $"{start}~{end}",
                $"End date cannot be before start date. Start: '{start}', End: '{end}'");

    public int TotalDays => End.DayNumber - Start.DayNumber + 1;
    public bool Contains(DateOnly date) => date >= Start && date <= End;
    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start; yield return End;
    }
}
```

**핵심 아이디어는 "범위 연산의 캡슐화"입니다.** 겹침 검사(`Start <= other.End && End >= other.Start`)와 같은 범위 로직이 값 객체 내부에 구현되어 어디서든 일관되게 사용됩니다.

### 두 번째 개념: TimeSlot (시간 슬롯)

TimeSlot은 하루 중 시간대를 표현합니다. 시간 충돌 검사와 기간 계산 기능을 제공합니다.

```csharp
public sealed class TimeSlot : ValueObject
{
    public sealed record EndNotAfterStart : DomainErrorType.Custom;

    public TimeOnly Start { get; }
    public TimeOnly End { get; }
    public TimeSpan Duration => End - Start;

    private TimeSlot(TimeOnly start, TimeOnly end)
    {
        Start = start; End = end;
    }

    public static Fin<TimeSlot> Create(TimeOnly start, TimeOnly end) =>
        CreateFromValidation(
            Validate(start, end),
            validValues => new TimeSlot(validValues.Start, validValues.End));

    public static Validation<Error, (TimeOnly Start, TimeOnly End)> Validate(TimeOnly start, TimeOnly end) =>
        (end > start ? end : DomainError.For<TimeSlot>(new EndNotAfterStart(), $"{start}~{end}",
            $"End time must be after start time. Start: '{start}', End: '{end}'"))
            .Map(_ => (start, end));

    public bool Contains(TimeOnly time) => time >= Start && time < End;
    public bool Conflicts(TimeSlot other) => Start < other.End && End > other.Start;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start; yield return End;
    }
}
```

**핵심 아이디어는 "예약 충돌 방지"입니다.** 두 TimeSlot이 충돌하는지 `Conflicts()` 메서드로 쉽게 판단할 수 있어, 예약 시스템에서 중복 방지 로직을 간단하게 구현할 수 있습니다.

### 세 번째 개념: Duration (기간)

Duration은 분 단위로 기간을 저장하고, 시간/일 단위로 변환하는 기능을 제공합니다.

```csharp
public sealed class Duration : ComparableSimpleValueObject<int>
{
    private Duration(int totalMinutes) : base(totalMinutes) { }

    public int TotalMinutes => Value;  // protected Value에 대한 public 접근자
    public double TotalHours => Value / 60.0;
    public double TotalDays => Value / (24.0 * 60.0);
    public static Duration Zero => new(0);

    public static Fin<Duration> FromMinutes(int minutes) =>
        CreateFromValidation(Validate(minutes), v => new Duration(v));

    public static Fin<Duration> FromHours(int hours) => FromMinutes(hours * 60);
    public static Fin<Duration> FromDays(int days) => FromMinutes(days * 24 * 60);

    public static Validation<Error, int> Validate(int minutes) =>
        ValidateNotNegative(minutes)
            .Bind(_ => ValidateNotExceedsMaximum(minutes))
            .Map(_ => minutes);

    private static Validation<Error, int> ValidateNotNegative(int minutes) =>
        minutes >= 0
            ? minutes
            : DomainError.For<Duration, int>(new DomainErrorType.Negative(), minutes,
                $"Duration cannot be negative. Current value: '{minutes}'");

    public Duration Add(Duration other) => new(Value + other.Value);
    public Duration Subtract(Duration other) => new(Math.Max(0, Value - other.Value));
}
```

**핵심 아이디어는 "단위 변환의 투명성"입니다.** 내부적으로 분 단위로 저장하고, `TotalHours`, `TotalDays` 속성으로 다른 단위로 변환합니다. `ToString()`은 사람이 읽기 좋은 형식으로 표시합니다.

### 네 번째 개념: RecurrenceRule (반복 규칙)

RecurrenceRule은 반복 일정의 규칙을 표현합니다. 다음 발생일들을 계산하는 기능을 제공합니다.

```csharp
public sealed class RecurrenceRule : ValueObject
{
    public RecurrenceType Type { get; }
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }
    public int? DayOfMonth { get; }
    public int Interval { get; }

    private RecurrenceRule(RecurrenceType type, IReadOnlyList<DayOfWeek> daysOfWeek, int? dayOfMonth, int interval)
    {
        Type = type; DaysOfWeek = daysOfWeek; DayOfMonth = dayOfMonth; Interval = interval;
    }

    public static Fin<RecurrenceRule> Daily(int interval = 1) =>
        CreateFromValidation(
            ValidateDailyInterval(interval),
            validInterval => new RecurrenceRule(RecurrenceType.Daily, [], null, validInterval));

    public static Fin<RecurrenceRule> Weekly(params DayOfWeek[] days) =>
        CreateFromValidation(
            ValidateWeeklyDays(days),
            validDays => new RecurrenceRule(RecurrenceType.Weekly, validDays, null, 1));

    public static Fin<RecurrenceRule> Monthly(int dayOfMonth) =>
        CreateFromValidation(
            ValidateMonthlyDay(dayOfMonth),
            validDay => new RecurrenceRule(RecurrenceType.Monthly, [], validDay, 1));

    private static Validation<Error, int> ValidateDailyInterval(int interval) =>
        interval >= 1
            ? interval
            : DomainError.For<RecurrenceRule, int>(new DomainErrorType.BelowMinimum(), interval,
                $"Interval must be at least 1. Current value: '{interval}'");

    public IEnumerable<DateOnly> GetOccurrences(DateOnly from, int count) { ... }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type; yield return DaysOfWeek.Count;
        foreach (var day in DaysOfWeek) yield return day;
        yield return DayOfMonth ?? 0; yield return Interval;
    }
}
```

**핵심 아이디어는 "반복 로직의 캡슐화"입니다.** "매주 월/수/금"이나 "매월 15일" 같은 반복 규칙을 값 객체로 표현하고, `GetOccurrences()`로 다음 발생일들을 계산합니다.

## 실전 지침

### 예상 출력
```
=== 일정/예약 도메인 값 객체 ===

1. DateRange (날짜 범위)
────────────────────────────────────────
   시작: 2025-01-01
   종료: 2025-01-10
   기간: 10일
   2025-01-05 포함: True
   2025-02-01 포함: False
   잘못된 범위: 종료일은 시작일보다 이전일 수 없습니다.
   범위 겹침: True

2. TimeSlot (시간 슬롯)
────────────────────────────────────────
   시간대: 09:00 - 10:30
   길이: 90분
   09:30 포함: True
   11:00 포함: False
   슬롯 충돌: True

3. Duration (기간)
────────────────────────────────────────
   90분: 1시간 30분
   시간: 1.5h
   분: 90m
   2시간: 2시간
   합계: 3시간 30분
   비교: 1시간 30분 < 2시간 = True
   음수 기간: 기간은 음수일 수 없습니다.

4. RecurrenceRule (반복 규칙)
────────────────────────────────────────
   규칙: 매주 월, 수, 금
   다음 5회: 2025-01-01, 2025-01-03, 2025-01-06, 2025-01-08, 2025-01-10
   규칙: 매월 15일
   다음 3회: 2025-01-15, 2025-02-15, 2025-03-15
   규칙: 매주 월, 화, 수, 목, 금
   다음 7회: 2025-01-01, 2025-01-02, 2025-01-03, 2025-01-06, 2025-01-07, 2025-01-08, 2025-01-09
```

## 프로젝트 설명

### 프로젝트 구조
```
04-Scheduling-Domain/
├── SchedulingDomain/
│   ├── Program.cs                  # 메인 실행 파일 (4개 값 객체 구현)
│   └── SchedulingDomain.csproj     # 프로젝트 파일
└── README.md                       # 프로젝트 문서
```

### 의존성
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### 값 객체별 프레임워크 타입

| 값 객체 | 프레임워크 타입 | 특징 |
|--------|---------------|------|
| DateRange | ValueObject | 범위 검증, 겹침/교집합 계산 |
| TimeSlot | ValueObject | 범위 검증, 충돌 검사 |
| Duration | ComparableSimpleValueObject\<int\> | 단위 변환, 산술 연산 |
| RecurrenceRule | ValueObject | 반복 규칙, 발생일 계산 |

## 한눈에 보는 정리

### 일정/예약 값 객체 요약

| 값 객체 | 주요 속성 | 검증 규칙 | 도메인 연산 |
|--------|----------|----------|------------|
| DateRange | Start, End | End >= Start | Contains, Overlaps, Intersect |
| TimeSlot | Start, End | End > Start | Contains, Conflicts, Duration |
| Duration | TotalMinutes | 0 ~ 525600 | Add, Subtract, 단위 변환 |
| RecurrenceRule | Type, Days, Interval | 유효한 규칙 | GetOccurrences |

### 범위/충돌 검사 공식

| 연산 | 공식 | 설명 |
|------|------|------|
| 겹침 검사 | `A.Start <= B.End && A.End >= B.Start` | 두 범위가 하나라도 겹침 |
| 포함 검사 | `date >= Start && date <= End` | 날짜가 범위 내에 있음 |
| 충돌 검사 | `A.Start < B.End && A.End > B.Start` | 두 슬롯이 시간이 겹침 (경계 제외) |

## FAQ

### Q1: DateRange에서 시작일과 종료일이 같은 경우는?
**A**: 현재 구현에서는 허용됩니다. 하루짜리 범위를 표현할 때 유용합니다.

```csharp
var singleDay = DateRange.Create(
    new DateOnly(2025, 1, 1),
    new DateOnly(2025, 1, 1)
);
// TotalDays = 1
```

하루짜리 범위를 금지하려면 검증 조건을 `end <= start`로 변경합니다.

### Q2: TimeSlot에서 자정을 넘는 시간대를 처리하려면?
**A**: 현재 구현은 자정을 넘는 슬롯(예: 23:00 - 01:00)을 지원하지 않습니다. 이를 지원하려면 별도의 로직이 필요합니다.

```csharp
public static Fin<TimeSlot> CreateCrossMidnight(TimeOnly start, TimeOnly end)
{
    // 자정을 넘는 경우 end < start
    if (start == end)
        return DomainErrors.ZeroDuration(start, end);
    return new TimeSlot(start, end, crossesMidnight: end < start);
}

public bool Contains(TimeOnly time)
{
    if (CrossesMidnight)
        return time >= Start || time < End;
    return time >= Start && time < End;
}
```

### Q3: RecurrenceRule에서 "매월 마지막 금요일"을 표현하려면?
**A**: 현재 구현보다 더 복잡한 규칙이 필요합니다. RFC 5545 (iCalendar) 스펙을 참고하여 확장할 수 있습니다.

```csharp
public static Fin<RecurrenceRule> MonthlyLastWeekday(DayOfWeek day)
{
    return new RecurrenceRule(
        RecurrenceType.MonthlyWeekday,
        new[] { day },
        weekOfMonth: -1,  // -1 = 마지막 주
        1
    );
}

private DateOnly GetLastWeekdayOfMonth(DateOnly date, DayOfWeek dayOfWeek)
{
    var lastDay = new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    while (lastDay.DayOfWeek != dayOfWeek)
        lastDay = lastDay.AddDays(-1);
    return lastDay;
}
```

### Q4: Duration에서 음수 결과를 허용하려면?
**A**: 현재 `Subtract()`는 `Math.Max(0, ...)`로 음수를 방지합니다. 음수를 허용하는 SignedDuration 타입을 별도로 만들 수 있습니다.

```csharp
public sealed class SignedDuration
{
    public int TotalMinutes { get; }  // 음수 허용

    public static Fin<SignedDuration> FromMinutes(int minutes) =>
        new SignedDuration(minutes);

    public SignedDuration Subtract(Duration other) =>
        new(TotalMinutes - other.TotalMinutes);

    public bool IsNegative => TotalMinutes < 0;
}
```

### Q5: 예약 시스템에서 DateRange와 TimeSlot을 함께 사용하려면?
**A**: 두 값 객체를 조합한 TimeWindow 복합 값 객체를 만들 수 있습니다.

```csharp
public sealed class TimeWindow : IEquatable<TimeWindow>
{
    public DateRange DateRange { get; }
    public TimeSlot TimeSlot { get; }

    public static Fin<TimeWindow> Create(DateRange dateRange, TimeSlot timeSlot)
    {
        return new TimeWindow(dateRange, timeSlot);
    }

    public bool Conflicts(TimeWindow other)
    {
        // 날짜 범위가 겹치고 시간 슬롯도 충돌하면 예약 충돌
        return DateRange.Overlaps(other.DateRange) && TimeSlot.Conflicts(other.TimeSlot);
    }

    public IEnumerable<DateTime> GetAllSlotStarts()
    {
        var current = DateRange.Start;
        while (current <= DateRange.End)
        {
            yield return current.ToDateTime(TimeSlot.Start);
            current = current.AddDays(1);
        }
    }
}
```

이 패턴으로 "2025년 1월 1일~10일, 매일 09:00~10:30" 같은 복잡한 예약 시간대를 표현할 수 있습니다.

---

## 테스트

이 프로젝트에는 단위 테스트가 포함되어 있습니다.

### 테스트 실행
```bash
cd SchedulingDomain.Tests.Unit
dotnet test
```

### 테스트 구조
```
SchedulingDomain.Tests.Unit/
├── DateRangeTests.cs       # 날짜 범위 검증/교집합 테스트
├── TimeSlotTests.cs        # 시간 슬롯 충돌 검사 테스트
├── DurationTests.cs        # 기간 산술 연산 테스트
└── RecurrenceRuleTests.cs  # 반복 규칙 발생일 테스트
```

### 주요 테스트 케이스

| 테스트 클래스 | 테스트 내용 |
|-------------|-----------|
| DateRangeTests | 범위 검증, Contains, Overlaps, Intersect |
| TimeSlotTests | 시간 검증, Contains, Conflicts |
| DurationTests | 단위 변환, Add/Subtract 연산 |
| RecurrenceRuleTests | Daily/Weekly/Monthly 발생일 계산 |
