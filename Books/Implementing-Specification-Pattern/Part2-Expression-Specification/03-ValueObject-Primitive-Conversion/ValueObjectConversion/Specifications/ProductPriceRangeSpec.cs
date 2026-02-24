using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ValueObjectConversion.Specifications;

public sealed class ProductPriceRangeSpec : ExpressionSpecification<Product>
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    public ProductPriceRangeSpec(Money min, Money max) { MinPrice = min; MaxPrice = max; }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        decimal min = MinPrice;
        decimal max = MaxPrice;
        return product => (decimal)product.Price >= min && (decimal)product.Price <= max;
    }
}
