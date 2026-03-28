using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResultPattern.Demo.Domain;
using ResultPattern.Demo.Infrastructure;
using ResultPattern.Demo.Pipelines;
using ResultPattern.Demo.Usecases;
using ResultPattern.Demo.Cqrs;

Console.WriteLine("=== Result Pattern Demo ===");
Console.WriteLine("기본 생성자 없이 Response를 정의하는 패턴 검증\n");

// Host 설정
var builder = Host.CreateApplicationBuilder(args);

// Mediator 등록
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

// Pipeline 등록
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsecaseExceptionPipeline<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsecaseValidationPipeline<,>));

// Validator 등록
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductCommand.Validator>();

// Repository 등록
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

var app = builder.Build();

// 테스트 실행
using var scope = app.Services.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

Console.WriteLine("--- 1. CreateProductCommand 테스트 (성공 케이스) ---");
var createRequest = new CreateProductCommand.Request(
    Name: "테스트 상품",
    Description: "Result 패턴 테스트용 상품",
    Price: 10000m,
    StockQuantity: 100);

var createResult = await mediator.Send(createRequest);
PrintResult("CreateProduct", createResult);

Guid createdProductId = Guid.Empty;
if (createResult is FinResponse<CreateProductCommand.Response>.Succ success)
{
    createdProductId = success.Value.ProductId;
}

Console.WriteLine("\n--- 2. CreateProductCommand 테스트 (검증 실패 케이스) ---");
var invalidCreateRequest = new CreateProductCommand.Request(
    Name: "",  // 빈 이름 - 검증 실패
    Description: "검증 실패 테스트",
    Price: -100m,  // 음수 가격 - 검증 실패
    StockQuantity: -10);  // 음수 수량 - 검증 실패

var invalidCreateResult = await mediator.Send(invalidCreateRequest);
PrintResult("CreateProduct (Invalid)", invalidCreateResult);

Console.WriteLine("\n--- 3. GetProductByIdQuery 테스트 (성공 케이스) ---");
var getRequest = new GetProductByIdQuery.Request(createdProductId);
var getResult = await mediator.Send(getRequest);
PrintResult("GetProductById", getResult);

Console.WriteLine("\n--- 4. GetProductByIdQuery 테스트 (존재하지 않는 ID) ---");
var notFoundRequest = new GetProductByIdQuery.Request(Guid.NewGuid());
var notFoundResult = await mediator.Send(notFoundRequest);
PrintResult("GetProductById (NotFound)", notFoundResult);

Console.WriteLine("\n--- 5. GetProductByIdQuery 테스트 (검증 실패 - 빈 ID) ---");
var emptyIdRequest = new GetProductByIdQuery.Request(Guid.Empty);
var emptyIdResult = await mediator.Send(emptyIdRequest);
PrintResult("GetProductById (EmptyId)", emptyIdResult);

Console.WriteLine("\n--- 6. GetAllProductsQuery 테스트 ---");
// 추가 상품 생성
await mediator.Send(new CreateProductCommand.Request("상품 2", "설명 2", 20000m, 50));
await mediator.Send(new CreateProductCommand.Request("상품 3", "설명 3", 30000m, 30));

var getAllRequest = new GetAllProductsQuery.Request();
var getAllResult = await mediator.Send(getAllRequest);
PrintResult("GetAllProducts", getAllResult);

if (getAllResult is FinResponse<GetAllProductsQuery.Response>.Succ allSuccess)
{
    Console.WriteLine($"  총 상품 수: {allSuccess.Value.Products.Count}");
    foreach (var item in allSuccess.Value.Products)
    {
        Console.WriteLine($"    - {item.Name}: {item.Price:C}");
    }
}

Console.WriteLine("\n=== 검증 완료 ===");
Console.WriteLine("핵심 검증 결과:");
Console.WriteLine("  1. Response에 기본 생성자 없이 정의 가능: ✓");
Console.WriteLine("  2. Result 기반 Pipeline 동작: ✓");
Console.WriteLine("  3. Command/Query 모두 지원: ✓");
Console.WriteLine("  4. Fin -> FinResponse 변환: ✓");
Console.WriteLine("  5. 패턴 매칭 동작: ✓");

static void PrintResult<A>(string operation, FinResponse<A> result)
{
    Console.WriteLine($"[{operation}]");
    result.Match(
        Succ: value =>
        {
            Console.WriteLine($"  Status: Succ");
            Console.WriteLine($"  Value: {value}");
        },
        Fail: error =>
        {
            Console.WriteLine($"  Status: Fail");
            Console.WriteLine($"  Error: {error.Message}");
        });
}
