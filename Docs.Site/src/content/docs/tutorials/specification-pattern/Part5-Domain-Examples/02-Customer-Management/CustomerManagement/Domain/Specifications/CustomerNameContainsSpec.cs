using System.Linq.Expressions;
using CustomerManagement.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace CustomerManagement.Domain.Specifications;

public sealed class CustomerNameContainsSpec(CustomerName searchName) : ExpressionSpecification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string searchLower = ((string)searchName).ToLower();
        return customer => ((string)customer.Name).ToLower().Contains(searchLower);
    }
}
