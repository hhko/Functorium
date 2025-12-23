using System.Diagnostics;
using System.Diagnostics.Metrics;
using CqrsPipeline.Demo;
using CqrsPipeline.Demo.Domain;
using CqrsPipeline.Demo.Infrastructure;
using CqrsPipeline.Demo.Usecases;
using FluentValidation;
using Functorium.Adapters.Observabilities;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

Console.WriteLine("=== CQRS Pipeline Pattern Demo ===");
Console.WriteLine();
Console.WriteLine("이 데모는 Functorium의 파이프라인 기능을 보여줍니다:");
Console.WriteLine("  1. UsecaseExceptionPipeline - 예외를 Error로 변환");
Console.WriteLine("  2. UsecaseValidationPipeline - FluentValidation 검증");
Console.WriteLine("  3. UsecaseLoggerPipeline - 요청/응답 로깅");
Console.WriteLine("  4. UsecaseTracePipeline - OpenTelemetry 분산 추적");
Console.WriteLine("  5. UsecaseMetricPipeline - OpenTelemetry 메트릭");
Console.WriteLine();

// =================================================================
// DI Container 구성
// =================================================================
ServiceCollection services = new();

// Logging 설정
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Mediator 등록
services.AddMediator();

// FluentValidation 등록 - 어셈블리에서 모든 Validator 자동 등록
services.AddValidatorsFromAssemblyContaining<Program>();

// OpenTelemetry 옵션 등록
services.AddSingleton<IOpenTelemetryOptions>(new DemoOpenTelemetryOptions());

// ActivitySource 등록 (Tracing용)
ActivitySource activitySource = new("CqrsPipeline.Demo");
services.AddSingleton(activitySource);

// MeterFactory 등록 (Metrics용)
services.AddMetrics();

// OpenTelemetry 설정
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("CqrsPipeline.Demo")
        .AddConsoleExporter())
    .WithMetrics(builder => builder
        .AddMeter("CqrsPipeline.Demo.Application")
        .AddConsoleExporter());

// =================================================================
// 파이프라인 등록 (순서 중요!)
// =================================================================
// 파이프라인은 외부에서 내부로 실행됩니다:
// Request -> Exception -> Validation -> Logger -> Trace -> Metric -> Handler
// Response <- Exception <- Validation <- Logger <- Trace <- Metric <- Handler

// 1. Exception Pipeline (가장 바깥쪽 - 모든 예외를 캐치)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseExceptionPipeline<,>));

// 2. Validation Pipeline (검증 후 핸들러 실행)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseValidationPipeline<,>));

// 3. Logger Pipeline (요청/응답 로깅)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseLoggerPipeline<,>));

// 4. Trace Pipeline (분산 추적)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseTracePipeline<,>));

// 5. Metric Pipeline (메트릭 수집)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseMetricPipeline<,>));

// Repository 등록
services.AddSingleton<IProductRepository, InMemoryProductRepository>();

// Service Provider 빌드
await using ServiceProvider serviceProvider = services.BuildServiceProvider();

IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

// =================================================================
// 데모 시나리오 실행
// =================================================================

Console.WriteLine("=================================================================");
Console.WriteLine("1. Validation Pipeline 데모 - 성공적인 상품 생성");
Console.WriteLine("=================================================================");

IFinResponse<CreateProductCommand.Response> createResult1 = await mediator.Send(
    new CreateProductCommand.Request("노트북", "고성능 개발용 노트북", 1500000m, 10));

PrintResult("상품 생성 (노트북)", createResult1);

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("2. Validation Pipeline 데모 - 검증 실패");
Console.WriteLine("=================================================================");

IFinResponse<CreateProductCommand.Response> createResult2 = await mediator.Send(
    new CreateProductCommand.Request("", "설명만 있음", -1000m, -5)); // 이름 누락, 가격 음수, 재고 음수

PrintResult("상품 생성 (검증 실패)", createResult2);

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("3. Exception Pipeline 데모 - 예외 발생 시뮬레이션");
Console.WriteLine("=================================================================");

// 먼저 상품 생성
IFinResponse<CreateProductCommand.Response> createResult3 = await mediator.Send(
    new CreateProductCommand.Request("마우스", "무선 마우스", 50000m, 100));

if (createResult3.IsSucc)
{
    // SimulateException = true로 예외 발생
    IFinResponse<UpdateProductCommand.Response> updateResult = await mediator.Send(
        new UpdateProductCommand.Request(
            createResult3.Value.ProductId,
            "마우스 (업데이트)",
            "무선 마우스 - 업데이트됨",
            55000m,
            90,
            SimulateException: true));

    PrintResult("상품 업데이트 (예외 시뮬레이션)", updateResult);
}

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("4. Logger Pipeline 데모 - 상품 조회");
Console.WriteLine("=================================================================");

if (createResult1.IsSucc)
{
    IFinResponse<GetProductByIdQuery.Response> getResult = await mediator.Send(
        new GetProductByIdQuery.Request(createResult1.Value.ProductId));

    PrintResult("상품 조회 (노트북)", getResult);
}

// 존재하지 않는 상품 조회
IFinResponse<GetProductByIdQuery.Response> notFoundResult = await mediator.Send(
    new GetProductByIdQuery.Request(Guid.NewGuid()));

PrintResult("상품 조회 (존재하지 않음)", notFoundResult);

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("5. Trace/Metric Pipeline 데모 - 전체 상품 조회");
Console.WriteLine("=================================================================");

IFinResponse<GetAllProductsQuery.Response> allProductsResult = await mediator.Send(
    new GetAllProductsQuery.Request());

if (allProductsResult.IsSucc)
{
    Console.WriteLine($"[SUCCESS] 전체 상품 조회 ({allProductsResult.Value.Products.Count}개):");
    foreach (var product in allProductsResult.Value.Products)
    {
        Console.WriteLine($"  - {product.Name}: {product.Price:N0}원 (재고: {product.StockQuantity})");
    }
}
else
{
    Console.WriteLine($"[FAILURE] 전체 상품 조회");
    Console.WriteLine($"  Error: {allProductsResult.Error.Message}");
}

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("=== Demo Completed ===");
Console.WriteLine("=================================================================");

// =================================================================
// Helper Methods
// =================================================================
static void PrintResult<T>(string operation, IFinResponse<T> result) where T : IResponse
{
    if (result.IsSucc)
    {
        Console.WriteLine($"[SUCCESS] {operation}");
        Console.WriteLine($"  Result: {result.Value}");
    }
    else
    {
        Console.WriteLine($"[FAILURE] {operation}");
        Console.WriteLine($"  Error: {result.Error.Message}");

        // ManyErrors인 경우 개별 에러 출력
        if (result.Error is ManyErrors manyErrors)
        {
            Console.WriteLine($"  Errors ({manyErrors.Errors.Count}):");
            foreach (var error in manyErrors.Errors)
            {
                Console.WriteLine($"    - {error.Message}");
            }
        }
    }
    Console.WriteLine();
}

// =================================================================
// Demo OpenTelemetry Options
// =================================================================
public sealed class DemoOpenTelemetryOptions : IOpenTelemetryOptions
{
    public string ServiceNamespace => "CqrsPipeline.Demo";
    public bool EnablePrometheusExporter => false;
}
