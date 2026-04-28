using MethodValidation.Domains;

namespace MethodValidation.Extensions;

public static class MoneyExtensions
{
    public static string FormatKrw(this Money money)
        => $"\u20a9{money.Amount:N0}";

    public static Money ApplyDiscount(this Money money, Discount discount)
        => Money.Create(money.Amount * (1 - discount.Percentage / 100), money.Currency);
}
