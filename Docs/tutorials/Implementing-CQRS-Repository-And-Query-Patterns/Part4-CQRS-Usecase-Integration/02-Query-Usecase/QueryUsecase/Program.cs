using QueryUsecase;

Console.WriteLine("=== Query Usecase 패턴 ===\n");

// 1. 테스트 데이터 준비
var products = new List<Product>
{
    Product.Create("노트북 Pro", 2_000_000m),
    Product.Create("노트북 Air", 1_500_000m),
    Product.Create("마우스", 25_000m),
};

// 2. Query 어댑터 및 Usecase 구성
var query = new InMemoryProductQuery(products);
var usecase = new SearchProductsQuery.Usecase(query);

// 3. Query 실행
var request = new SearchProductsQuery.Request("노트북");
var response = await usecase.Handle(request);

// 4. 결과 확인
response.Match(
    Succ: r =>
    {
        Console.WriteLine($"검색 결과: {r.Products.Count}건");
        foreach (var p in r.Products)
            Console.WriteLine($"  - {p.Name} ({p.Price:N0}원)");
    },
    Fail: e => Console.WriteLine($"검색 실패: {e}"));
