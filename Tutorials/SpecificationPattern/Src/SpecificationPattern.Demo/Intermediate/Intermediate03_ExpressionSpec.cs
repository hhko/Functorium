using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using SpecificationPattern.Demo.Domain;

namespace SpecificationPattern.Demo.Intermediate;

// --- Expression 기반 Specification 정의 ---

/// <summary>재고가 있는 상품 (Expression 기반).</summary>
public sealed class InStockExprSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Stock > 0;
}

/// <summary>가격 범위 내 상품 (Expression 기반).</summary>
public sealed class PriceRangeExprSpec(decimal min, decimal max) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Price >= min && p.Price <= max;
}

/// <summary>특정 카테고리 상품 (Expression 기반).</summary>
public sealed class CategoryExprSpec(string category) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Category == category;
}

// --- Demo ---

public static class Intermediate03_ExpressionSpec
{
    public static void Run()
    {
        Console.WriteLine("=== Intermediate03: Expression 기반 Specification ===");
        Console.WriteLine();

        var products = SampleProducts.Create();
        var inStock = new InStockExprSpec();

        // IsSatisfiedBy는 sealed — Expression 컴파일 후 캐싱
        Console.WriteLine("▶ ExpressionSpecification.IsSatisfiedBy (자동 컴파일):");
        foreach (var p in products.Where(inStock.IsSatisfiedBy))
            Console.WriteLine($"  {p.Name} (재고: {p.Stock})");

        Console.WriteLine();

        // ToExpression()으로 Expression Tree 직접 사용
        var affordable = new PriceRangeExprSpec(0, 10_000);
        Expression<Func<Product, bool>> expr = affordable.ToExpression();

        Console.WriteLine("▶ Expression Tree 직접 사용 (AsQueryable + Where):");
        var queryable = products.AsQueryable().Where(expr);
        foreach (var p in queryable)
            Console.WriteLine($"  {p.Name} ({p.Price:N0}원)");

        Console.WriteLine();

        // Expression Tree 출력
        Console.WriteLine($"▶ Expression Body: {expr.Body}");
    }
}
