namespace CommandUsecase.Tests.Unit;

public sealed class CreateProductCommandTests : IDisposable
{
    private readonly NoOpDomainEventCollector _eventCollector = new();
    private readonly InMemoryProductRepository _repository;
    private readonly CreateProductCommand.Usecase _sut;

    public CreateProductCommandTests()
    {
        InMemoryProductRepository.Clear();
        _repository = new InMemoryProductRepository(_eventCollector);
        _sut = new CreateProductCommand.Usecase(_repository);
    }

    public void Dispose() => InMemoryProductRepository.Clear();

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenProductIsCreated()
    {
        // Arrange
        var request = new CreateProductCommand.Request("노트북", 1_500_000m);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsProductId_WhenSuccessful()
    {
        // Arrange
        var request = new CreateProductCommand.Request("마우스", 25_000m);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.ProductId.ShouldNotBeNullOrEmpty();
        response.Name.ShouldBe("마우스");
        response.Price.ShouldBe(25_000m);
    }
}
