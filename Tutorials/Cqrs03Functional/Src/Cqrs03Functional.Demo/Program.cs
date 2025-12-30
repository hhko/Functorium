using System.Reflection;
using Cqrs03Functional.Demo;
using Cqrs03Functional.Demo.Domain;
using Cqrs03Functional.Demo.Infrastructure;
using Cqrs03Functional.Demo.Usecases;
using FluentValidation;
using Functorium.Abstractions.Registrations;
using Functorium.Applications.Cqrs;
using LanguageExt;
using LanguageExt.Common;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

Console.WriteLine("=== CQRS Functional Pattern Demo ===");
Console.WriteLine();
Console.WriteLine("이 데모는 Functorium의 함수형 CQRS 패턴을 보여줍니다:");
Console.WriteLine("  1. UsecaseExceptionPipeline - 예외를 Error로 변환");
Console.WriteLine("  2. UsecaseValidationPipeline - FluentValidation 검증");
Console.WriteLine("  3. UsecaseLoggerPipeline - OpenTelemetry 로그");
Console.WriteLine("  4. UsecaseTracePipeline - OpenTelemetry 추적");
Console.WriteLine("  5. UsecaseMetricPipeline - OpenTelemetry 지표");
Console.WriteLine();

// =================================================================
// DI Container 구성
// =================================================================
ServiceCollection services = new();

// Configuration 설정
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

services.AddSingleton(configuration);

// MeterFactory 등록 (UsecaseMetricPipeline에 필요)
services.AddMetrics();

// Mediator 등록
services.AddMediator();

// FluentValidation 등록 - 어셈블리에서 모든 Validator 자동 등록
services.AddValidatorsFromAssemblyContaining<Program>();

// OpenTelemetry 설정 (RegisterOpenTelemetry 사용)
services
    .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    .ConfigureTraces(tracing => tracing.Configure(builder => builder.AddConsoleExporter()))
    .ConfigureMetrics(metrics => metrics.Configure(builder => builder.AddConsoleExporter()))
    .Build();
// AdapterObservability는 자동으로 등록됨

// =================================================================
// 파이프라인 등록 (순서 중요!)
// =================================================================
// 파이프라인은 외부에서 내부로 실행됩니다:
// Request -> Metric -> Trace -> Logger -> Validation -> Exception -> Handler
// Response <- Metric <- Trace <- Logger <- Validation <- Exception <- Handler

// 1. Metric Pipeline (지표)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseMetricPipeline<,>));

// 2. Trace Pipeline (추적)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseTracePipeline<,>));

// 3. Logger Pipeline (로그)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseLoggerPipeline<,>));

// 4. Validation Pipeline (유효성 검사)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseValidationPipeline<,>));

// 5. Exception Pipeline (예외)
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseExceptionPipeline<,>));

// Repository 등록 (관찰 가능성 로그 지원)
// RegisterScopedAdapterPipeline은 ActivityContext를 첫 번째 매개변수로 받는 생성자를 사용
// 소스 생성기가 [GeneratePipeline] 애트리뷰트를 감지하여 InMemoryProductRepositoryPipeline 클래스를 자동 생성
// Pipeline이 자동으로 Activity 생성, 로깅, 추적, 메트릭 수집을 처리
services.RegisterScopedAdapterPipeline<IProductRepository, Cqrs03Functional.Demo.Infrastructure.InMemoryProductRepositoryPipeline>();

// Service Provider 빌드
await using ServiceProvider serviceProvider = services.BuildServiceProvider();

IMediator mediator = serviceProvider.GetRequiredService<IMediator>();

// =================================================================
// 데모 시나리오 실행
// =================================================================

Console.WriteLine("=================================================================");
Console.WriteLine("1. Validation Pipeline 데모 - 성공적인 상품 생성");
Console.WriteLine("=================================================================");

FinResponse<CreateProductCommand.Response> createResult1 = await mediator.Send(
    new CreateProductCommand.Request("노트북", "고성능 개발용 노트북", 1500000m, 10));

PrintResult("상품 생성 (노트북)", createResult1);

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("2. Validation Pipeline 데모 - 검증 실패");
Console.WriteLine("=================================================================");

FinResponse<CreateProductCommand.Response> createResult2 = await mediator.Send(
    new CreateProductCommand.Request("", "설명만 있음", -1000m, -5)); // 이름 누락, 가격 음수, 재고 음수

PrintResult("상품 생성 (검증 실패)", createResult2);

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("3. Exception Pipeline 데모 - 예외 발생 시뮬레이션");
Console.WriteLine("=================================================================");

// 먼저 상품 생성
FinResponse<CreateProductCommand.Response> createResult3 = await mediator.Send(
    new CreateProductCommand.Request("마우스", "무선 마우스", 50000m, 100));

if (createResult3.IsSucc)
{
    CreateProductCommand.Response value3 = createResult3.Match(Succ: v => v, Fail: _ => null!);
    // SimulateException = true로 예외 발생
    FinResponse<UpdateProductCommand.Response> updateResult = await mediator.Send(
        new UpdateProductCommand.Request(
            value3.ProductId,
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
    CreateProductCommand.Response value1 = createResult1.Match(Succ: v => v, Fail: _ => null!);
    FinResponse<GetProductByIdQuery.Response> getResult = await mediator.Send(
        new GetProductByIdQuery.Request(value1.ProductId));

    PrintResult("상품 조회 (노트북)", getResult);
}

// 존재하지 않는 상품 조회
FinResponse<GetProductByIdQuery.Response> notFoundResult = await mediator.Send(
    new GetProductByIdQuery.Request(Guid.NewGuid()));

PrintResult("상품 조회 (존재하지 않음)", notFoundResult);

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("5. Trace/Metric Pipeline 데모 - 전체 상품 조회");
Console.WriteLine("=================================================================");

FinResponse<GetAllProductsQuery.Response> allProductsResult = await mediator.Send(
    new GetAllProductsQuery.Request());

allProductsResult.Match(
    Succ: response =>
    {
        Console.WriteLine($"[SUCCESS] 전체 상품 조회 ({response.Products.Count}개):");
        foreach (var product in response.Products)
        {
            Console.WriteLine($"  - {product.Name}: {product.Price:N0}원 (재고: {product.StockQuantity})");
        }
        return LanguageExt.Unit.Default;
    },
    Fail: error =>
    {
        Console.WriteLine($"[FAILURE] 전체 상품 조회");
        Console.WriteLine($"  Error: {error.Message}");
        return LanguageExt.Unit.Default;
    });

Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("=== Demo Completed ===");
Console.WriteLine("=================================================================");

// =================================================================
// Helper Methods
// =================================================================
static void PrintResult<T>(string operation, FinResponse<T> result)
{
    result.Match(
        Succ: value =>
        {
            Console.WriteLine($"[SUCCESS] {operation}");
            Console.WriteLine($"  Result: {value}");
            return LanguageExt.Unit.Default;
        },
        Fail: error =>
        {
            Console.WriteLine($"[FAILURE] {operation}");
            Console.WriteLine($"  Error: {error.Message}");

            // ManyErrors인 경우 개별 에러 출력
            if (error is ManyErrors manyErrors)
            {
                Console.WriteLine($"  Errors ({manyErrors.Errors.Count}):");
                foreach (var e in manyErrors.Errors)
                {
                    Console.WriteLine($"    - {e.Message}");
                }
            }
            return LanguageExt.Unit.Default;
        });
    Console.WriteLine();
}
