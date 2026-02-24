using EfCoreImpl;
using EfCoreImpl.Specifications;

Console.WriteLine("=== EF Core 시뮬레이션: 전체 파이프라인 ===\n");

// 1) DB 데이터 (ProductDbModel)
var dbModels = new List<ProductDbModel>
{
    new("무선 마우스", 15_000, 50, "전자제품"),
    new("기계식 키보드", 89_000, 30, "전자제품"),
    new("USB 케이블", 3_000, 200, "전자제품"),
    new("볼펜 세트", 5_000, 100, "문구류"),
    new("노트", 2_000, 150, "문구류"),
    new("프리미엄 만년필", 120_000, 0, "문구류"),
    new("에르고 의자", 350_000, 5, "가구"),
    new("모니터 암", 45_000, 0, "가구"),
};

// 2) PropertyMap + SimulatedEfCoreRepository 생성
var propertyMap = ProductPropertyMap.Create();
var repository = new SimulatedEfCoreProductRepository(dbModels, propertyMap);

// 3) 도메인 Specification으로 조회 (Repository는 자동으로 Expression 변환)
Console.WriteLine("--- FindAll(재고 있는 상품) ---");
var inStock = new InStockExprSpec();
foreach (var p in repository.FindAll(inStock))
    Console.WriteLine($"  {p.Name} ({p.Price:N0}원, 재고: {p.Stock})");

Console.WriteLine();

// 4) 복합 Specification
Console.WriteLine("--- FindAll(재고 있고 1만원 이하) ---");
var affordable = new InStockExprSpec() & new PriceRangeExprSpec(0, 10_000);
foreach (var p in repository.FindAll(affordable))
    Console.WriteLine($"  {p.Name} ({p.Price:N0}원, 재고: {p.Stock})");

Console.WriteLine();

// 5) 카테고리 + 재고
Console.WriteLine("--- FindAll(전자제품 + 재고 있음) ---");
var electronics = new CategoryExprSpec("전자제품") & new InStockExprSpec();
foreach (var p in repository.FindAll(electronics))
    Console.WriteLine($"  {p.Name} ({p.Category})");

Console.WriteLine();

// 6) Exists
var expensiveFurniture = new CategoryExprSpec("가구") & new PriceRangeExprSpec(100_000, decimal.MaxValue);
Console.WriteLine($"--- 10만원 이상 가구 존재: {repository.Exists(expensiveFurniture)} ---");

Console.WriteLine();

// 7) Open-Closed Principle: 새 Specification 추가 시 Repository 변경 불필요
Console.WriteLine("--- Open-Closed Principle ---");
Console.WriteLine("  새로운 Specification을 추가해도 Repository 코드는 변경 불필요!");
Console.WriteLine("  SimulatedEfCoreProductRepository.BuildQuery는 어떤 Specification이든 처리 가능");
