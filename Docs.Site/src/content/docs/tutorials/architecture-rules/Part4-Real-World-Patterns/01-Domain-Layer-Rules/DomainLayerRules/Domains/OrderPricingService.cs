using LanguageExt;

namespace DomainLayerRules.Domains;

public sealed class OrderPricingService : IDomainService
{
    public Fin<Money> CalculateTotal(IReadOnlyList<Money> prices)
        => prices.Count == 0
            ? Fin<Money>.Fail("prices cannot be empty")
            : Money.Create(prices.Sum(p => p.Amount), prices.First().Currency);
}
