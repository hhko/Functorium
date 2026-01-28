namespace Functorium.Domains.Entities;

/// <summary>
/// Entity의 기본 추상 클래스.
/// ID 기반 동등성 비교를 제공합니다.
/// </summary>
/// <typeparam name="TId">EntityId 구현 타입</typeparam>
[Serializable]
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
    where TId : struct, IEntityId<TId>
{
    /// <summary>
    /// Entity의 고유 식별자.
    /// </summary>
    public TId Id { get; protected init; }

    /// <summary>
    /// 기본 생성자 (ORM/직렬화용).
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>
    /// ID를 지정하여 Entity를 생성합니다.
    /// </summary>
    /// <param name="id">Entity 식별자</param>
    protected Entity(TId id) => Id = id;

    /// <summary>
    /// ID 기반 동등성 비교.
    /// </summary>
    /// <param name="obj">비교할 객체</param>
    /// <returns>동등하면 true, 그렇지 않으면 false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (GetUnproxiedType(this) != GetUnproxiedType(obj))
            return false;

        if (obj is not Entity<TId> entity)
            return false;

        return Id.Equals(entity.Id);
    }

    /// <summary>
    /// Entity 간의 타입 안전한 동등성 비교.
    /// </summary>
    /// <param name="other">비교할 다른 Entity</param>
    /// <returns>동등하면 true, 그렇지 않으면 false</returns>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetUnproxiedType(this) != GetUnproxiedType(other))
            return false;

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// ID 기반 해시코드 반환.
    /// </summary>
    /// <returns>해시코드</returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// 동등성 연산자.
    /// </summary>
    /// <param name="a">첫 번째 Entity</param>
    /// <param name="b">두 번째 Entity</param>
    /// <returns>동등하면 true, 그렇지 않으면 false</returns>
    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    /// <summary>
    /// 부등성 연산자.
    /// </summary>
    /// <param name="a">첫 번째 Entity</param>
    /// <param name="b">두 번째 Entity</param>
    /// <returns>동등하지 않으면 true, 동등하면 false</returns>
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);

    /// <summary>
    /// LanguageExt Validation을 사용한 팩토리 메서드 템플릿.
    /// </summary>
    /// <typeparam name="TEntity">생성할 Entity 타입</typeparam>
    /// <typeparam name="TValue">검증할 값의 타입</typeparam>
    /// <param name="validation">LanguageExt Validation</param>
    /// <param name="factory">검증된 값으로 Entity를 생성하는 팩토리 함수</param>
    /// <returns>Fin&lt;TEntity&gt; - 성공 시 Entity, 실패 시 Error</returns>
    public static Fin<TEntity> CreateFromValidation<TEntity, TValue>(
        Validation<Error, TValue> validation,
        Func<TValue, TEntity> factory)
        where TEntity : Entity<TId>
    {
        return validation
            .Map(factory)
            .ToFin();
    }

    /// <summary>
    /// 프록시 타입을 제거하고 실제 타입을 반환.
    /// ORM 프레임워크의 프록시 객체를 처리하기 위함.
    /// </summary>
    /// <param name="obj">타입을 확인할 객체</param>
    /// <returns>실제 타입</returns>
    protected static Type GetUnproxiedType(object obj)
    {
        Type type = obj.GetType();

        if (IsProxyType(type))
            return type.BaseType ?? type;

        return type;
    }

    /// <summary>
    /// 타입이 ORM 프록시 타입인지 확인.
    /// </summary>
    /// <param name="type">확인할 타입</param>
    /// <returns>프록시 타입이면 true</returns>
    private static bool IsProxyType(Type type)
    {
        if (type.Assembly.IsDynamic)
            return true;

        string? ns = type.Namespace;
        if (ns is null)
            return false;

        return ns.StartsWith("Castle.Proxies", StringComparison.Ordinal)
            || ns.StartsWith("NHibernate.Proxy", StringComparison.Ordinal)
            || ns.StartsWith("Microsoft.EntityFrameworkCore.Proxies", StringComparison.Ordinal);
    }
}
