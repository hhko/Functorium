using EcommerceFiltering;
using EcommerceFiltering.Domain;
using EcommerceFiltering.Domain.Specifications;
using EcommerceFiltering.Domain.ValueObjects;
using EcommerceFiltering.Infrastructure;

Console.WriteLine("=== 전자상거래 상품 필터링 ===\n");

IProductRepository repository = new InMemoryProductRepository(SampleProducts.All);

// 1. 카테고리별 필터링
Console.WriteLine("--- 전자기기 카테고리 ---");
var electronicsSpec = new ProductCategorySpec(new Category("전자기기"));
foreach (var product in repository.FindAll(electronicsSpec))
    Console.WriteLine($"  {product.Name} - {product.Price} (재고: {product.Stock.Value}개)");

Console.WriteLine();

// 2. 가격 범위 필터링
Console.WriteLine("--- 10만원~100만원 상품 ---");
var priceRangeSpec = new ProductPriceRangeSpec(new Money(100_000m), new Money(1_000_000m));
foreach (var product in repository.FindAll(priceRangeSpec))
    Console.WriteLine($"  {product.Name} - {product.Price}");

Console.WriteLine();

// 3. 재고 부족 상품
Console.WriteLine("--- 재고 5개 미만 상품 ---");
var lowStockSpec = new ProductLowStockSpec(new Quantity(5));
foreach (var product in repository.FindAll(lowStockSpec))
    Console.WriteLine($"  {product.Name} (재고: {product.Stock.Value}개)");

Console.WriteLine();

// 4. 복합 조건: 전자기기 AND 재고 있음 AND 100만원 이하
Console.WriteLine("--- 전자기기 & 재고 있음 & 100만원 이하 ---");
var compositeSpec = electronicsSpec
    & new ProductInStockSpec()
    & new ProductPriceRangeSpec(new Money(0m), new Money(1_000_000m));
foreach (var product in repository.FindAll(compositeSpec))
    Console.WriteLine($"  {product.Name} - {product.Price} (재고: {product.Stock.Value}개)");

Console.WriteLine();

// 5. 상품명 존재 여부 확인
var nameSpec = new ProductNameUniqueSpec(new ProductName("맥북 프로 16인치"));
Console.WriteLine($"'맥북 프로 16인치' 존재 여부: {repository.Exists(nameSpec)}");

var unknownNameSpec = new ProductNameUniqueSpec(new ProductName("갤럭시 탭"));
Console.WriteLine($"'갤럭시 탭' 존재 여부: {repository.Exists(unknownNameSpec)}");
