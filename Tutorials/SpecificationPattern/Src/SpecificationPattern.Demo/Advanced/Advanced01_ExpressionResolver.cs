using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using SpecificationPattern.Demo.Basic;
using SpecificationPattern.Demo.Domain;
using SpecificationPattern.Demo.Intermediate;

namespace SpecificationPattern.Demo.Advanced;

public static class Advanced01_ExpressionResolver
{
    public static void Run()
    {
        Console.WriteLine("=== Advanced01: 복합 Expression 해석 ===");
        Console.WriteLine();

        // 1) 단일 ExpressionSpecification → Expression 추출
        var inStock = new InStockExprSpec();
        var expr1 = SpecificationExpressionResolver.TryResolve(inStock);
        Console.WriteLine($"▶ 단일 ExpressionSpec → Expression: {expr1?.Body}");

        Console.WriteLine();

        // 2) And/Or 복합 → 합성된 Expression Tree
        var composite = new InStockExprSpec() & new PriceRangeExprSpec(0, 10_000);
        var expr2 = SpecificationExpressionResolver.TryResolve(composite);
        Console.WriteLine($"▶ And 복합 → Expression: {expr2?.Body}");

        Console.WriteLine();

        // 3) Not → Expression
        Specification<Product> notExpensive = !new PriceRangeExprSpec(50_000, decimal.MaxValue);
        var expr3 = SpecificationExpressionResolver.TryResolve(notExpensive);
        Console.WriteLine($"▶ Not → Expression: {expr3?.Body}");

        Console.WriteLine();

        // 4) non-expression spec 혼합 → null (graceful fallback)
        var mixed = new InStockSpec() & new PriceRangeExprSpec(0, 10_000);
        var expr4 = SpecificationExpressionResolver.TryResolve(mixed);
        Console.WriteLine($"▶ non-expression 혼합 → null: {expr4 is null}");

        Console.WriteLine();

        // 5) EF Core 어댑터 시뮬레이션
        Console.WriteLine("▶ EF Core 어댑터 패턴:");
        Console.WriteLine("  1. Repository.FindAll(spec) 호출");
        Console.WriteLine("  2. Adapter가 TryResolve(spec) 시도");
        Console.WriteLine("  3. Expression 추출 성공 → DbContext.Set<T>().Where(expr) (SQL 변환)");
        Console.WriteLine("  4. Expression 추출 실패 → 전체 로드 후 IsSatisfiedBy로 메모리 필터링");

        if (expr2 is not null)
        {
            Console.WriteLine();
            Console.WriteLine("▶ AsQueryable 시뮬레이션:");
            var products = SampleProducts.Create();
            var results = products.AsQueryable().Where(expr2);
            foreach (var p in results)
                Console.WriteLine($"  {p.Name} ({p.Price:N0}원, 재고: {p.Stock})");
        }
    }
}
