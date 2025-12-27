using BindInternalApplyValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using Functorium.Abstractions.Errors;

namespace BindInternalApplyValidation;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Bind 내부 Apply 중첩 검증 (Bind Internal Apply) 예제 ===");
        Console.WriteLine("전화번호 값 객체의 Bind 내부에서 Apply를 사용한 중첩 검증 예제입니다.\n");

        // 성공 케이스
        DemonstratePhoneNumber("+821012345678", "유효한 한국 전화번호");
        DemonstratePhoneNumber("+12125551234", "유효한 미국 전화번호");

        // 전화번호 형식 오류 (Bind 단계에서 실패)
        DemonstratePhoneNumber("123", "전화번호 형식 오류");
        DemonstratePhoneNumber("", "전화번호 빈 값");

        // 국가 코드 오류 (Apply 단계에서 실패)
        DemonstratePhoneNumber("+861012345678", "국가 코드 오류");

        // 지역 코드 오류 (Apply 단계에서 실패)
        DemonstratePhoneNumber("+82abc123456", "지역 코드 오류");

        // 로컬 번호 오류 (Apply 단계에서 실패)
        DemonstratePhoneNumber("+82101234abc", "로컬 번호 오류");

        // 복수개 오류 (Apply 단계에서 실패)
        DemonstratePhoneNumber("+86abc123def", "국가 코드, 지역 코드, 로컬 번호 오류");
    }

    static void DemonstratePhoneNumber(string phoneNumber, string description)
    {
        Console.WriteLine($"\n--- {description} ---");
        Console.WriteLine($"전화번호: '{phoneNumber}'");

        var result = PhoneNumber.Create(phoneNumber);

        result.Match(
            Succ: phone => 
            {
                Console.WriteLine($"성공: 전화번호가 유효합니다.");
                Console.WriteLine($"   → 국가코드: {phone.CountryCode}");
                Console.WriteLine($"   → 지역코드: {phone.AreaCode}");
                Console.WriteLine($"   → 로컬번호: {phone.LocalNumber}");
                Console.WriteLine("   → 모든 중첩 검증 규칙을 통과했습니다.");
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
}
