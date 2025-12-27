using System.Diagnostics;
using System.Diagnostics.Metrics;
using CqrsPipeline.Demo.Infrastructure;
using FluentValidation;
using Functorium.Adapters.Observabilities;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Tests.Unit.PipelinesTests;

/// <summary>
/// 전체 파이프라인 통합 테스트
/// 모든 파이프라인이 순서대로 호출되는지 확인
/// </summary>
public sealed class PipelineIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly List<string> _pipelineExecutionOrder;

    public PipelineIntegrationTests()
    {
        _pipelineExecutionOrder = [];

        ServiceCollection services = new();

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

        // OpenTelemetry 옵션 등록
        services.AddSingleton<IOpenTelemetryOptions>(new TestOpenTelemetryOptions());

        // ActivitySource 등록 (Tracing용)
        ActivitySource activitySource = new("CqrsPipeline.Demo.Tests");
        services.AddSingleton(activitySource);

        // MeterFactory 등록 (Metrics용)
        services.AddMetrics();

        // 파이프라인 등록 (순서 중요!)
        // 1. Exception Pipeline (가장 바깥쪽)
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseExceptionPipeline<,>));
        // 2. Validation Pipeline
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseValidationPipeline<,>));
        // 3. Logger Pipeline
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseLoggerPipeline<,>));
        // 4. Trace Pipeline
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseTracePipeline<,>));
        // 5. Metric Pipeline
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseMetricPipeline<,>));

        // Repository 등록
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task AllPipelines_CreateProduct_ExecutesSuccessfully()
    {
        // Arrange
        var request = new CreateProductCommand.Request("테스트 상품", "설명", 10000m, 5);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        CreateProductCommand.Response result = await _mediator.Send(request, cancellationToken);

        // Assert - 모든 파이프라인을 통과하고 성공해야 함
        result.IsSuccess.ShouldBeTrue();
        result.Name.ShouldBe("테스트 상품");
        result.Price.ShouldBe(10000m);
    }

    [Fact]
    public async Task ValidationPipeline_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange - 모든 필드가 유효하지 않은 요청
        var request = new CreateProductCommand.Request("", "", -100m, -1);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        CreateProductCommand.Response result = await _mediator.Send(request, cancellationToken);

        // Assert - Validation Pipeline에서 실패해야 함
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBeOfType<ManyErrors>();
        var manyErrors = (ManyErrors)result.Error!;
        manyErrors.Errors.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ExceptionPipeline_SimulatedException_ReturnsExceptionalError()
    {
        // Arrange - 먼저 상품 생성
        var createRequest = new CreateProductCommand.Request("예외 테스트 상품", "설명", 5000m, 10);
        var cancellationToken = TestContext.Current.CancellationToken;
        CreateProductCommand.Response createResult = await _mediator.Send(createRequest, cancellationToken);
        createResult.IsSuccess.ShouldBeTrue();

        // 예외 시뮬레이션 요청
        var updateRequest = new UpdateProductCommand.Request(
            createResult.ProductId,
            "업데이트됨",
            "업데이트 설명",
            6000m,
            8,
            SimulateException: true);

        // Act
        UpdateProductCommand.Response result = await _mediator.Send(updateRequest, cancellationToken);

        // Assert - Exception Pipeline에서 예외를 Error로 변환해야 함
        result.IsSuccess.ShouldBeFalse();
        result.Error!.IsExceptional.ShouldBeTrue();
    }

    [Fact]
    public async Task AllPipelines_UpdateProduct_ExecutesSuccessfully()
    {
        // Arrange - 먼저 상품 생성
        var createRequest = new CreateProductCommand.Request("업데이트 테스트 상품", "원본 설명", 3000m, 15);
        var cancellationToken = TestContext.Current.CancellationToken;
        CreateProductCommand.Response createResult = await _mediator.Send(createRequest, cancellationToken);
        createResult.IsSuccess.ShouldBeTrue();

        var updateRequest = new UpdateProductCommand.Request(
            createResult.ProductId,
            "업데이트된 상품명",
            "업데이트된 설명",
            3500m,
            20);

        // Act
        UpdateProductCommand.Response result = await _mediator.Send(updateRequest, cancellationToken);

        // Assert - 모든 파이프라인을 통과하고 성공해야 함
        result.IsSuccess.ShouldBeTrue();
        result.Name.ShouldBe("업데이트된 상품명");
        result.Price.ShouldBe(3500m);
    }

    [Fact]
    public async Task AllPipelines_GetProductById_ExecutesSuccessfully()
    {
        // Arrange - 먼저 상품 생성
        var createRequest = new CreateProductCommand.Request("조회 테스트 상품", "조회용", 7000m, 25);
        var cancellationToken = TestContext.Current.CancellationToken;
        CreateProductCommand.Response createResult = await _mediator.Send(createRequest, cancellationToken);
        createResult.IsSuccess.ShouldBeTrue();

        var getRequest = new GetProductByIdQuery.Request(createResult.ProductId);

        // Act
        GetProductByIdQuery.Response result = await _mediator.Send(getRequest, cancellationToken);

        // Assert - 모든 파이프라인을 통과하고 성공해야 함
        result.IsSuccess.ShouldBeTrue();
        result.Name.ShouldBe("조회 테스트 상품");
    }

    [Fact]
    public async Task AllPipelines_GetProductById_NotFound_ReturnsFailure()
    {
        // Arrange
        var getRequest = new GetProductByIdQuery.Request(Guid.NewGuid());
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        GetProductByIdQuery.Response result = await _mediator.Send(getRequest, cancellationToken);

        // Assert - 모든 파이프라인을 통과했지만 상품이 없어서 실패
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldContain("찾을 수 없습니다");
    }

    [Fact]
    public async Task AllPipelines_GetAllProducts_ExecutesSuccessfully()
    {
        // Arrange - 여러 상품 생성
        var cancellationToken = TestContext.Current.CancellationToken;
        await _mediator.Send(new CreateProductCommand.Request("상품1", "설명1", 1000m, 10), cancellationToken);
        await _mediator.Send(new CreateProductCommand.Request("상품2", "설명2", 2000m, 20), cancellationToken);
        await _mediator.Send(new CreateProductCommand.Request("상품3", "설명3", 3000m, 30), cancellationToken);

        var getAllRequest = new GetAllProductsQuery.Request();

        // Act
        GetAllProductsQuery.Response result = await _mediator.Send(getAllRequest, cancellationToken);

        // Assert - 모든 파이프라인을 통과하고 성공해야 함
        result.IsSuccess.ShouldBeTrue();
        result.Products.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ValidationPipeline_DuplicateName_ReturnsError()
    {
        // Arrange - 같은 이름으로 두 번 생성
        var cancellationToken = TestContext.Current.CancellationToken;
        await _mediator.Send(new CreateProductCommand.Request("중복 테스트 상품", "첫 번째", 1000m, 10), cancellationToken);

        var duplicateRequest = new CreateProductCommand.Request("중복 테스트 상품", "두 번째", 2000m, 20);

        // Act
        CreateProductCommand.Response result = await _mediator.Send(duplicateRequest, cancellationToken);

        // Assert - 중복으로 인한 실패
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldContain("이미 존재합니다");
    }

    [Fact]
    public async Task UpdatePipeline_NonExistingProduct_ReturnsError()
    {
        // Arrange
        var updateRequest = new UpdateProductCommand.Request(
            Guid.NewGuid(),
            "존재하지 않는 상품",
            "설명",
            1000m,
            10);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        UpdateProductCommand.Response result = await _mediator.Send(updateRequest, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Message.ShouldContain("찾을 수 없습니다");
    }

    [Fact]
    public async Task AllPipelines_MultipleOperations_MaintainConsistency()
    {
        // 전체 파이프라인을 통한 복합 시나리오 테스트
        var cancellationToken = TestContext.Current.CancellationToken;

        // 1. 상품 생성
        var createResult = await _mediator.Send(
            new CreateProductCommand.Request("복합 테스트 상품", "원본", 5000m, 50), cancellationToken);
        createResult.IsSuccess.ShouldBeTrue();
        var productId = createResult.ProductId;

        // 2. 조회
        var getResult1 = await _mediator.Send(new GetProductByIdQuery.Request(productId), cancellationToken);
        getResult1.IsSuccess.ShouldBeTrue();
        getResult1.Name.ShouldBe("복합 테스트 상품");

        // 3. 업데이트
        var updateResult = await _mediator.Send(
            new UpdateProductCommand.Request(productId, "업데이트됨", "업데이트 설명", 6000m, 40), cancellationToken);
        updateResult.IsSuccess.ShouldBeTrue();
        updateResult.Name.ShouldBe("업데이트됨");

        // 4. 업데이트 후 조회
        var getResult2 = await _mediator.Send(new GetProductByIdQuery.Request(productId), cancellationToken);
        getResult2.IsSuccess.ShouldBeTrue();
        getResult2.Name.ShouldBe("업데이트됨");
        getResult2.Price.ShouldBe(6000m);

        // 5. 전체 목록에서 확인
        var allResult = await _mediator.Send(new GetAllProductsQuery.Request(), cancellationToken);
        allResult.IsSuccess.ShouldBeTrue();
        allResult.Products.Any(p => p.ProductId == productId && p.Name == "업데이트됨").ShouldBeTrue();
    }

    private sealed class TestOpenTelemetryOptions : IOpenTelemetryOptions
    {
        public string ServiceNamespace => "CqrsPipeline.Demo.Tests";
        public bool EnablePrometheusExporter => false;
    }
}
