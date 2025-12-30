using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace SchedulingDomain;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 일정/예약 도메인 값 객체 (Functorium 프레임워크 기반) ===\n");

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
        Console.WriteLine("1. DateRange (날짜 범위) - ValueObject");
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
        Console.WriteLine("2. TimeSlot (시간 슬롯) - ValueObject");
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
        Console.WriteLine("3. Duration (기간) - ComparableSimpleValueObject");
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
        Console.WriteLine("4. RecurrenceRule (반복 규칙) - ValueObject");
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
                var startDate = new DateOnly(2025, 1, 1);
                var occurrences = r.GetOccurrences(startDate, 7);
                Console.WriteLine($"   다음 7회: {string.Join(", ", occurrences)}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 구현 (Functorium 프레임워크 기반)
// ========================================

/// <summary>
/// DateRange 값 객체 (ValueObject 기반)
/// </summary>
public sealed class DateRange : ValueObject
{
    // 1.1 속성 선언
    public DateOnly Start { get; }
    public DateOnly End { get; }

    // 파생 속성
    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    // 2. Private 생성자 - 단순 대입만 처리
    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<DateRange> Create(DateOnly start, DateOnly end) =>
        CreateFromValidation(
            Validate(start, end),
            validValues => new DateRange(validValues.Start, validValues.End));

    // 5. Public Validate 메서드 - 단일 검증
    public static Validation<Error, (DateOnly Start, DateOnly End)> Validate(DateOnly start, DateOnly end) =>
        ValidateEndNotBeforeStart(start, end)
            .Map(_ => (start, end));

    // 5.1 종료일 검증
    private static Validation<Error, DateOnly> ValidateEndNotBeforeStart(DateOnly start, DateOnly end) =>
        end >= start
            ? end
            : DomainErrors.EndBeforeStart(start, end);

    // 도메인 메서드
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

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} ~ {End:yyyy-MM-dd}";

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error EndBeforeStart(DateOnly start, DateOnly end) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(EndBeforeStart)}",
                start, end,
                errorMessage: $"End date cannot be before start date. Start: '{start}', End: '{end}'");
    }
}

/// <summary>
/// TimeSlot 값 객체 (ValueObject 기반)
/// </summary>
public sealed class TimeSlot : ValueObject
{
    // 1.1 속성 선언
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    // 파생 속성
    public TimeSpan Duration => End - Start;

    // 2. Private 생성자 - 단순 대입만 처리
    private TimeSlot(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<TimeSlot> Create(TimeOnly start, TimeOnly end) =>
        CreateFromValidation(
            Validate(start, end),
            validValues => new TimeSlot(validValues.Start, validValues.End));

    // 5. Public Validate 메서드 - 단일 검증
    public static Validation<Error, (TimeOnly Start, TimeOnly End)> Validate(TimeOnly start, TimeOnly end) =>
        ValidateEndAfterStart(start, end)
            .Map(_ => (start, end));

    // 5.1 종료 시간 검증
    private static Validation<Error, TimeOnly> ValidateEndAfterStart(TimeOnly start, TimeOnly end) =>
        end > start
            ? end
            : DomainErrors.EndBeforeOrEqualStart(start, end);

    // 도메인 메서드
    public bool Contains(TimeOnly time) => time >= Start && time < End;

    public bool Conflicts(TimeSlot other) =>
        Start < other.End && End > other.Start;

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public override string ToString() => $"{Start:HH:mm} - {End:HH:mm}";

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error EndBeforeOrEqualStart(TimeOnly start, TimeOnly end) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(TimeSlot)}.{nameof(EndBeforeOrEqualStart)}",
                start, end,
                errorMessage: $"End time must be after start time. Start: '{start}', End: '{end}'");
    }
}

