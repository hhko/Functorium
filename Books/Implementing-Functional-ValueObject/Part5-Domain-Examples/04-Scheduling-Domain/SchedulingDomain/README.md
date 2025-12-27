# 일정/예약 도메인 값 객체

일정 및 예약 시스템에서 자주 사용되는 값 객체 구현 예제입니다.

## 학습 목표

1. **DateRange** - 날짜 범위와 겹침 검사를 처리하는 복합 값 객체
2. **TimeSlot** - 시간 슬롯과 충돌 감지를 처리하는 값 객체
3. **Duration** - 기간을 다양한 단위로 표현하는 비교 가능 값 객체
4. **RecurrenceRule** - 반복 일정을 표현하는 복합 값 객체

## 실행

```bash
dotnet run
```

## 예상 출력

```
=== 일정/예약 도메인 값 객체 ===

1. DateRange (날짜 범위)
────────────────────────────────────────
   시작: 2025-01-01
   종료: 2025-01-10
   기간: 10일
   2025-01-05 포함: True
   2025-02-01 포함: False
   잘못된 범위: 종료일이 시작일보다 이전입니다.
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
   음수 기간: 기간은 0 이상이어야 합니다.

4. RecurrenceRule (반복 규칙)
────────────────────────────────────────
   규칙: 매주 월, 수, 금
   다음 5회: 2025-01-01, 2025-01-03, 2025-01-06, 2025-01-08, 2025-01-10
   규칙: 매월 15일
   다음 3회: 2025-01-15, 2025-02-15, 2025-03-15
   규칙: 매주 월, 화, 수, 목, 금
   다음 7회: 2025-01-01, 2025-01-02, 2025-01-03, 2025-01-06, 2025-01-07, 2025-01-08, 2025-01-09
```

## 값 객체 설명

### DateRange

날짜 범위를 표현하는 복합 값 객체입니다.

**특징:**
- 시작일 ≤ 종료일 불변식 보장
- 범위 겹침(Overlaps) 검사
- 교집합(Intersect) 계산
- 기간(TotalDays) 계산

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

시간 슬롯을 표현하는 값 객체입니다.

**특징:**
- 시작 시간 < 종료 시간 불변식
- 시간 포함 여부 검사
- 슬롯 충돌(Conflicts) 감지
- Duration 속성 제공

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

기간을 표현하는 비교 가능 값 객체입니다.

**특징:**
- 분/시간/일 단위 생성
- 음수 기간 방지
- 최대 기간 제한 (1년)
- 연산 지원 (Add, Subtract)

```csharp
public sealed class Duration : IComparable<Duration>
{
    public int TotalMinutes { get; }

    public static Fin<Duration> FromMinutes(int minutes)
    {
        if (minutes < 0)
            return DomainErrors.NegativeDuration;
        if (minutes > 525600) // 1년
            return DomainErrors.ExceedsMaximum;
        return new Duration(minutes);
    }

    public double TotalHours => TotalMinutes / 60.0;
}
```

### RecurrenceRule

반복 규칙을 표현하는 복합 값 객체입니다.

**특징:**
- 일간/주간/월간 반복 패턴
- 팩토리 메서드로 생성
- 다음 발생일 계산
- 요일/날짜 기반 반복

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

## 핵심 패턴

### 1. 범위 검사 (Range Validation)

시작과 종료의 논리적 순서를 보장합니다.

```csharp
public static Fin<DateRange> Create(DateOnly start, DateOnly end)
{
    if (end < start)
        return DomainErrors.EndBeforeStart;
    return new DateRange(start, end);
}
```

### 2. 충돌/겹침 감지 (Conflict Detection)

두 범위가 겹치는지 확인합니다.

```csharp
// 날짜 범위 겹침
public bool Overlaps(DateRange other) =>
    Start <= other.End && End >= other.Start;

// 시간 슬롯 충돌
public bool Conflicts(TimeSlot other) =>
    Start < other.End && End > other.Start;
```

### 3. 다양한 단위 지원 (Multiple Units)

같은 값을 다양한 단위로 접근할 수 있습니다.

```csharp
public int TotalMinutes { get; }
public double TotalHours => TotalMinutes / 60.0;
public double TotalDays => TotalMinutes / (24.0 * 60.0);
```

### 4. 팩토리 메서드 패턴 (Factory Methods)

용도별로 명확한 생성 메서드를 제공합니다.

```csharp
public static Fin<RecurrenceRule> Daily(int interval = 1);
public static Fin<RecurrenceRule> Weekly(params DayOfWeek[] days);
public static Fin<RecurrenceRule> Weekdays();
public static Fin<RecurrenceRule> Monthly(int dayOfMonth);
```

### 5. 계산 메서드 (Computational Methods)

값 객체에 도메인 로직을 캡슐화합니다.

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
