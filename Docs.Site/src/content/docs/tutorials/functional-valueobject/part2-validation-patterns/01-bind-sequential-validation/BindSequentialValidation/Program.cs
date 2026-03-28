using BindSequentialValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace BindSequentialValidation;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 의존 검증 (Dependent Validation) 예제 ===");
        Console.WriteLine("주소 값 객체의 의존적인 검증 규칙들을 순차적으로 실행합니다.\n");

        // 성공 케이스
        DemonstrateAddress("강남대로 123", "서울", "12345", "KR", "유효한 한국 주소");
        DemonstrateAddress("123 Main St", "New York", "10001", "US", "유효한 미국 주소");
        DemonstrateAddress("1-2-3 Shibuya", "Tokyo", "1500002", "JP", "유효한 일본 주소");

        // 실패 케이스들
        DemonstrateAddress("", "서울", "12345", "KR", "도로명이 빈 값");
        DemonstrateAddress("강남대로 123", "A", "12345", "KR", "도시명이 너무 짧음");
        DemonstrateAddress("강남대로 123", "서울", "123", "KR", "우편번호가 너무 짧음");
        DemonstrateAddress("강남대로 123", "서울", "1234a", "KR", "한국 우편번호 형식 오류 (문자 포함)");
        DemonstrateAddress("123 Main St", "New York", "123", "US", "미국 우편번호 형식 오류");
    }

    static void DemonstrateAddress(string street, string city, string postalCode, string country, string description)
    {
        Console.WriteLine($"\n--- {description} ---");
        Console.WriteLine($"도로명: '{street}'");
        Console.WriteLine($"도시: '{city}'");
        Console.WriteLine($"우편번호: '{postalCode}'");
        Console.WriteLine($"국가: '{country}'");

        var result = Address.Create(street, city, postalCode, country);

        result.Match(
            Succ: address => 
            {
                Console.WriteLine($"성공: 주소가 유효합니다.");
                Console.WriteLine($"   → 완전한 주소: {address.Street}, {address.City} {address.PostalCode}, {address.Country}");
                Console.WriteLine("   → 모든 의존 검증 규칙을 순차적으로 통과했습니다.");
            },
            Fail: error =>
            {
                Console.WriteLine($"실패:");

                // ErrorCodeExpected 타입인지 확인하고 ErrorCode, ErrorCurrentValue 출력 (리플렉션 사용)
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
        );
    }
}
