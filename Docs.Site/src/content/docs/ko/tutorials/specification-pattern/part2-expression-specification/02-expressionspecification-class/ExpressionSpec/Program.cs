using System.Linq.Expressions;
using ExpressionSpec;
using ExpressionSpec.Specifications;

Console.WriteLine("=== ExpressionSpecification 클래스 ===\n");

var products = new List<Product>
{
    new("노트북", 1_500_000, 10, "전자제품"),
    new("마우스", 25_000, 50, "전자제품"),
    new("품절 키보드", 80_000, 0, "전자제품"),
    new("볼펜", 500, 100, "문구류"),
};

// --- IsSatisfiedBy: sealed 메서드로 자동 컴파일 + 캐싱 ---
Console.WriteLine("▶ ProductInStockSpec - IsSatisfiedBy (자동 컴파일):");
var inStock = new ProductInStockSpec();
foreach (var p in products.Where(inStock.IsSatisfiedBy))
    Console.WriteLine($"  {p.Name} (재고: {p.Stock})");

Console.WriteLine();

// --- ToExpression: Expression Tree 직접 사용 ---
Console.WriteLine("▶ ProductPriceRangeSpec - ToExpression (IQueryable 사용):");
var affordable = new ProductPriceRangeSpec(0, 50_000);
Expression<Func<Product, bool>> expr = affordable.ToExpression();
Console.WriteLine($"  Expression Body: {expr.Body}");

var queryResults = products.AsQueryable().Where(expr);
foreach (var p in queryResults)
    Console.WriteLine($"  {p.Name} ({p.Price:N0}원)");

Console.WriteLine();

// --- ProductCategorySpec ---
Console.WriteLine("▶ ProductCategorySpec - 카테고리 필터:");
var electronics = new ProductCategorySpec("전자제품");
foreach (var p in products.Where(electronics.IsSatisfiedBy))
    Console.WriteLine($"  {p.Name}");