/// <summary>
/// Duration 값 객체 (ComparableSimpleValueObject 기반)
/// </summary>
public sealed class Duration : ComparableSimpleValueObject<int>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private Duration(int totalMinutes) : base(totalMinutes) { }

    /// <summary>
    /// 총 분에 대한 public 접근자
    /// </summary>
    public int TotalMinutes => Value;

    // 파생 속성
    public double TotalHours => Value / 60.0;
    public double TotalDays => Value / (24.0 * 60.0);

    // 팩토리 속성
    public static Duration Zero => new(0);

    // 3. Public Create 팩토리 메서드들 - 검증과 생성을 연결
    public static Fin<Duration> FromMinutes(int minutes) =>
        CreateFromValidation(
            Validate(minutes),
            validValue => new Duration(validValue));

    public static Fin<Duration> FromHours(int hours) =>
        FromMinutes(hours * 60);

    public static Fin<Duration> FromDays(int days) =>
        FromMinutes(days * 24 * 60);

    // 5. Public Validate 메서드 - 순차 검증
    public static Validation<Error, int> Validate(int minutes) =>
        ValidateNotNegative(minutes)
            .Bind(_ => ValidateNotExceedsMaximum(minutes))
            .Map(_ => minutes);

    // 5.1 음수 검증
    private static Validation<Error, int> ValidateNotNegative(int minutes) =>
        minutes >= 0
            ? minutes
            : DomainErrors.NegativeDuration(minutes);

    // 5.2 최대값 검증 (1년 = 525,600분)
    private static Validation<Error, int> ValidateNotExceedsMaximum(int minutes) =>
        minutes <= 525600
            ? minutes
            : DomainErrors.ExceedsMaximum(minutes);

    // 도메인 메서드
    public Duration Add(Duration other) => new(Value + other.Value);
    public Duration Subtract(Duration other) =>
        new(Math.Max(0, Value - other.Value));

    public override string ToString()
    {
        if (Value < 60)
            return $"{Value}분";
        if (Value % 60 == 0)
            return $"{Value / 60}시간";
        return $"{Value / 60}시간 {Value % 60}분";
    }

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error NegativeDuration(int minutes) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Duration)}.{nameof(NegativeDuration)}",
                errorCurrentValue: minutes,
                errorMessage: $"Duration cannot be negative. Current value: '{minutes}'");

        public static Error ExceedsMaximum(int minutes) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Duration)}.{nameof(ExceedsMaximum)}",
                errorCurrentValue: minutes,
                errorMessage: $"Duration cannot exceed 1 year (525,600 minutes). Current value: '{minutes}'");
    }
}

/// <summary>
/// RecurrenceRule 값 객체 (ValueObject 기반)
/// </summary>
public sealed class RecurrenceRule : ValueObject
{
    // 1.1 속성 선언
    public RecurrenceType Type { get; }
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }
    public int? DayOfMonth { get; }
    public int Interval { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private RecurrenceRule(RecurrenceType type, IReadOnlyList<DayOfWeek> daysOfWeek, int? dayOfMonth, int interval)
    {
        Type = type;
        DaysOfWeek = daysOfWeek;
        DayOfMonth = dayOfMonth;
        Interval = interval;
    }

    // 3. Public Create 팩토리 메서드들
    public static Fin<RecurrenceRule> Daily(int interval = 1) =>
        CreateFromValidation(
            ValidateDailyInterval(interval),
            validInterval => new RecurrenceRule(RecurrenceType.Daily, [], null, validInterval));

    public static Fin<RecurrenceRule> Weekly(params DayOfWeek[] days) =>
        CreateFromValidation(
            ValidateWeeklyDays(days),
            validDays => new RecurrenceRule(RecurrenceType.Weekly, validDays, null, 1));

    public static Fin<RecurrenceRule> Weekdays() =>
        Weekly(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    public static Fin<RecurrenceRule> Monthly(int dayOfMonth) =>
        CreateFromValidation(
            ValidateMonthlyDay(dayOfMonth),
            validDay => new RecurrenceRule(RecurrenceType.Monthly, [], validDay, 1));

    // 5. Validate 메서드들 - 각 타입별 검증
    private static Validation<Error, int> ValidateDailyInterval(int interval) =>
        interval >= 1
            ? interval
            : DomainErrors.InvalidInterval(interval);

    private static Validation<Error, DayOfWeek[]> ValidateWeeklyDays(DayOfWeek[] days) =>
        days.Length > 0
            ? days.Distinct().OrderBy(d => d).ToArray()
            : DomainErrors.NoDaysSpecified(0);

    private static Validation<Error, int> ValidateMonthlyDay(int day) =>
        day >= 1 && day <= 31
            ? day
            : DomainErrors.InvalidDayOfMonth(day);

    // 도메인 메서드
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

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return DaysOfWeek.Count;
        foreach (var day in DaysOfWeek)
            yield return day;
        yield return DayOfMonth ?? 0;
        yield return Interval;
    }

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

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error InvalidInterval(int interval) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(RecurrenceRule)}.{nameof(InvalidInterval)}",
                errorCurrentValue: interval,
                errorMessage: $"Recurrence interval must be at least 1. Current value: '{interval}'");

        public static Error NoDaysSpecified(int count) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(RecurrenceRule)}.{nameof(NoDaysSpecified)}",
                errorCurrentValue: count,
                errorMessage: $"Weekly recurrence rule must specify at least one day of the week. Current count: '{count}'");

        public static Error InvalidDayOfMonth(int day) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(RecurrenceRule)}.{nameof(InvalidDayOfMonth)}",
                errorCurrentValue: day,
                errorMessage: $"Day of month must be between 1 and 31. Current value: '{day}'");
    }
}

public enum RecurrenceType
{
    Daily,
    Weekly,
    Monthly
}
