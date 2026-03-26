namespace AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

/// <summary>
/// 드리프트 임계값 값 객체 (0.0~1.0 범위)
/// </summary>
public sealed class DriftThreshold : ComparableSimpleValueObject<decimal>
{
    public const decimal MinValue = 0.0m;
    public const decimal MaxValue = 1.0m;

    private DriftThreshold(decimal value) : base(value) { }

    public static Fin<DriftThreshold> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new DriftThreshold(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<DriftThreshold>
            .Between(value, MinValue, MaxValue);

    public static DriftThreshold CreateFromValidated(decimal value) => new(value);

    public static implicit operator decimal(DriftThreshold threshold) => threshold.Value;
}
