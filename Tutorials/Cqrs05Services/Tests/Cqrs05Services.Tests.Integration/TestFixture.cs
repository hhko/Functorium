using System.Reflection;
using FluentValidation;
using Functorium.Abstractions.Registrations;
using Functorium.Applications.Cqrs;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OrderService.Adapters.Messaging;
using Cqrs05Services.Messages;
using OrderService.Domain;
using OrderService.Infrastructure;
using InventoryService.Adapters.Messaging;
using InventoryService.Domain;
using InventoryService.Handlers;
using InventoryService.Infrastructure;
using Testcontainers.RabbitMq;
using Wolverine;
using Wolverine.RabbitMQ;
using Xunit;

namespace Cqrs05Services.Tests.Integration;

/// <summary>
/// 통합 테스트용 TestFixture
/// RabbitMQ Testcontainers를 사용하여 격리된 테스트 환경 제공
/// </summary>
public class MessagingTestFixture : IAsyncLifetime
{
    private RabbitMqContainer? _rabbitMqContainer;
    private OrderServiceTestFixture? _orderServiceFixture;
    private InventoryServiceTestFixture? _inventoryServiceFixture;

    public RabbitMqContainer RabbitMq => _rabbitMqContainer
        ?? throw new InvalidOperationException("Fixture not initialized");

    public OrderServiceTestFixture OrderService => _orderServiceFixture
        ?? throw new InvalidOperationException("Fixture not initialized");

    public InventoryServiceTestFixture InventoryService => _inventoryServiceFixture
        ?? throw new InvalidOperationException("Fixture not initialized");

    public async ValueTask InitializeAsync()
    {
        // RabbitMQ 컨테이너 시작
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithPortBinding(5672, true)
            .Build();

        await _rabbitMqContainer.StartAsync();

        // RabbitMQ 연결 문자열 생성
        var rabbitMqConnectionString = $"amqp://guest:guest@{_rabbitMqContainer.Hostname}:{_rabbitMqContainer.GetMappedPublicPort(5672)}";

        // OrderService Fixture 초기화
        _orderServiceFixture = new OrderServiceTestFixture(rabbitMqConnectionString);
        await _orderServiceFixture.InitializeAsync();

        // InventoryService Fixture 초기화
        _inventoryServiceFixture = new InventoryServiceTestFixture(rabbitMqConnectionString);
        await _inventoryServiceFixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_orderServiceFixture != null)
            await _orderServiceFixture.DisposeAsync();

        if (_inventoryServiceFixture != null)
            await _inventoryServiceFixture.DisposeAsync();

        if (_rabbitMqContainer != null)
            await _rabbitMqContainer.DisposeAsync();
    }
}

/// <summary>
/// OrderService 통합 테스트용 Fixture
/// IHost를 직접 생성하여 콘솔 애플리케이션을 테스트합니다.
/// </summary>
public class OrderServiceTestFixture : IAsyncLifetime
{
    private readonly string _rabbitMqConnectionString;
    private IHost? _host;

    public OrderServiceTestFixture(string rabbitMqConnectionString)
    {
        _rabbitMqConnectionString = rabbitMqConnectionString;
    }

    public IServiceProvider Services => _host?.Services
        ?? throw new InvalidOperationException("Fixture not initialized");

