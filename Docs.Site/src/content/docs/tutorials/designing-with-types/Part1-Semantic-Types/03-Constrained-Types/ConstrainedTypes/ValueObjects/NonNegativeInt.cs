using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;

namespace ConstrainedTypes.ValueObjects;

/// <summary>
/// 0 이상의 정수 값 객체
/// </summary>
public sealed class NonNegativeInt : SimpleValueObject<int>
{
    private NonNegativeInt(int value) : base(value) { }

    public static Fin<NonNegativeInt> Create(int value) =>
        CreateFromValidation(Validate(value), v => new NonNegativeInt(v));

    public static Validation<Error, int> Validate(int value) =>
        value >= 0
            ? value
            : DomainError.For<NonNegativeInt, int>(new DomainErrorType.Negative(), value,
                $"Value must be non-negative. Current value: {value}");
}
