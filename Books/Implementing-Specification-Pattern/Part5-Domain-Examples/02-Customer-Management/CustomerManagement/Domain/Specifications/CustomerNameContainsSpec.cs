using System.Linq.Expressions;
using CustomerManagement.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace CustomerManagement.Domain.Specifications;

public sealed class CustomerNameContainsSpec : ExpressionSpecification<Customer>
{
    public CustomerName SearchName { get; }

    public CustomerNameContainsSpec(CustomerName searchName) => SearchName = searchName;

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string searchLower = ((string)SearchName).ToLower();
        return customer => ((string)customer.Name).ToLower().Contains(searchLower);
    }
}
