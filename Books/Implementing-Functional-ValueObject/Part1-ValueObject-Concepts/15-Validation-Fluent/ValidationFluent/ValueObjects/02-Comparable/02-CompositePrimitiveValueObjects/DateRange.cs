using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.Comparable.CompositePrimitiveValueObjects;

/// <summary>
/// 날짜 범위를 나타내는 복합 값 객체 (2개 DateTime 조합 예제)
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
/// </summary>
public sealed class DateRange : ComparableValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Fin<DateRange> Create(DateTime startDate, DateTime endDate) =>
        CreateFromValidation(
            Validate(startDate, endDate),
            validValues => new DateRange(validValues.Min, validValues.Max));

    internal static DateRange CreateFromValidated(DateTime startDate, DateTime endDate) =>
        new DateRange(startDate, endDate);

    public static Validation<Error, (DateTime Min, DateTime Max)> Validate(DateTime startDate, DateTime endDate) =>
        from validStartDate in (Validation<Error, DateTime>)Validate<DateRange>.NotDefault(startDate)
        from validEndDate in (Validation<Error, DateTime>)Validate<DateRange>.NotDefault(endDate)
        from validRange in (Validation<Error, (DateTime, DateTime)>)Validate<DateRange>.ValidStrictRange(validStartDate, validEndDate)
        select validRange;

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() =>
        $"{StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}";
}
