using System.Linq.Expressions;
using EcommerceFiltering.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain.Specifications;

public sealed class ProductCategorySpec : ExpressionSpecification<Product>
{
    public Category Category { get; }

    public ProductCategorySpec(Category category) => Category = category;

    public override Expression<Func<Product, bool>> ToExpression()
    {
        string categoryStr = Category;
        return product => (string)product.Category == categoryStr;
    }
}
