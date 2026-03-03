namespace DefensiveProgramming;

/// <summary>
/// 방어적 프로그래밍의 두 가지 구현 방법을 보여주는 수학 연산 클래스
/// 
/// 이 클래스는 방어적 프로그래밍의 발전 과정을 보여줍니다:
/// 1. 사전 검증을 통한 정의된(의도된) 예외 처리 (Divide 메서드)
/// 2. 예외 없이 bool 반환을 활용한 TryDivide 패턴 (TryDivide 메서드)
/// 
/// 두 방법 모두 여전히 부작용을 가지고 있습니다:
/// - Divide: 프로그램 흐름 중단 부작용 (예외 발생)
/// - TryDivide: 외부 상태 변경 부작용 (out 매개변수)
/// 
/// 다음 단계인 Functional Result에서는 이러한 모든 부작용을 해결합니다.
/// </summary>
public static class MathOperations
{
    /// <summary>
    /// 첫 번째 방법: 사전 검증을 통한 정의된(의도된) 예외 처리
    /// 
    /// 이 방법은 방어적 프로그래밍의 기본 원칙을 구현합니다.
    /// 입력값을 사전에 검증하고, 유효하지 않은 경우 명확하고 의도된 예외를 발생시킵니다.
    /// 
    /// 장점:
    /// - 명확한 오류 메시지와 예외 타입
    /// - 디버깅 시 상세한 스택 트레이스 정보
    /// - 호출자가 예외를 처리할 수 있음
    /// 
    /// 한계점:
    /// - 여전히 예외를 발생시켜 프로그램 흐름을 중단시킴 (프로그램 흐름 중단 부작용)
    /// - 성능 오버헤드 존재 (예외 처리 비용)
    /// - try-catch 블록으로 인한 코드 복잡성 증가
    /// - 함수형 프로그래밍 관점에서 부작용(side effect) 발생
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모</param>
    /// <returns>나눗셈 결과</returns>
    /// <exception cref="ArgumentException">denominator가 0일 때 발생 (프로그램 흐름 중단 부작용)</exception>
    public static int Divide(int numerator, int denominator)
    {
        // 방어적 프로그래밍: 사전 검증을 통한 정의된 예외
        if (denominator == 0)
        {
            // 명확하고 의도된 예외 발생 (프로그램 흐름 중단 부작용)
            throw new ArgumentException("0으로 나눌 수 없습니다", nameof(denominator));
        }

        return numerator / denominator;
    }

    /// <summary>
    /// 두 번째 방법: 예외 없이 bool 반환을 활용한 TryDivide 패턴
    /// 
    /// 이 방법은 예외 없이도 안전한 실패 처리를 할 수 있는 방어적 프로그래밍의 진화된 형태입니다.
    /// bool TryParse(string? s, out int result) 패턴과 동일한 방식으로, 
    /// .NET Framework에서 널리 사용되는 표준 패턴입니다.
    /// 
    /// 핵심 아이디어: "예외 발생이라는 부작용은 제거했지만, 
    /// 여전히 out 매개변수를 통해 함수 외부 상태를 변경하는 부작용은 존재"
    /// 
    /// 장점:
    /// - 예외 발생 없음 (예외 부작용 제거)
    /// - 성능 향상 (예외 처리 오버헤드 제거)
    /// - 명시적 성공/실패 처리
    /// - 안전한 처리 (실패 시에도 프로그램이 중단되지 않음)
    /// - 실무 표준 (.NET Framework 표준 패턴)
    /// 
    /// 한계점:
    /// - 타입 안전성 부족: 런타임에 성공/실패를 결정하여 컴파일 타임 검증 불가
    /// - 부작용 존재: out 매개변수로 함수 외부 상태를 변경하여 함수형 프로그래밍의 순수성 위반
    /// - 명시적 오류 처리 부족: 성공/실패 여부만 알 수 있고 구체적인 오류 정보 부족
    /// - 합성성 제한: 여러 Try 패턴을 조합할 때 중첩된 if 문으로 인한 코드 복잡성 증가
    /// - 타입 시스템 활용 부족: 기본 타입에 의존하여 도메인 특화 타입의 장점 활용 불가
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모</param>
    /// <param name="result">나눗셈 결과 (성공 시에만 유효한 값, 실패 시에는 기본값)</param>
    /// <returns>나눗셈 성공 여부 (true: 성공, false: 실패)</returns>
    public static bool TryDivide(int numerator, int denominator, out int result)
    {
        // 방어적 프로그래밍: 사전 검증을 통한 예외 없는 안전한 실패 처리
        if (denominator == 0)
        {
            // 예외 발생 없음! (예외 부작용 제거)
            // 하지만 여전히 out 매개변수를 통해 외부 상태를 변경하는 부작용 존재
            result = default; // 기본값 설정 (외부 상태 변경 부작용)
            return false;     // 실패를 명시적으로 반환
        }

        result = numerator / denominator;
        return true;          // 성공을 명시적으로 반환
    }
}
