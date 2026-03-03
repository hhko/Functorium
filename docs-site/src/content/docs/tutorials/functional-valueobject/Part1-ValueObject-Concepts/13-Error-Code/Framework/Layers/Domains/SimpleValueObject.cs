using LanguageExt;
using LanguageExt.Common;

namespace Framework.Layers.Domains;

/// <summary>
/// LanguageExt 기반 단일 값 객체를 위한 기본 클래스
/// 단일 값을 래핑하는 값 객체의 공통 기능을 제공
/// </summary>
/// <typeparam name="T">래핑할 값의 타입</typeparam>
[Serializable]
public abstract class SimpleValueObject<T> : ValueObject
    where T : notnull
{
    /// <summary>
    /// 래핑된 값
    /// </summary>
    protected T Value { get; }

    /// <summary>
    /// SimpleValueObject 인스턴스를 생성하는 protected 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="value">래핑할 값</param>
    protected SimpleValueObject(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }


    /// <summary>
    /// 동등성 비교 구성 요소 반환
    /// </summary>
    /// <returns>Value만 포함하는 열거</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// 문자열 표현 반환
    /// </summary>
    /// <returns>Value의 문자열 표현</returns>
    public override string ToString() =>
        Value.ToString() ?? string.Empty;

    /// <summary>
    /// SimpleValueObject에서 T로의 명시적 변환
    /// </summary>
    /// <param name="valueObject">변환할 값 객체</param>
    public static explicit operator T(SimpleValueObject<T>? valueObject)
    {
        if (valueObject is null)
            throw new InvalidOperationException("Cannot convert null SimpleValueObject to T");

        return valueObject.Value;
    }

    /// <summary>
    /// Object와의 동등성 비교 (부모 클래스의 구현 사용)
    /// </summary>
    /// <param name="obj">비교할 객체</param>
    /// <returns>동등하면 true, 그렇지 않으면 false</returns>
    public sealed override bool Equals(object? obj) =>
        base.Equals(obj);

    /// <summary>
    /// 해시코드 반환 (부모 클래스의 구현 사용)
    /// </summary>
    /// <returns>해시코드</returns>
    public sealed override int GetHashCode() =>
        base.GetHashCode();

    /// <summary>
    /// 동등성 연산자
    /// </summary>
    /// <param name="left">왼쪽 SimpleValueObject</param>
    /// <param name="right">오른쪽 SimpleValueObject</param>
    /// <returns>동등하면 true, 그렇지 않으면 false</returns>
    public static bool operator ==(SimpleValueObject<T>? left, SimpleValueObject<T>? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>
    /// 부등성 연산자
    /// </summary>
    /// <param name="left">왼쪽 SimpleValueObject</param>
    /// <param name="right">오른쪽 SimpleValueObject</param>
    /// <returns>동등하지 않으면 true, 동등하면 false</returns>
    public static bool operator !=(SimpleValueObject<T>? left, SimpleValueObject<T>? right) =>
        !(left == right);

    /// <summary>
    /// LanguageExt Validation을 사용한 팩토리 메서드 템플릿
    /// </summary>
    /// <typeparam name="TValueObject">생성할 값 객체 타입</typeparam>
    /// <param name="validation">LanguageExt Validation</param>
    /// <param name="factory">검증된 값으로 값 객체를 생성하는 팩토리 함수</param>
    /// <returns>Fin<TValueObject></returns>
    public static Fin<TValueObject> CreateFromValidation<TValueObject>(
        Validation<Error, T> validation,
        Func<T, TValueObject> factory)
        where TValueObject : SimpleValueObject<T>
    {
        return validation
            .Map(factory)
            .ToFin();
    }
}
