using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCodeFluent.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// 거리명을 나타내는 값 객체
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Street : SimpleValueObject<string>
{
    private Street(string value) : base(value) { }

    public static Fin<Street> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new Street(validValue));

    internal static Street CreateFromValidated(string validatedValue) =>
        new Street(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainError.For<Street>(new DomainErrorType.Empty(), value ?? "",
                $"Street name cannot be empty. Current value: '{value}'")
            : value;
}
