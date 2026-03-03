using ApplyBindCombinedValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyBindCombinedValidation;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 혼합 검증 (Mixed Validation) 예제 ===");
        Console.WriteLine("Apply(독립) + Bind(의존) 혼용 패턴을 보여주는 예제입니다.\n");

        // 성공 케이스
        DemonstrateOrderInfo("홍길동", "hong@example.com", "100000", "10000", "성공");

        // Apply 단계 실패 (독립 검증)
        DemonstrateOrderInfo("", "hong@example.com", "100000", "10000", "Apply 실패 - 고객명");
        DemonstrateOrderInfo("홍길동", "invalid", "100000", "10000", "Apply 실패 - 이메일");
        DemonstrateOrderInfo("", "invalid", "100000", "10000", "Apply 실패 - 둘 다");

        // Bind 단계 실패 (의존 검증)
        DemonstrateOrderInfo("홍길동", "hong@example.com", "invalid", "10000", "Bind 실패 - 주문금액");
        DemonstrateOrderInfo("홍길동", "hong@example.com", "100000", "150000", "Bind 실패 - 할인금액 초과");
    }

    static void DemonstrateOrderInfo(string customerName, string customerEmail, string orderAmountInput, string discountInput, string description)
    {
        Console.WriteLine($"\n--- {description} ---");
        Console.WriteLine($"입력: '{customerName}', '{customerEmail}', '{orderAmountInput}', '{discountInput}'");

        var result = OrderInfo.Create(customerName, customerEmail, orderAmountInput, discountInput);

        result.Match(
            Succ: orderInfo => 
            {
                Console.WriteLine($"성공: {orderInfo.CustomerName} - {orderInfo.OrderAmount:C} → {orderInfo.FinalAmount:C}");
            },
            Fail: error => 
            {
                Console.WriteLine($"실패:");
                
                if (error is ManyErrors manyErrors)
                {
                    Console.WriteLine($"   → Apply 단계에서 {manyErrors.Errors.Count}개 에러 수집");
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
                                Console.WriteLine($"     {i + 1}. {errorCode}: '{errorCurrentValue}'");
                            }
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
                            Console.WriteLine($"   → Bind 단계에서 단일 에러: {errorCode}: '{errorCurrentValue}'");
                        }
                    }
                }
            }
        );
    }
}
