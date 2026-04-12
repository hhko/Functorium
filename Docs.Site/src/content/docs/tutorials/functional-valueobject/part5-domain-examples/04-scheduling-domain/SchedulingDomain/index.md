---
title: "Scheduling Domain Value Objects"
---

Implementation examples of value objects commonly used in scheduling and reservation systems.

## Learning Objectives

1. **DateRange** - Composite value object handling date ranges and overlap detection
2. **TimeSlot** - Value object handling time slots and conflict detection
3. **Duration** - Comparable value object expressing durations in various units
4. **RecurrenceRule** - Composite value object expressing recurring schedules

## Run

```bash
dotnet run
```

## Expected Output

```
=== Scheduling Domain Value Objects ===

1. DateRange
────────────────────────────────────────
   Start: 2025-01-01
   End: 2025-01-10
   Duration: 10 days
   Contains 2025-01-05: True
   Contains 2025-02-01: False
   Invalid range: End date is before start date.
   Range overlap: True

2. TimeSlot
────────────────────────────────────────
   Time range: 09:00 - 10:30
   Length: 90 minutes
   Contains 09:30: True
   Contains 11:00: False
   Slot conflict: True

3. Duration
────────────────────────────────────────
   90 min: 1 hour 30 minutes
   Hours: 1.5h
   Minutes: 90m
   2 hours: 2 hours
   Total: 3 hours 30 minutes
   Comparison: 1 hour 30 min < 2 hours = True
   Negative duration: Duration must be 0 or greater.

4. RecurrenceRule
────────────────────────────────────────
   Rule: Every Mon, Wed, Fri
   Next 5 occurrences: 2025-01-01, 2025-01-03, 2025-01-06, 2025-01-08, 2025-01-10
   Rule: Monthly on the 15th
   Next 3 occurrences: 2025-01-15, 2025-02-15, 2025-03-15
   Rule: Every Mon, Tue, Wed, Thu, Fri
   Next 7 occurrences: 2025-01-01, 2025-01-02, 2025-01-03, 2025-01-06, 2025-01-07, 2025-01-08, 2025-01-09
```

## Value Object Descriptions

### DateRange

A composite value object representing a date range.

**Features:**
- Guarantees start <= end invariant
- Overlap detection (Overlaps)
- Intersection calculation (Intersect)
- Duration calculation (TotalDays)

```csharp
public sealed class DateRange : IEquatable<DateRange>
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public static Fin<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
            return DomainErrors.EndBeforeStart;
        return new DateRange(start, end);
    }

    public bool Overlaps(DateRange other) =>
        Start <= other.End && End >= other.Start;
}
```

### TimeSlot

 hours 슬롯을 표현하는 value object입니다.

**Characteristics:**
- 시작  hours < 종료  hours 불변식
-  hours 포함 여부 검사
- Slot conflict (Conflicts) detection
- Duration property provided

```csharp
public sealed class TimeSlot : IEquatable<TimeSlot>
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public TimeSpan Duration => End - Start;

    public bool Conflicts(TimeSlot other) =>
        Start < other.End && End > other.Start;
}
```

### Duration

A comparable value object representing durations.

**Characteristics:**
- minutes/ hours/days 단위 생성
- Negative duration prevention
- 최대 기간 제한 (1 years)
- 연산 지 won (Add, Subtract)

```csharp
public sealed class Duration : IComparable<Duration>
{
    public int TotalMinutes { get; }

    public static Fin<Duration> FromMinutes(int minutes)
    {
        if (minutes < 0)
            return DomainErrors.NegativeDuration;
        if (minutes > 525600) // 1 years
            return DomainErrors.ExceedsMaximum;
        return new Duration(minutes);
    }

    public double TotalHours => TotalMinutes / 60.0;
}
```

### RecurrenceRule

A composite value object representing recurrence rules.

**Characteristics:**
- days간/주간/월간 반복 Pattern
- Created via factory methods
- 다음 발생days 계산
- 요days/날짜 기반 반복

```csharp
public sealed class RecurrenceRule : IEquatable<RecurrenceRule>
{
    public RecurrenceType Type { get; }
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }
    public int? DayOfMonth { get; }

    public static Fin<RecurrenceRule> Weekly(params DayOfWeek[] days)
    {
        if (days.Length == 0)
            return DomainErrors.NoDaysSpecified;
        return new RecurrenceRule(RecurrenceType.Weekly, days, null, 1);
    }

    public IEnumerable<DateOnly> GetOccurrences(DateOnly from, int count);
}
```

## 핵심 Pattern

### 1. Range Validation

Guarantees logical order of start and end.

```csharp
public static Fin<DateRange> Create(DateOnly start, DateOnly end)
{
    if (end < start)
        return DomainErrors.EndBeforeStart;
    return new DateRange(start, end);
}
```

### 2. Conflict Detection

Verifies whether two ranges overlap.

```csharp
// Date range overlap
public bool Overlaps(DateRange other) =>
    Start <= other.End && End >= other.Start;

//  hours 슬롯 충돌
public bool Conflicts(TimeSlot other) =>
    Start < other.End && End > other.Start;
```

### 3. 다양한 단위 지 won (Multiple Units)

The same value can be accessed in various units.

```csharp
public int TotalMinutes { get; }
public double TotalHours => TotalMinutes / 60.0;
public double TotalDays => TotalMinutes / (24.0 * 60.0);
```

### 4. factory method Pattern (Factory Methods)

Provides clear creation methods for each use case.

```csharp
public static Fin<RecurrenceRule> Daily(int interval = 1);
public static Fin<RecurrenceRule> Weekly(params DayOfWeek[] days);
public static Fin<RecurrenceRule> Weekdays();
public static Fin<RecurrenceRule> Monthly(int dayOfMonth);
```

### 5. Computational Methods

Encapsulates domain logic within value objects.

```csharp
public IEnumerable<DateOnly> GetOccurrences(DateOnly from, int count)
{
    var results = new List<DateOnly>();
    var current = from;

    while (results.Count < count)
    {
        if (IsOccurrence(current))
            results.Add(current);
        current = current.AddDays(1);
    }

    return results;
}
```

## FAQ

### Q1: `DateRange`와 `TimeSlot`을 별도 value object로 minutes리하는 이유는 무엇인가요?
**A**: `DateRange`는 날짜 수준의 범위(휴가 기간, 프로젝트 days정)를, `TimeSlot`은 하루 내  hours 범위(회의  hours, 진료  hours)를 표현합니다. Since the concerns differ, managing each invariant and operation independently is appropriate for domain modeling.

### Q2: `Duration`에서 최대 기간을 1 years(525,600minutes)으로 be limited?
**A**: days정/예약 도메인에서 1 years을 초과하는 기간은 실무적으로 거의 사용되지 않습니다. Setting an upper bound prevents accidentally entering excessive values. This limit can be adjusted according to domain requirements.

### Q3: `RecurrenceRule`에서 factory method(`Daily`, `Weekly`, `Monthly`)를 minutes리한 이유는 무엇인가요?
**A**: Handling all recurrence types with a single constructor makes parameter combinations complex and can create invalid combinations. `Weekly(DayOfWeek.Monday, DayOfWeek.Wednesday)` purpose-specific factory methods make the intent clear on the calling side, and each method only performs validation appropriate for that type.

---

We have explored all value object application cases across various domains in Part 5. 부록에서는 LanguageExt 주요 타입 참조, Framework Type 선택 가이드, 용어집 등을 확인할 수 있습니다.

→ [부록 A: LanguageExt 주요 타입 참조](../../../Appendix/A-languageext-reference.md)
