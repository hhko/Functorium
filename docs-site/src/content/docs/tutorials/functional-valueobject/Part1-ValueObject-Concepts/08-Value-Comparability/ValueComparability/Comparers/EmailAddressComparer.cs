namespace ValueComparability.Comparers;

/// <summary>
/// EmailAddress 값 객체를 위한 IEqualityComparer<T> 구현
/// 정렬이 필요없는 값 객체에서 컬렉션 작업을 위한 대안
/// </summary>
public class EmailAddressComparer : IEqualityComparer<ValueObjects.EmailAddress>
{
    /// <summary>
    /// 두 EmailAddress의 동등성을 비교
    /// </summary>
    /// <param name="x">첫 번째 EmailAddress</param>
    /// <param name="y">두 번째 EmailAddress</param>
    /// <returns>동등하면 true, 아니면 false</returns>
    public bool Equals(ValueObjects.EmailAddress? x, ValueObjects.EmailAddress? y)
    {
        // 둘 다 null이면 같음
        if (x is null && y is null)
            return true;

        // 하나만 null이면 다름
        if (x is null || y is null)
            return false;

        // 둘 다 null이 아니면 값 비교
        return x.Equals(y);
    }

    /// <summary>
    /// EmailAddress의 해시 코드를 생성
    /// </summary>
    /// <param name="obj">해시 코드를 생성할 EmailAddress</param>
    /// <returns>해시 코드</returns>
    public int GetHashCode(ValueObjects.EmailAddress obj)
    {
        return obj?.GetHashCode() ?? 0;
    }
}
