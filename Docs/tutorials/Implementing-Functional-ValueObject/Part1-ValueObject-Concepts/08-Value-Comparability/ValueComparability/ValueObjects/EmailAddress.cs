using LanguageExt;
using LanguageExt.Common;

namespace ValueComparability.ValueObjects;

/// <summary>
/// 이메일 주소를 나타내는 값 객체
/// 정렬이 의미없으므로 IComparable<T>를 구현하지 않음
/// </summary>
public sealed class EmailAddress
    : IEquatable<EmailAddress>
{
    private readonly string _value;

    // Private constructor - 직접 인스턴스 생성 방지
    private EmailAddress(string value) =>
        _value = value;

    /// <summary>
    /// EmailAddress 인스턴스를 생성하는 팩토리 메서드
    /// </summary>
    /// <param name="value">유효한 이메일 주소</param>
    /// <returns>성공 시 EmailAddress, 실패 시 Error</returns>
    public static Fin<EmailAddress> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.New("이메일 주소는 비어있을 수 없습니다");

        if (!value.Contains('@'))
            return Error.New("이메일 주소는 @를 포함해야 합니다");

        if (!value.Contains('.'))
            return Error.New("이메일 주소는 도메인을 포함해야 합니다");

        return new EmailAddress(value.Trim().ToLowerInvariant());
    }

    ///// <summary>
    ///// 내부 값을 안전하게 접근하는 프로퍼티
    ///// </summary>
    //public string Value => 
    //    _value;

    // 값 기반 동등성 구현: IEquatable<T>

    /// <summary>
    /// IEquatable<T> 구현 - 타입 안전한 동등성 비교
    /// </summary>
    public bool Equals(EmailAddress? other) =>
        other is not null && _value == other._value;

    /// <summary>
    /// Object.Equals 오버라이드 - 참조 동등성이 아닌 값 동등성 사용
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is EmailAddress other && Equals(other);

    /// <summary>
    /// 동등성 연산자 오버로딩
    /// </summary>
    public static bool operator ==(EmailAddress? left, EmailAddress? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// 부등성 연산자 오버로딩
    /// </summary>
    public static bool operator !=(EmailAddress? left, EmailAddress? right) =>
        !(left == right);

    /// <summary>
    /// GetHashCode 오버라이드 - 값 기반 해시 코드 생성
    /// Equals와 GetHashCode는 항상 함께 오버라이드해야 함
    /// </summary>
    public override int GetHashCode() =>
        _value.GetHashCode();

    /// <summary>
    /// 문자열 표현
    /// </summary>
    public override string ToString() =>
        _value;

    // 변환 연산자

    public static explicit operator EmailAddress(string value) =>
       Create(value).Match(
           Succ: x => x,
           Fail: _ => throw new InvalidCastException($"{value}은 EmailAddress로 변환할 수 없습니다")
       );

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    public static explicit operator string(EmailAddress value) =>
        value._value;
}
