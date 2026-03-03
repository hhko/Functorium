using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ValueObjectConversion.Specifications;

public sealed class ProductNameSpec(ProductName name) : ExpressionSpecification<Product>
{
    public ProductName Name { get; } = name;

    public override Expression<Func<Product, bool>> ToExpression()
    {
        // Value Object를 로컬 변수로 추출하여 primitive로 변환
        string nameStr = Name;
        return product => (string)product.Name == nameStr;
    }
}
