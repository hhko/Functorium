using FirstSpecification;
using FirstSpecification.Specifications;

Console.WriteLine("=== 첫 번째 Specification ===\n");

var products = new[]
{
    new Product("노트북", 1_200_000m, 5, "전자제품"),
    new Product("마우스", 25_000m, 0, "전자제품"),
    new Product("키보드", 89_000m, 3, "전자제품"),
    new Product("모니터", 350_000m, 0, "전자제품"),
};

var inStock = new ProductInStockSpec();
var midRange = new ProductPriceRangeSpec(50_000m, 500_000m);

Console.WriteLine("--- 재고 있는 상품 ---");
foreach (var p in products.Where(p => inStock.IsSatisfiedBy(p)))
    Console.WriteLine($"  {p.Name} (재고: {p.Stock})");

Console.WriteLine();

Console.WriteLine("--- 중간 가격대 상품 (50,000 ~ 500,000) ---");
foreach (var p in products.Where(p => midRange.IsSatisfiedBy(p)))
    Console.WriteLine($"  {p.Name} (가격: {p.Price:N0}원)");
