---
title: "Scheduling/Reservation Domain"
---
## Overview

종료days이 시작days보다 이른 예약이 저장된다면? 두 회의가 같은  hours대에 겹치는 줄 모르고 확정된다면? "매월 31days" 반복 days정을 2월에 어떻게 처리해야 하는가? days정/예약 시스템에서  hours 데이터를  won시 타입으로 다루면 이런 문제들이 runtime 버그로 이어집니다.

In this chapter, we implement 4 core concepts needed for calendar and reservation systems as value objects, guaranteeing range validation and conflict detection at the type level.

- **DateRange**: 시작days과 종료days을 관리하며 Overlap detection 기능 제공
- **TimeSlot**: 시작  hours과 종료  hours을 관리하며 Conflict detection 기능 제공
- **Duration**: minutes/ hours/days 단위의 기간을 표현하며 연산 기능 제공
- **RecurrenceRule**: 매days/매주/매월 반복 규칙을 표현하며 발생days 계산 기능 제공

## Learning Objectives

### **Core Learning Objectives**
- DateRange와 TimeSlot에서 시작 < 종료 **범위 검증 Pattern을 구현할 수** 있습니다.
- You can **implement overlap/conflict detection logic** between two ranges.
- Duration에서 minutes/ hours/days 간의 **unit conversion을 캡슐화할 수** 있습니다.
- RecurrenceRule에서 다음 발생days을 계산하는 **반복 Pattern 알고리즘을 구현할 수** 있습니다.

### **What You Will Verify Through Practice**
- DateRange Contains, Overlaps, Intersect operations
- TimeSlot Duration calculation and Conflicts detection
- Duration unit conversion and Add/Subtract operations
- RecurrenceRule의 GetOccurrences로 다음 발생days 계산

## Why Is This Needed?

days정과 예약 시스템은  hours 관련 로직이 복잡합니다.  won시 타입으로  hours 데이터를 다루면 여러 문제가 발생합니다.

종료days이 시작days보다 이른 잘못된 데이터가 저장될 수 있는데, DateRange와 TimeSlot은 생성 시점에 범위 유효성을 검증합니다. 두 예약이 겹치는지 판단하는 로직이 여러 곳에 흩어지면 버그가 발생하기 쉬운데, value object에 Overlaps/Conflicts 메서드를 캡슐화하면 days관된 중복 검사를 guarantees. "매월 31days"을 2월에 처리하거나 "매주 월/수/금"의 다음 발생days을 계산하는 복잡한 반복 로직도 RecurrenceRule이 캡슐화합니다.

## Core Concepts

### DateRange (날짜 범위)

DateRange는 시작days과 종료days을 관리합니다. 포함 여부, Overlap detection, 교집합 계산 기능을 provides.

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

Overlap detection(`Start <= other.End && End >= other.Start`)와 같은 범위 로직이 value object 내부에 구현되어 어디서든 days관되게 사용됩니다.

### TimeSlot ( hours 슬롯)

TimeSlot은 하루 중  hours대를 표현합니다.  hours Conflict detection와 기간 계산 기능을 provides.

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

Whether two TimeSlots conflict can be easily determined with `Conflicts()`, simplifying duplicate prevention in reservation systems.

### Duration (기간)

Duration은 minutes 단위로 기간을 저장하고,  hours/days 단위로 변환하는 기능을 provides.

```csharp
public sealed class Duration : ComparableSimpleValueObject<int>
{
    private Duration(int totalMinutes) : base(totalMinutes) { }

    public int TotalMinutes => Value;  // Public accessor for protected Value
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

내부적으로 minutes 단위로 저장하고, `TotalHours`, `TotalDays` 속성으로 다른 단위로 변환합니다. `ToString()`은 사람이 읽기 좋은 형식으로 표시합니다.

### RecurrenceRule (반복 규칙)

RecurrenceRule은 반복 days정의 규칙을 표현합니다. 다음 발생days들을 계산하는 기능을 provides.

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

"매주 월/수/금"이나 "매월 15days" 같은 반복 규칙을 value object로 표현하고, `GetOccurrences()`로 다음 발생days들을 계산합니다.

## Practical Guidelines

### Expected Output
```
=== Scheduling/Reservation Domain Value Objects ===

1. DateRange (Date Range)
────────────────────────────────────────
   Start: 2025-01-01
   End: 2025-01-10
   Period: 10days
   2025-01-05 Contains: True
   2025-02-01 Contains: False
   Invalid range: 종료days은 시작days보다 이전days 수 없습니다.
   Range overlap: True

2. TimeSlot (Time Slot)
────────────────────────────────────────
   Time range: 09:00 - 10:30
   Length: 90minutes
   09:30 Contains: True
   11:00 Contains: False
   Slot conflict: True

3. Duration
────────────────────────────────────────
   90minutes: 1 hours 30minutes
   Hours: 1.5h
   minutes: 90m
   2Hours: 2 hours
   Total: 3 hours 30minutes
   Comparison: 1 hours 30minutes < 2 hours = True
   음수 Period: 기간은 음수days 수 없습니다.

