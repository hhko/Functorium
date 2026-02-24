using System.Linq.Expressions;
using CustomerManagement.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace CustomerManagement.Domain.Specifications;

public sealed class CustomerEmailSpec : ExpressionSpecification<Customer>
{
    public Email Email { get; }

    public CustomerEmailSpec(Email email) => Email = email;

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string emailStr = Email;
        return customer => (string)customer.Email == emailStr;
    }
}
