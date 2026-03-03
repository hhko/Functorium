using ApplyInternalBindValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using Functorium.Abstractions.Errors;

namespace ApplyInternalBindValidation;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 중첩 검증 (Nested Validation) 예제 ===");
        Console.WriteLine("회원가입 정보 값 객체의 Apply 내부에서 Bind를 사용한 중첩 검증 예제입니다.\n");

        // 성공 케이스
        DemonstrateMemberRegistration("john_doe", "john@example.com", "SecurePass123", "유효한 회원가입 정보");

        // 사용자명 중첩 검증 실패 케이스들
        DemonstrateMemberRegistration("ab", "john@example.com", "SecurePass123", "사용자명 형식 오류");
        DemonstrateMemberRegistration("admin_user", "john@example.com", "SecurePass123", "사용자명 예약어 사용");

        // 이메일 중첩 검증 실패 케이스들
        DemonstrateMemberRegistration("ab", "invalid-email", "SecurePass123", "사용자명과 이메일 형식 오류");
        DemonstrateMemberRegistration("ab", "john@invalid", "SecurePass123", "사용자명과 이메일 도메인 오류");

        // 비밀번호 중첩 검증 실패 케이스들
        DemonstrateMemberRegistration("ab", "invalid-email", "weak", "사용자명, 이메일, 비밀번호 형식 오류");
        DemonstrateMemberRegistration("ab", "invalid-email", "password123", "사용자명, 이메일, 비밀번호 히스토리 오류");
    }

    static void DemonstrateMemberRegistration(string username, string email, string password, string description)
    {
        Console.WriteLine($"\n--- {description} ---");
        Console.WriteLine($"사용자명: '{username}'");
        Console.WriteLine($"이메일: '{email}'");
        Console.WriteLine($"비밀번호: '{password}'");

        var result = MemberRegistration.Create(username, email, password);

        result.Match(
            Succ: memberRegistration => 
            {
                Console.WriteLine($" ✅ 성공: 회원가입 정보가 유효합니다.");
                Console.WriteLine($"   → 사용자: {memberRegistration.Username} ({memberRegistration.Email})");
                Console.WriteLine("   → 모든 중첩 검증 규칙을 통과했습니다.");
            },
            Fail: error => 
            {
                Console.WriteLine($" ❌ 실패:");
                
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
}
