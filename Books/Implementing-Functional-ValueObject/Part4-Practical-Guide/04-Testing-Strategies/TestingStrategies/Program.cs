using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace TestingStrategies;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 값 객체 테스트 전략 (Functorium 프레임워크 기반) ===\n");

        // 1. 생성 테스트 패턴
        DemonstrateCreationTests();

        // 2. 동등성 테스트 패턴
        DemonstrateEqualityTests();

        // 3. 비교 가능성 테스트 패턴
        DemonstrateComparabilityTests();

        // 4. 테스트 헬퍼 사용
        DemonstrateTestHelpers();

        // 5. 유효성 검사 분리 테스트
        DemonstrateValidationSeparationTests();
    }

    static void DemonstrateCreationTests()
    {
        Console.WriteLine("1. 생성 테스트 패턴");
        Console.WriteLine("─".PadRight(40, '─'));

        // 유효한 입력 테스트
        var validResult = Email.Create("user@example.com");
        var validPassed = validResult.IsSucc;
        Console.WriteLine($"   [유효한 입력 테스트] user@example.com → {(validPassed ? "PASS" : "FAIL")}");

        // 유효하지 않은 입력 테스트
        var invalidResult = Email.Create("invalid-email");
        var invalidPassed = invalidResult.IsFail;
        Console.WriteLine($"   [유효하지 않은 입력 테스트] invalid-email → {(invalidPassed ? "PASS" : "FAIL")}");

        // 에러 메시지 검증
        var errorMessage = invalidResult.Match(
            Succ: _ => "",
            Fail: e => e.Message
        );
        var errorPassed = errorMessage.Contains("Invalid email format");
        Console.WriteLine($"   [에러 메시지 검증] 'Invalid email format' 포함 → {(errorPassed ? "PASS" : "FAIL")}");

        // 경계값 테스트
        var emptyResult = Email.Create("");
        var nullResult = Email.Create(null!);
        var boundaryPassed = emptyResult.IsFail && nullResult.IsFail;
        Console.WriteLine($"   [경계값 테스트] 빈 문자열/null → {(boundaryPassed ? "PASS" : "FAIL")}");

        Console.WriteLine();
    }

    static void DemonstrateEqualityTests()
    {
        Console.WriteLine("2. 동등성 테스트 패턴");
        Console.WriteLine("─".PadRight(40, '─'));

        var email1 = Email.CreateFromValidated("user@example.com");
        var email2 = Email.CreateFromValidated("user@example.com");
        var email3 = Email.CreateFromValidated("other@example.com");

        // 같은 값 동등성
        var equalsPassed = email1.Equals(email2);
        Console.WriteLine($"   [같은 값 동등성] email1 == email2 → {(equalsPassed ? "PASS" : "FAIL")}");

        // 다른 값 비동등성
        var notEqualsPassed = !email1.Equals(email3);
        Console.WriteLine($"   [다른 값 비동등성] email1 != email3 → {(notEqualsPassed ? "PASS" : "FAIL")}");

        // 해시코드 일관성
        var hashPassed = email1.GetHashCode() == email2.GetHashCode();
        Console.WriteLine($"   [해시코드 일관성] hash(email1) == hash(email2) → {(hashPassed ? "PASS" : "FAIL")}");

        // == 연산자
        var operatorPassed = email1 == email2 && email1 != email3;
        Console.WriteLine($"   [연산자 테스트] == 및 != → {(operatorPassed ? "PASS" : "FAIL")}");

        Console.WriteLine();
    }

    static void DemonstrateComparabilityTests()
    {
        Console.WriteLine("3. 비교 가능성 테스트 패턴");
        Console.WriteLine("─".PadRight(40, '─'));

        var age20 = Age.CreateFromValidated(20);
        var age25 = Age.CreateFromValidated(25);
        var age30 = Age.CreateFromValidated(30);

        // CompareTo 테스트
        var comparePassed = age20.CompareTo(age25) < 0 && age30.CompareTo(age25) > 0;
        Console.WriteLine($"   [CompareTo 테스트] 20 < 25 < 30 → {(comparePassed ? "PASS" : "FAIL")}");

        // 연산자 테스트
        var operatorPassed = age20 < age25 && age25 < age30;
        Console.WriteLine($"   [비교 연산자 테스트] < 연산자 → {(operatorPassed ? "PASS" : "FAIL")}");

        // 정렬 테스트
        var ages = new[] { age30, age20, age25 };
        System.Array.Sort(ages);
        var sortPassed = ages[0].Years == 20 && ages[1].Years == 25 && ages[2].Years == 30;
        Console.WriteLine($"   [정렬 테스트] 정렬 후 순서 → {(sortPassed ? "PASS" : "FAIL")}");

        Console.WriteLine();
    }

    static void DemonstrateTestHelpers()
    {
        Console.WriteLine("4. 테스트 헬퍼 사용");
        Console.WriteLine("─".PadRight(40, '─'));

        // ShouldBeSuccess 헬퍼
        var successResult = Email.Create("user@example.com");
        try
        {
            successResult.ShouldBeSuccess();
            Console.WriteLine("   [ShouldBeSuccess 헬퍼] → PASS");
        }
        catch
        {
            Console.WriteLine("   [ShouldBeSuccess 헬퍼] → FAIL");
        }

        // ShouldBeFail 헬퍼
        var failResult = Email.Create("invalid");
        try
        {
            failResult.ShouldBeFail();
            Console.WriteLine("   [ShouldBeFail 헬퍼] → PASS");
        }
        catch
        {
            Console.WriteLine("   [ShouldBeFail 헬퍼] → FAIL");
        }

        // GetSuccessValue 헬퍼
        var email = successResult.GetSuccessValue();
        var valuePassed = email.Address == "user@example.com";
        Console.WriteLine($"   [GetSuccessValue 헬퍼] → {(valuePassed ? "PASS" : "FAIL")}");

        // GetFailError 헬퍼
        var error = failResult.GetFailError();
        var errorPassed = !string.IsNullOrEmpty(error.Message);
        Console.WriteLine($"   [GetFailError 헬퍼] → {(errorPassed ? "PASS" : "FAIL")}");

        Console.WriteLine();
    }

    static void DemonstrateValidationSeparationTests()
    {
        Console.WriteLine("5. 유효성 검사 분리 테스트");
        Console.WriteLine("─".PadRight(40, '─'));

        // Validate 메서드 직접 호출 테스트
        var emailValidation = Email.Validate("user@example.com");
        var emailValidationPassed = emailValidation.IsSuccess;
        Console.WriteLine($"   [Validate 직접 호출] Email.Validate → {(emailValidationPassed ? "PASS" : "FAIL")}");

        // 병렬 검증 - 모든 오류 수집 테스트
        var invalidEmailValidation = Email.Validate("");
        var invalidEmailErrorCount = invalidEmailValidation.Match(
            Succ: _ => 0,
            Fail: errors => errors.Count
        );
        var multiErrorPassed = invalidEmailErrorCount >= 1; // Empty, InvalidFormat 두 개 이상
        Console.WriteLine($"   [병렬 검증 오류 수집] 빈 이메일 오류 수: {invalidEmailErrorCount} → {(multiErrorPassed ? "PASS" : "FAIL")}");

        // 순차 검증 - 첫 번째 오류만 반환 테스트
        var ageValidation = Age.Validate(-10);
        var ageErrorMessage = ageValidation.Match(
            Succ: _ => "",
            Fail: errors => errors.Head.Message
        );
        var sequentialPassed = ageErrorMessage.Contains("negative");
        Console.WriteLine($"   [순차 검증 첫 오류] Age.Validate(-10) → {(sequentialPassed ? "PASS" : "FAIL")}");

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 정의 (Functorium 프레임워크 기반)
// ========================================

/// <summary>
/// Email 값 객체 (SimpleValueObject 기반)
/// </summary>
public sealed class Email : SimpleValueObject<string>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private Email(string value) : base(value) { }

    /// <summary>
    /// 이메일 주소에 대한 public 접근자
    /// </summary>
    public string Address => Value;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    // 4. Internal CreateFromValidated 메서드
    internal static Email CreateFromValidated(string value) => new(value);

    // 5. Public Validate 메서드 - 독립 검증 규칙들을 병렬로 실행
    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant())
            .As();

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    // 5.2 형식 검증
    private static Validation<Error, string> ValidateFormat(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@')
            ? value
            : DomainErrors.InvalidFormat(value);

    public static implicit operator string(Email email) => email.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidateNotEmpty 메서드와 1:1 매핑되는 에러
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Email address cannot be empty. Current value: '{value}'");

        // ValidateFormat 메서드와 1:1 매핑되는 에러
        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid email format. Current value: '{value}'");
    }
}

