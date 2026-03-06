namespace DomainLayerRules.Domains;

public static class OrderPricingService
{
    public static Money CalculateTotal(IReadOnlyList<Money> prices)
        => Money.Create(prices.Sum(p => p.Amount), prices.First().Currency);
}
