using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using static LanguageExt.Prelude;

namespace ValueObjectComposite.ValueObjects;

/// <summary>
/// 이메일 로컬 부분을 나타내는 값 객체
/// </summary>
public sealed class EmailLocalPart : SimpleValueObject<string>
{
    private EmailLocalPart(string value) 
        : base(value) 
    {
    }

    /// <summary>
    /// 이메일 로컬 부분 값 객체 생성
    /// </summary>
    /// <param name="value">로컬 부분</param>
    /// <returns>성공 시 EmailLocalPart 값 객체, 실패 시 에러</returns>
    public static Fin<EmailLocalPart> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new EmailLocalPart(validValue));

    /// <summary>
    /// 이미 검증된 로컬 부분으로 값 객체 생성
    /// </summary>
    /// <param name="validatedValue">검증된 로컬 부분</param>
    /// <returns>EmailLocalPart 값 객체</returns>
    internal static EmailLocalPart CreateFromValidated(string validatedValue) =>
        new EmailLocalPart(validatedValue);

    /// <summary>
    /// 이메일 로컬 부분 유효성 검증
    /// </summary>
    /// <param name="value">검증할 로컬 부분</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 1 && value.Length <= 64
            ? value
            : DomainErrors.EmptyOrOutOfRange(value);

    public override string ToString() => 
        Value;

    internal static class DomainErrors
    {
        public static Error EmptyOrOutOfRange(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(EmailLocalPart)}.{nameof(EmptyOrOutOfRange)}",
                errorCurrentValue: value,
                errorMessage: $"Email local part is empty or out of range. Must be 1-64 characters. Current value: '{value}'");
    }
}