/// <summary>
/// Age 값 객체 (ComparableSimpleValueObject 기반)
/// </summary>
public sealed class Age : ComparableSimpleValueObject<int>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private Age(int value) : base(value) { }

    /// <summary>
    /// 나이 값에 대한 public 접근자
    /// </summary>
    public int Years => Value;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Age> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Age(validValue));

    // 4. Internal CreateFromValidated 메서드
    internal static Age CreateFromValidated(int value) => new(value);

    // 5. Public Validate 메서드 - 순차 검증 (범위 검증은 의존성이 있음)
    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotTooOld(value))
            .Map(_ => value);

    // 5.1 음수 검증
    private static Validation<Error, int> ValidateNotNegative(int value) =>
        value >= 0
            ? value
            : DomainErrors.Negative(value);

    // 5.2 최대값 검증
    private static Validation<Error, int> ValidateNotTooOld(int value) =>
        value <= 150
            ? value
            : DomainErrors.TooOld(value);

    public static implicit operator int(Age age) => age.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidateNotNegative 메서드와 1:1 매핑되는 에러
        public static Error Negative(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Age)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: $"Age cannot be negative. Current value: '{value}'");

        // ValidateNotTooOld 메서드와 1:1 매핑되는 에러
        public static Error TooOld(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Age)}.{nameof(TooOld)}",
                errorCurrentValue: value,
                errorMessage: $"Age cannot exceed 150 years. Current value: '{value}'");
    }
}

