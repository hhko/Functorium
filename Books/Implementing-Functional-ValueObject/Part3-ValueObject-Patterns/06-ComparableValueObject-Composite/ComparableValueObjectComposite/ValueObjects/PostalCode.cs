using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;

namespace ComparableValueObjectComposite.ValueObjects;

/// <summary>
/// 우편번호를 나타내는 비교 가능한 값 객체
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class PostalCode : ComparableSimpleValueObject<string>
{
    private PostalCode(string value) : base(value) { }

    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(Validate(value), v => new PostalCode(v));

    internal static PostalCode CreateFromValidated(string validatedValue) =>
        new(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length == 5 && value.All(char.IsDigit)
            ? value
            : DomainError.For<PostalCode>(new DomainErrorType.WrongLength(), value,
                $"Postal code must be exactly 5 digits. Current value: '{value}'");

    public override string ToString() => Value;
}
