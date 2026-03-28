using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;

namespace ValueObjectComposite.ValueObjects;

/// <summary>
/// 이메일 로컬 부분을 나타내는 값 객체
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class EmailLocalPart : SimpleValueObject<string>
{
    private EmailLocalPart(string value) : base(value) { }

    public static Fin<EmailLocalPart> Create(string value) =>
        CreateFromValidation(Validate(value), v => new EmailLocalPart(v));

    public static EmailLocalPart CreateFromValidated(string validatedValue) =>
        new(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 1 && value.Length <= 64
            ? value
            : DomainError.For<EmailLocalPart>(new DomainErrorType.WrongLength(), value,
                $"Email local part is empty or out of range. Must be 1-64 characters. Current value: '{value}'");

    public override string ToString() => Value;
}
