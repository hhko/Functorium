namespace Functorium.Domains.ValueObjects;

/// <summary>
/// LanguageExt 기반 비교 가능한 값 객체를 위한 기본 클래스
/// 복합 값 객체의 공통 기능과 Validation 조합 헬퍼를 제공하며 IComparable<T> 인터페이스를 지원
/// </summary>
[Serializable]
public abstract class ComparableValueObject : ValueObject, IComparable<ComparableValueObject>
{
    /// <summary>
    /// 비교 가능한 구성 요소들을 반환
    /// IComparable을 구현하는 값들만 반환해야 함
    /// </summary>
    /// <returns>비교 가능한 구성 요소들의 열거</returns>
    protected abstract IEnumerable<IComparable> GetComparableEqualityComponents();

    /// <summary>
    /// 동등성 비교에 사용할 구성 요소들을 반환
    /// GetComparableEqualityComponents()를 래핑하여 object 타입으로 반환
    /// </summary>
    /// <returns>동등성 비교 대상이 되는 객체들의 열거</returns>
    protected override IEnumerable<object> GetEqualityComponents() =>
        GetComparableEqualityComponents();

    /// <summary>
    /// ComparableValueObject 간의 비교
    /// </summary>
    /// <param name="other">비교할 다른 ComparableValueObject</param>
    /// <returns>비교 결과 (음수: this < other, 0: this == other, 양수: this > other)</returns>
    public virtual int CompareTo(ComparableValueObject? other)
    {
        if (other is null)
            return 1;

        if (ReferenceEquals(this, other))
            return 0;

        Type thisType = GetUnproxiedType(this);
        Type otherType = GetUnproxiedType(other);
        if (thisType != otherType)
            return string.Compare($"{thisType}", $"{otherType}", StringComparison.Ordinal);

        return GetComparableEqualityComponents()
            .Zip(other.GetComparableEqualityComponents(),
                (left, right) => left?.CompareTo(right) ?? (right is null ? 0 : -1))
            .FirstOrDefault(cmp => cmp != 0);
    }


    /// <summary>
    /// 비교 연산자 (작음)
    /// </summary>
    /// <param name="left">왼쪽 ComparableValueObject</param>
    /// <param name="right">오른쪽 ComparableValueObject</param>
    /// <returns>left가 right보다 작으면 true</returns>
    public static bool operator <(ComparableValueObject? left, ComparableValueObject? right) =>
        left?.CompareTo(right) < 0;

    /// <summary>
    /// 비교 연산자 (작거나 같음)
    /// </summary>
    /// <param name="left">왼쪽 ComparableValueObject</param>
    /// <param name="right">오른쪽 ComparableValueObject</param>
    /// <returns>left가 right보다 작거나 같으면 true</returns>
    public static bool operator <=(ComparableValueObject? left, ComparableValueObject? right) =>
        left?.CompareTo(right) <= 0;

    /// <summary>
    /// 비교 연산자 (큼)
    /// </summary>
    /// <param name="left">왼쪽 ComparableValueObject</param>
    /// <param name="right">오른쪽 ComparableValueObject</param>
    /// <returns>left가 right보다 크면 true</returns>
    public static bool operator >(ComparableValueObject? left, ComparableValueObject? right) =>
        left?.CompareTo(right) > 0;

    /// <summary>
    /// 비교 연산자 (크거나 같음)
    /// </summary>
    /// <param name="left">왼쪽 ComparableValueObject</param>
    /// <param name="right">오른쪽 ComparableValueObject</param>
    /// <returns>left가 right보다 크거나 같으면 true</returns>
    public static bool operator >=(ComparableValueObject? left, ComparableValueObject? right) =>
        left?.CompareTo(right) >= 0;
}
