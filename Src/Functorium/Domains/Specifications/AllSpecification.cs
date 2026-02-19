using System.Linq.Expressions;

namespace Functorium.Domains.Specifications;

internal sealed class AllSpecification<T> : ExpressionSpecification<T>
{
    public static readonly AllSpecification<T> Instance = new();
    private AllSpecification() { }
    public override bool IsAll => true;
    public override Expression<Func<T, bool>> ToExpression() => _ => true;
}
