using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using Microsoft.EntityFrameworkCore;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace OrmIntegration;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== ORM 통합 패턴 (Functorium 프레임워크 기반) ===\n");

        // 1. Value Converter 패턴 (SimpleValueObject)
        await DemonstrateValueConverterPattern();

        // 2. OwnsOne 패턴 (ValueObject)
        await DemonstrateOwnsOnePattern();

        // 3. OwnsMany 패턴 (ValueObject 컬렉션)
        await DemonstrateOwnsManyPattern();
    }

    static async Task DemonstrateValueConverterPattern()
    {
        Console.WriteLine("1. Value Converter 패턴 - SimpleValueObject 매핑");
        Console.WriteLine("─".PadRight(40, '─'));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("ValueConverterDemo")
            .Options;

        await using var context = new AppDbContext(options);

        var email = Email.Create("hong@example.com").Match(Succ: e => e, Fail: _ => throw new Exception());
        var code = ProductCode.Create("EL-001234").Match(Succ: c => c, Fail: _ => throw new Exception());
        var price = Money.Create(50000, "KRW").Match(Succ: m => m, Fail: _ => throw new Exception());

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "홍길동",
            Email = email
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = code,
            Price = price
        };

        context.Users.Add(user);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var loadedUser = await context.Users.FirstAsync();
        var loadedProduct = await context.Products.FirstAsync();

        Console.WriteLine($"   저장된 사용자: {loadedUser.Name}");
        Console.WriteLine($"   이메일: {loadedUser.Email}");
        Console.WriteLine($"   상품 코드: {loadedProduct.Code}");
        Console.WriteLine($"   가격: {loadedProduct.Price}");
        Console.WriteLine();
    }

    static async Task DemonstrateOwnsOnePattern()
    {
        Console.WriteLine("2. OwnsOne 패턴 - ValueObject 매핑");
        Console.WriteLine("─".PadRight(40, '─'));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OwnsOneDemo")
            .Options;

        await using var context = new AppDbContext(options);

        var address = Address.Create("서울", "강남구 테헤란로 123", "06234").Match(Succ: a => a, Fail: _ => throw new Exception());

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "김철수",
            ShippingAddress = address
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var loaded = await context.Customers.FirstAsync();
        Console.WriteLine($"   저장된 고객: {loaded.Name}");
        Console.WriteLine($"   배송 주소: {loaded.ShippingAddress}");
        Console.WriteLine();
    }

    static async Task DemonstrateOwnsManyPattern()
    {
        Console.WriteLine("3. OwnsMany 패턴 - ValueObject 컬렉션 매핑");
        Console.WriteLine("─".PadRight(40, '─'));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OwnsManyDemo")
            .Options;

        await using var context = new AppDbContext(options);

        var lineItem1 = OrderLineItem.Create("상품 A", 2, 10000).Match(Succ: i => i, Fail: _ => throw new Exception());
        var lineItem2 = OrderLineItem.Create("상품 B", 1, 25000).Match(Succ: i => i, Fail: _ => throw new Exception());

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = "김철수",
            LineItems = new List<OrderLineItem> { lineItem1, lineItem2 }
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var loaded = await context.Orders.FirstAsync();
        Console.WriteLine($"   주문자: {loaded.CustomerName}");
        Console.WriteLine($"   주문 항목:");
        foreach (var item in loaded.LineItems)
        {
            Console.WriteLine($"      - {item.Name}: {item.Qty}개 x {item.Price:N0}원");
        }
        Console.WriteLine();
    }
}

// ========================================
// 값 객체 정의 (Functorium 프레임워크 기반)
// ========================================

