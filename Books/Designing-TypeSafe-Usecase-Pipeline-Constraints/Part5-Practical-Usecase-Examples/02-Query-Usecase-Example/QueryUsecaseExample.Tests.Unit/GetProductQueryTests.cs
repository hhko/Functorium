using Functorium.Applications.Usecases;
using Shouldly;

namespace QueryUsecaseExample.Tests.Unit;

public class GetProductQueryTests
{
    [Fact]
    public void Handle_ReturnsProduct_WhenProductExists()
    {
        // Arrange
        var sut = new GetProductQuery.Handler();
        var request = new GetProductQuery.Request("prod-001");

        // Act
        var actual = sut.Handle(request);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Widget");
    }

    [Fact]
    public void Handle_ReturnsFail_WhenProductNotFound()
    {
        // Arrange
        var sut = new GetProductQuery.Handler();
        var request = new GetProductQuery.Request("nonexistent");

        // Act
        var actual = sut.Handle(request);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
