using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 금액을 나타내는 비교 가능한 값 객체
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
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
        Validate<MoneyAmount>.NonNegative(value)
            .ThenAtMost(999999.99m);

    public override string ToString() =>
        $"{Value:N2}";
}
