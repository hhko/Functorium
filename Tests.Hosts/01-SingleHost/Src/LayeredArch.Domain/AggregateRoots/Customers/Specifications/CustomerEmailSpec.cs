using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

namespace LayeredArch.Domain.AggregateRoots.Customers.Specifications;

/// <summary>
/// 이메일 중복 확인 Specification.
/// Expression 기반으로 EF Core 자동 SQL 번역을 지원합니다.
/// </summary>
public sealed class CustomerEmailSpec : ExpressionSpecification<Customer>
{
    public Email Email { get; }

    public CustomerEmailSpec(Email email)
    {
        Email = email;
    }

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string emailStr = Email;
        return customer => (string)customer.Email == emailStr;
    }
}
