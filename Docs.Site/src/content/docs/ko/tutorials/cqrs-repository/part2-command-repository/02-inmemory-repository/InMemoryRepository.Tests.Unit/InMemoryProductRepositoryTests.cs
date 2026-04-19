using Functorium.Applications.Events;
using Functorium.Domains.Events;
using InMemoryRepository;

namespace InMemoryRepository.Tests.Unit;

public sealed class InMemoryProductRepositoryTests : IDisposable
{
    private readonly InMemoryProductRepository _sut;

    public InMemoryProductRepositoryTests()
    {
        var eventCollector = new NoOpDomainEventCollector();
        _sut = new InMemoryProductRepository(eventCollector);
    }

    public void Dispose()
    {
        _sut.Clear();
    }

    [Fact]
    public async Task Create_ShouldReturnSucc_WhenNewProduct()
    {
        // Arrange
        var product = new Product(ProductId.New(), "Keyboard", 49_900m);

        // Act
        var result = await _sut.Create(product).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().Name.ShouldBe("Keyboard");
    }

    [Fact]
    public async Task GetById_ShouldReturnSucc_WhenProductExists()
    {
        // Arrange
        var product = new Product(ProductId.New(), "Mouse", 29_900m);
        await _sut.Create(product).Run().RunAsync();

        // Act
        var result = await _sut.GetById(product.Id).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().Name.ShouldBe("Mouse");
    }

    [Fact]
    public async Task GetById_ShouldThrow_WhenProductNotFound()
    {
        // Arrange
        var nonExistentId = ProductId.New();

        // Act & Assert
        await Should.ThrowAsync<Exception>(
            async () => await _sut.GetById(nonExistentId).Run().RunAsync());
    }

    [Fact]
    public async Task Update_ShouldReturnSucc_WhenProductExists()
    {
        // Arrange
        var product = new Product(ProductId.New(), "Keyboard", 49_900m);
        await _sut.Create(product).Run().RunAsync();
        product.UpdatePrice(39_900m);

        // Act
        var result = await _sut.Update(product).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().Price.ShouldBe(39_900m);
    }

    [Fact]
    public async Task Update_ShouldThrow_WhenProductNotFound()
    {
        // Arrange
        var product = new Product(ProductId.New(), "Keyboard", 49_900m);

        // Act & Assert
        await Should.ThrowAsync<Exception>(
            async () => await _sut.Update(product).Run().RunAsync());
    }

    [Fact]
    public async Task Delete_ShouldReturnOne_WhenProductExists()
    {
        // Arrange
        var product = new Product(ProductId.New(), "Keyboard", 49_900m);
        await _sut.Create(product).Run().RunAsync();

        // Act
        var result = await _sut.Delete(product.Id).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().ShouldBe(1);
    }

    [Fact]
    public async Task Delete_ShouldReturnZero_WhenProductNotFound()
    {
        // Act
        var result = await _sut.Delete(ProductId.New()).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().ShouldBe(0);
    }

    [Fact]
    public async Task CreateRange_ShouldReturnAllProducts_WhenValid()
    {
        // Arrange
        var products = new List<Product>
        {
            new(ProductId.New(), "Keyboard", 49_900m),
            new(ProductId.New(), "Mouse", 29_900m),
        };

        // Act
        var result = await _sut.CreateRange(products).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().ShouldBe(2);
    }

    [Fact]
    public async Task GetByIds_ShouldReturnAllProducts_WhenAllExist()
    {
        // Arrange
        var p1 = new Product(ProductId.New(), "Keyboard", 49_900m);
        var p2 = new Product(ProductId.New(), "Mouse", 29_900m);
        await _sut.CreateRange([p1, p2]).Run().RunAsync();

        // Act
        var result = await _sut.GetByIds([p1.Id, p2.Id]).Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.ThrowIfFail().Count.ShouldBe(2);
    }
}

internal sealed class NoOpDomainEventCollector : IDomainEventCollector
{
    public void Track(IHasDomainEvents aggregate) { }
    public void TrackRange(IEnumerable<IHasDomainEvents> aggregates) { }
    public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates() => [];
        public void TrackEvent(IDomainEvent domainEvent) { }
        public IReadOnlyList<IDomainEvent> GetDirectlyTrackedEvents() => [];
}
