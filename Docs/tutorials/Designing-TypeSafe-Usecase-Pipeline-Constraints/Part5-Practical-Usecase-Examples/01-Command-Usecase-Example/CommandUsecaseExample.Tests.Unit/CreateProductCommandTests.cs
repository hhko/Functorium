using Functorium.Applications.Usecases;
using Shouldly;

namespace CommandUsecaseExample.Tests.Unit;

public class CreateProductCommandTests
{
    [Fact]
    public void Handle_ReturnsSuccess_WhenRequestIsValid()
    {
        // Arrange
        var sut = new CreateProductCommand.Handler();
        var request = new CreateProductCommand.Request("Widget", 9.99m);

        // Act
        var actual = sut.Handle(request);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Widget");
    }

    [Fact]
    public void Handle_ReturnsFail_WhenNameIsEmpty()
    {
        // Arrange
        var sut = new CreateProductCommand.Handler();
        var request = new CreateProductCommand.Request("", 9.99m);

        // Act
        var actual = sut.Handle(request);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Handle_ReturnsFail_WhenPriceIsZero()
    {
        // Arrange
        var sut = new CreateProductCommand.Handler();
        var request = new CreateProductCommand.Request("Widget", 0m);

        // Act
        var actual = sut.Handle(request);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Widget", 9.99m);

        // Act
        var actual = CreateProductCommand.Validator.Validate(request);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsFail_WhenPriceIsNegative()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Widget", -1m);

        // Act
        var actual = CreateProductCommand.Validator.Validate(request);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
