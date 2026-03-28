using Functorium.Domains.Specifications;
using SpecificationPattern.Demo.Domain;

namespace SpecificationPattern.Demo.Basic;

/// <summary>특정 카테고리 상품.</summary>
public sealed class CategorySpec(string category) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity)
        => entity.Category.Equals(category, StringComparison.OrdinalIgnoreCase);
}

public static class Basic02_Composition
{
    public static void Run()
    {
        Console.WriteLine("=== Basic02: And, Or, Not 조합 ===");
        Console.WriteLine();

        var products = SampleProducts.Create();

        // And: 재고 있고 1만원 이하
        var inStockAndAffordable = new InStockSpec().And(new PriceRangeSpec(0, 10_000));
        Console.WriteLine("▶ 재고 있고 1만원 이하:");
        foreach (var p in products.Where(inStockAndAffordable.IsSatisfiedBy))
            Console.WriteLine($"  {p.Name} ({p.Price:N0}원, 재고: {p.Stock})");

        Console.WriteLine();

        // Or: 전자제품이거나 5천원 이하
        var electronicsOrCheap = new CategorySpec("전자제품").Or(new PriceRangeSpec(0, 5_000));
        Console.WriteLine("▶ 전자제품이거나 5천원 이하:");
        foreach (var p in products.Where(electronicsOrCheap.IsSatisfiedBy))
            Console.WriteLine($"  {p.Name} ({p.Category}, {p.Price:N0}원)");

        Console.WriteLine();

        // Not: 재고 없는 상품
        var outOfStock = new InStockSpec().Not();
        Console.WriteLine("▶ 재고 없는 상품:");
        foreach (var p in products.Where(outOfStock.IsSatisfiedBy))
            Console.WriteLine($"  {p.Name} (재고: {p.Stock})");
    }
}