// ========================================
// 테스트 헬퍼 확장 메서드
// ========================================

public static class FinTestExtensions
{
    /// <summary>
    /// Fin이 성공 상태인지 검증합니다. 실패 시 예외를 던집니다.
    /// </summary>
    public static void ShouldBeSuccess<T>(this Fin<T> fin)
    {
        if (fin.IsFail)
        {
            var message = fin.Match(_ => "", e => e.Message);
            throw new Exception($"Expected Succ but got Fail: {message}");
        }
    }

    /// <summary>
    /// Fin이 실패 상태인지 검증합니다. 성공 시 예외를 던집니다.
    /// </summary>
    public static void ShouldBeFail<T>(this Fin<T> fin)
    {
        if (fin.IsSucc)
        {
            throw new Exception("Expected Fail but got Succ");
        }
    }

    /// <summary>
    /// Fin에서 성공 값을 추출합니다. 실패 시 예외를 던집니다.
    /// </summary>
    public static T GetSuccessValue<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: value => value,
            Fail: error => throw new Exception($"Expected Succ but got Fail: {error.Message}")
        );
    }

    /// <summary>
    /// Fin에서 실패 Error를 추출합니다. 성공 시 예외를 던집니다.
    /// </summary>
    public static Error GetFailError<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: _ => throw new Exception("Expected Fail but got Succ"),
            Fail: error => error
        );
    }

    /// <summary>
    /// Fin이 특정 에러 코드를 포함하는지 검증합니다.
    /// </summary>
    public static void ShouldHaveErrorCode<T>(this Fin<T> fin, string expectedCode)
    {
        if (fin.IsSucc)
        {
            throw new Exception("Expected Fail but got Succ");
        }

        var error = fin.GetFailError();
        if (!error.Message.Contains(expectedCode))
        {
            throw new Exception($"Expected error code '{expectedCode}' but got '{error.Message}'");
        }
    }
}

