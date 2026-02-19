namespace Functorium.Domains.Specifications;

/// <summary>
/// Specification 패턴의 추상 기반 클래스.
/// 도메인 조건을 캡슐화하고 And/Or/Not 조합을 지원합니다.
/// </summary>
/// <typeparam name="T">검증 대상 엔터티 타입</typeparam>
public abstract class Specification<T>
{
    /// <summary>
    /// 모든 엔터티를 만족하는 Specification (Null Object).
    /// And 연산의 항등원: All &amp; X = X, X &amp; All = X.
    /// </summary>
    public static Specification<T> All => AllSpecification<T>.Instance;

    /// <summary>
    /// 이 Specification이 All(항등원)인지 여부를 반환합니다.
    /// </summary>
    public virtual bool IsAll => false;

    /// <summary>
    /// 엔터티가 조건을 만족하는지 확인합니다.
    /// </summary>
    public abstract bool IsSatisfiedBy(T entity);

    /// <summary>
    /// 두 Specification을 AND 조합합니다.
    /// </summary>
    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);

    /// <summary>
    /// 두 Specification을 OR 조합합니다.
    /// </summary>
    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);

    /// <summary>
    /// Specification을 NOT으로 부정합니다.
    /// </summary>
    public Specification<T> Not() => new NotSpecification<T>(this);

    /// <summary>
    /// AND 연산자 오버로드. left.And(right)와 동일합니다.
    /// </summary>
    public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        => left.IsAll ? right : right.IsAll ? left : new AndSpecification<T>(left, right);

    /// <summary>
    /// OR 연산자 오버로드. left.Or(right)와 동일합니다.
    /// </summary>
    public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        => new OrSpecification<T>(left, right);

    /// <summary>
    /// NOT 연산자 오버로드. spec.Not()와 동일합니다.
    /// </summary>
    public static Specification<T> operator !(Specification<T> spec)
        => new NotSpecification<T>(spec);
}
