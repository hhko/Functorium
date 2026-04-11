---
title: "일정/예약 도메인 value object"
---

일정 및 예약 시스템에서 자주 사용되는 value object 구현 예제입니다.

## Learning Objectives

1. **DateRange** - 날짜 범위와 겹침 검사를 처리하는 복합 value object
2. **TimeSlot** - 시간 슬롯과 충돌 감지를 처리하는 value object
3. **Duration** - 기간을 다양한 단위로 표현하는 비교 가능 value object
4. **RecurrenceRule** - 반복 일정을 표현하는 복합 value object

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

## value object 설명

### DateRange

날짜 범위를 표현하는 복합 value object입니다.

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

시간 슬롯을 표현하는 value object입니다.

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

기간을 표현하는 비교 가능 value object입니다.

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

반복 규칙을 표현하는 복합 value object입니다.

**특징:**
- 일간/주간/월간 반복 패턴
- factory method로 생성
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

시작과 종료의 논리적 순서를 guarantees.

```csharp
public static Fin<DateRange> Create(DateOnly start, DateOnly end)
{
    if (end < start)
        return DomainErrors.EndBeforeStart;
    return new DateRange(start, end);
}
```

### 2. 충돌/겹침 감지 (Conflict Detection)

두 범위가 겹치는지 verifies.

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

### 4. factory method 패턴 (Factory Methods)

용도별로 명확한 생성 메서드를 provides.

```csharp
public static Fin<RecurrenceRule> Daily(int interval = 1);
public static Fin<RecurrenceRule> Weekly(params DayOfWeek[] days);
public static Fin<RecurrenceRule> Weekdays();
public static Fin<RecurrenceRule> Monthly(int dayOfMonth);
```

### 5. 계산 메서드 (Computational Methods)

value object에 도메인 로직을 캡슐화합니다.

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

### Q1: `DateRange`와 `TimeSlot`을 별도 value object로 분리하는 이유는 무엇인가요?
**A**: `DateRange`는 날짜 수준의 범위(휴가 기간, 프로젝트 일정)를, `TimeSlot`은 하루 내 시간 범위(회의 시간, 진료 시간)를 표현합니다. 관심사가 다르므로 각각의 불변식과 연산을 독립적으로 관리하는 것이 도메인 모델링에 적합합니다.

### Q2: `Duration`에서 최대 기간을 1년(525,600분)으로 제한하는 이유는 무엇인가요?
**A**: 일정/예약 도메인에서 1년을 초과하는 기간은 실무적으로 거의 사용되지 않습니다. 상한을 설정하면 실수로 과도한 값이 입력되는 것을 방지합니다. 도메인 요구사항에 따라 이 제한은 조정할 수 있습니다.

### Q3: `RecurrenceRule`에서 factory method(`Daily`, `Weekly`, `Monthly`)를 분리한 이유는 무엇인가요?
**A**: 생성자 하나로 모든 반복 유형을 처리하면 매개변수 조합이 복잡해지고 유효하지 않은 조합이 생길 수 있습니다. `Weekly(DayOfWeek.Monday, DayOfWeek.Wednesday)`처럼 용도별 factory method를 제공하면 호출 측에서 의도가 명확해지고, 각 메서드가 해당 유형에 맞는 검증만 수행합니다.

---

Part 5에서 다양한 도메인의 value object 적용 사례를 모두 살펴보았습니다. 부록에서는 LanguageExt 주요 타입 참조, 프레임워크 타입 선택 가이드, 용어집 등을 확인할 수 있습니다.

→ [부록 A: LanguageExt 주요 타입 참조](../../../Appendix/A-languageext-reference.md)
