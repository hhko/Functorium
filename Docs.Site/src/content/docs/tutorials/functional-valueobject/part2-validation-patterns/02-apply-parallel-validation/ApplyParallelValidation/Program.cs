using ApplyParallelValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyParallelValidation;

class Program
{
    static void Main(string[] args)
    {
        // =================================================================
        // 방법 1: 튜플 기반 Apply (권장)
        // =================================================================
        Console.WriteLine("=== 방법 1: 튜플 기반 Apply (권장) ===");
        Console.WriteLine("사용자 등록 값 객체의 모든 검증 규칙을 병렬로 실행합니다.\n");

        // 성공 케이스
        DemonstrateUserRegistration("newuser@example.com", "newpass123", "홍길동", "25", "유효한 사용자 등록");

        // 단일 검증 실패 케이스들
        DemonstrateUserRegistration("", "newpass123", "홍길동", "25", "이메일 형식 오류 (단일 실패)");
        DemonstrateUserRegistration("user@example.com", "short", "홍길동", "25", "비밀번호 강도 부족 (단일 실패)");
        DemonstrateUserRegistration("user@example.com", "newpass123", "A", "25", "이름 형식 오류 (단일 실패)");
        DemonstrateUserRegistration("user@example.com", "newpass123", "홍길동", "abc", "나이 형식 오류 (단일 실패)");

        // 여러 검증 동시 실패 케이스들 (Apply의 핵심 학습 목표)
        DemonstrateUserRegistration("", "short", "홍길동", "25", "이메일 + 비밀번호 동시 실패");
        DemonstrateUserRegistration("invalid-email", "weak", "A", "25", "이메일 + 비밀번호 + 이름 동시 실패");
        DemonstrateUserRegistration("", "short", "A", "abc", "모든 검증 동시 실패 (Apply의 핵심)");

        // =================================================================
        // 방법 2: fun 기반 개별 Apply
        // =================================================================
        Console.WriteLine("\n\n=== 방법 2: fun 기반 개별 Apply ===");
        Console.WriteLine("Currying을 통해 단계적으로 Apply를 적용하는 방식입니다.\n");

        // 성공 케이스
        DemonstrateFunApply("newuser@example.com", "newpass123", "홍길동", "25", "유효한 사용자 등록 (fun Apply)");

        // 여러 검증 동시 실패 케이스
        DemonstrateFunApply("", "short", "A", "abc", "모든 검증 동시 실패 (fun Apply)");

        // =================================================================
        // 두 방법 비교
        // =================================================================
        Console.WriteLine("\n\n=== 두 방법 비교 ===");
        CompareTwoMethods("", "short", "A", "abc");
    }

    static void DemonstrateUserRegistration(string email, string password, string name, string ageInput, string description)
    {
        Console.WriteLine($"\n--- {description} ---");
        Console.WriteLine($"이메일: '{email}'");
        Console.WriteLine($"비밀번호: '{password}'");
        Console.WriteLine($"이름: '{name}'");
        Console.WriteLine($"나이: '{ageInput}'");

        var result = UserRegistration.Create(email, password, name, ageInput);

        result.Match(
            Succ: userRegistration => 
            {
                Console.WriteLine($"성공: 사용자 등록이 유효합니다.");
                Console.WriteLine($"   → 등록된 사용자: {userRegistration.Name} ({userRegistration.Email})");
                Console.WriteLine("   → 모든 독립 검증 규칙을 통과했습니다.");
            },
            Fail: error => 
            {
                Console.WriteLine($"실패:");
                
                // ManyErrors 타입인지 확인하여 복수개 에러 처리
                if (error is ManyErrors manyErrors)
                {
                    Console.WriteLine($"   → 총 {manyErrors.Errors.Count}개의 검증 실패:");
                    for (int i = 0; i < manyErrors.Errors.Count; i++)
                    {
                        var individualError = manyErrors.Errors[i];
                        var individualErrorType = individualError.GetType();
                        if (individualErrorType.Name.StartsWith("ErrorCodeExpected"))
                        {
                            var errorCodeProperty = individualErrorType.GetProperty("ErrorCode");
                            var errorCurrentValueProperty = individualErrorType.GetProperty("ErrorCurrentValue");
                            if (errorCodeProperty != null && errorCurrentValueProperty != null)
                            {
                                var errorCode = errorCodeProperty.GetValue(individualError)?.ToString() ?? "Unknown";
                                var errorCurrentValue = errorCurrentValueProperty.GetValue(individualError)?.ToString() ?? "Unknown";
                                Console.WriteLine($"     {i + 1}. 에러 코드: {errorCode}");
                                Console.WriteLine($"        현재 값: '{errorCurrentValue}'");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"     {i + 1}. 에러 메시지: {individualError.Message}");
                            Console.WriteLine($"        에러 코드: {individualError.Code}");
                        }
                    }
                }
                else
                {
                    var errorType = error.GetType();
                    if (errorType.Name.StartsWith("ErrorCodeExpected"))
                    {
                        var errorCodeProperty = errorType.GetProperty("ErrorCode");
                        var errorCurrentValueProperty = errorType.GetProperty("ErrorCurrentValue");
                        if (errorCodeProperty != null && errorCurrentValueProperty != null)
                        {
                            var errorCode = errorCodeProperty.GetValue(error)?.ToString() ?? "Unknown";
                            var errorCurrentValue = errorCurrentValueProperty.GetValue(error)?.ToString() ?? "Unknown";
                            Console.WriteLine($"   → 에러 코드: {errorCode}");
                            Console.WriteLine($"   → 현재 값: '{errorCurrentValue}'");
                        }
                    }
                    else
                    {
                        // 기존 Error 타입인 경우
                        Console.WriteLine($"   → 에러 메시지: {error.Message}");
                        Console.WriteLine($"   → 에러 코드: {error.Code}");
                    }
                }
            }
        );
    }

