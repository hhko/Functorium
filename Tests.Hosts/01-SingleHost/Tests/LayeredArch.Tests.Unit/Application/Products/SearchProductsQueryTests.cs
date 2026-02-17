using FluentValidation;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class SearchProductsQueryValidatorTests
{
    private readonly SearchProductsQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenNoPricesProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(null, null);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenBothPricesProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(100m, 200m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOnlyMinPriceProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(100m, null);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e =>
            e.PropertyName == "MaxPrice"
            && e.ErrorMessage.Contains("최소 가격을 지정할 때는 최대 가격도 함께 지정해야 합니다"));
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOnlyMaxPriceProvided()
    {
        // Arrange
        var request = new SearchProductsQuery.Request(null, 200m);

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e =>
            e.PropertyName == "MinPrice"
            && e.ErrorMessage.Contains("최대 가격을 지정할 때는 최소 가격도 함께 지정해야 합니다"));
    }
}

public class SearchProductsQueryTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly SearchProductsQuery.Usecase _sut;

    public SearchProductsQueryTests()
    {
        _sut = new SearchProductsQuery.Usecase(_productRepository);
    }

    private static Seq<Product> CreateSampleProducts()
    {
        return Seq(
            Product.Create(
                ProductName.Create("Cheap Item").ThrowIfFail(),
                ProductDescription.Create("Desc").ThrowIfFail(),
                Money.Create(50m).ThrowIfFail()),
            Product.Create(
                ProductName.Create("Mid Item").ThrowIfFail(),
                ProductDescription.Create("Desc").ThrowIfFail(),
                Money.Create(150m).ThrowIfFail()),
            Product.Create(
                ProductName.Create("Expensive Item").ThrowIfFail(),
                ProductDescription.Create("Desc").ThrowIfFail(),
                Money.Create(500m).ThrowIfFail()));
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
    {
        // Arrange
        var products = CreateSampleProducts();
        var request = new SearchProductsQuery.Request(null, null);

        _productRepository.GetAll()
            .Returns(FinTFactory.Succ(products));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenPriceRangeProvided()
    {
        // Arrange
        var products = CreateSampleProducts();
        var matchingProducts = products.Where(p => p.Price >= (Money)Money.Create(100m).ThrowIfFail()
                                                && p.Price <= (Money)Money.Create(200m).ThrowIfFail()).ToSeq();
        var request = new SearchProductsQuery.Request(100m, 200m);

        _productRepository.FindAll(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(matchingProducts));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(1);
        actual.ThrowIfFail().Products[0].Name.ShouldBe("Mid Item");
    }
}
