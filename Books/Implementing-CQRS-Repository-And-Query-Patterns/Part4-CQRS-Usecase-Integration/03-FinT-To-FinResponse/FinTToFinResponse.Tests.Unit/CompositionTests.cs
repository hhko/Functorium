using FinTToFinResponse;
using LanguageExt;

namespace FinTToFinResponse.Tests.Unit;

public sealed class CompositionTests : IDisposable
{
    private readonly NoOpDomainEventCollector _eventCollector = new();
    private readonly InMemoryProductRepository _repository;

    public CompositionTests()
    {
        InMemoryProductRepository.Clear();
        _repository = new InMemoryProductRepository(_eventCollector);
    }

    public void Dispose() => InMemoryProductRepository.Clear();

    [Fact]
    public async Task SimpleCreate_ReturnsSuccess_WhenProductCreated()
    {
        // Act
        var result = await CompositionExamples.SimpleCreate(_repository, "노트북", 1_500_000m);

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.Name.ShouldBe("노트북");
        response.Price.ShouldBe(1_500_000m);
    }

    [Fact]
    public async Task ChainedUpdate_ReturnsOldAndNewPrice_WhenUpdated()
    {
        // Arrange
        var product = Product.Create("마우스", 25_000m);
        await _repository.Create(product).Run().RunAsync();

        // Act
        var result = await CompositionExamples.ChainedUpdate(_repository, product.Id, 30_000m);

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.OldPrice.ShouldBe(25_000m);
        response.NewPrice.ShouldBe(30_000m);
    }

    [Fact]
    public async Task ChainedUpdate_ReturnsFail_WhenProductNotFound()
    {
        // Act
        var result = await CompositionExamples.ChainedUpdate(
            _repository, ProductId.New(), 30_000m);

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public async Task GuardedUpdate_ReturnsSuccess_WhenProductIsActive()
    {
        // Arrange
        var product = Product.Create("키보드", 50_000m);
        await _repository.Create(product).Run().RunAsync();

        // Act
        var result = await CompositionExamples.GuardedUpdate(_repository, product.Id, 55_000m);

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.NewPrice.ShouldBe(55_000m);
    }

    [Fact]
    public async Task GuardedUpdate_ReturnsFail_WhenProductIsInactive()
    {
        // Arrange
        var product = Product.Create("단종 상품", 10_000m).Deactivate();
        await _repository.Create(product).Run().RunAsync();

        // Act
        var result = await CompositionExamples.GuardedUpdate(_repository, product.Id, 15_000m);

        // Assert
        result.IsFail.ShouldBeTrue();
    }
}
