using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;
using Ardalis.SmartEnum;
using static LanguageExt.Prelude;

namespace EcommerceDomain;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 이커머스 도메인 값 객체 ===\n");

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
        Console.WriteLine("1. Money (금액) - ComparableValueObject");
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
                var _ = usd.Add(krw);  // 예외 발생
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
        Console.WriteLine($"   정렬: [{string.Join(", ", quantities.Select(q => q.Value))}]");

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

                // 배송 완료로 전이
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
// 값 객체 구현
// ========================================

public sealed class Money : IComparable<Money>, IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Fin<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return DomainErrors.NegativeAmount(amount);
        if (string.IsNullOrWhiteSpace(currency))
            return DomainErrors.EmptyCurrency(currency ?? "");
        if (currency.Length != 3)
            return DomainErrors.InvalidCurrencyLength(currency);
        return new Money(amount, currency.ToUpperInvariant());
    }

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

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public override string ToString() => $"{Amount:N0} {Currency}";

    public static bool operator ==(Money? left, Money? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(Money? left, Money? right) => !(left == right);

    internal static class DomainErrors
    {
        public static Error NegativeAmount(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Money)}.{nameof(NegativeAmount)}",
                errorCurrentValue: value,
                errorMessage: "금액은 음수일 수 없습니다.");

        public static Error EmptyCurrency(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Money)}.{nameof(EmptyCurrency)}",
                errorCurrentValue: value,
                errorMessage: "통화 코드가 비어있습니다.");

        public static Error InvalidCurrencyLength(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Money)}.{nameof(InvalidCurrencyLength)}",
                errorCurrentValue: value,
                errorMessage: "통화 코드는 3자여야 합니다.");
    }
}

public sealed class ProductCode : IEquatable<ProductCode>
{
    public string Value { get; }

    private ProductCode(string value) => Value = value;

    public static Fin<ProductCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "");

        var normalized = value.ToUpperInvariant().Trim();

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z]{2}-\d{6}$"))
            return DomainErrors.InvalidFormat(value);

        return new ProductCode(normalized);
    }

    public string Category => Value[..2];
    public string Number => Value[3..];

    public bool Equals(ProductCode? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is ProductCode other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(ProductCode code) => code.Value;

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ProductCode)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "상품 코드가 비어있습니다.");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ProductCode)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: "상품 코드 형식이 올바르지 않습니다. (예: EL-001234)");
    }
}

public sealed class Quantity : IComparable<Quantity>, IEquatable<Quantity>
{
    public int Value { get; }

    private Quantity(int value) => Value = value;

    public static Fin<Quantity> Create(int value)
    {
        if (value < 0)
            return DomainErrors.Negative(value);
        if (value > 10000)
            return DomainErrors.ExceedsLimit(value);
        return new Quantity(value);
    }

    public static Quantity Zero => new(0);
    public static Quantity One => new(1);

    public Quantity Add(Quantity other) => new(Value + other.Value);
    public Quantity Subtract(Quantity other) => new(Math.Max(0, Value - other.Value));

    public static Quantity operator +(Quantity a, Quantity b) => a.Add(b);
    public static Quantity operator -(Quantity a, Quantity b) => a.Subtract(b);

    public int CompareTo(Quantity? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public bool Equals(Quantity? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Quantity other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator <(Quantity left, Quantity right) => left.CompareTo(right) < 0;
    public static bool operator >(Quantity left, Quantity right) => left.CompareTo(right) > 0;
    public static bool operator <=(Quantity left, Quantity right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Quantity left, Quantity right) => left.CompareTo(right) >= 0;

    public static implicit operator int(Quantity quantity) => quantity.Value;

    internal static class DomainErrors
    {
        public static Error Negative(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Quantity)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: "수량은 음수일 수 없습니다.");

        public static Error ExceedsLimit(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Quantity)}.{nameof(ExceedsLimit)}",
                errorCurrentValue: value,
                errorMessage: "수량은 10,000을 초과할 수 없습니다.");
    }
}

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
                errorCode: $"{nameof(OrderStatus)}.{nameof(AlreadyCancelled)}",
                current, target,
                errorMessage: "이미 취소된 주문은 상태를 변경할 수 없습니다.");

        public static Error AlreadyDelivered(string current, string target) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(OrderStatus)}.{nameof(AlreadyDelivered)}",
                current, target,
                errorMessage: "이미 배송 완료된 주문은 상태를 변경할 수 없습니다.");

        public static Error CannotRevertToPending(string current, string target) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(OrderStatus)}.{nameof(CannotRevertToPending)}",
                current, target,
                errorMessage: "대기중 상태로 되돌릴 수 없습니다.");
    }
}

public sealed class ShippingAddress : IEquatable<ShippingAddress>
{
    public string RecipientName { get; }
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private ShippingAddress(string recipientName, string street, string city, string postalCode, string country)
    {
        RecipientName = recipientName;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public static Fin<ShippingAddress> Create(
        string recipientName, string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
            return DomainErrors.EmptyRecipientName(recipientName ?? "");
        if (string.IsNullOrWhiteSpace(street))
            return DomainErrors.EmptyStreet(street ?? "");
        if (string.IsNullOrWhiteSpace(city))
            return DomainErrors.EmptyCity(city ?? "");
        if (string.IsNullOrWhiteSpace(postalCode))
            return DomainErrors.EmptyPostalCode(postalCode ?? "");
        if (string.IsNullOrWhiteSpace(country))
            return DomainErrors.EmptyCountry(country ?? "");

        var normalizedPostal = postalCode.Replace("-", "").Replace(" ", "");
        if (normalizedPostal.Length < 5 || normalizedPostal.Length > 10)
            return DomainErrors.InvalidPostalCodeFormat(postalCode);

        return new ShippingAddress(
            recipientName.Trim(),
            street.Trim(),
            city.Trim(),
            normalizedPostal,
            country.Trim().ToUpperInvariant()
        );
    }

    public bool Equals(ShippingAddress? other) =>
        other is not null &&
        RecipientName == other.RecipientName &&
        Street == other.Street &&
        City == other.City &&
        PostalCode == other.PostalCode &&
        Country == other.Country;

    public override bool Equals(object? obj) => obj is ShippingAddress other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(RecipientName, Street, City, PostalCode, Country);

    internal static class DomainErrors
    {
        public static Error EmptyRecipientName(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ShippingAddress)}.{nameof(EmptyRecipientName)}",
                errorCurrentValue: value,
                errorMessage: "수령인 이름이 비어있습니다.");

        public static Error EmptyStreet(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ShippingAddress)}.{nameof(EmptyStreet)}",
                errorCurrentValue: value,
                errorMessage: "도로명 주소가 비어있습니다.");

        public static Error EmptyCity(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ShippingAddress)}.{nameof(EmptyCity)}",
                errorCurrentValue: value,
                errorMessage: "도시명이 비어있습니다.");

        public static Error EmptyPostalCode(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ShippingAddress)}.{nameof(EmptyPostalCode)}",
                errorCurrentValue: value,
                errorMessage: "우편번호가 비어있습니다.");

        public static Error EmptyCountry(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ShippingAddress)}.{nameof(EmptyCountry)}",
                errorCurrentValue: value,
                errorMessage: "국가 코드가 비어있습니다.");

        public static Error InvalidPostalCodeFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ShippingAddress)}.{nameof(InvalidPostalCodeFormat)}",
                errorCurrentValue: value,
                errorMessage: "우편번호 형식이 올바르지 않습니다.");
    }
}
