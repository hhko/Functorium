using Framework.Abstractions.Errors;
using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCode.ValueObjects.Comparable.CompositePrimitiveValueObjects;

/// <summary>
/// 날짜 범위를 나타내는 복합 값 객체 (2개 DateTime 조합 예제)
/// ComparableValueObject 기반으로 비교 가능성 자동 구현
/// 10-Validated-Value-Creation 패턴 적용
/// </summary>
public sealed class DateRange : ComparableValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    /// <summary>
    /// DateRange 인스턴스를 생성하는 private 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="startDate">시작일</param>
    /// <param name="endDate">종료일</param>
    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// DateRange 인스턴스를 생성하는 팩토리 메서드
    /// 부모 클래스의 CreateFromValidation 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="startDate">시작일</param>
    /// <param name="endDate">종료일</param>
    /// <returns>성공 시 DateRange, 실패 시 Error</returns>
    public static Fin<DateRange> Create(DateTime startDate, DateTime endDate) =>
        CreateFromValidation(
            Validate(startDate, endDate),
            validValues => new DateRange(validValues.StartDate, validValues.EndDate));

    /// <summary>
    /// 이미 검증된 값으로 DateRange 인스턴스를 생성하는 static internal 메서드
    /// 부모 값 객체에서만 사용
    /// </summary>
    /// <param name="startDate">이미 검증된 시작일</param>
    /// <param name="endDate">이미 검증된 종료일</param>
    /// <returns>생성된 DateRange 인스턴스</returns>
    internal static DateRange CreateFromValidated(DateTime startDate, DateTime endDate) =>
        new DateRange(startDate, endDate);

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 부모 클래스의 CombineValidations 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="startDate">검증할 시작일</param>
    /// <param name="endDate">검증할 종료일</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (DateTime StartDate, DateTime EndDate)> Validate(DateTime startDate, DateTime endDate) =>
        from validStartDate in ValidateStartDate(startDate)
        from validEndDate in ValidateEndDate(endDate)
        from validRange in ValidateDateRange(validStartDate, validEndDate)
        select (StartDate: validStartDate, EndDate: validEndDate);

    /// <summary>
    /// 시작일 검증
    /// </summary>
    /// <param name="startDate">검증할 시작일</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, DateTime> ValidateStartDate(DateTime startDate) =>
        startDate < DateTime.MinValue || startDate > DateTime.MaxValue
            ? DomainErrors.InvalidStartDate(startDate)
            : startDate;

    /// <summary>
    /// 종료일 검증
    /// </summary>
    /// <param name="endDate">검증할 종료일</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, DateTime> ValidateEndDate(DateTime endDate) =>
        endDate < DateTime.MinValue || endDate > DateTime.MaxValue
            ? DomainErrors.InvalidEndDate(endDate)
            : endDate;

    /// <summary>
    /// 날짜 범위 검증
    /// </summary>
    /// <param name="startDate">시작일</param>
    /// <param name="endDate">종료일</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, (DateTime StartDate, DateTime EndDate)> ValidateDateRange(DateTime startDate, DateTime endDate) =>
        startDate >= endDate
            ? DomainErrors.StartAfterEnd(startDate, endDate)
            : (StartDate: startDate, EndDate: endDate);

    internal static class DomainErrors
    {
        /// <summary>
        /// 유효하지 않은 시작일에 대한 에러
        /// </summary>
        /// <param name="value">실패한 시작일 값</param>
        /// <returns>구조화된 에러 정보</returns>
        public static Error InvalidStartDate(DateTime value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(InvalidStartDate)}",
                errorCurrentValue: value,
                errorMessage: $"Start date is invalid. Current value: '{value}'");

        /// <summary>
        /// 유효하지 않은 종료일에 대한 에러
        /// </summary>
        /// <param name="value">실패한 종료일 값</param>
        /// <returns>구조화된 에러 정보</returns>
        public static Error InvalidEndDate(DateTime value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(InvalidEndDate)}",
                errorCurrentValue: value,
                errorMessage: $"End date is invalid. Current value: '{value}'");

        /// <summary>
        /// 시작일이 종료일 이후인 날짜 범위에 대한 에러
        /// </summary>
        /// <param name="startDate">시작일</param>
        /// <param name="endDate">종료일</param>
        /// <returns>구조화된 에러 정보</returns>
        public static Error StartAfterEnd(DateTime startDate, DateTime endDate) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(StartAfterEnd)}",
                errorCurrentValue: $"StartDate: {startDate}, EndDate: {endDate}",
                errorMessage: $"Start date cannot be after or equal to end date. Start: '{startDate}', End: '{endDate}'");
    }

    /// <summary>
    /// 비교 가능한 구성 요소 반환
    /// ComparableValueObject에서 요구하는 메서드
    /// </summary>
    /// <returns>비교 가능한 구성 요소들의 열거</returns>
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() =>
        $"{StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}";
}
