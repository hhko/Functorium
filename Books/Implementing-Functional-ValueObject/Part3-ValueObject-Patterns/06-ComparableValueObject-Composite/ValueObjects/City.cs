using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;

namespace ComparableValueObjectComposite.ValueObjects;

/// <summary>
/// 도시명을 나타내는 비교 가능한 값 객체
/// </summary>
public sealed class City : ComparableSimpleValueObject<string>
{
    private City(string value) : base(value) { }

    /// <summary>
    /// 도시명 값 객체 생성
    /// </summary>
    /// <param name="value">도시명</param>
    /// <returns>성공 시 City 값 객체, 실패 시 에러</returns>
    public static Fin<City> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new City(validValue));

    /// <summary>
    /// 이미 검증된 도시명으로 값 객체 생성
    /// </summary>
    /// <param name="validatedValue">검증된 도시명</param>
    /// <returns>City 값 객체</returns>
    internal static City CreateFromValidated(string validatedValue) =>
        new City(validatedValue);

    /// <summary>
    /// 도시명 유효성 검증
    /// </summary>
    /// <param name="value">검증할 도시명</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 2 && value.Length <= 30
            ? value.Trim()
            : DomainErrors.InvalidLength(value);

    public override string ToString() => Value;

    internal static class DomainErrors
    {
        public static Error InvalidLength(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(City)}.{nameof(InvalidLength)}",
                errorCurrentValue: value,
                errorMessage: "");
    }
}