/// <summary>
/// Email 값 객체 (SimpleValueObject 기반)
/// EF Core Value Converter로 매핑
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

    /// <summary>
    /// 이미 검증된 값으로 생성 (DB 로드 시 사용)
    /// </summary>
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
/// ProductCode 값 객체 (SimpleValueObject 기반)
/// EF Core Value Converter로 매핑
/// </summary>
public sealed class ProductCode : SimpleValueObject<string>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private ProductCode(string value) : base(value) { }

    /// <summary>
    /// 상품 코드에 대한 public 접근자
    /// </summary>
    public string Code => Value;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<ProductCode> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new ProductCode(validValue));

    /// <summary>
    /// 이미 검증된 값으로 생성 (DB 로드 시 사용)
    /// </summary>
    internal static ProductCode CreateFromValidated(string value) => new(value);

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

    // 5.2 형식 검증 (정규식 패턴 매칭)
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
        // ValidateNotEmpty 메서드와 1:1 매핑되는 에러
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ProductCode)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Product code cannot be empty. Current value: '{value}'");

        // ValidateFormat 메서드와 1:1 매핑되는 에러
        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ProductCode)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid product code format. Expected: 'XX-NNNNNN'. Current value: '{value}'");
    }
}

/// <summary>
/// Address 값 객체 (ValueObject 기반)
/// EF Core OwnsOne 패턴으로 매핑
/// </summary>
public sealed class Address : ValueObject
{
    // 1.1 속성 선언
    public string City { get; private set; }
    public string Street { get; private set; }
    public string PostalCode { get; private set; }

    /// <summary>
    /// EF Core용 private 생성자
    /// </summary>
    private Address()
    {
        City = string.Empty;
        Street = string.Empty;
        PostalCode = string.Empty;
    }

    // 2. Private 생성자 - 단순 대입만 처리
    private Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Address> Create(string? city, string? street, string? postalCode) =>
        CreateFromValidation(
            Validate(city ?? "null", street ?? "null", postalCode ?? "null"),
            validValues => new Address(validValues.City.Trim(), validValues.Street.Trim(), validValues.PostalCode.Trim()));

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
            : DomainErrors.CityEmpty(city);

    // 5.2 도로명 검증
    private static Validation<Error, string> ValidateStreetNotEmpty(string street) =>
        !string.IsNullOrWhiteSpace(street)
            ? street
            : DomainErrors.StreetEmpty(street);

    // 5.3 우편번호 검증
    private static Validation<Error, string> ValidatePostalCodeNotEmpty(string postalCode) =>
        !string.IsNullOrWhiteSpace(postalCode)
            ? postalCode
            : DomainErrors.PostalCodeEmpty(postalCode);

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

/// <summary>
/// Money 값 객체 (ValueObject 기반)
/// EF Core OwnsOne 패턴으로 매핑
/// </summary>
public sealed class Money : ValueObject
{
    // 1.1 속성 선언
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    /// <summary>
    /// EF Core용 private 생성자
    /// </summary>
    private Money()
    {
        Amount = 0;
        Currency = "KRW";
    }

    // 2. Private 생성자 - 단순 대입만 처리
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Money> Create(decimal amount, string? currency) =>
        CreateFromValidation(
            Validate(amount, currency ?? "null"),
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
        // ValidateAmountNotNegative 메서드와 1:1 매핑되는 에러
        public static Error NegativeAmount(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(NegativeAmount)}",
                errorCurrentValue: value,
                errorMessage: $"Amount cannot be negative. Current value: '{value}'");

        // ValidateCurrencyNotEmpty 메서드와 1:1 매핑되는 에러
        public static Error EmptyCurrency(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(EmptyCurrency)}",
                errorCurrentValue: value,
                errorMessage: $"Currency cannot be empty. Current value: '{value}'");

        // ValidateCurrencyLength 메서드와 1:1 매핑되는 에러
        public static Error InvalidCurrencyLength(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(InvalidCurrencyLength)}",
                errorCurrentValue: value,
                errorMessage: $"Currency must be 3 characters. Current value: '{value}'");
    }
}

/// <summary>
/// OrderLineItem 값 객체 (ValueObject 기반)
/// EF Core OwnsMany 패턴으로 매핑
/// </summary>
public sealed class OrderLineItem : ValueObject
{
    // 1.1 속성 선언
    public string Name { get; private set; }
    public int Qty { get; private set; }
    public decimal Price { get; private set; }

    /// <summary>
    /// EF Core용 private 생성자
    /// </summary>
    private OrderLineItem()
    {
        Name = string.Empty;
        Qty = 0;
        Price = 0;
    }

