using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using Functorium.Abstractions.Errors;

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

        Console.WriteLine("   정렬 전: " + string.Join(", ", ages.Select(a => a.Value)));

        System.Array.Sort(ages);
        Console.WriteLine("   정렬 후: " + string.Join(", ", ages.Select(a => a.Value)));

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
           │   ├── SimpleValueObject<T>
           │   │   └── ComparableSimpleValueObject<T>
           │   │
           │   └── ComparableValueObject
           │
           └── SmartEnum + IValueObject (열거형)
");
    }
}

// ========================================
// 값 객체 구현 예시
// ========================================

/// <summary>
/// 추상 기본 클래스 (Functorium 프레임워크)
/// </summary>
public abstract class AbstractValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (AbstractValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(AbstractValueObject? left, AbstractValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(AbstractValueObject? left, AbstractValueObject? right) => !(left == right);
}

/// <summary>
/// 단일 값 래퍼 (SimpleValueObject)
/// </summary>
public abstract class SimpleValueObject<T> : AbstractValueObject
{
    public T Value { get; }

    protected SimpleValueObject(T value) => Value = value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value!;
    }

    public override string ToString() => Value?.ToString() ?? string.Empty;
}

/// <summary>
/// 비교 가능한 단일 값 래퍼
/// </summary>
public abstract class ComparableSimpleValueObject<T> : SimpleValueObject<T>, IComparable<ComparableSimpleValueObject<T>>
    where T : IComparable<T>
{
    protected ComparableSimpleValueObject(T value) : base(value) { }

    public int CompareTo(ComparableSimpleValueObject<T>? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    public static bool operator <(ComparableSimpleValueObject<T> left, ComparableSimpleValueObject<T> right)
        => left.CompareTo(right) < 0;

    public static bool operator >(ComparableSimpleValueObject<T> left, ComparableSimpleValueObject<T> right)
        => left.CompareTo(right) > 0;

    public static bool operator <=(ComparableSimpleValueObject<T> left, ComparableSimpleValueObject<T> right)
        => left.CompareTo(right) <= 0;

    public static bool operator >=(ComparableSimpleValueObject<T> left, ComparableSimpleValueObject<T> right)
        => left.CompareTo(right) >= 0;
}

/// <summary>
/// Email 값 객체 (SimpleValueObject 기반)
/// </summary>
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");

        if (!value.Contains('@'))
            return DomainErrors.InvalidFormat(value);

        return new Email(value.ToLowerInvariant());
    }

    public static implicit operator string(Email email) => email.Value;

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
                errorMessage: "유효하지 않은 이메일 형식입니다.");
    }
}

/// <summary>
/// Age 값 객체 (ComparableSimpleValueObject 기반)
/// </summary>
public sealed class Age : ComparableSimpleValueObject<int>
{
    private Age(int value) : base(value) { }

    public static Fin<Age> Create(int value)
    {
        if (value < 0)
            return DomainErrors.Negative(value);
        if (value > 150)
            return DomainErrors.TooOld(value);
        return new Age(value);
    }

    public static Age CreateFromValidated(int value) => new(value);

    public static implicit operator int(Age age) => age.Value;

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
                errorMessage: "나이가 150세를 초과할 수 없습니다.");
    }
}

/// <summary>
/// Address 값 객체 (복합 ValueObject)
/// </summary>
public sealed class Address : AbstractValueObject
{
    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    private Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    public static Fin<Address> Create(string city, string street, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(city))
            return DomainErrors.CityEmpty(city ?? "null");
        if (string.IsNullOrWhiteSpace(street))
            return DomainErrors.StreetEmpty(street ?? "null");
        if (string.IsNullOrWhiteSpace(postalCode))
            return DomainErrors.PostalCodeEmpty(postalCode ?? "null");

        return new Address(city, street, postalCode);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
        yield return PostalCode;
    }

    public override string ToString() => $"{City} {Street} ({PostalCode})";

    internal static class DomainErrors
    {
        public static Error CityEmpty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Address)}.{nameof(CityEmpty)}",
                errorCurrentValue: value,
                errorMessage: "도시명이 비어있습니다.");

        public static Error StreetEmpty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Address)}.{nameof(StreetEmpty)}",
                errorCurrentValue: value,
                errorMessage: "거리명이 비어있습니다.");

        public static Error PostalCodeEmpty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Address)}.{nameof(PostalCodeEmpty)}",
                errorCurrentValue: value,
                errorMessage: "우편번호가 비어있습니다.");
    }
}
