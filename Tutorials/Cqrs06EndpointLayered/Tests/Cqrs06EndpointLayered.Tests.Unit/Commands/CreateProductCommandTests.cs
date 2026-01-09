using Cqrs06EndpointLayered.Applications.Commands;
using Cqrs06EndpointLayered.Domains.Entities;
using Cqrs06EndpointLayered.Domains.Repositories;
using Microsoft.Extensions.Logging;

namespace Cqrs06EndpointLayered.Tests.Unit.Commands;

public class CreateProductCommandTests
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CreateProductCommand.Usecase> _logger;

    public CreateProductCommandTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _logger = Substitute.For<ILogger<CreateProductCommand.Usecase>>();
    }

    [Fact]
    public async Task Handle_ShouldCreateProduct_WhenNameDoesNotExist()
    {
        // Arrange
        var request = new CreateProductCommand.Request(
            Name: "Test Product",
            Description: "Test Description",
            Price: 100m,
            StockQuantity: 10);

        _productRepository.ExistsByName(Arg.Any<string>())
            .Returns(IO.lift(() => Fin.Succ(false)));

        _productRepository.Create(Arg.Any<Product>())
            .Returns(call =>
            {
                var product = call.Arg<Product>();
                return IO.lift(() => Fin.Succ(product));
            });

        var usecase = new CreateProductCommand.Usecase(_logger, _productRepository);

        // Act
        var result = await usecase.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: response =>
            {
                response.Name.ShouldBe(request.Name);
                response.Description.ShouldBe(request.Description);
                response.Price.ShouldBe(request.Price);
                response.StockQuantity.ShouldBe(request.StockQuantity);
            },
            Fail: _ => Assert.Fail("Expected success but got failure"));
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenNameAlreadyExists()
    {
        // Arrange
        var request = new CreateProductCommand.Request(
            Name: "Existing Product",
            Description: "Test Description",
            Price: 100m,
            StockQuantity: 10);

        _productRepository.ExistsByName(Arg.Any<string>())
            .Returns(IO.lift(() => Fin.Succ(true)));

        var usecase = new CreateProductCommand.Usecase(_logger, _productRepository);

        // Act
        var result = await usecase.Handle(request, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => Assert.Fail("Expected failure but got success"),
            Fail: error => error.Message.ShouldContain("already exists"));
    }

    [Fact]
    public void Validator_ShouldReturnError_WhenNameIsEmpty()
    {
        // Arrange
        var validator = new CreateProductCommand.Validator();
        var request = new CreateProductCommand.Request(
            Name: "",
            Description: "Test Description",
            Price: 100m,
            StockQuantity: 10);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validator_ShouldReturnError_WhenPriceIsZero()
    {
        // Arrange
        var validator = new CreateProductCommand.Validator();
        var request = new CreateProductCommand.Request(
            Name: "Test Product",
            Description: "Test Description",
            Price: 0m,
            StockQuantity: 10);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void Validator_ShouldReturnError_WhenStockQuantityIsNegative()
    {
        // Arrange
        var validator = new CreateProductCommand.Validator();
        var request = new CreateProductCommand.Request(
            Name: "Test Product",
            Description: "Test Description",
            Price: 100m,
            StockQuantity: -1);

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "StockQuantity");
    }
}
