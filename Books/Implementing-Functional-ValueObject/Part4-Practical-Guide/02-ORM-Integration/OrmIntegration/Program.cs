using Functorium.Abstractions.Errors;
using Microsoft.EntityFrameworkCore;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace OrmIntegration;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== ORM 통합 패턴 ===\n");

        // 1. OwnsOne 패턴
        await DemonstrateOwnsOnePattern();

        // 2. Value Converter 패턴
        await DemonstrateValueConverterPattern();

        // 3. OwnsMany 패턴
        await DemonstrateOwnsManyPattern();
    }

    static async Task DemonstrateOwnsOnePattern()
    {
        Console.WriteLine("1. OwnsOne 패턴 - 복합 값 객체 매핑");
        Console.WriteLine("─".PadRight(40, '─'));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OwnsOneDemo")
            .Options;

        await using var context = new AppDbContext(options);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "홍길동",
            Email = Email.CreateFromValidated("hong@example.com"),
            Address = new Address("서울", "강남구 테헤란로 123", "06234")
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loaded = await context.Users.FirstAsync();
        Console.WriteLine($"   저장된 사용자: {loaded.Name}");
        Console.WriteLine($"   이메일: {loaded.Email}");
        Console.WriteLine($"   주소: {loaded.Address}");
        Console.WriteLine();
    }

    static async Task DemonstrateValueConverterPattern()
    {
        Console.WriteLine("2. Value Converter 패턴 - 단일 값 객체 변환");
        Console.WriteLine("─".PadRight(40, '─'));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("ValueConverterDemo")
            .Options;

        await using var context = new AppDbContext(options);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = ProductCode.CreateFromValidated("EL-001234"),
            Price = Money.CreateFromValidated(50000, "KRW")
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var loaded = await context.Products.FirstAsync();
        Console.WriteLine($"   상품 코드: {loaded.Code}");
        Console.WriteLine($"   가격: {loaded.Price}");
        Console.WriteLine();
    }

    static async Task DemonstrateOwnsManyPattern()
    {
        Console.WriteLine("3. OwnsMany 패턴 - 컬렉션 값 객체 매핑");
        Console.WriteLine("─".PadRight(40, '─'));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OwnsManyDemo")
            .Options;

        await using var context = new AppDbContext(options);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = "김철수",
            LineItems = new List<OrderLineItem>
            {
                new("상품 A", 2, 10000),
                new("상품 B", 1, 25000)
            }
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var loaded = await context.Orders.FirstAsync();
        Console.WriteLine($"   주문자: {loaded.CustomerName}");
        Console.WriteLine($"   주문 항목:");
        foreach (var item in loaded.LineItems)
        {
            Console.WriteLine($"      - {item.ProductName}: {item.Quantity}개 x {item.UnitPrice:N0}원");
        }
        Console.WriteLine();
    }
}

// ========================================
// 값 객체 정의
// ========================================

public sealed class Email
{
    public string Value { get; private set; }

    private Email() => Value = string.Empty;
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");
        if (!value.Contains('@'))
            return DomainErrors.InvalidFormat(value);
        return new Email(value.ToLowerInvariant());
    }

    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

    public override string ToString() => Value;

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

public sealed class Address
{
    public string City { get; private set; }
    public string Street { get; private set; }
    public string PostalCode { get; private set; }

    private Address()
    {
        City = string.Empty;
        Street = string.Empty;
        PostalCode = string.Empty;
    }

    public Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    public override string ToString() => $"{City} {Street} ({PostalCode})";
}

public sealed class ProductCode
{
    public string Value { get; private set; }

    private ProductCode() => Value = string.Empty;
    private ProductCode(string value) => Value = value;

    public static ProductCode CreateFromValidated(string value) => new(value);

    public override string ToString() => Value;
}

public sealed class Money
{
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

    public static Money CreateFromValidated(decimal amount, string currency) => new(amount, currency);

    public override string ToString() => $"{Amount:N0} {Currency}";
}

public class OrderLineItem
{
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private OrderLineItem()
    {
        ProductName = string.Empty;
        Quantity = 0;
        UnitPrice = 0;
    }

    public OrderLineItem(string productName, int quantity, decimal unitPrice)
    {
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
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
    public Address Address { get; set; } = null!;
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
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // OwnsOne 패턴: Email 값 객체
        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Email);

        // OwnsOne 패턴: Address 복합 값 객체
        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Address);

        // Value Converter 패턴: ProductCode
        modelBuilder.Entity<Product>()
            .Property(p => p.Code)
            .HasConversion(
                code => code.Value,
                value => ProductCode.CreateFromValidated(value));

        // OwnsOne 패턴: Money
        modelBuilder.Entity<Product>()
            .OwnsOne(p => p.Price);

        // OwnsMany 패턴: OrderLineItem 컬렉션
        modelBuilder.Entity<Order>()
            .OwnsMany(o => o.LineItems);
    }
}
