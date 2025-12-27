using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace TestingStrategies;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 값 객체 테스트 전략 ===\n");

        // 1. 생성 테스트 패턴
        DemonstrateCreationTests();

        // 2. 동등성 테스트 패턴
        DemonstrateEqualityTests();

        // 3. 비교 가능성 테스트 패턴
        DemonstrateComparabilityTests();

        // 4. 테스트 헬퍼 사용
        DemonstrateTestHelpers();
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
        var errorPassed = errorMessage.Contains("Email.InvalidFormat");
        Console.WriteLine($"   [에러 코드 검증] 'Email.InvalidFormat' 포함 → {(errorPassed ? "PASS" : "FAIL")}");

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
        var sortPassed = ages[0].Value == 20 && ages[1].Value == 25 && ages[2].Value == 30;
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
        var valuePassed = email.Value == "user@example.com";
        Console.WriteLine($"   [GetSuccessValue 헬퍼] → {(valuePassed ? "PASS" : "FAIL")}");

        // GetFailError 헬퍼
        var error = failResult.GetFailError();
        var errorPassed = !string.IsNullOrEmpty(error.Message);
        Console.WriteLine($"   [GetFailError 헬퍼] → {(errorPassed ? "PASS" : "FAIL")}");

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 정의
// ========================================

public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Fin<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");
        if (!value.Contains('@'))
            return DomainErrors.InvalidFormat(value);
        return new Email(value.ToLowerInvariant());
    }

    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

    public bool Equals(Email? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => obj is Email other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Email? left, Email? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Email? left, Email? right) => !(left == right);

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "이메일 주소가 비어있습니다.");
        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: "이메일 형식이 올바르지 않습니다.");
    }
}

public sealed class Age : IComparable<Age>
{
    public int Value { get; }

    private Age(int value) => Value = value;

    public static Fin<Age> Create(int value)
    {
        if (value < 0)
            return DomainErrors.Negative(value);
        if (value > 150)
            return DomainErrors.TooOld(value);
        return new Age(value);
    }

    public static Age CreateFromValidated(int value) => new(value);

    public int CompareTo(Age? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    public static bool operator <(Age left, Age right) => left.CompareTo(right) < 0;
    public static bool operator >(Age left, Age right) => left.CompareTo(right) > 0;
    public static bool operator <=(Age left, Age right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Age left, Age right) => left.CompareTo(right) >= 0;

    internal static class DomainErrors
    {
        public static Error Negative(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Age)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: "나이는 음수일 수 없습니다.");
        public static Error TooOld(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Age)}.{nameof(TooOld)}",
                errorCurrentValue: value,
                errorMessage: "나이는 150세를 초과할 수 없습니다.");
    }
}

// ========================================
// 테스트 헬퍼
// ========================================

public static class FinTestExtensions
{
    public static void ShouldBeSuccess<T>(this Fin<T> fin)
    {
        if (fin.IsFail)
        {
            var message = fin.Match(_ => "", e => e.Message);
            throw new Exception($"Expected Succ but got Fail: {message}");
        }
    }

    public static void ShouldBeFail<T>(this Fin<T> fin)
    {
        if (fin.IsSucc)
        {
            throw new Exception("Expected Fail but got Succ");
        }
    }

    public static T GetSuccessValue<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: value => value,
            Fail: error => throw new Exception($"Expected Succ but got Fail: {error.Message}")
        );
    }

    public static Error GetFailError<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: _ => throw new Exception("Expected Fail but got Succ"),
            Fail: error => error
        );
    }
}
