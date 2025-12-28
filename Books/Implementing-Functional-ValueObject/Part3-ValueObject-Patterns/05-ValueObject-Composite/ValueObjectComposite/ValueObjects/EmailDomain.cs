using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using static LanguageExt.Prelude;

namespace ValueObjectComposite.ValueObjects;

/// <summary>
/// 이메일 도메인을 나타내는 값 객체
/// </summary>
public sealed class EmailDomain : SimpleValueObject<string>
{
    private EmailDomain(string value) 
        : base(value) 
    { 
    }

    /// <summary>
    /// 이메일 도메인 값 객체 생성
    /// </summary>
    /// <param name="value">도메인</param>
    /// <returns>성공 시 EmailDomain 값 객체, 실패 시 에러</returns>
    public static Fin<EmailDomain> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new EmailDomain(validValue));

    /// <summary>
    /// 이미 검증된 도메인으로 값 객체 생성
    /// </summary>
    /// <param name="validatedValue">검증된 도메인</param>
    /// <returns>EmailDomain 값 객체</returns>
    internal static EmailDomain CreateFromValidated(string validatedValue) =>
        new EmailDomain(validatedValue);

    /// <summary>
    /// 이메일 도메인 유효성 검증
    /// </summary>
    /// <param name="value">검증할 도메인</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 3 && value.Contains('.')
            ? value.ToLowerInvariant()
            : DomainErrors.EmptyOrInvalidFormat(value);

    public override string ToString() => 
        Value;

    internal static class DomainErrors
    {
        public static Error EmptyOrInvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(EmailDomain)}.{nameof(EmptyOrInvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Email domain is empty or invalid. Must be at least 3 characters and contain '.'. Current value: '{value}'");
    }
}
