using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;

namespace ComparableValueObjectPrimitive.ValueObjects;

/// <summary>
/// 4. 비교 가능한 복합 primitive 값 객체 - ComparableValueObject
/// 날짜 범위를 나타내는 값 객체
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
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

    public static Validation<Error, (DateTime startDate, DateTime endDate)> Validate(DateTime startDate, DateTime endDate) =>
        startDate <= endDate
            ? (startDate, endDate)
            : DomainError.For<DateRange, DateTime, DateTime>(new StartAfterEnd(), startDate, endDate,
                $"Start date cannot be after end date. Start: '{startDate:yyyy-MM-dd}', End: '{endDate:yyyy-MM-dd}'");

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}";
}
