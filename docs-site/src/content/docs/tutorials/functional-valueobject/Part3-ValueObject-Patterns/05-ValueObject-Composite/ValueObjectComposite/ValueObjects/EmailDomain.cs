using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;

namespace ValueObjectComposite.ValueObjects;

/// <summary>
/// 이메일 도메인을 나타내는 값 객체
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class EmailDomain : SimpleValueObject<string>
{
    private EmailDomain(string value) : base(value) { }

    public static Fin<EmailDomain> Create(string value) =>
        CreateFromValidation(Validate(value), v => new EmailDomain(v));

    public static EmailDomain CreateFromValidated(string validatedValue) =>
        new(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 3 && value.Contains('.')
            ? value.ToLowerInvariant()
            : DomainError.For<EmailDomain>(new DomainErrorType.InvalidFormat(), value,
                $"Email domain is empty or invalid. Must be at least 3 characters and contain '.'. Current value: '{value}'");

    public override string ToString() => Value;
}
