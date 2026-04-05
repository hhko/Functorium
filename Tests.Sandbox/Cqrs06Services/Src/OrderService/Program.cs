using System.Reflection;
using FluentValidation;
using Functorium.Abstractions.Registrations;
using Functorium.Adapters.Abstractions.Registrations;
using Functorium.Applications.Usecases;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OrderService.Adapters.Messaging;
using Cqrs06Services.Messages;
using OrderService.Domain;
using OrderService.Infrastructure;
using Wolverine;
using Wolverine.RabbitMQ;

Console.WriteLine("=== OrderService ===");

ServiceCollection services = new();

// Configuration 설정
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

services.AddSingleton(configuration);

// MeterFactory 등록 (UsecaseMetricsPipeline에 필요)
services.AddMetrics();

// Mediator 등록
services.AddMediator();

// FluentValidation 등록 - 어셈블리에서 모든 Validator 자동 등록
services.AddValidatorsFromAssemblyContaining<Program>();

// OpenTelemetry 및 파이프라인 설정
services
    .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    .ConfigureTracing(tracing => tracing.Configure(builder => builder.AddConsoleExporter()))
    .ConfigureMetrics(metrics => metrics.Configure(builder => builder.AddConsoleExporter()))
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()
        .UseValidation()
        .UseException()
        .WithLifetime(ServiceLifetime.Singleton))
    .Build();

// Repository 등록 (관찰 가능성 로그 지원)
// RegisterScopedObservablePort은 ActivityContext를 첫 번째 매개변수로 받는 생성자를 사용
// 소스 생성기가 [GenerateObservablePort] 애트리뷰트를 감지하여 InMemoryOrderRepositoryObservable 클래스를 자동 생성
// Observable이 자동으로 Activity 생성, 로깅, 추적, 메트릭 수집을 처리
services.RegisterScopedObservablePort<IOrderRepository, OrderService.Infrastructure.InMemoryOrderRepositoryObservable>();

// Messaging Adapter 등록 (관찰 가능성 로그 지원)
// 소스 생성기가 [GenerateObservablePort] 애트리뷰트를 감지하여 RabbitMqInventoryMessagingObservable 클래스를 자동 생성
services.RegisterScopedObservablePort<IInventoryMessaging, OrderService.Adapters.Messaging.RabbitMqInventoryMessagingObservable>();

// Wolverine 및 RabbitMQ 설정
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, hostServices) =>
    {
        // ServiceCollection의 서비스를 Host의 Services에 추가
        foreach (var service in services)
        {
            hostServices.Add(service);
        }
    })
    .UseWolverine(opts =>
    {
        // RabbitMQ 연결 설정
        var rabbitMqConnectionString = configuration["RabbitMQ:ConnectionString"] ?? "amqp://guest:guest@localhost:5672";
        opts.UseRabbitMq(new Uri(rabbitMqConnectionString))
            .AutoProvision(); // 큐/익스체인지 자동 생성

        // Request/Reply 패턴: CheckInventoryRequest → CheckInventoryResponse
        // InventoryService의 CheckInventoryRequestHandler가 처리
        opts.PublishMessage<CheckInventoryRequest>()
            .ToRabbitQueue("inventory.check-inventory");

        // Fire and Forget 패턴: ReserveInventoryCommand
        // InventoryService의 ReserveInventoryCommandHandler가 처리
        opts.PublishMessage<ReserveInventoryCommand>()
            .ToRabbitQueue("inventory.reserve-inventory");

        // OpenTelemetry 추적 소스 추가 (Wolverine 메시징 추적)
        opts.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource("Wolverine") // Wolverine 메시징 추적
                .AddConsoleExporter());
    })
    .Build();

// Host 시작 (Wolverine이 작동하려면 호스트가 시작되어야 함)
await host.StartAsync();

// Service Provider에서 Mediator 가져오기
await using var scope = host.Services.CreateAsyncScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

// 데모 시나리오 실행
Console.WriteLine();
Console.WriteLine("=================================================================");
Console.WriteLine("데모: 주문 생성 (재고 확인 → 주문 생성 → 재고 예약)");
Console.WriteLine("=================================================================");
Console.WriteLine();

// 먼저 InventoryService가 시작될 시간을 기다림
await Task.Delay(3000);

var productId = Guid.NewGuid();
var request = new OrderService.Usecases.CreateOrderCommand.Request(productId, Quantity: 5);

Console.WriteLine($"주문 생성 요청: ProductId={productId}, Quantity=5");
Console.WriteLine();

var result = await mediator.Send(request, CancellationToken.None);

if (result.IsSucc)
{
    var response = result.Match(Succ: v => v, Fail: _ => null!);
    Console.WriteLine($"✅ 주문 생성 성공:");
    Console.WriteLine($"   OrderId: {response.OrderId}");
    Console.WriteLine($"   ProductId: {response.ProductId}");
    Console.WriteLine($"   Quantity: {response.Quantity}");
    Console.WriteLine($"   CreatedAt: {response.CreatedAt:yyyy-MM-dd HH:mm:ss}");
}
else
{
    var error = result.Match(Succ: _ => null!, Fail: e => e);
    Console.WriteLine($"❌ 주문 생성 실패: {error?.Message}");
}

Console.WriteLine();
Console.WriteLine("데모 완료. 서비스를 종료합니다.");
Console.WriteLine();

// 데모 목적이므로 서비스를 종료
await host.StopAsync();

// WebApplicationFactory를 위한 Program 클래스 선언
namespace OrderService
{
    public partial class Program { }
}