    /// <summary>
    /// fun 기반 개별 Apply 패턴을 사용하는 UserRegistrationFunApply 데모
    /// </summary>
    static void DemonstrateFunApply(string email, string password, string name, string ageInput, string description)
    {
        Console.WriteLine($"\n--- {description} ---");
        Console.WriteLine($"이메일: '{email}'");
        Console.WriteLine($"비밀번호: '{password}'");
        Console.WriteLine($"이름: '{name}'");
        Console.WriteLine($"나이: '{ageInput}'");

        var result = UserRegistrationFunApply.Create(email, password, name, ageInput);

        result.Match(
            Succ: userRegistration =>
            {
                Console.WriteLine($"성공: 사용자 등록이 유효합니다.");
                Console.WriteLine($"   → 등록된 사용자: {userRegistration.Name} ({userRegistration.Email})");
            },
            Fail: error => PrintError(error)
        );
    }

    /// <summary>
    /// 튜플 기반 Apply와 fun 기반 Apply의 결과를 비교합니다.
    /// 두 방법 모두 동일한 에러를 수집하는지 확인합니다.
    /// </summary>
    static void CompareTwoMethods(string email, string password, string name, string ageInput)
    {
        Console.WriteLine($"입력값: email='{email}', password='{password}', name='{name}', age='{ageInput}'\n");

        // 방법 1: 튜플 기반 Apply (UserRegistration)
        var tupleResult = UserRegistration.Validate(email, password, name, ageInput);

        // 방법 2: fun 기반 개별 Apply (UserRegistrationFunApply)
        var funResult = UserRegistrationFunApply.Validate(email, password, name, ageInput);

        Console.WriteLine("방법 1 (튜플 기반 Apply):");
        tupleResult.Match(
            Succ: _ => Console.WriteLine("   성공"),
            Fail: error => PrintError(error, "   ")
        );

        Console.WriteLine("\n방법 2 (fun 기반 개별 Apply):");
        funResult.Match(
            Succ: _ => Console.WriteLine("   성공"),
            Fail: error => PrintError(error, "   ")
        );

        // 결과 비교
        var tupleErrorCount = tupleResult.Match(Succ: _ => 0, Fail: e => e is ManyErrors m ? m.Errors.Count : 1);
        var funErrorCount = funResult.Match(Succ: _ => 0, Fail: e => e is ManyErrors m ? m.Errors.Count : 1);

        Console.WriteLine($"\n결과 비교:");
        Console.WriteLine($"   → 튜플 기반 Apply 에러 개수: {tupleErrorCount}");
        Console.WriteLine($"   → fun 기반 Apply 에러 개수: {funErrorCount}");
        Console.WriteLine($"   → 두 방법의 에러 수집 결과가 동일합니다: {tupleErrorCount == funErrorCount}");
    }

    /// <summary>
    /// 에러를 출력하는 헬퍼 메서드
    /// </summary>
    static void PrintError(Error error, string indent = "   ")
    {
        if (error is ManyErrors manyErrors)
        {
            Console.WriteLine($"{indent}→ 총 {manyErrors.Errors.Count}개의 검증 실패:");
            for (int i = 0; i < manyErrors.Errors.Count; i++)
            {
                var individualError = manyErrors.Errors[i];
                var individualErrorType = individualError.GetType();
                if (individualErrorType.Name.StartsWith("ErrorCodeExpected"))
                {
                    var errorCodeProperty = individualErrorType.GetProperty("ErrorCode");
                    var errorCurrentValueProperty = individualErrorType.GetProperty("ErrorCurrentValue");
                    if (errorCodeProperty != null && errorCurrentValueProperty != null)
                    {
                        var errorCode = errorCodeProperty.GetValue(individualError)?.ToString() ?? "Unknown";
                        var errorCurrentValue = errorCurrentValueProperty.GetValue(individualError)?.ToString() ?? "Unknown";
                        Console.WriteLine($"{indent}  {i + 1}. 에러 코드: {errorCode}");
                        Console.WriteLine($"{indent}     현재 값: '{errorCurrentValue}'");
                    }
                }
                else
                {
                    Console.WriteLine($"{indent}  {i + 1}. 에러 메시지: {individualError.Message}");
                }
            }
        }
        else
        {
            var errorType = error.GetType();
            if (errorType.Name.StartsWith("ErrorCodeExpected"))
            {
                var errorCodeProperty = errorType.GetProperty("ErrorCode");
                var errorCurrentValueProperty = errorType.GetProperty("ErrorCurrentValue");
                if (errorCodeProperty != null && errorCurrentValueProperty != null)
                {
                    var errorCode = errorCodeProperty.GetValue(error)?.ToString() ?? "Unknown";
                    var errorCurrentValue = errorCurrentValueProperty.GetValue(error)?.ToString() ?? "Unknown";
                    Console.WriteLine($"{indent}→ 에러 코드: {errorCode}");
                    Console.WriteLine($"{indent}→ 현재 값: '{errorCurrentValue}'");
                }
            }
            else
            {
                Console.WriteLine($"{indent}→ 에러 메시지: {error.Message}");
            }
        }
    }
}
