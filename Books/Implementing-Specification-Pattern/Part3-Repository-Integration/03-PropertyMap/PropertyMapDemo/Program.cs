using System.Linq.Expressions;
using Functorium.Domains.Specifications.Expressions;
using PropertyMapDemo;
using PropertyMapDemo.Specifications;

Console.WriteLine("=== PropertyMap: 엔티티 -> 모델 Expression 변환 ===\n");

// 1) PropertyMap 정의
var map = ProductPropertyMap.Create();

// 2) 필드명 변환 확인
Console.WriteLine("--- 필드명 변환 ---");
Console.WriteLine($"  Name -> {map.TranslateFieldName("Name")}");
Console.WriteLine($"  Price -> {map.TranslateFieldName("Price")}");
Console.WriteLine($"  Stock -> {map.TranslateFieldName("Stock")}");
Console.WriteLine($"  Category -> {map.TranslateFieldName("Category")}");

Console.WriteLine();

// 3) 단일 Expression 변환
var inStock = new InStockExprSpec();
Expression<Func<Product, bool>> domainExpr = inStock.ToExpression();
var modelExpr = map.Translate(domainExpr);

Console.WriteLine("--- 단일 Expression 변환 ---");
Console.WriteLine($"  도메인: {domainExpr.Body}");
Console.WriteLine($"  모델:   {modelExpr.Body}");

Console.WriteLine();

// 4) 복합 Expression 변환
var composite = new InStockExprSpec() & new PriceRangeExprSpec(0, 10_000);
var compositeExpr = SpecificationExpressionResolver.TryResolve(composite);
if (compositeExpr is not null)
{
    var compositeModelExpr = map.Translate(compositeExpr);
    Console.WriteLine("--- 복합 Expression 변환 ---");
    Console.WriteLine($"  도메인: {compositeExpr.Body}");
    Console.WriteLine($"  모델:   {compositeModelExpr.Body}");
}

Console.WriteLine();

// 5) 변환된 Expression으로 DbModel 필터링
Console.WriteLine("--- 변환된 Expression으로 DbModel 필터링 ---");
var dbModels = new List<ProductDbModel>
{
    new("무선 마우스", 15_000, 50, "전자제품"),
    new("기계식 키보드", 89_000, 30, "전자제품"),
    new("프리미엄 만년필", 120_000, 0, "문구류"),
    new("모니터 암", 45_000, 0, "가구"),
};

foreach (var m in dbModels.AsQueryable().Where(modelExpr))
    Console.WriteLine($"  {m.ProductName} (재고: {m.StockQuantity})");
