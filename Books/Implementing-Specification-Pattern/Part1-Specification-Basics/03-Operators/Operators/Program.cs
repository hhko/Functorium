using Operators;
using Operators.Specifications;

Console.WriteLine("=== Specification 연산자 ===\n");

var products = new[]
{
    new Product("노트북", 1_200_000m, 5, "전자제품"),
    new Product("마우스", 25_000m, 0, "전자제품"),
    new Product("키보드", 89_000m, 3, "주변기기"),
    new Product("모니터", 350_000m, 2, "전자제품"),
    new Product("USB 케이블", 5_000m, 10, "주변기기"),
};

var inStock = new InStockSpec();
var affordable = new PriceRangeSpec(10_000m, 100_000m);
var electronics = new CategorySpec("전자제품");

// 메서드 방식
var method = inStock.And(affordable).And(electronics.Not());

// 연산자 방식 (동일한 결과)
var op = inStock & affordable & !electronics;

Console.WriteLine("--- 메서드 방식: inStock.And(affordable).And(electronics.Not()) ---");
foreach (var p in products.Where(p => method.IsSatisfiedBy(p)))
    Console.WriteLine($"  {p.Name}");

Console.WriteLine();

Console.WriteLine("--- 연산자 방식: inStock & affordable & !electronics ---");
foreach (var p in products.Where(p => op.IsSatisfiedBy(p)))
    Console.WriteLine($"  {p.Name}");

Console.WriteLine();
Console.WriteLine("두 방식의 결과가 동일합니다.");
