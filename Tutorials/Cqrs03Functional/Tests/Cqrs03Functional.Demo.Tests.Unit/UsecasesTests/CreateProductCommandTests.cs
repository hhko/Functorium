using System.Reflection;
using Cqrs03Functional.Demo.Infrastructure;
using FluentValidation;
using Functorium.Abstractions.Registrations;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cqrs03Functional.Demo.Tests.Unit.UsecasesTests;

/// <summary>
/// CreateProductCommand Handler 테스트
/// </summary>
public sealed class CreateProductCommandTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly IProductRepository _productRepository;

    public CreateProductCommandTests()
    {
        ServiceCollection services = new();

        // Configuration 설정
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Logging 설정
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Mediator 등록
        services.AddMediator();

        // FluentValidation 등록
        services.AddValidatorsFromAssemblyContaining<CreateProductCommand.Validator>();

        // MeterFactory 등록 (Metrics용)
        services.AddMetrics();

        // OpenTelemetry 및 Validation Pipeline 등록 (ConfigurePipelines API 사용)
        services
            .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
            .ConfigurePipelines(pipelines => pipelines
                .UseValidation()
                .WithLifetime(ServiceLifetime.Singleton))
            .Build();

        // InMemoryProductRepository 등록 (테스트용 생성자 사용)
        services.AddScoped<IProductRepository, InMemoryProductRepository>();

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _productRepository = _serviceProvider.GetRequiredService<IProductRepository>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }


    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        // Act
        var actual = await _mediator.Send(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.Name.ShouldBe("Test Product");
                response.Price.ShouldBe(100m);
                response.StockQuantity.ShouldBe(10);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        // Arrange - 먼저 상품을 생성하여 중복 상황 만들기
        var createRequest = new CreateProductCommand.Request("Existing Product", "Description", 100m, 10);
        await _mediator.Send(createRequest, CancellationToken.None);

        // 같은 이름으로 다시 생성 시도
        var duplicateRequest = new CreateProductCommand.Request("Existing Product", "New Description", 200m, 20);

        // Act
        var actual = await _mediator.Send(duplicateRequest, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("already exists"));
    }

    [Fact]
    public async Task Handle_InvalidPrice_ReturnsValidationError()
    {
        // Arrange - 유효하지 않은 가격 (음수)
        var request = new CreateProductCommand.Request("Test Product", "Description", -100m, 10);

        // Act
        var actual = await _mediator.Send(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("가격"));
    }

    [Fact]
    public async Task Handle_InvalidStockQuantity_ReturnsValidationError()
    {
        // Arrange - 유효하지 않은 재고 수량 (음수)
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, -10);

        // Act
        var actual = await _mediator.Send(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("재고"));
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesProductWithCorrectData()
    {
        // Arrange
        var request = new CreateProductCommand.Request("New Product", "New Description", 200m, 50);

        // Act
        var actual = await _mediator.Send(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.Name.ShouldBe("New Product");
                response.Description.ShouldBe("New Description");
                response.Price.ShouldBe(200m);
                response.StockQuantity.ShouldBe(50);
            },
            Fail: _ => throw new Exception("Should be success"));
    }
}
