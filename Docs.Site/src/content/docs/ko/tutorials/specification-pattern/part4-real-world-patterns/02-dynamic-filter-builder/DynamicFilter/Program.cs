using DynamicFilter;

Console.WriteLine("=== Dynamic Filter Builder ===\n");

var products = SampleProducts.All;

// 1. 빈 요청 (전체 조회)
var emptyRequest = new SearchProductsRequest();
var allSpec = ProductFilterBuilder.Build(emptyRequest);
Console.WriteLine($"빈 요청 - IsAll: {allSpec.IsAll}");
var allResults = products.Where(allSpec.IsSatisfiedBy).ToList();
Console.WriteLine($"결과: {allResults.Count}개 (전체)\n");

// 2. 이름 검색
var nameRequest = new SearchProductsRequest(Name: "Keyboard");
var nameSpec = ProductFilterBuilder.Build(nameRequest);
var nameResults = products.Where(nameSpec.IsSatisfiedBy).ToList();
Console.WriteLine($"이름 'Keyboard' 검색:");
foreach (var p in nameResults)
    Console.WriteLine($"  - {p.Name} (${p.Price})");

// 3. 복합 필터
Console.WriteLine("\nElectronics, $50~$500, 재고 있음:");
var complexRequest = new SearchProductsRequest(
    Category: "Electronics",
    MinPrice: 50,
    MaxPrice: 500,
    InStockOnly: true);
var complexSpec = ProductFilterBuilder.Build(complexRequest);
var complexResults = products.Where(complexSpec.IsSatisfiedBy).ToList();
foreach (var p in complexResults)
    Console.WriteLine($"  - {p.Name} (${p.Price}, 재고: {p.Stock})");

// 4. 모든 필터 적용
Console.WriteLine("\n모든 필터 (이름 'Mouse', Electronics, $10~$100, 재고 있음):");
var fullRequest = new SearchProductsRequest(
    Name: "Mouse",
    Category: "Electronics",
    MinPrice: 10,
    MaxPrice: 100,
    InStockOnly: true);
var fullSpec = ProductFilterBuilder.Build(fullRequest);
var fullResults = products.Where(fullSpec.IsSatisfiedBy).ToList();
foreach (var p in fullResults)
    Console.WriteLine($"  - {p.Name} (${p.Price}, 재고: {p.Stock})");
