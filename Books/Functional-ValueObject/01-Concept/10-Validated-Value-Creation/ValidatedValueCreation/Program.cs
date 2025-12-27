using ValidatedValueCreation.ValueObjects;

namespace ValidatedValueCreation;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== 복합 값 객체의 3가지 메서드 패턴 ===\n");

        // Create 메서드 테스트
        DemonstrateCreate();

        // Validate 메서드 테스트
        DemonstrateValidate();

        // CreateFromValidated 메서드 테스트
        DemonstrateCreateFromValidated();
    }

    /// <summary>
    /// Create 메서드 시연
    /// </summary>
    private static void DemonstrateCreate()
    {
        Console.WriteLine("1. Create: 검증 후 생성");

        Console.WriteLine("\n  성공 케이스:");
        var successResult = Address.Create("123 Main St", "Seoul", "12345");
        successResult.Match(
            Succ: address => Console.WriteLine($"    성공: {address}"),
            Fail: error => Console.WriteLine($"    실패: {error}")
        );

        Console.WriteLine("\n  실패 케이스들:");
        var failureResult1 = Address.Create("", "Seoul", "12345"); // 빈 거리명
        failureResult1.Match(
            Succ: address => Console.WriteLine($"    성공: {address}"),
            Fail: error => Console.WriteLine($"    실패: {error}")
        );

        var failureResult2 = Address.Create("123 Main St", "Seoul", "123"); // 잘못된 우편번호
        failureResult2.Match(
            Succ: address => Console.WriteLine($"    성공: {address}"),
            Fail: error => Console.WriteLine($"    실패: {error}")
        );
    }

    /// <summary>
    /// Validate 메서드 시연
    /// </summary>
    private static void DemonstrateValidate()
    {
        Console.WriteLine("\n2. Validate: 검증 메서드");

        Console.WriteLine("\n  검증 성공 케이스:");
        var successValidation = Address.Validate("123 Main St", "Seoul", "12345");
        successValidation.Match(
            Succ: validatedValues => Console.WriteLine($"    검증 성공: {validatedValues.Street}, {validatedValues.City} {validatedValues.PostalCode}"),
            Fail: error => Console.WriteLine($"    검증 실패: {error}")
        );

        Console.WriteLine("\n  검증 실패 케이스:");
        var failureValidation = Address.Validate("", "Seoul", "123");
        failureValidation.Match(
            Succ: validatedValues => Console.WriteLine($"    검증 성공: {validatedValues.Street}, {validatedValues.City} {validatedValues.PostalCode}"),
            Fail: error => Console.WriteLine($"    검증 실패: {error}")
        );
    }

    /// <summary>
    /// CreateFromValidated 메서드 시연
    /// </summary>
    private static void DemonstrateCreateFromValidated()
    {
        Console.WriteLine("\n3. CreateFromValidated: 이미 검증된 값 객체들로 직접 생성");

        // 이미 검증된 값 객체들 생성 (CreateFromValidated 메서드 사용)
        var street = Street.CreateFromValidated("123 Main St");
        var city = City.CreateFromValidated("Seoul");
        var postalCode = PostalCode.CreateFromValidated("12345");

        // CreateFromValidated 메서드 사용: 직접 생성
        var address = Address.CreateFromValidated(street, city, postalCode);
        Console.WriteLine($"  생성된 주소: {address}");
    }
}
