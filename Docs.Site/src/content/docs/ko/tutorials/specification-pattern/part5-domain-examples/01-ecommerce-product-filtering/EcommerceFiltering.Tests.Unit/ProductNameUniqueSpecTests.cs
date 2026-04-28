using EcommerceFiltering.Domain;
using EcommerceFiltering.Domain.Specifications;
using EcommerceFiltering.Domain.ValueObjects;

namespace EcommerceFiltering.Tests.Unit;

public class ProductNameUniqueSpecTests
{
    private static readonly Product _맥북 = new(
        new ProductName("맥북 프로"), new Money(3_000_000m), new Quantity(5), new Category("전자기기"));

    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenNameMatches()
    {
        // Arrange
        var spec = new ProductNameUniqueSpec(new ProductName("맥북 프로"));

        // Act
        var result = spec.IsSatisfiedBy(_맥북);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenNameDoesNotMatch()
    {
        // Arrange
        var spec = new ProductNameUniqueSpec(new ProductName("아이패드"));

        // Act
        var result = spec.IsSatisfiedBy(_맥북);

        // Assert
        result.ShouldBeFalse();
    }
}
