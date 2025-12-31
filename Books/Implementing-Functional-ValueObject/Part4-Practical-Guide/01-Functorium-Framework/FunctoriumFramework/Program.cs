using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;

namespace FunctoriumFramework;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Functorium 프레임워크 통합 ===\n");

        // 1. SimpleValueObject 사용 예시
        DemonstrateSimpleValueObject();

        // 2. ComparableSimpleValueObject 사용 예시
        DemonstrateComparableSimpleValueObject();

        // 3. ValueObject (복합) 사용 예시
        DemonstrateValueObject();

        // 4. 프레임워크 계층 구조 설명
        DemonstrateFrameworkHierarchy();
    }

    static void DemonstrateSimpleValueObject()
    {
        Console.WriteLine("1. SimpleValueObject<T> 사용 예시");
        Console.WriteLine("─".PadRight(40, '─'));

        var email1 = Email.Create("user@example.com");
        var email2 = Email.Create("invalid-email");

        email1.Match(
            Succ: e => Console.WriteLine($"   유효한 이메일: {e}"),
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        email2.Match(
            Succ: e => Console.WriteLine($"   유효한 이메일: {e}"),
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateComparableSimpleValueObject()
    {
        Console.WriteLine("2. ComparableSimpleValueObject<T> 사용 예시");
        Console.WriteLine("─".PadRight(40, '─'));

        var ages = new[]
        {
            Age.Create(30).IfFail(Age.CreateFromValidated(0)),
            Age.Create(25).IfFail(Age.CreateFromValidated(0)),
            Age.Create(35).IfFail(Age.CreateFromValidated(0))
        };

        Console.WriteLine("   정렬 전: " + string.Join(", ", ages.Select(a => a.Id)));

        System.Array.Sort(ages);
        Console.WriteLine("   정렬 후: " + string.Join(", ", ages.Select(a => a.Id)));

        Console.WriteLine();
    }

    static void DemonstrateValueObject()
    {
        Console.WriteLine("3. ValueObject (복합) 사용 예시");
        Console.WriteLine("─".PadRight(40, '─'));

        var address = Address.Create(
            "서울",
            "강남구 테헤란로 123",
            "06234"
        );

        address.Match(
            Succ: a => Console.WriteLine($"   주소: {a}"),
            Fail: error => Console.WriteLine($"   오류: {error.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateFrameworkHierarchy()
    {
        Console.WriteLine("4. 프레임워크 타입 계층 구조");
        Console.WriteLine("─".PadRight(40, '─'));
        Console.WriteLine(@"
   IValueObject (인터페이스)
       │
       └── AbstractValueObject (기본 클래스)
           │
           ├── ValueObject (복합 값 객체)
           │   │
           │   └── SimpleValueObject<T>
           │
           ├── ComparableValueObject
           │   │
           │   └── ComparableSimpleValueObject<T>
           │
           └── SmartEnum + IValueObject (열거형)
");
    }
}

// ========================================
// 값 객체 구현 예시 (Functorium 프레임워크 기반)
// ========================================

/// <summary>
/// Email 값 객체 (SimpleValueObject 기반)
/// Functorium.Domains.ValueObjects.SimpleValueObject{T}를 상속하여 구현
/// </summary>
public sealed class Email : SimpleValueObject<string>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private Email(string value) : base(value) { }

    /// <summary>
    /// 이메일 주소 값에 대한 public 접근자
    /// </summary>
    public string Address => Value;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Email> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Email(validValue));

    // 5. Public Validate 메서드 - 독립 검증 규칙들을 병렬로 실행
    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant())
            .As();

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value ?? "null");

    // 5.2 형식 검증
    private static Validation<Error, string> ValidateFormat(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@')
            ? value
            : DomainErrors.InvalidFormat(value ?? "null");

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
/// Functorium.Domains.ValueObjects.ComparableSimpleValueObject{T}를 상속하여 구현
/// </summary>
public sealed class Age : ComparableSimpleValueObject<int>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private Age(int value) : base(value) { }

    /// <summary>
    /// 나이 값에 대한 public 접근자
    /// </summary>
    public int Id => Value;

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

/// <summary>
/// Address 값 객체 (복합 ValueObject)
/// Functorium.Domains.ValueObjects.ValueObject를 상속하여 구현
/// </summary>
public sealed class Address : ValueObject
{
    // 1.1 readonly 속성 선언 - 불변성 보장
    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Address> Create(string city, string street, string postalCode) =>
        CreateFromValidation(
            Validate(city, street, postalCode),
            validValues => new Address(validValues.City, validValues.Street, validValues.PostalCode));

    // 4. Internal CreateFromValidated 메서드
    internal static Address CreateFromValidated(string city, string street, string postalCode) =>
        new Address(city, street, postalCode);

    // 5. Public Validate 메서드 - 독립 검증 규칙들을 병렬로 실행
    public static Validation<Error, (string City, string Street, string PostalCode)> Validate(
        string city, string street, string postalCode) =>
        (ValidateCityNotEmpty(city), ValidateStreetNotEmpty(street), ValidatePostalCodeNotEmpty(postalCode))
            .Apply((validCity, validStreet, validPostalCode) => (validCity, validStreet, validPostalCode))
            .As();

    // 5.1 도시 검증
    private static Validation<Error, string> ValidateCityNotEmpty(string city) =>
        !string.IsNullOrWhiteSpace(city)
            ? city
            : DomainErrors.CityEmpty(city ?? "null");

    // 5.2 도로명 검증
    private static Validation<Error, string> ValidateStreetNotEmpty(string street) =>
        !string.IsNullOrWhiteSpace(street)
            ? street
            : DomainErrors.StreetEmpty(street ?? "null");

    // 5.3 우편번호 검증
    private static Validation<Error, string> ValidatePostalCodeNotEmpty(string postalCode) =>
        !string.IsNullOrWhiteSpace(postalCode)
            ? postalCode
            : DomainErrors.PostalCodeEmpty(postalCode ?? "null");

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
        yield return PostalCode;
    }

    public override string ToString() => $"{City} {Street} ({PostalCode})";

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidateCityNotEmpty 메서드와 1:1 매핑되는 에러
        public static Error CityEmpty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Address)}.{nameof(CityEmpty)}",
                errorCurrentValue: value,
                errorMessage: $"City cannot be empty. Current value: '{value}'");

        // ValidateStreetNotEmpty 메서드와 1:1 매핑되는 에러
        public static Error StreetEmpty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Address)}.{nameof(StreetEmpty)}",
                errorCurrentValue: value,
                errorMessage: $"Street cannot be empty. Current value: '{value}'");

        // ValidatePostalCodeNotEmpty 메서드와 1:1 매핑되는 에러
        public static Error PostalCodeEmpty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Address)}.{nameof(PostalCodeEmpty)}",
                errorCurrentValue: value,
                errorMessage: $"Postal code cannot be empty. Current value: '{value}'");
    }
}
