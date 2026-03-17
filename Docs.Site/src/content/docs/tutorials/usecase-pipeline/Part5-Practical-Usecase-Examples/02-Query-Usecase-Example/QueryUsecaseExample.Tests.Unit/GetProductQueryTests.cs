using Functorium.Applications.Usecases;
using Shouldly;

namespace QueryUsecaseExample.Tests.Unit;

public class GetProductQueryTests
{
    [Fact]
    public async Task Handle_ReturnsProduct_WhenProductExists()
    {
        // Arrange
        var sut = new GetProductQuery.Handler();
        var request = new GetProductQuery.Request("prod-001");

        // Act
        var actual = await sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Widget");
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenProductNotFound()
    {
        // Arrange
        var sut = new GetProductQuery.Handler();
        var request = new GetProductQuery.Request("nonexistent");

        // Act
        var actual = await sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Request_ImplementsICacheable_WithCorrectProperties()
    {
        // Arrange
        var sut = new GetProductQuery.Request("prod-001");

        // Act & Assert
        (sut is ICacheable).ShouldBeTrue();
        sut.CacheKey.ShouldBe("product:prod-001");
        sut.Duration.ShouldBe(TimeSpan.FromMinutes(5));
    }
}
