using LanguageExt;
using LanguageExt.Common;

namespace AlwaysValid.ValueObjects;

/// <summary>
/// 0이 아닌 정수를 표현하는 분모 값 객체
/// 생성 시점에 유효성 검사를 수행하여 항상 유효한 값만 보장합니다.
/// </summary>
public sealed class Denominator
{
    private readonly int _value;

    // Private constructor - 직접 인스턴스 생성 방지
    private Denominator(int value) =>
        _value = value;

    /// <summary>
    /// Denominator를 생성합니다. 0인 경우 실패를 반환합니다.
    /// </summary>
    /// <param name="value">0이 아닌 정수 값</param>
    /// <returns>성공 시 Denominator, 실패 시 Error</returns>
    public static Fin<Denominator> Create(int value)
    {
        if (value == 0)
            return Error.New("0은 허용되지 않습니다");

        return new Denominator(value);
    }

    /// <summary>
    /// 내부 값을 안전하게 반환합니다.
    /// </summary>
    public int Value =>
        _value;
}
