namespace DomainLayerRules.Domains;

public sealed class ActiveOrderSpecification : Specification<Order>
{
    public override bool IsSatisfiedBy(Order entity)
        => entity.Items.Count > 0;
}