    // 2. Private 생성자 - 단순 대입만 처리
    private OrderLineItem(string name, int qty, decimal price)
    {
        Name = name;
        Qty = qty;
        Price = price;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<OrderLineItem> Create(string? name, int qty, decimal price) =>
        CreateFromValidation(
            Validate(name ?? "null", qty, price),
            validValues => new OrderLineItem(validValues.Name.Trim(), validValues.Qty, validValues.Price));

    // 5. Public Validate 메서드 - 독립 검증 규칙들을 병렬로 실행
    public static Validation<Error, (string Name, int Qty, decimal Price)> Validate(string name, int qty, decimal price) =>
        (ValidateNameNotEmpty(name), ValidateQuantityPositive(qty), ValidatePriceNotNegative(price))
            .Apply((validName, validQty, validPrice) => (validName, validQty, validPrice))
            .As();

    // 5.1 상품명 검증
    private static Validation<Error, string> ValidateNameNotEmpty(string name) =>
        !string.IsNullOrWhiteSpace(name)
            ? name
            : DomainErrors.EmptyName(name);

    // 5.2 수량 검증
    private static Validation<Error, int> ValidateQuantityPositive(int qty) =>
        qty > 0
            ? qty
            : DomainErrors.InvalidQuantity(qty);

    // 5.3 가격 검증
    private static Validation<Error, decimal> ValidatePriceNotNegative(decimal price) =>
        price >= 0
            ? price
            : DomainErrors.NegativePrice(price);

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Qty;
        yield return Price;
    }

    public override string ToString() => $"{Name} x {Qty} @ {Price:N0}";

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidateNameNotEmpty 메서드와 1:1 매핑되는 에러
        public static Error EmptyName(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderLineItem)}.{nameof(EmptyName)}",
                errorCurrentValue: value,
                errorMessage: $"Product name cannot be empty. Current value: '{value}'");

        // ValidateQuantityPositive 메서드와 1:1 매핑되는 에러
        public static Error InvalidQuantity(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderLineItem)}.{nameof(InvalidQuantity)}",
                errorCurrentValue: value,
                errorMessage: $"Quantity must be positive. Current value: '{value}'");

        // ValidatePriceNotNegative 메서드와 1:1 매핑되는 에러
        public static Error NegativePrice(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(OrderLineItem)}.{nameof(NegativePrice)}",
                errorCurrentValue: value,
                errorMessage: $"Price cannot be negative. Current value: '{value}'");
    }
}

// ========================================
// 엔티티 정의
// ========================================

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Email Email { get; set; } = null!;
}

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address ShippingAddress { get; set; } = null!;
}

public class Product
{
    public Guid Id { get; set; }
    public ProductCode Code { get; set; } = null!;
    public Money Price { get; set; } = null!;
}

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderLineItem> LineItems { get; set; } = new();
}

// ========================================
// DbContext 설정
// ========================================

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // =============================================
        // Value Converter 패턴: SimpleValueObject 매핑
        // =============================================

        // Email: SimpleValueObject<string> -> string 변환
        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasConversion(
                email => (string)email,                    // 저장: 암시적 변환 사용
                value => Email.CreateFromValidated(value)); // 로드: 검증 없이 생성

        // ProductCode: SimpleValueObject<string> -> string 변환
        modelBuilder.Entity<Product>()
            .Property(p => p.Code)
            .HasConversion(
                code => (string)code,
                value => ProductCode.CreateFromValidated(value));

        // =============================================
        // OwnsOne 패턴: ValueObject 매핑
        // =============================================

        // Address: 복합 ValueObject (OwnsOne)
        modelBuilder.Entity<Customer>()
            .OwnsOne(c => c.ShippingAddress);

        // Money: 복합 ValueObject (OwnsOne)
        modelBuilder.Entity<Product>()
            .OwnsOne(p => p.Price);

        // =============================================
        // OwnsMany 패턴: ValueObject 컬렉션 매핑
        // =============================================

        // OrderLineItem: 컬렉션 ValueObject (OwnsMany)
        modelBuilder.Entity<Order>()
            .OwnsMany(o => o.LineItems);
    }
}
