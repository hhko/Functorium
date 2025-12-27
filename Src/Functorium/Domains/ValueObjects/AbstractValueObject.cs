namespace Functorium.Domains.ValueObjects;

/// <summary>
/// 값 객체의 기본 추상 클래스
/// 값 기반 동등성 비교와 해시코드 생성을 제공
/// </summary>
[Serializable]
public abstract class AbstractValueObject
    : IValueObject
    , IEquatable<AbstractValueObject>
{
    private int? _cachedHashCode;

    /// <summary>
    /// 동등성 비교에 사용할 구성 요소들을 반환
    /// </summary>
    /// <returns>동등성 비교 대상이 되는 객체들의 열거</returns>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// 값 기반 동등성 비교
    /// </summary>
    /// <param name="obj">비교할 객체</param>
    /// <returns>동등하면 true, 그렇지 않으면 false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (GetUnproxiedType(this) != GetUnproxiedType(obj))
            return false;

        if (obj is not AbstractValueObject valueObject)
            return false;

        return GetEqualityComponents().SequenceEqual(
            valueObject.GetEqualityComponents(),
            ValueObjectEqualityComparer.Instance);
    }

    /// <summary>
    /// AbstractValueObject 간의 타입 안전한 동등성 비교
    /// IEquatable 인터페이스의 의도에 맞게 최적화된 구현
    /// </summary>
    /// <param name="other">비교할 다른 AbstractValueObject</param>
    /// <returns>동등하면 true, 그렇지 않으면 false</returns>
    public bool Equals(AbstractValueObject? other)
    {
        if (other is null)
            return false;

        if (GetUnproxiedType(this) != GetUnproxiedType(other))
            return false;

        return GetEqualityComponents().SequenceEqual(
            other.GetEqualityComponents(),
            ValueObjectEqualityComparer.Instance);
    }

    /// <summary>
    /// 캐시된 해시코드 반환
    /// </summary>
    /// <returns>해시코드</returns>
    public override int GetHashCode()
    {
        if (!_cachedHashCode.HasValue)
        {
            _cachedHashCode = GetEqualityComponents()
                .Aggregate(1, (current, obj) =>
                {
                    unchecked
                    {
                        return current * 23 + ValueObjectEqualityComparer.Instance.GetHashCode(obj!);
                    }
                });
        }

        return _cachedHashCode.Value;
    }

    /// <summary>
    /// 동등성 연산자
    /// </summary>
    /// <param name="a">첫 번째 값 객체</param>
    /// <param name="b">두 번째 값 객체</param>
    /// <returns>동등하면 true, 그렇지 않으면 false</returns>
    public static bool operator ==(AbstractValueObject? a, AbstractValueObject? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    /// <summary>
    /// 부등성 연산자
    /// </summary>
    /// <param name="a">첫 번째 값 객체</param>
    /// <param name="b">두 번째 값 객체</param>
    /// <returns>동등하지 않으면 true, 동등하면 false</returns>
    public static bool operator !=(AbstractValueObject? a, AbstractValueObject? b)
    {
        return !(a == b);
    }

    /// <summary>
    /// 프록시 타입을 제거하고 실제 타입을 반환
    /// ORM 프레임워크의 프록시 객체를 처리하기 위함
    /// </summary>
    /// <param name="obj">타입을 확인할 객체</param>
    /// <returns>실제 타입</returns>
    protected static Type GetUnproxiedType(object obj)
    {
        Type type = obj.GetType();

        // 프록시 타입은 런타임에 동적 생성되므로 Assembly가 null이거나
        // 특정 프록시 네임스페이스에 속함
        if (IsProxyType(type))
            return type.BaseType ?? type;

        return type;
    }

    /// <summary>
    /// 타입이 ORM 프록시 타입인지 확인
    /// </summary>
    /// <param name="type">확인할 타입</param>
    /// <returns>프록시 타입이면 true</returns>
    private static bool IsProxyType(Type type)
    {
        // 동적 생성 프록시는 Module이 동적임
        if (type.Assembly.IsDynamic)
            return true;

        // 네임스페이스 기반 검사 (Castle.Proxies, NHibernate.Proxy 등)
        string? ns = type.Namespace;
        if (ns is null)
            return false;

        return ns.StartsWith("Castle.Proxies", StringComparison.Ordinal)
            || ns.StartsWith("NHibernate.Proxy", StringComparison.Ordinal)
            || ns.StartsWith("Microsoft.EntityFrameworkCore.Proxies", StringComparison.Ordinal);
    }

    /// <summary>
    /// 값 객체 동등성 비교를 위한 커스텀 EqualityComparer
    /// 배열 타입에 대해 요소별 내용 비교를 수행
    /// </summary>
    /// <remarks>
    /// 성능 고려사항:
    /// - 배열 비교는 O(n) 시간 복잡도를 가짐
    /// - 대용량 배열(100KB 이상)에는 적합하지 않음
    ///   예: byte[] 102,400건, int[] 25,600건, Guid[] 6,400건
    /// - 해시코드는 캐시되므로 첫 계산 이후 O(1)
    /// - 작은 배열(해시값, 서명 등)에 최적화된 구현
    /// </remarks>
    private sealed class ValueObjectEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ValueObjectEqualityComparer Instance = new();

        private ValueObjectEqualityComparer() { }

        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            // 배열 타입에 대해 요소별 비교
            if (x is Array xArray && y is Array yArray)
            {
                if (xArray.Length != yArray.Length)
                    return false;

                for (int i = 0; i < xArray.Length; i++)
                {
                    if (!Equals(xArray.GetValue(i), yArray.GetValue(i)))
                        return false;
                }
                return true;
            }

            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            if (obj is Array array)
            {
                unchecked
                {
                    int hash = 17;
                    foreach (var item in array)
                    {
                        hash = hash * 23 + (item?.GetHashCode() ?? 0);
                    }
                    return hash;
                }
            }
            return obj?.GetHashCode() ?? 0;
        }
    }
}
