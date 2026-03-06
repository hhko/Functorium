using LanguageExt;
using LanguageExt.Common;

namespace FunctionalResult;

public static class MathOperations
{
    /// <summary>
    /// 함수형 결과 타입을 사용한 나눗셈 함수
    /// 성공 시 Fin<int>.Succ(결과), 실패 시 Fin<int>.Fail(오류)를 반환합니다.
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모</param>
    /// <returns>성공/실패를 명시적으로 표현하는 Fin<int> 타입</returns>
    public static Fin<int> Divide(int numerator, int denominator)
    {
        if (denominator == 0)
            return Error.New("0은 허용되지 않습니다");

        return numerator / denominator;
    }
}
