using InMemoryImpl;
using InMemoryImpl.Specifications;

Console.WriteLine("=== InMemory Repository 구현 ===\n");

var repository = new InMemoryProductRepository(SampleProducts.Create());

// FindAll: 재고 있는 상품
var inStock = new InStockSpec();
Console.WriteLine("--- FindAll(재고 있는 상품) ---");
foreach (var p in repository.FindAll(inStock))
    Console.WriteLine($"  {p.Name} (재고: {p.Stock})");

Console.WriteLine();

// FindAll: 재고 있고 1만원 이하
var affordable = inStock & new PriceRangeSpec(0, 10_000);
Console.WriteLine("--- FindAll(재고 있고 1만원 이하) ---");
foreach (var p in repository.FindAll(affordable))
    Console.WriteLine($"  {p.Name} ({p.Price:N0}원, 재고: {p.Stock})");

Console.WriteLine();

// FindAll: 전자제품 + 재고 있음
var electronics = new CategorySpec("전자제품") & inStock;
Console.WriteLine("--- FindAll(전자제품 + 재고 있음) ---");
foreach (var p in repository.FindAll(electronics))
    Console.WriteLine($"  {p.Name} ({p.Category}, 재고: {p.Stock})");

Console.WriteLine();

// Exists: 존재 여부 확인
var expensiveFurniture = new CategorySpec("가구") & new PriceRangeSpec(100_000, decimal.MaxValue);
Console.WriteLine($"--- 10만원 이상 가구 존재: {repository.Exists(expensiveFurniture)} ---");

var cheapFurniture = new CategorySpec("가구") & new PriceRangeSpec(0, 10_000);
Console.WriteLine($"--- 1만원 이하 가구 존재: {repository.Exists(cheapFurniture)} ---");
