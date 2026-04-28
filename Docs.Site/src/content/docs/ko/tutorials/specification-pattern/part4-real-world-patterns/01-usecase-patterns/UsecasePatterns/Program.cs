using UsecasePatterns;
using UsecasePatterns.Usecases;

Console.WriteLine("=== Usecase Patterns: Specification 활용 ===\n");

// 샘플 데이터
var products = new List<Product>
{
    new("Laptop", 1500, 10, "Electronics"),
    new("Mouse", 25, 50, "Electronics"),
    new("Desk", 300, 0, "Furniture"),
    new("Chair", 200, 5, "Furniture"),
    new("Keyboard", 75, 30, "Electronics"),
};

var repository = new InMemoryProductRepository(products);

// --- Command: 중복 검사 ---
Console.WriteLine("--- Command: 상품 생성 (중복 검사) ---\n");

var createHandler = new CreateProductCommandHandler(repository);

var newProduct = new CreateProductCommand("Monitor", 500, 15, "Electronics");
var result1 = createHandler.Handle(newProduct);
Console.WriteLine($"'{newProduct.Name}' 생성: {(result1 ? "성공" : "실패 (중복)")}");

var duplicateProduct = new CreateProductCommand("Laptop", 2000, 5, "Electronics");
var result2 = createHandler.Handle(duplicateProduct);
Console.WriteLine($"'{duplicateProduct.Name}' 생성: {(result2 ? "성공" : "실패 (중복)")}");

// --- Query: 검색 필터 ---
Console.WriteLine("\n--- Query: 상품 검색 (동적 필터) ---\n");

var searchHandler = new SearchProductsQueryHandler(repository);

// 카테고리 필터만
var query1 = new SearchProductsQuery(Category: "Electronics", MinPrice: null, MaxPrice: null, InStockOnly: null);
var results1 = searchHandler.Handle(query1);
Console.WriteLine("Electronics 카테고리:");
foreach (var p in results1)
    Console.WriteLine($"  - {p.Name} (${p.Price}, 재고: {p.Stock})");

// 가격 범위 + 재고 있는 상품
var query2 = new SearchProductsQuery(Category: null, MinPrice: 50, MaxPrice: 500, InStockOnly: true);
var results2 = searchHandler.Handle(query2);
Console.WriteLine("\n가격 $50~$500, 재고 있음:");
foreach (var p in results2)
    Console.WriteLine($"  - {p.Name} (${p.Price}, 재고: {p.Stock})");

// 필터 없음 (전체 조회)
var query3 = new SearchProductsQuery(Category: null, MinPrice: null, MaxPrice: null, InStockOnly: null);
var results3 = searchHandler.Handle(query3);
Console.WriteLine($"\n필터 없음 (전체): {results3.Count()}개 상품");
