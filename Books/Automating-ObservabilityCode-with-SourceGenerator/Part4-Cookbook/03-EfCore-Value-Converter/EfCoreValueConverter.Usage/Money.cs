using EfCoreValueConverter.Generated;

namespace EfCoreValueConverter.Usage;

[GenerateConverter]
public partial class Money
{
    public string Value { get; }
    private Money(string value) => Value = value;
    public static Money From(string value) => new(value);
}
