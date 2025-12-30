using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using Ardalis.SmartEnum;
using static LanguageExt.Prelude;

namespace EcommerceDomain;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 이커머스 도메인 값 객체 (Functorium 프레임워크 기반) ===\n");

        // 1. Money (금액)
        DemonstrateMoney();

        // 2. ProductCode (상품 코드)
        DemonstrateProductCode();

        // 3. Quantity (수량)
        DemonstrateQuantity();

        // 4. OrderStatus (주문 상태)
        DemonstrateOrderStatus();

        // 5. ShippingAddress (배송 주소)
        DemonstrateShippingAddress();
    }

    static void DemonstrateMoney()
    {
        Console.WriteLine("1. Money (금액) - ValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var price = Money.Create(10000, "KRW");
        var discount = Money.Create(1000, "KRW");

        (price, discount).Apply((p, d) =>
        {
            Console.WriteLine($"   상품 가격: {p}");
            Console.WriteLine($"   할인 금액: {d}");

            var final = p.Subtract(d);
            Console.WriteLine($"   최종 가격: {final}");

            return unit;
        });

        // 잘못된 통화 합산 시도
        (Money.Create(100, "USD"), Money.Create(1000, "KRW")).Apply((usd, krw) =>
        {
            try
            {
                var _ = usd.Add(krw);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"   다른 통화 합산 시도: {ex.Message}");
            }
            return unit;
        });
        Console.WriteLine();
    }

    static void DemonstrateProductCode()
    {
        Console.WriteLine("2. ProductCode (상품 코드) - SimpleValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var code = ProductCode.Create("EL-001234");
        code.Match(
            Succ: c =>
            {
                Console.WriteLine($"   상품 코드: {c}");
                Console.WriteLine($"   카테고리: {c.Category}");
                Console.WriteLine($"   번호: {c.Number}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        var invalid = ProductCode.Create("invalid");
        invalid.Match(
            Succ: _ => { },
            Fail: e => Console.WriteLine($"   잘못된 형식: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateQuantity()
    {
        Console.WriteLine("3. Quantity (수량) - ComparableSimpleValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var qty1 = Quantity.Create(5).IfFail(Quantity.Zero);
        var qty2 = Quantity.Create(3).IfFail(Quantity.Zero);

        Console.WriteLine($"   수량 1: {qty1}");
        Console.WriteLine($"   수량 2: {qty2}");
        Console.WriteLine($"   합계: {qty1 + qty2}");
        Console.WriteLine($"   비교: {qty1} > {qty2} = {qty1 > qty2}");

        // 정렬
        var quantities = new[] { qty1, qty2, Quantity.One };
        System.Array.Sort(quantities);
        Console.WriteLine($"   정렬: [{string.Join(", ", quantities.Select(q => q.Amount))}]");

        Console.WriteLine();
    }

    static void DemonstrateOrderStatus()
    {
        Console.WriteLine("4. OrderStatus (주문 상태) - SmartEnum");
        Console.WriteLine("─".PadRight(40, '─'));

        var status = OrderStatus.Pending;
        Console.WriteLine($"   현재 상태: {status.DisplayName}");
        Console.WriteLine($"   취소 가능: {status.CanCancel}");

        // 상태 전이
        var confirmed = status.TransitionTo(OrderStatus.Confirmed);
        confirmed.Match(
            Succ: s =>
            {
                Console.WriteLine($"   전이 후: {s.DisplayName}");

                var shipped = s.TransitionTo(OrderStatus.Shipped);
                shipped.Match(
                    Succ: s2 => Console.WriteLine($"   배송 중: {s2.DisplayName}, 취소 가능: {s2.CanCancel}"),
                    Fail: e => Console.WriteLine($"   전이 실패: {e.Message}")
                );
            },
            Fail: e => Console.WriteLine($"   전이 실패: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateShippingAddress()
    {
        Console.WriteLine("5. ShippingAddress (배송 주소) - ValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var address = ShippingAddress.Create(
            "홍길동",
            "테헤란로 123",
            "서울",
            "06234",
            "KR"
        );

        address.Match(
            Succ: a =>
            {
                Console.WriteLine($"   수령인: {a.RecipientName}");
                Console.WriteLine($"   주소: {a.Street}, {a.City}");
                Console.WriteLine($"   우편번호: {a.PostalCode}");
                Console.WriteLine($"   국가: {a.Country}");
            },
            Fail: error => Console.WriteLine($"   검증 오류: {error.Message}")
        );

        // 잘못된 주소 (모든 오류 수집)
        var invalid = ShippingAddress.Create("", "", "", "", "");
        invalid.Match(
            Succ: _ => { },
            Fail: error => Console.WriteLine($"\n   빈 주소 검증 결과: {error.Message}")
        );

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 구현 (Functorium 프레임워크 기반)
// ========================================

/// <summary>
/// Money 값 객체 (ValueObject 기반)
/// </summary>
public sealed class Money : ValueObject, IComparable<Money>
{
    // 1.1 속성 선언
    public decimal Amount { get; }
    public string Currency { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Money> Create(decimal amount, string? currency) =>
        CreateFromValidation(
            Validate(amount, currency ?? ""),
            validValues => new Money(validValues.Amount, validValues.Currency.ToUpperInvariant()));

    // 5. Public Validate 메서드 - 독립 검증 규칙들을 병렬로 실행
    public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
        (ValidateAmountNotNegative(amount), ValidateCurrencyNotEmpty(currency), ValidateCurrencyLength(currency))
            .Apply((validAmount, validCurrency, _) => (validAmount, validCurrency))
            .As();

    // 5.1 금액 검증
    private static Validation<Error, decimal> ValidateAmountNotNegative(decimal amount) =>
        amount >= 0
            ? amount
            : DomainErrors.NegativeAmount(amount);

    // 5.2 통화 코드 빈 값 검증
    private static Validation<Error, string> ValidateCurrencyNotEmpty(string currency) =>
        !string.IsNullOrWhiteSpace(currency)
            ? currency
            : DomainErrors.EmptyCurrency(currency);

    // 5.3 통화 코드 길이 검증
    private static Validation<Error, string> ValidateCurrencyLength(string currency) =>
        !string.IsNullOrWhiteSpace(currency) && currency.Length == 3
            ? currency
            : DomainErrors.InvalidCurrencyLength(currency);

    // 도메인 메서드
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("다른 통화끼리 합산할 수 없습니다.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("다른 통화끼리 뺄 수 없습니다.");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        if (Currency != other.Currency)
            throw new InvalidOperationException("다른 통화끼리 비교할 수 없습니다.");
        return Amount.CompareTo(other.Amount);
    }

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N0} {Currency}";

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error NegativeAmount(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(NegativeAmount)}",
                errorCurrentValue: value,
                errorMessage: $"Amount cannot be negative. Current value: '{value}'");

        public static Error EmptyCurrency(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(EmptyCurrency)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code cannot be empty. Current value: '{value}'");

        public static Error InvalidCurrencyLength(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(InvalidCurrencyLength)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code must be exactly 3 characters. Current value: '{value}'");
    }
}

/// <summary>
/// ProductCode 값 객체 (SimpleValueObject 기반)
/// </summary>
public sealed class ProductCode : SimpleValueObject<string>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private ProductCode(string value) : base(value) { }

    /// <summary>
    /// 상품 코드에 대한 public 접근자
    /// </summary>
    public string Code => Value;

    // 파생 속성
    public string Category => Value[..2];
    public string Number => Value[3..];

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<ProductCode> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? ""),
            validValue => new ProductCode(validValue));

    // 5. Public Validate 메서드 - 순차 검증 (형식 검증은 빈 값 검증에 의존)
    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateFormat(value))
            .Map(normalized => normalized);

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    // 5.2 형식 검증
    private static Validation<Error, string> ValidateFormat(string value)
    {
        var normalized = value.ToUpperInvariant().Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z]{2}-\d{6}$")
            ? normalized
            : DomainErrors.InvalidFormat(value);
    }

    public static implicit operator string(ProductCode code) => code.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ProductCode)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Product code cannot be empty. Current value: '{value}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ProductCode)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid product code format. Expected format: 'XX-NNNNNN' (e.g., EL-001234). Current value: '{value}'");
    }
}

/// <summary>
/// Quantity 값 객체 (ComparableSimpleValueObject 기반)
/// </summary>
public sealed class Quantity : ComparableSimpleValueObject<int>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private Quantity(int value) : base(value) { }

    /// <summary>
    /// 수량 값에 대한 public 접근자
    /// </summary>
    public int Amount => Value;

    // 팩토리 속성
    public static Quantity Zero => new(0);
    public static Quantity One => new(1);

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Quantity(validValue));

    // 5. Public Validate 메서드 - 순차 검증 (범위 검증은 의존성이 있음)
    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotExceedsLimit(value))
            .Map(_ => value);

    // 5.1 음수 검증
    private static Validation<Error, int> ValidateNotNegative(int value) =>
        value >= 0
            ? value
            : DomainErrors.Negative(value);

    // 5.2 최대값 검증
    private static Validation<Error, int> ValidateNotExceedsLimit(int value) =>
        value <= 10000
            ? value
            : DomainErrors.ExceedsLimit(value);

    // 도메인 메서드
    public Quantity Add(Quantity other) => new(Value + other.Value);
    public Quantity Subtract(Quantity other) => new(Math.Max(0, Value - other.Value));

    public static Quantity operator +(Quantity a, Quantity b) => a.Add(b);
    public static Quantity operator -(Quantity a, Quantity b) => a.Subtract(b);

    public static implicit operator int(Quantity quantity) => quantity.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Negative(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Quantity)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: $"Quantity cannot be negative. Current value: '{value}'");

        public static Error ExceedsLimit(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Quantity)}.{nameof(ExceedsLimit)}",
                errorCurrentValue: value,
                errorMessage: $"Quantity cannot exceed 10,000. Current value: '{value}'");
    }
}

