using Functorium.Adapters.Abstractions.Registrations;
using Functorium.Adapters.Events;
using Functorium.Applications.Observabilities;
using Functorium.Applications.Usecases;

using Mediator;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ObservabilityHost.DomainEvents;
using ObservabilityHost.Usecases;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// DI
var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();
services.AddSingleton<IConfiguration>(configuration);

// Mediator
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
});

// OpenTelemetry + Pipelines
services
    .RegisterOpenTelemetry(configuration, ObservabilityHost.AssemblyReference.Assembly)
    .ConfigureTracing(t => t.Configure(b => b.AddConsoleExporter()))
    .ConfigureMetrics(m => m.Configure(b => b.AddConsoleExporter()))
    .ConfigurePipelines(p => p
        .UseMetrics()
        .UseTracing()
        .UseCtxEnricher()
        .UseLogging()
        .UseException())
    .Build();

// Ctx Enricher (별도 등록 — ICustomUsecasePipeline이 아니므로 Scrutor 스캔 대상 아님)
services.AddScoped<
    IUsecaseCtxEnricher<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>>,
    PlaceOrderCommandRequestCtxEnricher>();

// Domain Event Ctx Enricher
services.AddScoped<
    IDomainEventCtxEnricher<OrderPlacedEvent>,
    OrderPlacedEventCtxEnricher>();

await using var sp = services.BuildServiceProvider();
using var scope = sp.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

// Scenario 1: PlaceOrderCommand (커스텀 Metrics + Tracing + Log Enricher)
Console.WriteLine("=== PlaceOrderCommand (Custom Observability) ===");
var placeOrderResponse = await mediator.Send(new PlaceOrderCommand.Request("CUST-001",
[
    new PlaceOrderCommand.OrderLine("PROD-A", 2, 100.00m),
    new PlaceOrderCommand.OrderLine("PROD-B", 1, 250.00m)
], "admin@example.com"));
Console.WriteLine($"PlaceOrder Result: {placeOrderResponse}");
Console.WriteLine();

// Scenario 5: DomainEvent Enricher (PlaceOrderCommand 성공 후 도메인 이벤트 발행)
Console.WriteLine("=== OrderPlacedEvent (DomainEvent Enricher) ===");
if (placeOrderResponse.IsSucc)
{
    var response = placeOrderResponse.ThrowIfFail();
    var orderPlacedEvent = new OrderPlacedEvent(
        CustomerId: "CUST-001",
        OrderId: response.OrderId,
        LineCount: response.LineCount,
        TotalAmount: response.TotalAmount,
        OperatorId: "admin@example.com");
    await mediator.Publish(orderPlacedEvent);
}
Console.WriteLine();

// Scenario 2: GetOrderSummaryQuery (기준선 — 커스텀 관찰 가능성 없음)
Console.WriteLine("=== GetOrderSummaryQuery (Baseline) ===");
var getOrderResponse = await mediator.Send(new GetOrderSummaryQuery.Request("ORD-123"));
Console.WriteLine($"GetOrderSummary Result: {getOrderResponse}");
Console.WriteLine();

// Scenario 3: FailExpectedCommand (Expected 비즈니스 에러 → Warning 레벨)
Console.WriteLine("=== FailExpectedCommand (Expected Error) ===");
var failExpectedResponse = await mediator.Send(new FailExpectedCommand.Request("ORD-NOT-EXIST"));
Console.WriteLine($"FailExpected Result: {failExpectedResponse}");
Console.WriteLine();

// Scenario 4: FailExceptionalCommand (Exceptional 시스템 에러 → Error 레벨)
Console.WriteLine("=== FailExceptionalCommand (Exceptional Error) ===");
var failExceptionalResponse = await mediator.Send(new FailExceptionalCommand.Request("ORD-DB-FAIL"));
Console.WriteLine($"FailExceptional Result: {failExceptionalResponse}");
Console.WriteLine();

Console.WriteLine("=== Done ===");
