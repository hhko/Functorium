using System.Linq.Expressions;

namespace Functorium.Domains.Specifications;

/// <summary>
/// Specification이 Expression Tree를 제공할 수 있음을 나타내는 인터페이스.
/// EF Core 등의 LINQ 프로바이더에서 자동 SQL 번역에 사용됩니다.
/// </summary>
/// <typeparam name="T">검증 대상 엔터티 타입</typeparam>
public interface IExpressionSpec<T>
{
    /// <summary>
    /// 조건을 Expression Tree로 반환합니다.
    /// </summary>
    Expression<Func<T, bool>> ToExpression();
}
