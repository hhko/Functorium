using Composition;
using Composition.Specifications;

Console.WriteLine("=== Specification 조합 ===\n");

var products = new[]
{
    new Product("노트북", 1_200_000m, 5, "전자제품"),
    new Product("마우스", 25_000m, 0, "전자제품"),
    new Product("키보드", 89_000m, 3, "주변기기"),
    new Product("모니터", 350_000m, 2, "전자제품"),
    new Product("USB 케이블", 5_000m, 10, "주변기기"),
};

var inStock = new ProductInStockSpec();
var affordable = new ProductPriceRangeSpec(10_000m, 100_000m);
var electronics = new ProductCategorySpec("전자제품");

// And 조합: 재고 있고 AND 저렴한 상품
var inStockAndAffordable = inStock.And(affordable);
Console.WriteLine("--- 재고 있고 저렴한 상품 (And) ---");
foreach (var p in products.Where(p => inStockAndAffordable.IsSatisfiedBy(p)))
    Console.WriteLine($"  {p.Name} (가격: {p.Price:N0}원, 재고: {p.Stock})");

Console.WriteLine();

// Or 조합: 전자제품이거나 OR 저렴한 상품
var electronicsOrAffordable = electronics.Or(affordable);
Console.WriteLine("--- 전자제품이거나 저렴한 상품 (Or) ---");
foreach (var p in products.Where(p => electronicsOrAffordable.IsSatisfiedBy(p)))
    Console.WriteLine($"  {p.Name} ({p.Category}, 가격: {p.Price:N0}원)");

Console.WriteLine();

// Not 조합: 전자제품이 아닌 상품
var notElectronics = electronics.Not();
Console.WriteLine("--- 전자제품이 아닌 상품 (Not) ---");
foreach (var p in products.Where(p => notElectronics.IsSatisfiedBy(p)))
    Console.WriteLine($"  {p.Name} ({p.Category})");
