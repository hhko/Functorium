using System.Linq.Expressions;
using CustomerManagement.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace CustomerManagement.Domain.Specifications;

public sealed class CustomerEmailSpec(Email email) : ExpressionSpecification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string emailStr = email;
        return customer => (string)customer.Email == emailStr;
    }
}
