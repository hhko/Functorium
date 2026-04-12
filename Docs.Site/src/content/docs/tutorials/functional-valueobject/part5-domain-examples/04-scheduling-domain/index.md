---
title: "Scheduling/Reservation Domain"
---
## Overview

What if a reservation where the end date is earlier than the start date gets saved? What if two meetings are confirmed without knowing they overlap in the same time slot? How should a "monthly on the 31st" recurrence rule be handled in February? In scheduling/reservation systems, handling time data with primitive types leads to these problems becoming runtime bugs.

In this chapter, we implement 4 core concepts needed for calendar and reservation systems as value objects, guaranteeing range validation and conflict detection at the type level.

- **DateRange**: Manages start and end dates with overlap detection
- **TimeSlot**: Manages start and end times with conflict detection
- **Duration**: Represents durations in minutes/hours/days with arithmetic operations
- **RecurrenceRule**: Represents daily/weekly/monthly recurrence rules with occurrence date calculation

## Learning Objectives

### **Core Learning Objectives**
- You can **implement a range validation pattern** with start < end in DateRange and TimeSlot.
- You can **implement overlap/conflict detection logic** between two ranges.
- You can **encapsulate unit conversion** between minutes/hours/days in Duration.
- You can **implement recurrence pattern algorithms** that calculate next occurrence dates in RecurrenceRule.

### **What You Will Verify Through Practice**
- DateRange Contains, Overlaps, Intersect operations
- TimeSlot Duration calculation and Conflicts detection
- Duration unit conversion and Add/Subtract operations
- RecurrenceRule's GetOccurrences for calculating next occurrence dates

## Why Is This Needed?

Scheduling and reservation systems have complex time-related logic. Handling time data with primitive types causes several problems.

Invalid data where the end date is earlier than the start date can be stored, but DateRange and TimeSlot validate range validity at creation time. When logic for determining whether two reservations overlap is scattered across multiple locations, bugs are likely to occur, but encapsulating Overlaps/Conflicts methods in value objects guarantees consistent duplicate checking. Complex recurrence logic such as handling "monthly on the 31st" in February or calculating the next occurrence dates for "every Mon/Wed/Fri" is also encapsulated by RecurrenceRule.

## Core Concepts

### DateRange (Date Range)

DateRange manages start and end dates. It provides containment checks, overlap detection, and intersection calculation.

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

Range logic such as overlap detection (`Start <= other.End && End >= other.Start`) is implemented within the value object and used consistently everywhere.

### TimeSlot (Time Slot)

TimeSlot represents a time period within a day. It provides time conflict detection and duration calculation.

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

### Duration

Duration stores durations in minutes and provides conversion to hours/days.

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

Internally stored in minutes, with conversion to other units via the `TotalHours` and `TotalDays` properties. `ToString()` displays in a human-readable format.

### RecurrenceRule (Recurrence Rule)

RecurrenceRule represents recurrence rules for recurring schedules. It provides the ability to calculate next occurrence dates.

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

Recurrence rules like "every Mon/Wed/Fri" or "monthly on the 15th" are represented as value objects, and `GetOccurrences()` calculates the next occurrence dates.

## Practical Guidelines

### Expected Output
```
=== Scheduling/Reservation Domain Value Objects ===

1. DateRange (Date Range)
────────────────────────────────────────
   Start: 2025-01-01
   End: 2025-01-10
   Period: 10 days
   2025-01-05 Contains: True
   2025-02-01 Contains: False
   Invalid range: End date cannot be before start date.
   Range overlap: True

2. TimeSlot (Time Slot)
────────────────────────────────────────
   Time range: 09:00 - 10:30
   Length: 90 minutes
   09:30 Contains: True
   11:00 Contains: False
   Slot conflict: True

3. Duration
────────────────────────────────────────
   90 minutes: 1 hour 30 minutes
   Hours: 1.5h
   Minutes: 90m
   2 hours: 2 hours
   Total: 3 hours 30 minutes
   Comparison: 1 hour 30 minutes < 2 hours = True
   Negative duration: Duration cannot be negative.

4. RecurrenceRule (Recurrence Rule)
────────────────────────────────────────
   Rule: Every Mon, Wed, Fri
   Next 5: 2025-01-01, 2025-01-03, 2025-01-06, 2025-01-08, 2025-01-10
   Rule: Monthly on the 15th
   Next 3: 2025-01-15, 2025-02-15, 2025-03-15
   Rule: Every Mon, Tue, Wed, Thu, Fri
   Next 7: 2025-01-01, 2025-01-02, 2025-01-03, 2025-01-06, 2025-01-07, 2025-01-08, 2025-01-09
```

## Project Description

### Project Structure
```
04-Scheduling-Domain/
├── SchedulingDomain/
│   ├── Program.cs                  # Main executable (4 value object implementations)
│   └── SchedulingDomain.csproj     # Project file
└── README.md                       # Project documentation
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### Framework Type per Value Object

Summarizes the framework type each value object inherits and its key characteristics.

| value object | Framework Type | Characteristics |
|--------|---------------|------|
| DateRange | ValueObject | Range validation, overlap/intersection calculation |
| TimeSlot | ValueObject | Range validation, conflict detection |
| Duration | ComparableSimpleValueObject\<int\> | Unit conversion, arithmetic operations |
| RecurrenceRule | ValueObject | Recurrence rules, occurrence date calculation |

## Summary at a Glance

### Scheduling/Reservation Value Object Summary

You can compare the properties, validation rules, and domain operations of each value object at a glance.

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
| Conflict detection | `A.Start < B.End && A.End > B.Start` | Two slots overlap in time (excluding boundaries) |

## FAQ

### Q1: What about when start and end dates are the same in DateRange?

Allowed in the current implementation. Useful for single-day ranges. To prohibit, change validation to `end <= start`.

```csharp
var singleDay = DateRange.Create(
    new DateOnly(2025, 1, 1),
    new DateOnly(2025, 1, 1)
);
// TotalDays = 1
```

### Q2: How to handle time slots that cross midnight in TimeSlot?

The current implementation does not support slots that cross midnight (e.g., 23:00 - 01:00). To support this, a separate flag for midnight crossing must be managed.

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

### Q3: How to represent "last Friday of every month" in RecurrenceRule?

You can extend it to support week-based offsets by referencing the RFC 5545 (iCalendar) specification.

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

In Part 5, we explored value object implementations across various domains. The appendix provides reference materials needed for learning, including LanguageExt type reference, framework type selection guide, and glossary.

---

## Tests

This project includes unit tests.

### Running Tests
```bash
cd SchedulingDomain.Tests.Unit
dotnet test
```

### Test Structure
```
SchedulingDomain.Tests.Unit/
├── DateRangeTests.cs       # Date range validation/intersection tests
├── TimeSlotTests.cs        # Time slot conflict detection tests
├── DurationTests.cs        # Duration arithmetic tests
└── RecurrenceRuleTests.cs  # Recurrence rule occurrence date tests
```

### Key Test Cases

| Test Class | Test Content |
|-------------|-----------|
| DateRangeTests | Range validation, Contains, Overlaps, Intersect |
| TimeSlotTests | Time validation, Contains, Conflicts |
| DurationTests | Unit conversion, Add/Subtract operations |
| RecurrenceRuleTests | Daily/Weekly/Monthly occurrence date calculation |

---

We have explored all value object application cases across various domains in Part 5. The appendix covers LanguageExt key type reference, framework type selection guide, and glossary.

→ [Appendix A: LanguageExt Key Type Reference](../../Appendix/A-languageext-reference.md)
