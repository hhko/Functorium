using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;
using Functorium.Domains.Errors;

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
   IValueObject (인터페이스 — 명명 규칙 상수)
       └── AbstractValueObject (기본 클래스 — 동등성, 해시코드, ORM 프록시)
           ├── ValueObject (CreateFromValidation<TVO, TValue> 헬퍼)
           │   └── SimpleValueObject<T> (단일 값 래퍼, protected T Value)
           └── ComparableValueObject (IComparable, 비교 연산자)
               └── ComparableSimpleValueObject<T> (단일 비교 가능 값 래퍼, protected T Value)
");
    }
}

// ========================================
// 값 객체 구현 예시 (Functorium 프레임워크 기반)
// DomainError 헬퍼를 사용한 간결한 에러 처리
// ========================================

/// <summary>
/// Email 값 객체 (SimpleValueObject 기반)
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public string Address => Value;

    public static Fin<Email> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Email(validValue));

    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant());

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<Email>(new DomainErrorKind.Empty(), value ?? "null",
                $"Email address cannot be empty. Current value: '{value}'");

    private static Validation<Error, string> ValidateFormat(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@')
            ? value
            : DomainError.For<Email>(new DomainErrorKind.InvalidFormat(), value ?? "null",
                $"Invalid email format. Current value: '{value}'");

    public static implicit operator string(Email email) => email.Value;
}

/// <summary>
/// Age 값 객체 (ComparableSimpleValueObject 기반)
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Age : ComparableSimpleValueObject<int>
{
    private Age(int value) : base(value) { }

    public int Id => Value;

    public static Fin<Age> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Age(validValue));

    public static Age CreateFromValidated(int value) => new(value);

    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotTooOld(value))
            .Map(_ => value);

    private static Validation<Error, int> ValidateNotNegative(int value) =>
        value >= 0
            ? value
            : DomainError.For<Age, int>(new DomainErrorKind.Negative(), value,
                $"Age cannot be negative. Current value: '{value}'");

    private static Validation<Error, int> ValidateNotTooOld(int value) =>
        value <= 150
            ? value
            : DomainError.For<Age, int>(new DomainErrorKind.AboveMaximum(), value,
                $"Age cannot exceed 150 years. Current value: '{value}'");

    public static implicit operator int(Age age) => age.Value;
}

/// <summary>
/// Address 값 객체 (복합 ValueObject)
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Address : ValueObject
{
    public sealed record CityEmpty : DomainErrorKind.Custom;
    public sealed record StreetEmpty : DomainErrorKind.Custom;
    public sealed record PostalCodeEmpty : DomainErrorKind.Custom;

    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    private Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    public static Fin<Address> Create(string city, string street, string postalCode) =>
        CreateFromValidation(
            Validate(city, street, postalCode),
            validValues => new Address(validValues.City, validValues.Street, validValues.PostalCode));

    public static Address CreateFromValidated(string city, string street, string postalCode) =>
        new Address(city, street, postalCode);

    public static Validation<Error, (string City, string Street, string PostalCode)> Validate(
        string city, string street, string postalCode) =>
        (ValidateCityNotEmpty(city), ValidateStreetNotEmpty(street), ValidatePostalCodeNotEmpty(postalCode))
            .Apply((validCity, validStreet, validPostalCode) => (validCity, validStreet, validPostalCode));

    private static Validation<Error, string> ValidateCityNotEmpty(string city) =>
        !string.IsNullOrWhiteSpace(city)
            ? city
            : DomainError.For<Address>(new CityEmpty(), city ?? "null",
                $"City cannot be empty. Current value: '{city}'");

    private static Validation<Error, string> ValidateStreetNotEmpty(string street) =>
        !string.IsNullOrWhiteSpace(street)
            ? street
            : DomainError.For<Address>(new StreetEmpty(), street ?? "null",
                $"Street cannot be empty. Current value: '{street}'");

    private static Validation<Error, string> ValidatePostalCodeNotEmpty(string postalCode) =>
        !string.IsNullOrWhiteSpace(postalCode)
            ? postalCode
            : DomainError.For<Address>(new PostalCodeEmpty(), postalCode ?? "null",
                $"Postal code cannot be empty. Current value: '{postalCode}'");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
        yield return PostalCode;
    }

    public override string ToString() => $"{City} {Street} ({PostalCode})";
}
