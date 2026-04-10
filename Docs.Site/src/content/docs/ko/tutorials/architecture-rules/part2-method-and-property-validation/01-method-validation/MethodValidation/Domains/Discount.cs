namespace MethodValidation.Domains;

public sealed class Discount
{
    public decimal Percentage { get; }

    private Discount(decimal percentage) => Percentage = percentage;

    public static Discount Create(decimal percentage)
        => new(percentage);
}
