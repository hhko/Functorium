using Functorium.Applications.Queries;
using QueryUsecase;

Console.WriteLine("=== Query Usecase 패턴 ===\n");

// 1. 테스트 데이터 준비
var query = new InMemoryProductQuery();
query.Add(Product.Create("노트북 Pro", 2_000_000m));
query.Add(Product.Create("노트북 Air", 1_500_000m));
query.Add(Product.Create("마우스", 25_000m));

// 2. Usecase 구성
var usecase = new SearchProductsQuery.Usecase(query);

// 3. Query 실행
var request = new SearchProductsQuery.Request("노트북", new PageRequest(1, 10), SortExpression.Empty);
var response = await usecase.Handle(request, CancellationToken.None);

// 4. 결과 확인
response.Match(
    Succ: r =>
    {
        Console.WriteLine($"검색 결과: {r.Products.TotalCount}건");
        foreach (var p in r.Products.Items)
            Console.WriteLine($"  - {p.Name} ({p.Price:N0}원)");
    },
    Fail: e => Console.WriteLine($"검색 실패: {e}"));