/// <summary>
/// OrderStatus 값 객체 (SmartEnum 기반)
/// </summary>
public sealed class OrderStatus : SmartEnum<OrderStatus, string>
{
    public static readonly OrderStatus Pending = new("PENDING", "대기중", canCancel: true);
    public static readonly OrderStatus Confirmed = new("CONFIRMED", "확인됨", canCancel: true);
    public static readonly OrderStatus Shipped = new("SHIPPED", "배송중", canCancel: false);
    public static readonly OrderStatus Delivered = new("DELIVERED", "배송완료", canCancel: false);
    public static readonly OrderStatus Cancelled = new("CANCELLED", "취소됨", canCancel: false);

    public string DisplayName { get; }
    public bool CanCancel { get; }

    private OrderStatus(string value, string displayName, bool canCancel)
        : base(displayName, value)
    {
        DisplayName = displayName;
        CanCancel = canCancel;
    }

    public Fin<OrderStatus> TransitionTo(OrderStatus next)
    {
        return (this, next) switch
        {
            (var s, _) when s == Cancelled => DomainErrors.AlreadyCancelled(Value, next.Value),
            (var s, _) when s == Delivered => DomainErrors.AlreadyDelivered(Value, next.Value),
            (_, var n) when n == Pending => DomainErrors.CannotRevertToPending(Value, next.Value),
            _ => next
        };
    }

