using Functorium.Domains.Specifications;
using SpecificationPattern.Demo.Basic;
using SpecificationPattern.Demo.Domain;

namespace SpecificationPattern.Demo.Intermediate;

/// <summary>이름에 특정 문자열을 포함하는 상품.</summary>
public sealed class NameContainsSpec(string keyword) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity)
        => entity.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}

public static class Intermediate01_AllIdentity
{
    public static void Run()
    {
        Console.WriteLine("=== Intermediate01: All 항등원 + 동적 필터 ===");
        Console.WriteLine();

        var products = SampleProducts.Create();

        // All은 모든 엔터티를 만족
        var all = Specification<Product>.All;
        Console.WriteLine($"▶ All.IsAll: {all.IsAll}");
        Console.WriteLine($"▶ All로 필터링: {products.Count(all.IsSatisfiedBy)}개 (전체: {products.Count}개)");

        Console.WriteLine();

        // 항등원: All & X = X (참조 동일)
        var inStock = new InStockSpec();
        var combined = Specification<Product>.All & inStock;
        Console.WriteLine($"▶ All & InStock == InStock (참조 동일): {ReferenceEquals(combined, inStock)}");

        Console.WriteLine();

        // 실전 패턴: 조건부 필터 체이닝
        Console.WriteLine("▶ 동적 필터 체이닝 (이름: '마우스', 최대가격: 20000):");
        var filtered = BuildDynamicFilter(products, name: "마우스", maxPrice: 20_000);
        foreach (var p in filtered)
            Console.WriteLine($"  {p.Name} ({p.Price:N0}원)");

        Console.WriteLine();
        Console.WriteLine("▶ 동적 필터 체이닝 (조건 없음 → 전체 반환):");
        var all2 = BuildDynamicFilter(products, name: null, maxPrice: 0);
        Console.WriteLine($"  {all2.Count()}개 반환");
    }

    public static IEnumerable<Product> BuildDynamicFilter(
        List<Product> products, string? name, decimal maxPrice)
    {
        var spec = Specification<Product>.All;
        if (name is not null)
            spec &= new NameContainsSpec(name);
        if (maxPrice > 0)
            spec &= new PriceRangeSpec(0, maxPrice);

        return products.Where(spec.IsSatisfiedBy);
    }
}
