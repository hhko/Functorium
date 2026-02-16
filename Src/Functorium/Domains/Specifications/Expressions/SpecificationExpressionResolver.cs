using System.Linq.Expressions;

namespace Functorium.Domains.Specifications.Expressions;

/// <summary>
/// Specification에서 Expression Tree를 추출하고 합성하는 유틸리티.
/// And/Or/Not 조합 Specification도 재귀적으로 Expression을 합성합니다.
/// </summary>
public static class SpecificationExpressionResolver
{
    /// <summary>
    /// Specification에서 Expression을 추출합니다.
    /// IExpressionSpec 구현 시 직접 추출, And/Or/Not 조합 시 재귀 합성.
    /// 지원하지 않는 Specification은 null을 반환합니다.
    /// </summary>
    public static Expression<Func<T, bool>>? TryResolve<T>(Specification<T> spec)
    {
        return spec switch
        {
            IExpressionSpec<T> e => e.ToExpression(),
            AndSpecification<T> a => CombineAnd(a),
            OrSpecification<T> o => CombineOr(o),
            NotSpecification<T> n => CombineNot(n),
            _ => null
        };
    }

    private static Expression<Func<T, bool>>? CombineAnd<T>(AndSpecification<T> spec)
    {
        var left = TryResolve(spec.Left);
        var right = TryResolve(spec.Right);
        if (left is null || right is null) return null;
        return Combine(left, right, Expression.AndAlso);
    }

    private static Expression<Func<T, bool>>? CombineOr<T>(OrSpecification<T> spec)
    {
        var left = TryResolve(spec.Left);
        var right = TryResolve(spec.Right);
        if (left is null || right is null) return null;
        return Combine(left, right, Expression.OrElse);
    }

    private static Expression<Func<T, bool>>? CombineNot<T>(NotSpecification<T> spec)
    {
        var inner = TryResolve(spec.Inner);
        if (inner is null) return null;

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = Expression.Not(
            new ParameterReplacer(inner.Parameters[0], parameter).Visit(inner.Body));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> combiner)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(combiner(leftBody, rightBody), parameter);
    }

    /// <summary>
    /// Expression Tree 내의 파라미터를 교체하는 ExpressionVisitor.
    /// </summary>
    private sealed class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == oldParam ? newParam : base.VisitParameter(node);
    }
}
