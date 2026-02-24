using Functorium.Domains.Specifications;
using SpecificationPattern.Demo.Basic;
using SpecificationPattern.Demo.Domain;
using SpecificationPattern.Demo.Infrastructure;

namespace SpecificationPattern.Demo.Intermediate;

public static class Intermediate02_WithRepository
{
    public static void Run()
    {
        Console.WriteLine("=== Intermediate02: Repository + Specification 연동 ===");
        Console.WriteLine();

        var repository = new InMemoryProductRepository(SampleProducts.Create());

        // Repository + Specification: 도메인 규칙과 데이터 접근 분리
        var spec = new InStockSpec() & new PriceRangeSpec(0, 10_000);

        Console.WriteLine("▶ Repository.FindAll(재고 있고 1만원 이하):");
        foreach (var p in repository.FindAll(spec))
            Console.WriteLine($"  {p.Name} ({p.Price:N0}원, 재고: {p.Stock})");

        Console.WriteLine();

        // 같은 Specification을 다른 조건으로 재사용
        var electronics = new CategorySpec("전자제품") & new InStockSpec();
        Console.WriteLine("▶ Repository.FindAll(전자제품 + 재고 있음):");
        foreach (var p in repository.FindAll(electronics))
            Console.WriteLine($"  {p.Name} ({p.Category}, 재고: {p.Stock})");

        Console.WriteLine();

        // Exists: 중복 검사
        var expensiveFurniture = new CategorySpec("가구") & new PriceRangeSpec(100_000, decimal.MaxValue);
        Console.WriteLine($"▶ 10만원 이상 가구 존재: {repository.Exists(expensiveFurniture)}");

        var cheapFurniture = new CategorySpec("가구") & new PriceRangeSpec(0, 10_000);
        Console.WriteLine($"▶ 1만원 이하 가구 존재: {repository.Exists(cheapFurniture)}");
    }
}
