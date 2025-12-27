namespace ValueComparability.Comparers;

/// <summary>
/// 대소문자를 무시하는 EmailAddress 비교자
/// </summary>
public class EmailAddressCaseInsensitiveComparer : IEqualityComparer<ValueObjects.EmailAddress>
{
    /// <summary>
    /// 대소문자를 무시하고 두 EmailAddress의 동등성을 비교
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

        // 형변환 필요 이유
        //
        // 1. 제네릭 인터페이스 제약: IEqualityComparer<T>에서 T는 컴파일 타임에 결정되지 않음
        // 2. 타입 안전성: 컴파일러가 런타임에 결정되는 타입에 대해 암시적 변환을 자동 적용하지 않음
        // 3. 명시적 변환 필요: 개발자가 의도를 명확히 표현해야 함

        // 방법 1: 캐스팅 연산자 사용 (명시적 캐스팅)
        string xValue = (string)x;
        string yValue = (string)y;

        // 방법 2: ToString() 사용
        // string xValue = x.ToString();
        // string yValue = y.ToString();

        return xValue.Equals(yValue, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 대소문자를 무시한 해시 코드를 생성
    /// </summary>
    /// <param name="obj">해시 코드를 생성할 EmailAddress</param>
    /// <returns>해시 코드</returns>
    public int GetHashCode(ValueObjects.EmailAddress obj)
    {
        if (obj is null)
            return 0;

        // 캐스팅 연산자를 사용
        string value = (string)obj;

        return value.ToLowerInvariant().GetHashCode();
    }
}