    internal static class DomainErrors
    {
        public static Error AlreadyCancelled(string current, string target) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderStatus)}.{nameof(AlreadyCancelled)}",
                current, target,
                errorMessage: $"Cannot change status of a cancelled order. Current status: '{current}', Target status: '{target}'");

        public static Error AlreadyDelivered(string current, string target) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderStatus)}.{nameof(AlreadyDelivered)}",
                current, target,
                errorMessage: $"Cannot change status of a delivered order. Current status: '{current}', Target status: '{target}'");

        public static Error CannotRevertToPending(string current, string target) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderStatus)}.{nameof(CannotRevertToPending)}",
                current, target,
                errorMessage: $"Cannot revert to pending status. Current status: '{current}', Target status: '{target}'");
    }
}

/// <summary>
/// ShippingAddress 값 객체 (ValueObject 기반)
/// </summary>
public sealed class ShippingAddress : ValueObject
{
    // 1.1 속성 선언
    public string RecipientName { get; }
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private ShippingAddress(string recipientName, string street, string city, string postalCode, string country)
    {
        RecipientName = recipientName;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<ShippingAddress> Create(
        string? recipientName, string? street, string? city, string? postalCode, string? country) =>
        CreateFromValidation(
            Validate(recipientName ?? "", street ?? "", city ?? "", postalCode ?? "", country ?? ""),
            validValues => new ShippingAddress(
                validValues.RecipientName.Trim(),
                validValues.Street.Trim(),
                validValues.City.Trim(),
                validValues.PostalCode,
                validValues.Country.Trim().ToUpperInvariant()));

    // 5. Public Validate 메서드 - 병렬 검증 후 순차 검증
    public static Validation<Error, (string RecipientName, string Street, string City, string PostalCode, string Country)> Validate(
        string recipientName, string street, string city, string postalCode, string country) =>
        (ValidateRecipientName(recipientName), ValidateStreet(street), ValidateCity(city), ValidateCountry(country))
            .Apply((validRecipient, validStreet, validCity, validCountry) => (validRecipient, validStreet, validCity, validCountry))
            .As()
            .Bind(values => ValidatePostalCode(postalCode)
                .Map(validPostal => (values.validRecipient, values.validStreet, values.validCity, validPostal, values.validCountry)));

    // 5.1 수령인 검증
    private static Validation<Error, string> ValidateRecipientName(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.EmptyRecipientName(value);

    // 5.2 도로명 검증
    private static Validation<Error, string> ValidateStreet(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.EmptyStreet(value);

    // 5.3 도시 검증
    private static Validation<Error, string> ValidateCity(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.EmptyCity(value);

    // 5.4 우편번호 검증
    private static Validation<Error, string> ValidatePostalCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.EmptyPostalCode(value);

        var normalized = value.Replace("-", "").Replace(" ", "");
        if (normalized.Length < 5 || normalized.Length > 10)
            return DomainErrors.InvalidPostalCodeFormat(value);

        return normalized;
    }

    // 5.5 국가 검증
    private static Validation<Error, string> ValidateCountry(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.EmptyCountry(value);

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RecipientName;
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error EmptyRecipientName(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ShippingAddress)}.{nameof(EmptyRecipientName)}",
                errorCurrentValue: value,
                errorMessage: $"Recipient name cannot be empty. Current value: '{value}'");

        public static Error EmptyStreet(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ShippingAddress)}.{nameof(EmptyStreet)}",
                errorCurrentValue: value,
                errorMessage: $"Street address cannot be empty. Current value: '{value}'");

        public static Error EmptyCity(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ShippingAddress)}.{nameof(EmptyCity)}",
                errorCurrentValue: value,
                errorMessage: $"City cannot be empty. Current value: '{value}'");

        public static Error EmptyPostalCode(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ShippingAddress)}.{nameof(EmptyPostalCode)}",
                errorCurrentValue: value,
                errorMessage: $"Postal code cannot be empty. Current value: '{value}'");

        public static Error EmptyCountry(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ShippingAddress)}.{nameof(EmptyCountry)}",
                errorCurrentValue: value,
                errorMessage: $"Country code cannot be empty. Current value: '{value}'");

        public static Error InvalidPostalCodeFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ShippingAddress)}.{nameof(InvalidPostalCodeFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid postal code format. Current value: '{value}'");
    }
}
