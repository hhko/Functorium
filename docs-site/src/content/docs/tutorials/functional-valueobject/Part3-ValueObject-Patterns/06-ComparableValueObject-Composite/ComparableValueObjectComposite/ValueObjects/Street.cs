using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;

namespace ComparableValueObjectComposite.ValueObjects;

/// <summary>
/// 도로명을 나타내는 비교 가능한 값 객체
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class Street : ComparableSimpleValueObject<string>
{
    private Street(string value) : base(value) { }

    public static Fin<Street> Create(string value) =>
        CreateFromValidation(Validate(value), v => new Street(v));

    public static Street CreateFromValidated(string validatedValue) =>
        new(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 2 && value.Length <= 50
            ? value.Trim()
            : DomainError.For<Street>(new DomainErrorType.WrongLength(), value,
                $"Street name length is invalid. Must be 2-50 characters. Current value: '{value}'");

    public override string ToString() => Value;
}
