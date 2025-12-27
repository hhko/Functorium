using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace SchedulingDomain;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 일정/예약 도메인 값 객체 ===\n");

        // 1. DateRange
        DemonstrateDateRange();

        // 2. TimeSlot
        DemonstrateTimeSlot();

        // 3. Duration
        DemonstrateDuration();

        // 4. RecurrenceRule
        DemonstrateRecurrenceRule();
    }

    static void DemonstrateDateRange()
    {
        Console.WriteLine("1. DateRange (날짜 범위)");
        Console.WriteLine("─".PadRight(40, '─'));

        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 1, 10);

        var range = DateRange.Create(startDate, endDate);
        range.Match(
            Succ: r =>
            {
                Console.WriteLine($"   시작: {r.Start}");
                Console.WriteLine($"   종료: {r.End}");
                Console.WriteLine($"   기간: {r.TotalDays}일");
                Console.WriteLine($"   2025-01-05 포함: {r.Contains(new DateOnly(2025, 1, 5))}");
                Console.WriteLine($"   2025-02-01 포함: {r.Contains(new DateOnly(2025, 2, 1))}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        // 잘못된 범위 (종료일이 시작일보다 이전)
        var invalid = DateRange.Create(endDate, startDate);
        invalid.Match(
            Succ: _ => { },
            Fail: e => Console.WriteLine($"   잘못된 범위: {e.Message}")
        );

        // 겹침 확인
        var range2 = DateRange.Create(new DateOnly(2025, 1, 8), new DateOnly(2025, 1, 15));
        (range, range2).Apply((r1, r2) =>
        {
            Console.WriteLine($"   범위 겹침: {r1.Overlaps(r2)}");
            return unit;
        });

        Console.WriteLine();
    }

    static void DemonstrateTimeSlot()
    {
        Console.WriteLine("2. TimeSlot (시간 슬롯)");
        Console.WriteLine("─".PadRight(40, '─'));

        var slot = TimeSlot.Create(
            new TimeOnly(9, 0),
            new TimeOnly(10, 30)
        );

        slot.Match(
            Succ: s =>
            {
                Console.WriteLine($"   시간대: {s}");
                Console.WriteLine($"   길이: {s.Duration.TotalMinutes}분");
                Console.WriteLine($"   09:30 포함: {s.Contains(new TimeOnly(9, 30))}");
                Console.WriteLine($"   11:00 포함: {s.Contains(new TimeOnly(11, 0))}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        // 슬롯 충돌 확인
        var slot2 = TimeSlot.Create(new TimeOnly(10, 0), new TimeOnly(11, 0));
        (slot, slot2).Apply((s1, s2) =>
        {
            Console.WriteLine($"   슬롯 충돌: {s1.Conflicts(s2)}");
            return unit;
        });

        Console.WriteLine();
    }

    static void DemonstrateDuration()
    {
        Console.WriteLine("3. Duration (기간)");
        Console.WriteLine("─".PadRight(40, '─'));

        var duration1 = Duration.FromMinutes(90);
        var duration2 = Duration.FromHours(2);

        duration1.Match(
            Succ: d =>
            {
                Console.WriteLine($"   90분: {d}");
                Console.WriteLine($"   시간: {d.TotalHours}h");
                Console.WriteLine($"   분: {d.TotalMinutes}m");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        duration2.Match(
            Succ: d => Console.WriteLine($"   2시간: {d}"),
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        // 연산
        (duration1, duration2).Apply((d1, d2) =>
        {
            Console.WriteLine($"   합계: {d1.Add(d2)}");
            Console.WriteLine($"   비교: {d1} < {d2} = {d1.CompareTo(d2) < 0}");
            return unit;
        });

        // 잘못된 기간 (음수)
        var invalid = Duration.FromMinutes(-10);
        invalid.Match(
            Succ: _ => { },
            Fail: e => Console.WriteLine($"   음수 기간: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateRecurrenceRule()
    {
        Console.WriteLine("4. RecurrenceRule (반복 규칙)");
        Console.WriteLine("─".PadRight(40, '─'));

        // 매주 월, 수, 금
        var weekly = RecurrenceRule.Weekly(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        weekly.Match(
            Succ: r =>
            {
                Console.WriteLine($"   규칙: {r}");
                var startDate = new DateOnly(2025, 1, 1);
                var occurrences = r.GetOccurrences(startDate, 5);
                Console.WriteLine($"   다음 5회: {string.Join(", ", occurrences)}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        // 매월 15일
        var monthly = RecurrenceRule.Monthly(15);
        monthly.Match(
            Succ: r =>
            {
                Console.WriteLine($"   규칙: {r}");
                var startDate = new DateOnly(2025, 1, 1);
                var occurrences = r.GetOccurrences(startDate, 3);
                Console.WriteLine($"   다음 3회: {string.Join(", ", occurrences)}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        // 매일 (평일만)
        var daily = RecurrenceRule.Weekdays();
        daily.Match(
            Succ: r =>
            {
                Console.WriteLine($"   규칙: {r}");
                var startDate = new DateOnly(2025, 1, 1); // 수요일
                var occurrences = r.GetOccurrences(startDate, 7);
                Console.WriteLine($"   다음 7회: {string.Join(", ", occurrences)}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 구현
// ========================================

public sealed class DateRange : IEquatable<DateRange>
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static Fin<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
            return DomainErrors.EndBeforeStart(start, end);
        return new DateRange(start, end);
    }

    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other) =>
        Start <= other.End && End >= other.Start;

    public Option<DateRange> Intersect(DateRange other)
    {
        if (!Overlaps(other))
            return None;

        var newStart = Start > other.Start ? Start : other.Start;
        var newEnd = End < other.End ? End : other.End;
        return new DateRange(newStart, newEnd);
    }

    public DateRange Extend(int days) =>
        new(Start, End.AddDays(days));

    public bool Equals(DateRange? other) =>
        other is not null && Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => obj is DateRange other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Start, End);
    public override string ToString() => $"{Start:yyyy-MM-dd} ~ {End:yyyy-MM-dd}";

    internal static class DomainErrors
    {
        public static Error EndBeforeStart(DateOnly start, DateOnly end) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DateRange)}.{nameof(EndBeforeStart)}",
                start, end,
                errorMessage: "종료일은 시작일보다 이전일 수 없습니다.");
    }
}

public sealed class TimeSlot : IEquatable<TimeSlot>
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    private TimeSlot(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    public static Fin<TimeSlot> Create(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            return DomainErrors.EndBeforeOrEqualStart(start, end);
        return new TimeSlot(start, end);
    }

    public TimeSpan Duration => End - Start;

    public bool Contains(TimeOnly time) => time >= Start && time < End;

    public bool Conflicts(TimeSlot other) =>
        Start < other.End && End > other.Start;

    public bool Equals(TimeSlot? other) =>
        other is not null && Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => obj is TimeSlot other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Start, End);
    public override string ToString() => $"{Start:HH:mm} - {End:HH:mm}";

    internal static class DomainErrors
    {
        public static Error EndBeforeOrEqualStart(TimeOnly start, TimeOnly end) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(TimeSlot)}.{nameof(EndBeforeOrEqualStart)}",
                start, end,
                errorMessage: "종료 시간은 시작 시간보다 이후여야 합니다.");
    }
}

public sealed class Duration : IComparable<Duration>, IEquatable<Duration>
{
    public int TotalMinutes { get; }

    private Duration(int totalMinutes) => TotalMinutes = totalMinutes;

    public static Fin<Duration> FromMinutes(int minutes)
    {
        if (minutes < 0)
            return DomainErrors.NegativeDuration(minutes);
        if (minutes > 525600) // 1년 = 365일 * 24시간 * 60분
            return DomainErrors.ExceedsMaximum(minutes);
        return new Duration(minutes);
    }

    public static Fin<Duration> FromHours(int hours) =>
        FromMinutes(hours * 60);

    public static Fin<Duration> FromDays(int days) =>
        FromMinutes(days * 24 * 60);

    public static Duration Zero => new(0);

    public double TotalHours => TotalMinutes / 60.0;
    public double TotalDays => TotalMinutes / (24.0 * 60.0);

    public Duration Add(Duration other) => new(TotalMinutes + other.TotalMinutes);
    public Duration Subtract(Duration other) =>
        new(Math.Max(0, TotalMinutes - other.TotalMinutes));

    public int CompareTo(Duration? other) =>
        other is null ? 1 : TotalMinutes.CompareTo(other.TotalMinutes);

    public bool Equals(Duration? other) =>
        other is not null && TotalMinutes == other.TotalMinutes;

    public override bool Equals(object? obj) => obj is Duration other && Equals(other);
    public override int GetHashCode() => TotalMinutes.GetHashCode();

    public override string ToString()
    {
        if (TotalMinutes < 60)
            return $"{TotalMinutes}분";
        if (TotalMinutes % 60 == 0)
            return $"{TotalMinutes / 60}시간";
        return $"{TotalMinutes / 60}시간 {TotalMinutes % 60}분";
    }

    internal static class DomainErrors
    {
        public static Error NegativeDuration(int minutes) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Duration)}.{nameof(NegativeDuration)}",
                errorCurrentValue: minutes,
                errorMessage: "기간은 음수일 수 없습니다.");
        public static Error ExceedsMaximum(int minutes) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Duration)}.{nameof(ExceedsMaximum)}",
                errorCurrentValue: minutes,
                errorMessage: "기간은 1년(525,600분)을 초과할 수 없습니다.");
    }
}

public sealed class RecurrenceRule : IEquatable<RecurrenceRule>
{
    public RecurrenceType Type { get; }
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }
    public int? DayOfMonth { get; }
    public int Interval { get; }

    private RecurrenceRule(RecurrenceType type, IReadOnlyList<DayOfWeek> daysOfWeek, int? dayOfMonth, int interval)
    {
        Type = type;
        DaysOfWeek = daysOfWeek;
        DayOfMonth = dayOfMonth;
        Interval = interval;
    }

    public static Fin<RecurrenceRule> Daily(int interval = 1)
    {
        if (interval < 1)
            return DomainErrors.InvalidInterval(interval);
        return new RecurrenceRule(RecurrenceType.Daily, [], null, interval);
    }

    public static Fin<RecurrenceRule> Weekly(params DayOfWeek[] days)
    {
        if (days.Length == 0)
            return DomainErrors.NoDaysSpecified(0);
        return new RecurrenceRule(RecurrenceType.Weekly, days.Distinct().OrderBy(d => d).ToArray(), null, 1);
    }

    public static Fin<RecurrenceRule> Weekdays() =>
        Weekly(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    public static Fin<RecurrenceRule> Monthly(int dayOfMonth)
    {
        if (dayOfMonth < 1 || dayOfMonth > 31)
            return DomainErrors.InvalidDayOfMonth(dayOfMonth);
        return new RecurrenceRule(RecurrenceType.Monthly, [], dayOfMonth, 1);
    }

    public IEnumerable<DateOnly> GetOccurrences(DateOnly from, int count)
    {
        var results = new List<DateOnly>();
        var current = from;

        while (results.Count < count)
        {
            if (IsOccurrence(current))
                results.Add(current);
            current = current.AddDays(1);

            // 무한 루프 방지 (최대 3년)
            if (current > from.AddYears(3))
                break;
        }

        return results;
    }

    private bool IsOccurrence(DateOnly date) => Type switch
    {
        RecurrenceType.Daily => true,
        RecurrenceType.Weekly => DaysOfWeek.Contains(date.DayOfWeek),
        RecurrenceType.Monthly => date.Day == DayOfMonth,
        _ => false
    };

    public bool Equals(RecurrenceRule? other) =>
        other is not null &&
        Type == other.Type &&
        DaysOfWeek.SequenceEqual(other.DaysOfWeek) &&
        DayOfMonth == other.DayOfMonth &&
        Interval == other.Interval;

    public override bool Equals(object? obj) => obj is RecurrenceRule other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Type, DaysOfWeek.Count, DayOfMonth, Interval);

    public override string ToString() => Type switch
    {
        RecurrenceType.Daily when Interval == 1 => "매일",
        RecurrenceType.Daily => $"{Interval}일마다",
        RecurrenceType.Weekly => $"매주 {string.Join(", ", DaysOfWeek.Select(GetKoreanDay))}",
        RecurrenceType.Monthly => $"매월 {DayOfMonth}일",
        _ => "알 수 없음"
    };

    private static string GetKoreanDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "일",
        DayOfWeek.Monday => "월",
        DayOfWeek.Tuesday => "화",
        DayOfWeek.Wednesday => "수",
        DayOfWeek.Thursday => "목",
        DayOfWeek.Friday => "금",
        DayOfWeek.Saturday => "토",
        _ => "?"
    };

    internal static class DomainErrors
    {
        public static Error InvalidInterval(int interval) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(RecurrenceRule)}.{nameof(InvalidInterval)}",
                errorCurrentValue: interval,
                errorMessage: "반복 간격은 1 이상이어야 합니다.");
        public static Error NoDaysSpecified(int count) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(RecurrenceRule)}.{nameof(NoDaysSpecified)}",
                errorCurrentValue: count,
                errorMessage: "주간 반복 규칙에는 최소 하나의 요일을 지정해야 합니다.");
        public static Error InvalidDayOfMonth(int day) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(RecurrenceRule)}.{nameof(InvalidDayOfMonth)}",
                errorCurrentValue: day,
                errorMessage: "월간 반복 일은 1에서 31 사이여야 합니다.");
    }
}

public enum RecurrenceType
{
    Daily,
    Weekly,
    Monthly
}
