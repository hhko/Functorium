using AllIdentity;
using AllIdentity.Specifications;
using Functorium.Domains.Specifications;

Console.WriteLine("=== All 항등원과 동적 필터 ===\n");

// All은 모든 엔터티를 만족한다
var all = Specification<Product>.All;
Console.WriteLine($"All.IsSatisfiedBy(아무 상품) = {all.IsSatisfiedBy(SampleProducts.All[0])}");
Console.WriteLine($"All.IsAll = {all.IsAll}");
Console.WriteLine();

// All & X == X (항등원)
var inStock = new ProductInStockSpec();
var combined = all & inStock;
Console.WriteLine($"All & ProductInStock은 ProductInStock과 동일 객체: {ReferenceEquals(combined, inStock)}");
Console.WriteLine();

// 동적 필터 패턴
Console.WriteLine("--- 동적 필터 구성 ---");
string? categoryFilter = "전자제품";
string? nameFilter = null;
bool onlyInStock = true;

var spec = Specification<Product>.All;

if (categoryFilter is not null)
    spec &= new ProductCategorySpec(categoryFilter);

if (nameFilter is not null)
    spec &= new ProductNameContainsSpec(nameFilter);

if (onlyInStock)
    spec &= new ProductInStockSpec();

Console.WriteLine($"필터: 카테고리={categoryFilter ?? "없음"}, 이름={nameFilter ?? "없음"}, 재고만={onlyInStock}");
Console.WriteLine("결과:");
foreach (var p in SampleProducts.All.Where(p => spec.IsSatisfiedBy(p)))
    Console.WriteLine($"  {p.Name} ({p.Category}, 가격: {p.Price:N0}원, 재고: {p.Stock})");
