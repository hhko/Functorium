using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.ErrorScenarios;

public class HandlerExceptionTests : IntegrationTestBase
{
    public HandlerExceptionTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateProduct_ShouldReturnCreated_WhenHandlerError()
    {
        // Arrange - [handler-error] triggers exception in OnProductCreated event handler
        // UsecaseTransactionPipeline에서 이벤트 발행 실패 시에도 성공 응답 유지
        // (데이터는 이미 커밋됨, 이벤트 발행 실패는 경고 로그만 기록)
        var request = new
        {
            Name = $"[handler-error] Product {Guid.NewGuid()}",
            Description = "Handler error test",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
