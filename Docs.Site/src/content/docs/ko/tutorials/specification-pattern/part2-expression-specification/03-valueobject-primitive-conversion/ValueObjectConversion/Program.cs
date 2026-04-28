using ValueObjectConversion;
using ValueObjectConversion.Specifications;

Console.WriteLine("=== Value Object → Primitive 변환 ===\n");

var products = new List<Product>
{
    new(new ProductName("노트북"), new Money(1_500_000), new Quantity(10), "전자제품"),
    new(new ProductName("마우스"), new Money(25_000), new Quantity(50), "전자제품"),
    new(new ProductName("품절 키보드"), new Money(80_000), new Quantity(0), "전자제품"),
    new(new ProductName("볼펜"), new Money(500), new Quantity(3), "문구류"),
};

// --- ProductNameSpec ---
Console.WriteLine("▶ ProductNameSpec - 이름으로 검색:");
var nameSpec = new ProductNameSpec(new ProductName("노트북"));
foreach (var p in products.Where(nameSpec.IsSatisfiedBy))
    Console.WriteLine($"  {p.Name} ({p.Price.Amount:N0}원)");

Console.WriteLine();

// --- ProductPriceRangeSpec ---
Console.WriteLine("▶ ProductPriceRangeSpec - 가격 범위:");
var priceSpec = new ProductPriceRangeSpec(new Money(1_000), new Money(100_000));
foreach (var p in products.Where(priceSpec.IsSatisfiedBy))
    Console.WriteLine($"  {p.Name} ({p.Price.Amount:N0}원)");

Console.WriteLine();

// --- ProductLowStockSpec ---
Console.WriteLine("▶ ProductLowStockSpec - 재고 부족 (5개 이하):");
var lowStockSpec = new ProductLowStockSpec(new Quantity(5));
foreach (var p in products.Where(lowStockSpec.IsSatisfiedBy))
    Console.WriteLine($"  {p.Name} (재고: {p.Stock.Value})");

Console.WriteLine();

// --- Expression Tree 확인 ---
Console.WriteLine("▶ Expression Tree 확인:");
Console.WriteLine($"  NameSpec: {nameSpec.ToExpression().Body}");
Console.WriteLine($"  PriceSpec: {priceSpec.ToExpression().Body}");
Console.WriteLine($"  LowStockSpec: {lowStockSpec.ToExpression().Body}");
