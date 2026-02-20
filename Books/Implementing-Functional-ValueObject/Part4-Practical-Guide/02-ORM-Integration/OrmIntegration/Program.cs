using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;
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
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public string Address => Value;

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    public static Email CreateFromValidated(string value) => new(value);

    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant())
            .As();

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<Email>(new DomainErrorType.Empty(), value ?? "null",
                $"Email address cannot be empty. Current value: '{value}'");

    private static Validation<Error, string> ValidateFormat(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@')
            ? value
            : DomainError.For<Email>(new DomainErrorType.InvalidFormat(), value ?? "null",
                $"Invalid email format. Current value: '{value}'");

    public static implicit operator string(Email email) => email.Value;
}

/// <summary>
/// ProductCode 값 객체 (SimpleValueObject 기반)
/// EF Core Value Converter로 매핑
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class ProductCode : SimpleValueObject<string>
{
    private ProductCode(string value) : base(value) { }

    public string Code => Value;

    public static Fin<ProductCode> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new ProductCode(validValue));

    public static ProductCode CreateFromValidated(string value) => new(value);

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateFormat(value))
            .Map(normalized => normalized);

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<ProductCode>(new DomainErrorType.Empty(), value ?? "null",
                $"Product code cannot be empty. Current value: '{value}'");

    private static Validation<Error, string> ValidateFormat(string value)
    {
        var normalized = value.ToUpperInvariant().Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z]{2}-\d{6}$")
            ? normalized
            : DomainError.For<ProductCode>(new DomainErrorType.InvalidFormat(), value,
                $"Invalid product code format. Expected: 'XX-NNNNNN'. Current value: '{value}'");
    }

    public static implicit operator string(ProductCode code) => code.Value;
}

/// <summary>
/// Address 값 객체 (ValueObject 기반)
/// EF Core OwnsOne 패턴으로 매핑
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Address : ValueObject
{
    public sealed record CityEmpty : DomainErrorType.Custom;
    public sealed record StreetEmpty : DomainErrorType.Custom;
    public sealed record PostalCodeEmpty : DomainErrorType.Custom;

    public string City { get; private set; }
    public string Street { get; private set; }
    public string PostalCode { get; private set; }

    private Address()
    {
        City = string.Empty;
        Street = string.Empty;
        PostalCode = string.Empty;
    }

    private Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    public static Fin<Address> Create(string? city, string? street, string? postalCode) =>
        CreateFromValidation(
            Validate(city ?? "null", street ?? "null", postalCode ?? "null"),
            validValues => new Address(validValues.City.Trim(), validValues.Street.Trim(), validValues.PostalCode.Trim()));

    public static Validation<Error, (string City, string Street, string PostalCode)> Validate(
        string city, string street, string postalCode) =>
        (ValidateCityNotEmpty(city), ValidateStreetNotEmpty(street), ValidatePostalCodeNotEmpty(postalCode))
            .Apply((validCity, validStreet, validPostalCode) => (validCity, validStreet, validPostalCode))
            .As();

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

/// <summary>
/// Money 값 객체 (ValueObject 기반)
/// EF Core OwnsOne 패턴으로 매핑
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Money : ValueObject
{
    public sealed record EmptyCurrency : DomainErrorType.Custom;
    public sealed record InvalidCurrencyLength : DomainErrorType.Custom;

    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    private Money()
    {
        Amount = 0;
        Currency = "KRW";
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Fin<Money> Create(decimal amount, string? currency) =>
        CreateFromValidation(
            Validate(amount, currency ?? "null"),
            validValues => new Money(validValues.Amount, validValues.Currency.ToUpperInvariant()));

    public static Money CreateFromValidated(decimal amount, string currency) =>
        new(amount, currency.ToUpperInvariant());

    public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
        (ValidateAmountNotNegative(amount), ValidateCurrencyNotEmpty(currency), ValidateCurrencyLength(currency))
            .Apply((validAmount, validCurrency, _) => (validAmount, validCurrency))
            .As();

    private static Validation<Error, decimal> ValidateAmountNotNegative(decimal amount) =>
        amount >= 0
            ? amount
            : DomainError.For<Money, decimal>(new DomainErrorType.Negative(), amount,
                $"Amount cannot be negative. Current value: '{amount}'");

    private static Validation<Error, string> ValidateCurrencyNotEmpty(string currency) =>
        !string.IsNullOrWhiteSpace(currency)
            ? currency
            : DomainError.For<Money>(new EmptyCurrency(), currency ?? "null",
                $"Currency cannot be empty. Current value: '{currency}'");

    private static Validation<Error, string> ValidateCurrencyLength(string currency) =>
        !string.IsNullOrWhiteSpace(currency) && currency.Length == 3
            ? currency
            : DomainError.For<Money>(new InvalidCurrencyLength(), currency ?? "null",
                $"Currency must be 3 characters. Current value: '{currency}'");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}

/// <summary>
/// OrderLineItem 값 객체 (ValueObject 기반)
/// EF Core OwnsMany 패턴으로 매핑
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class OrderLineItem : ValueObject
{
    public string Name { get; private set; }
    public int Qty { get; private set; }
    public decimal Price { get; private set; }

    private OrderLineItem()
    {
        Name = string.Empty;
        Qty = 0;
        Price = 0;
    }

    private OrderLineItem(string name, int qty, decimal price)
    {
        Name = name;
        Qty = qty;
        Price = price;
    }

    public static Fin<OrderLineItem> Create(string? name, int qty, decimal price) =>
        CreateFromValidation(
            Validate(name ?? "null", qty, price),
            validValues => new OrderLineItem(validValues.Name.Trim(), validValues.Qty, validValues.Price));

    public static OrderLineItem CreateFromValidated(string name, int qty, decimal price) =>
        new(name, qty, price);

    public static Validation<Error, (string Name, int Qty, decimal Price)> Validate(string name, int qty, decimal price) =>
        (ValidateNameNotEmpty(name), ValidateQuantityPositive(qty), ValidatePriceNotNegative(price))
            .Apply((validName, validQty, validPrice) => (validName, validQty, validPrice))
            .As();

    private static Validation<Error, string> ValidateNameNotEmpty(string name) =>
        !string.IsNullOrWhiteSpace(name)
            ? name
            : DomainError.For<OrderLineItem>(new DomainErrorType.Empty(), name ?? "null",
                $"Product name cannot be empty. Current value: '{name}'");

    private static Validation<Error, int> ValidateQuantityPositive(int qty) =>
        qty > 0
            ? qty
            : DomainError.For<OrderLineItem, int>(new DomainErrorType.NotPositive(), qty,
                $"Quantity must be positive. Current value: '{qty}'");

    private static Validation<Error, decimal> ValidatePriceNotNegative(decimal price) =>
        price >= 0
            ? price
            : DomainError.For<OrderLineItem, decimal>(new DomainErrorType.Negative(), price,
                $"Price cannot be negative. Current value: '{price}'");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Qty;
        yield return Price;
    }

    public override string ToString() => $"{Name} x {Qty} @ {Price:N0}";
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
