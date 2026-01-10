using System.Reflection;
using FluentValidation;
using Functorium.Abstractions.Registrations;
using Functorium.Applications.Cqrs;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InventoryService.Adapters.Messaging;
using Cqrs05Services.Messages;
using InventoryService.Domain;
using InventoryService.Handlers;
using InventoryService.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Wolverine;
using Wolverine.RabbitMQ;

Console.WriteLine("=== InventoryService ===");

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
        .UseAll()
        .WithLifetime(ServiceLifetime.Singleton))
    .Build();

// Repository 등록 (관찰 가능성 로그 지원)
// RegisterScopedAdapterPipeline은 ActivityContext를 첫 번째 매개변수로 받는 생성자를 사용
// 소스 생성기가 [GeneratePipeline] 애트리뷰트를 감지하여 InMemoryInventoryRepositoryPipeline 클래스를 자동 생성
// Pipeline이 자동으로 Activity 생성, 로깅, 추적, 메트릭 수집을 처리
services.RegisterScopedAdapterPipeline<IInventoryRepository, InventoryService.Infrastructure.InMemoryInventoryRepositoryPipeline>();

// Messaging Adapter 등록 (관찰 가능성 로그 지원)
// 소스 생성기가 [GeneratePipeline] 애트리뷰트를 감지하여 RabbitMqOrderMessagingPipeline 클래스를 자동 생성
services.RegisterScopedAdapterPipeline<IOrderMessaging, InventoryService.Adapters.Messaging.RabbitMqOrderMessagingPipeline>();

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

        // Request/Reply 패턴: CheckInventoryRequest 핸들러 등록
        // CheckInventoryRequestHandler가 처리
        opts.ListenToRabbitQueue("inventory.check-inventory");

        // Fire and Forget 패턴: ReserveInventoryCommand 핸들러 등록
        // ReserveInventoryCommandHandler가 처리
        opts.ListenToRabbitQueue("inventory.reserve-inventory");

        // OpenTelemetry 추적 소스 추가 (Wolverine 메시징 추적)
        opts.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource("Wolverine") // Wolverine 메시징 추적
                .AddConsoleExporter());
    })
    .Build();

// Host 시작 (Wolverine이 작동하려면 호스트가 시작되어야 함)
await host.StartAsync();

Console.WriteLine("InventoryService가 메시지를 수신 대기 중입니다...");
Console.WriteLine();

// 데모 목적: OrderService가 메시지를 보낼 때까지 대기
// 실제로는 메시지가 처리될 때까지 대기해야 하므로 짧은 시간만 대기
await Task.Delay(10000); // 10초 대기

Console.WriteLine();
Console.WriteLine("데모 완료. 서비스를 종료합니다.");
Console.WriteLine();

// 데모 목적이므로 서비스를 종료
await host.StopAsync();

// WebApplicationFactory를 위한 Program 클래스 선언
namespace InventoryService
{
    public partial class Program { }
}
