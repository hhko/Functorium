using Functorium.Domains.Specifications;
using SpecificationPattern.Demo.Domain;

namespace SpecificationPattern.Demo.Basic;

// --- Specification 정의 ---

/// <summary>재고가 있는 상품.</summary>
public sealed class InStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}

/// <summary>가격 범위 내 상품.</summary>
public sealed class PriceRangeSpec(decimal min, decimal max) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Price >= min && entity.Price <= max;
}

// --- Demo ---

public static class Basic01_SimpleSpec
{
    public static void Run()
    {
        Console.WriteLine("=== Basic01: Specification 정의와 평가 ===");
        Console.WriteLine();

        var products = SampleProducts.Create();
        var inStock = new InStockSpec();
        var affordable = new PriceRangeSpec(0, 10_000);

        Console.WriteLine("▶ 재고 있는 상품:");
        foreach (var p in products.Where(inStock.IsSatisfiedBy))
            Console.WriteLine($"  {p.Name} (재고: {p.Stock})");

        Console.WriteLine();
        Console.WriteLine("▶ 1만원 이하 상품:");
        foreach (var p in products.Where(affordable.IsSatisfiedBy))
            Console.WriteLine($"  {p.Name} ({p.Price:N0}원)");
    }
}
