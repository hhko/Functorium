using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCodeFluent.ValueObjects.Comparable.CompositePrimitiveValueObjects;

/// <summary>
/// 날짜 범위를 나타내는 복합 값 객체 (2개 DateTime 조합 예제)
/// DomainError 헬퍼를 사용한 간결한 에러 처리
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
            validValues => new DateRange(validValues.StartDate, validValues.EndDate));

    public static DateRange CreateFromValidated(DateTime startDate, DateTime endDate) =>
        new DateRange(startDate, endDate);

    public static Validation<Error, (DateTime StartDate, DateTime EndDate)> Validate(DateTime startDate, DateTime endDate) =>
        from validStartDate in ValidateStartDate(startDate)
        from validEndDate in ValidateEndDate(endDate)
        from validRange in ValidateDateRange(validStartDate, validEndDate)
        select (StartDate: validStartDate, EndDate: validEndDate);

    private static Validation<Error, DateTime> ValidateStartDate(DateTime startDate) =>
        startDate < DateTime.MinValue || startDate > DateTime.MaxValue
            ? DomainError.For<DateRange, DateTime>(new DomainErrorType.Custom("InvalidStartDate"), startDate,
                $"Start date is invalid. Current value: '{startDate}'")
            : startDate;

    private static Validation<Error, DateTime> ValidateEndDate(DateTime endDate) =>
        endDate < DateTime.MinValue || endDate > DateTime.MaxValue
            ? DomainError.For<DateRange, DateTime>(new DomainErrorType.Custom("InvalidEndDate"), endDate,
                $"End date is invalid. Current value: '{endDate}'")
            : endDate;

    private static Validation<Error, (DateTime StartDate, DateTime EndDate)> ValidateDateRange(DateTime startDate, DateTime endDate) =>
        startDate >= endDate
            ? DomainError.For<DateRange, DateTime, DateTime>(new DomainErrorType.Custom("StartAfterEnd"), startDate, endDate,
                $"Start date cannot be after or equal to end date. Start: '{startDate}', End: '{endDate}'")
            : (StartDate: startDate, EndDate: endDate);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() =>
        $"{StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}";
}
