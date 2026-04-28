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
            return DomainError.For<DateRange>(new EndBeforeStart(), $"{start}~{end}", "End is before start");
        return new DateRange(start, end);
    }

    public bool Overlaps(DateRange other) =>
        Start <= other.End && End >= other.Start;
}
```

### TimeSlot

A value object representing time slots.

**Characteristics:**
- Start time < end time invariant
- Time containment check
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
- Creation in minutes/hours/days units
- Negative duration prevention
- Maximum duration limit (1 year)
- Arithmetic operations (Add, Subtract)

```csharp
public sealed class Duration : IComparable<Duration>
{
    public int TotalMinutes { get; }

    public static Fin<Duration> FromMinutes(int minutes)
    {
        if (minutes < 0)
            return DomainError.For<Duration, int>(new DomainErrorKind.Negative(), minutes, "Duration is negative");
        if (minutes > 525600) // 1 year
            return DomainError.For<Duration, int>(new DomainErrorKind.AboveMaximum(), minutes, "Duration exceeds maximum");
        return new Duration(minutes);
    }

    public double TotalHours => TotalMinutes / 60.0;
}
```

### RecurrenceRule

A composite value object representing recurrence rules.

**Characteristics:**
- Daily/weekly/monthly recurrence patterns
- Created via factory methods
- Next occurrence date calculation
- Day-of-week/date-based recurrence

```csharp
public sealed class RecurrenceRule : IEquatable<RecurrenceRule>
{
    public RecurrenceType Type { get; }
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }
    public int? DayOfMonth { get; }

    public static Fin<RecurrenceRule> Weekly(params DayOfWeek[] days)
    {
        if (days.Length == 0)
            return DomainError.For<RecurrenceRule, int>(new DomainErrorKind.Empty(), 0, "No days specified");
        return new RecurrenceRule(RecurrenceType.Weekly, days, null, 1);
    }

    public IEnumerable<DateOnly> GetOccurrences(DateOnly from, int count);
}
```

## Core Patterns

### 1. Range Validation

Guarantees logical order of start and end.

```csharp
public static Fin<DateRange> Create(DateOnly start, DateOnly end)
{
    if (end < start)
        return DomainError.For<DateRange>(new EndBeforeStart(), $"{start}~{end}", "End is before start");
    return new DateRange(start, end);
}
```

### 2. Conflict Detection

Verifies whether two ranges overlap.

```csharp
// Date range overlap
public bool Overlaps(DateRange other) =>
    Start <= other.End && End >= other.Start;

// Time slot conflict
public bool Conflicts(TimeSlot other) =>
    Start < other.End && End > other.Start;
```

### 3. Multiple Unit Support

The same value can be accessed in various units.

```csharp
public int TotalMinutes { get; }
public double TotalHours => TotalMinutes / 60.0;
public double TotalDays => TotalMinutes / (24.0 * 60.0);
```

### 4. Factory Method Pattern

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

### Q1: Why separate `DateRange` and `TimeSlot` into different value objects?
**A**: `DateRange` represents date-level ranges (vacation periods, project schedules), while `TimeSlot` represents intra-day time ranges (meeting times, appointment times). Since the concerns differ, managing each invariant and operation independently is appropriate for domain modeling.

### Q2: Why is the maximum duration in `Duration` limited to 1 year (525,600 minutes)?
**A**: In the scheduling/reservation domain, durations exceeding 1 year are rarely used in practice. Setting an upper bound prevents accidentally entering excessive values. This limit can be adjusted according to domain requirements.

### Q3: Why are separate factory methods (`Daily`, `Weekly`, `Monthly`) used in `RecurrenceRule`?
**A**: Handling all recurrence types with a single constructor makes parameter combinations complex and can create invalid combinations. `Weekly(DayOfWeek.Monday, DayOfWeek.Wednesday)` purpose-specific factory methods make the intent clear on the calling side, and each method only performs validation appropriate for that type.

---

We have explored all value object application cases across various domains in Part 5. The appendix covers LanguageExt key type reference, framework type selection guide, and glossary.

→ [Appendix A: LanguageExt Key Type Reference](../../../Appendix/A-languageext-reference.md)
