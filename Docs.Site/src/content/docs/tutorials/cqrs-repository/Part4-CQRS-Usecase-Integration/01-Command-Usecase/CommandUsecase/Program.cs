using CommandUsecase;

Console.WriteLine("=== Command Usecase 패턴 ===\n");

// 1. 의존성 구성
var eventCollector = new NoOpDomainEventCollector();
var repository = new InMemoryProductRepository(eventCollector);
var usecase = new CreateProductCommand.Usecase(repository);

// 2. Command 실행
var request = new CreateProductCommand.Request("노트북", 1_500_000m);
var response = await usecase.Handle(request);

// 3. 결과 확인
response.Match(
    Succ: r => Console.WriteLine(
        $"상품 생성 성공!\n  ID: {r.ProductId}\n  이름: {r.Name}\n  가격: {r.Price:N0}원\n  생성일: {r.CreatedAt:O}"),
    Fail: e => Console.WriteLine($"상품 생성 실패: {e}"));
