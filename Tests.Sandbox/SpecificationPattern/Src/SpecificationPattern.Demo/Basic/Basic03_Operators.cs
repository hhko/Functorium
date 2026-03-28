using Functorium.Domains.Specifications;
using SpecificationPattern.Demo.Domain;

namespace SpecificationPattern.Demo.Basic;

public static class Basic03_Operators
{
    public static void Run()
    {
        Console.WriteLine("=== Basic03: 연산자로 동일 표현 ===");
        Console.WriteLine();

        var products = SampleProducts.Create();

        // 메서드 vs 연산자
        var methodResult = new InStockSpec().And(new PriceRangeSpec(0, 10_000));
        var operatorResult = new InStockSpec() & new PriceRangeSpec(0, 10_000);

        var methodFiltered = products.Where(methodResult.IsSatisfiedBy).ToList();
        var operatorFiltered = products.Where(operatorResult.IsSatisfiedBy).ToList();

        Console.WriteLine("▶ 메서드: spec.And(other)");
        foreach (var p in methodFiltered)
            Console.WriteLine($"  {p.Name}");

        Console.WriteLine();
        Console.WriteLine("▶ 연산자: spec & other");
        foreach (var p in operatorFiltered)
            Console.WriteLine($"  {p.Name}");

        Console.WriteLine();
        Console.WriteLine($"▶ 결과 동일: {methodFiltered.SequenceEqual(operatorFiltered)}");

        Console.WriteLine();

        // Not 연산자
        var notMethod = new InStockSpec().Not();
        var notOperator = !new InStockSpec();

        var notMethodFiltered = products.Where(notMethod.IsSatisfiedBy).ToList();
        var notOperatorFiltered = products.Where(notOperator.IsSatisfiedBy).ToList();

        Console.WriteLine($"▶ Not 결과 동일: {notMethodFiltered.SequenceEqual(notOperatorFiltered)}");
    }
}
