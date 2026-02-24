using System.Linq.Expressions;
using Functorium.Domains.Specifications.Expressions;
using SpecificationPattern.Demo.Domain;
using SpecificationPattern.Demo.Intermediate;

namespace SpecificationPattern.Demo.Advanced;

/// <summary>DB 테이블에 대응하는 퍼시스턴스 모델.</summary>
public sealed class ProductDbModel
{
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public string CategoryCode { get; set; } = "";
}

public static class Advanced02_PropertyMap
{
    public static void Run()
    {
        Console.WriteLine("=== Advanced02: 엔티티→모델 Expression 변환 ===");
        Console.WriteLine();

        // 1) PropertyMap 정의: Product → ProductDbModel
        var map = new PropertyMap<Product, ProductDbModel>()
            .Map(p => p.Name, m => m.ProductName)
            .Map(p => p.Price, m => m.UnitPrice)
            .Map(p => p.Stock, m => m.StockQuantity)
            .Map(p => p.Category, m => m.CategoryCode);

        // 2) 도메인 Expression 정의
        var inStock = new InStockExprSpec();
        Expression<Func<Product, bool>> domainExpr = inStock.ToExpression();
        Console.WriteLine($"▶ 도메인 Expression: {domainExpr.Body}");

        // 3) 모델 Expression으로 변환
        var modelExpr = map.Translate(domainExpr);
        Console.WriteLine($"▶ 모델 Expression:  {modelExpr.Body}");

        Console.WriteLine();

        // 4) 복합 Expression 변환
        var composite = new InStockExprSpec() & new PriceRangeExprSpec(0, 10_000);
        var compositeExpr = SpecificationExpressionResolver.TryResolve(composite);
        if (compositeExpr is not null)
        {
            var compositeModelExpr = map.Translate(compositeExpr);
            Console.WriteLine($"▶ 복합 도메인 Expression: {compositeExpr.Body}");
            Console.WriteLine($"▶ 복합 모델 Expression:  {compositeModelExpr.Body}");
        }

        Console.WriteLine();

        // 5) EF Core 시뮬레이션: 모델 컬렉션에 대해 실행
        Console.WriteLine("▶ DbContext 시뮬레이션:");
        var dbModels = CreateDbModels();
        var filtered = dbModels.AsQueryable().Where(modelExpr);
        foreach (var m in filtered)
            Console.WriteLine($"  {m.ProductName} (재고: {m.StockQuantity})");
    }

    private static List<ProductDbModel> CreateDbModels() =>
    [
        new() { ProductName = "무선 마우스", UnitPrice = 15_000, StockQuantity = 50, CategoryCode = "전자제품" },
        new() { ProductName = "기계식 키보드", UnitPrice = 89_000, StockQuantity = 30, CategoryCode = "전자제품" },
        new() { ProductName = "프리미엄 만년필", UnitPrice = 120_000, StockQuantity = 0, CategoryCode = "문구류" },
        new() { ProductName = "모니터 암", UnitPrice = 45_000, StockQuantity = 0, CategoryCode = "가구" },
    ];
}