4. RecurrenceRule (Recurrence Rule)
────────────────────────────────────────
   Rule: Every Mon, Wed, Fri
   Next 5: 2025-01-01, 2025-01-03, 2025-01-06, 2025-01-08, 2025-01-10
   Rule: 매월 15days
   Next 3: 2025-01-15, 2025-02-15, 2025-03-15
   Rule: Every Mon, Tue, Wed, Thu, Fri
   Next 7: 2025-01-01, 2025-01-02, 2025-01-03, 2025-01-06, 2025-01-07, 2025-01-08, 2025-01-09
```

## Project Description

### Project Structure
```
04-Scheduling-Domain/
├── SchedulingDomain/
│   ├── Program.cs                  # 메인 실행 파days (4개 값 객체 구현)
│   └── SchedulingDomain.csproj     # 프로젝트 파days
└── README.md                       # 프로젝트 문서
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### value object별 Framework Type

각 value object가 상속하는 Framework Type과 주요 Characteristics을 정리한 것입니다.

| value object | Framework Type | Characteristics |
|--------|---------------|------|
| DateRange | ValueObject | Range validation, overlap/intersection calculation |
| TimeSlot | ValueObject | Range validation, conflict detection |
| Duration | ComparableSimpleValueObject\<int\> | Unit conversion, arithmetic operations |
| RecurrenceRule | ValueObject | 반복 규칙, 발생days 계산 |

## Summary at a Glance

### days정/예약 value object 요약

각 value object의 속성, Validation Rules, Domain Operations을 한눈에 비교할 수 있습니다.

| value object | Key Properties | Validation Rules | Domain Operations |
|--------|----------|----------|------------|
| DateRange | Start, End | End >= Start | Contains, Overlaps, Intersect |
| TimeSlot | Start, End | End > Start | Contains, Conflicts, Duration |
| Duration | TotalMinutes | 0 ~ 525600 | Add, Subtract, unit conversion |
| RecurrenceRule | Type, Days, Interval | Valid rules | GetOccurrences |

### Range/Conflict Detection Formulas

Summarizes conditional expressions used for overlap, containment, and conflict detection.

| Operation | Formula | Description |
|------|------|------|
| Overlap detection | `A.Start <= B.End && A.End >= B.Start` | Two ranges overlap at all |
| Containment check | `date >= Start && date <= End` | Date is within range |
| Conflict detection | `A.Start < B.End && A.End > B.Start` | 두 슬롯이  hours이 겹침 (경계 제외) |

## FAQ

### Q1: DateRange에서 시작days과 종료days이 같은 경우는?

Allowed in the current implementation. Useful for single-day ranges. To prohibit, change validation to `end <= start`.

```csharp
var singleDay = DateRange.Create(
    new DateOnly(2025, 1, 1),
    new DateOnly(2025, 1, 1)
);
// TotalDays = 1
```

### Q2: TimeSlot에서 자정을 넘는  hours대를 처리하려면?

현재 구현은 자정을 넘는 슬롯(예: 23:00 - 01:00)을 지 won하지 않습니다. 이를 지 won하려면 자정 통과 여부를 별도 플래그로 관리해야 합니다.

```csharp
public static Fin<TimeSlot> CreateCrossMidnight(TimeOnly start, TimeOnly end)
{
    // When crossing midnight, end < start
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

### Q3: RecurrenceRule에서 "매월 마지막 금요days"을 표현하려면?

RFC 5545 (iCalendar) 스펙을 참고하여 주 단위 오프셋을 지 won하도록 확장할 수 있습니다.

```csharp
public static Fin<RecurrenceRule> MonthlyLastWeekday(DayOfWeek day)
{
    return new RecurrenceRule(
        RecurrenceType.MonthlyWeekday,
        new[] { day },
        weekOfMonth: -1,  // -1 = last week
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

In Part 5, we explored value object implementations across various domains. 부록에서는 LanguageExt 타입 참조, Framework Type 선택 가이드, 용어집 등 학습에 필요한 참고 자료를 provides.

---

## Tests

This project includes unit tests.

### Tests 실행
```bash
cd SchedulingDomain.Tests.Unit
dotnet test
```

### Tests 구조
```
SchedulingDomain.Tests.Unit/
├── DateRangeTests.cs       # Date range validation/intersection tests
├── TimeSlotTests.cs        #  hours 슬롯 Conflict detection 테스트
├── DurationTests.cs        # Duration arithmetic tests
└── RecurrenceRuleTests.cs  # 반복 규칙 발생days 테스트
```

### Key Test Cases

| Test Class | Test Content |
|-------------|-----------|
| DateRangeTests | Range validation, Contains, Overlaps, Intersect |
| TimeSlotTests |  hours 검증, Contains, Conflicts |
| DurationTests | unit conversion, Add/Subtract 연산 |
| RecurrenceRuleTests | Daily/Weekly/Monthly 발생days 계산 |

---

We have explored all value object application cases across various domains in Part 5. 부록에서는 LanguageExt 주요 타입 참조, Framework Type 선택 가이드, 용어집 등을 확인할 수 있습니다.

→ [부록 A: LanguageExt 주요 타입 참조](../../Appendix/A-languageext-reference.md)
