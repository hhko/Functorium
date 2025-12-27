using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using static LanguageExt.Prelude;

namespace ComparableValueObjectPrimitive.ValueObjects;

/// <summary>
/// 4. 비교 가능한 복합 primitive 값 객체 - ComparableValueObject
/// 날짜 범위를 나타내는 값 객체
/// 
/// 특징:
/// - 여러 primitive 값을 조합
/// - 비교 기능 자동 제공
/// - 날짜 범위의 유효성 검증
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

    /// <summary>
    /// 날짜 범위 값 객체 생성
    /// </summary>
    /// <param name="startDate">시작 날짜</param>
    /// <param name="endDate">종료 날짜</param>
    /// <returns>성공 시 DateRange 값 객체, 실패 시 에러</returns>
    public static Fin<DateRange> Create(DateTime startDate, DateTime endDate) =>
        CreateFromValidation(
            Validate(startDate, endDate),
            validValues => new DateRange(validValues.startDate, validValues.endDate));

    /// <summary>
    /// 이미 검증된 날짜 범위로 값 객체 생성
    /// </summary>
    /// <param name="validatedValues">검증된 날짜 범위 값들</param>
    /// <returns>DateRange 값 객체</returns>
    internal static DateRange CreateFromValidated((DateTime startDate, DateTime endDate) validatedValues) =>
        new DateRange(validatedValues.startDate, validatedValues.endDate);

    /// <summary>
    /// 날짜 범위 유효성 검증
    /// </summary>
    /// <param name="startDate">시작 날짜</param>
    /// <param name="endDate">종료 날짜</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (DateTime startDate, DateTime endDate)> Validate(DateTime startDate, DateTime endDate) =>
        startDate <= endDate
            ? (startDate, endDate)
            : DomainErrors.StartAfterEnd(startDate, endDate);

    // /// <summary>
    // /// 동등성 비교를 위한 구성 요소 반환
    // /// </summary>
    // /// <returns>동등성 비교 구성 요소</returns>
    // protected override IEnumerable<object> GetEqualityComponents()
    // {
    //     yield return StartDate;
    //     yield return EndDate;
    // }

    /// <summary>
    /// 비교 가능한 구성 요소 반환
    /// </summary>
    /// <returns>비교 가능한 구성 요소</returns>
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() => 
        $"{StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}";

    internal static class DomainErrors
    {
        public static Error StartAfterEnd(DateTime startDate, DateTime endDate) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(StartAfterEnd)}",
                errorCurrentValue1: startDate,
                errorCurrentValue2: endDate,
                errorMessage: "");
    }
}
