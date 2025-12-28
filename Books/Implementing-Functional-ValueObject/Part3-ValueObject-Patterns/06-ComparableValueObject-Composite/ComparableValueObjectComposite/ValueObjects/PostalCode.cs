using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;

namespace ComparableValueObjectComposite.ValueObjects;

/// <summary>
/// 우편번호를 나타내는 비교 가능한 값 객체
/// </summary>
public sealed class PostalCode : ComparableSimpleValueObject<string>
{
    private PostalCode(string value) : base(value) { }

    /// <summary>
    /// 우편번호 값 객체 생성
    /// </summary>
    /// <param name="value">우편번호</param>
    /// <returns>성공 시 PostalCode 값 객체, 실패 시 에러</returns>
    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new PostalCode(validValue));

    /// <summary>
    /// 이미 검증된 우편번호로 값 객체 생성
    /// </summary>
    /// <param name="validatedValue">검증된 우편번호</param>
    /// <returns>PostalCode 값 객체</returns>
    internal static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    /// <summary>
    /// 우편번호 유효성 검증
    /// </summary>
    /// <param name="value">검증할 우편번호</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length == 5 && value.All(char.IsDigit)
            ? value
            : DomainErrors.NotFiveDigits(value);

    public override string ToString() => Value;

    internal static class DomainErrors
    {
        public static Error NotFiveDigits(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PostalCode)}.{nameof(NotFiveDigits)}",
                errorCurrentValue: value,
                errorMessage: $"Postal code must be exactly 5 digits. Current value: '{value}'");
    }
}



