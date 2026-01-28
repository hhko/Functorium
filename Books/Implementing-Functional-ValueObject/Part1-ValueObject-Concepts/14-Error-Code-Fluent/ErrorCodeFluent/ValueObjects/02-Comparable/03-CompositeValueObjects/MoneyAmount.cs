using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCodeFluent.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 금액을 나타내는 비교 가능한 값 객체
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class MoneyAmount : ComparableSimpleValueObject<decimal>
{
    private MoneyAmount(decimal value)
        : base(value)
    {
    }

    public static Fin<MoneyAmount> Create(decimal value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new MoneyAmount(validValue));

    public static MoneyAmount CreateFromValidated(decimal validatedValue) =>
        new MoneyAmount(validatedValue);

    public static Validation<Error, decimal> Validate(decimal value) =>
        value >= 0 && value <= 999999.99m
            ? value
            : DomainError.For<MoneyAmount, decimal>(new DomainErrorType.OutOfRange(), value,
                $"Money amount must be between 0 and 999999.99. Current value: '{value}'");

    public override string ToString() =>
        $"{Value:N2}";
}
