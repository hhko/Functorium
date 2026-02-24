using System.Linq.Expressions;
using ExpressionResolver;
using ExpressionResolver.Specifications;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;

Console.WriteLine("=== Expression Resolver ===\n");

var products = new List<Product>
{
    new("노트북", 1_500_000, 10, "전자제품"),
    new("마우스", 25_000, 50, "전자제품"),
    new("품절 키보드", 80_000, 0, "전자제품"),
    new("볼펜", 500, 100, "문구류"),
};

// --- 1) 단일 ExpressionSpecification → Expression 추출 ---
Console.WriteLine("▶ 단일 ExpressionSpec → Expression:");
var inStock = new InStockExprSpec();
var expr1 = SpecificationExpressionResolver.TryResolve(inStock);
Console.WriteLine($"  Body: {expr1?.Body}");

Console.WriteLine();

// --- 2) And 복합 → 합성된 Expression ---
Console.WriteLine("▶ And 복합 → Expression:");
Specification<Product> andSpec = new InStockExprSpec() & new PriceRangeExprSpec(0, 50_000);
var expr2 = SpecificationExpressionResolver.TryResolve(andSpec);
Console.WriteLine($"  Body: {expr2?.Body}");

Console.WriteLine();

// --- 3) Or 복합 → 합성된 Expression ---
Console.WriteLine("▶ Or 복합 → Expression:");
Specification<Product> orSpec = new InStockExprSpec() | new CategoryExprSpec("문구류");
var expr3 = SpecificationExpressionResolver.TryResolve(orSpec);
Console.WriteLine($"  Body: {expr3?.Body}");

Console.WriteLine();

// --- 4) Not → 부정 Expression ---
Console.WriteLine("▶ Not → Expression:");
Specification<Product> notSpec = !new PriceRangeExprSpec(50_000, decimal.MaxValue);
var expr4 = SpecificationExpressionResolver.TryResolve(notSpec);
Console.WriteLine($"  Body: {expr4?.Body}");

Console.WriteLine();

// --- 5) Non-expression Spec → null (graceful fallback) ---
Console.WriteLine("▶ Non-expression Spec → null:");
var nonExprSpec = new InStockSpec();
var expr5 = SpecificationExpressionResolver.TryResolve(nonExprSpec);
Console.WriteLine($"  Result is null: {expr5 is null}");

Console.WriteLine();

// --- 6) AsQueryable 시뮬레이션 ---
if (expr2 is not null)
{
    Console.WriteLine("▶ AsQueryable 시뮬레이션 (재고 있고 5만원 이하):");
    var results = products.AsQueryable().Where(expr2);
    foreach (var p in results)
        Console.WriteLine($"  {p.Name} ({p.Price:N0}원, 재고: {p.Stock})");
}
