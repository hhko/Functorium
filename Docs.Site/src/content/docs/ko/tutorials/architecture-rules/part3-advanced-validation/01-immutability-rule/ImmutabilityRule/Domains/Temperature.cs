namespace ImmutabilityRule.Domains;

/// <summary>
/// 올바른 불변 클래스: private 생성자, getter-only 속성, 팩토리 메서드
/// </summary>
public sealed class Temperature
{
    public double Value { get; }
    public string Unit { get; }

    private Temperature(double value, string unit)
    {
        Value = value;
        Unit = unit;
    }

    public static Temperature Create(double value, string unit)
        => new(value, unit);

    public Temperature ToCelsius()
        => Unit == "F" ? Create((Value - 32) * 5 / 9, "C") : this;

    public override string ToString() => $"{Value}°{Unit}";
}
