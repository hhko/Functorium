using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;

namespace ComparableValueObjectComposite.ValueObjects;

/// <summary>
/// 도로명을 나타내는 비교 가능한 값 객체
/// </summary>
public sealed class Street : ComparableSimpleValueObject<string>
{
    private Street(string value) : base(value) { }

    /// <summary>
    /// 도로명 값 객체 생성
    /// </summary>
    /// <param name="value">도로명</param>
    /// <returns>성공 시 Street 값 객체, 실패 시 에러</returns>
    public static Fin<Street> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Street(validValue));

    /// <summary>
    /// 이미 검증된 도로명으로 값 객체 생성
    /// </summary>
    /// <param name="validatedValue">검증된 도로명</param>
    /// <returns>Street 값 객체</returns>
    internal static Street CreateFromValidated(string validatedValue) =>
        new Street(validatedValue);

    /// <summary>
    /// 도로명 유효성 검증
    /// </summary>
    /// <param name="value">검증할 도로명</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 2 && value.Length <= 50
            ? value.Trim()
            : DomainErrors.InvalidLength(value);

    public override string ToString() => Value;

    internal static class DomainErrors
    {
        public static Error InvalidLength(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Street)}.{nameof(InvalidLength)}",
                errorCurrentValue: value,
                errorMessage: $"Street name length is invalid. Must be 2-50 characters. Current value: '{value}'");
    }
}



