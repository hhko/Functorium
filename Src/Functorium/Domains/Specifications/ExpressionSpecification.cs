using System.Linq.Expressions;

namespace Functorium.Domains.Specifications;

/// <summary>
/// Expression Tree 기반 Specification 추상 클래스.
/// ToExpression()을 구현하면 IsSatisfiedBy()가 자동으로 제공됩니다.
/// EF Core 어댑터에서 PropertyMap과 결합하여 자동 SQL 번역을 지원합니다.
/// </summary>
/// <typeparam name="T">검증 대상 엔터티 타입</typeparam>
public abstract class ExpressionSpecification<T> : Specification<T>, IExpressionSpec<T>
{
    private Func<T, bool>? _compiled;

    /// <summary>
    /// 조건을 Expression Tree로 반환합니다.
    /// </summary>
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Expression을 컴파일하여 엔터티가 조건을 만족하는지 확인합니다.
    /// 컴파일된 delegate는 캐싱됩니다.
    /// </summary>
    public sealed override bool IsSatisfiedBy(T entity)
    {
        _compiled ??= ToExpression().Compile();
        return _compiled(entity);
    }
}