    public async ValueTask InitializeAsync()
    {
        // Configuration 설정
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RabbitMQ:ConnectionString", _rabbitMqConnectionString }
            })
            .Build();

        // ServiceCollection 설정 (Program.cs와 동일)
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMetrics();
        // Mediator 등록 (확장 메서드 - 모호성 해결을 위해 리플렉션 사용)
        // OrderService 어셈블리의 Mediator 확장 메서드 사용
        var orderServiceAssembly = typeof(OrderService.Program).Assembly;
        var mediatorExtensionsType = orderServiceAssembly.GetType("Microsoft.Extensions.DependencyInjection.MediatorDependencyInjectionExtensions");
        var addMediatorMethod = mediatorExtensionsType?.GetMethod("AddMediator", new[] { typeof(IServiceCollection) });
        addMediatorMethod?.Invoke(null, new object[] { services });
        services.AddValidatorsFromAssemblyContaining<OrderService.Program>();

        // OpenTelemetry 및 파이프라인 설정
        services
            .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
            .ConfigureTracing(tracing => tracing.Configure(builder => builder.AddConsoleExporter()))
            .ConfigureMetrics(metrics => metrics.Configure(builder => builder.AddConsoleExporter()))
            .ConfigurePipelines(pipelines => pipelines
                .UseAll()
                .WithLifetime(ServiceLifetime.Singleton))
            .Build();

        // Repository 및 Messaging Adapter 등록
        services.RegisterScopedAdapterPipeline<IOrderRepository, InMemoryOrderRepositoryPipeline>();
        services.RegisterScopedAdapterPipeline<IInventoryMessaging, RabbitMqInventoryMessagingPipeline>();

        // Host 생성
        _host = Host.CreateDefaultBuilder()
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
                opts.UseRabbitMq(new Uri(_rabbitMqConnectionString))
                    .AutoProvision();

                opts.PublishMessage<CheckInventoryRequest>()
                    .ToRabbitQueue("inventory.check-inventory");

                opts.PublishMessage<ReserveInventoryCommand>()
                    .ToRabbitQueue("inventory.reserve-inventory");

                opts.Services.AddOpenTelemetry()
                    .WithTracing(tracing => tracing
                        .AddSource("Wolverine")
                        .AddConsoleExporter());
            })
            .Build();

        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    public IMessageBus GetMessageBus()
    {
        return Services.GetRequiredService<IMessageBus>();
    }
}

/// <summary>
/// InventoryService 통합 테스트용 Fixture
/// IHost를 직접 생성하여 콘솔 애플리케이션을 테스트합니다.
/// </summary>
public class InventoryServiceTestFixture : IAsyncLifetime
{
    private readonly string _rabbitMqConnectionString;
    private IHost? _host;

    public InventoryServiceTestFixture(string rabbitMqConnectionString)
    {
        _rabbitMqConnectionString = rabbitMqConnectionString;
    }

    public IServiceProvider Services => _host?.Services
        ?? throw new InvalidOperationException("Fixture not initialized");

    public async ValueTask InitializeAsync()
    {
        // Configuration 설정
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RabbitMQ:ConnectionString", _rabbitMqConnectionString }
            })
            .Build();

        // ServiceCollection 설정 (Program.cs와 동일)
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMetrics();
        // Mediator 등록 (확장 메서드 - 모호성 해결을 위해 직접 호출)
        // InventoryService 어셈블리의 Mediator 확장 메서드 사용
        var inventoryServiceAssembly = typeof(InventoryService.Program).Assembly;
        var mediatorExtensionsType = inventoryServiceAssembly.GetType("Microsoft.Extensions.DependencyInjection.MediatorDependencyInjectionExtensions");
        var addMediatorMethod = mediatorExtensionsType?.GetMethod("AddMediator", new[] { typeof(IServiceCollection) });
        addMediatorMethod?.Invoke(null, new object[] { services });
        services.AddValidatorsFromAssemblyContaining<InventoryService.Program>();

        // OpenTelemetry 및 파이프라인 설정
        services
            .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
            .ConfigureTracing(tracing => tracing.Configure(builder => builder.AddConsoleExporter()))
            .ConfigureMetrics(metrics => metrics.Configure(builder => builder.AddConsoleExporter()))
            .ConfigurePipelines(pipelines => pipelines
                .UseAll()
                .WithLifetime(ServiceLifetime.Singleton))
            .Build();

        // Repository 및 Messaging Adapter 등록
        services.RegisterScopedAdapterPipeline<IInventoryRepository, InMemoryInventoryRepositoryPipeline>();
        services.RegisterScopedAdapterPipeline<IOrderMessaging, RabbitMqOrderMessagingPipeline>();

        // Host 생성
        _host = Host.CreateDefaultBuilder()
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
                opts.UseRabbitMq(new Uri(_rabbitMqConnectionString))
                    .AutoProvision();

                opts.ListenToRabbitQueue("inventory.check-inventory");
                opts.ListenToRabbitQueue("inventory.reserve-inventory");

                opts.Services.AddOpenTelemetry()
                    .WithTracing(tracing => tracing
                        .AddSource("Wolverine")
                        .AddConsoleExporter());
            })
            .Build();

        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    public IMessageBus GetMessageBus()
    {
        return Services.GetRequiredService<IMessageBus>();
    }
}
