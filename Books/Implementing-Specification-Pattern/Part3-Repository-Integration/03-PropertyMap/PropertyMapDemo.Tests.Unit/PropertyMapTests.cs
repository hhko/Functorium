using Functorium.Domains.Specifications.Expressions;
using PropertyMapDemo;
using PropertyMapDemo.Specifications;

namespace PropertyMapDemo.Tests.Unit;

/// <summary>
/// PropertyMap 필드명 변환 및 Expression 변환 테스트
/// </summary>
[Trait("Part3-PropertyMap", "PropertyMapTests")]
public class PropertyMapTests
{
    private readonly PropertyMap<Product, ProductDbModel> _map = ProductPropertyMap.Create();

    // 테스트 시나리오: Name 필드가 ProductName으로 변환되어야 한다
    [Theory]
    [InlineData("Name", "ProductName")]
    [InlineData("Price", "UnitPrice")]
    [InlineData("Stock", "StockQuantity")]
    [InlineData("Category", "CategoryCode")]
    public void TranslateFieldName_ShouldReturnMappedName(string entityField, string expectedModelField)
    {
        // Act
        var actual = _map.TranslateFieldName(entityField);

        // Assert
        actual.ShouldBe(expectedModelField);
    }

    // 테스트 시나리오: 매핑되지 않은 필드는 null을 반환해야 한다
    [Fact]
    public void TranslateFieldName_ShouldReturnNull_WhenFieldNotMapped()
    {
        // Act
        var actual = _map.TranslateFieldName("UnknownField");

        // Assert
        actual.ShouldBeNull();
    }

    // 테스트 시나리오: 도메인 Expression이 모델 Expression으로 변환되어야 한다
    [Fact]
    public void Translate_ShouldConvertDomainExpressionToModelExpression()
    {
        // Arrange
        var spec = new ProductInStockSpec();
        var domainExpr = spec.ToExpression();

        // Act
        var modelExpr = _map.Translate(domainExpr);

        // Assert
        modelExpr.ShouldNotBeNull();
        modelExpr.Body.ToString().ShouldContain("StockQuantity");
    }

    // 테스트 시나리오: 변환된 Expression이 DbModel 컬렉션에서 올바르게 필터링해야 한다
    [Fact]
    public void TranslatedExpression_ShouldFilterDbModels_WhenInStockSpec()
    {
        // Arrange
        var spec = new ProductInStockSpec();
        var modelExpr = _map.Translate(spec.ToExpression());
        var dbModels = new List<ProductDbModel>
        {
            new("마우스", 15_000, 50, "전자제품"),
            new("만년필", 120_000, 0, "문구류"),
        };

        // Act
        var actual = dbModels.AsQueryable().Where(modelExpr).ToList();

        // Assert
        actual.Count.ShouldBe(1);
        actual[0].ProductName.ShouldBe("마우스");
    }

    // 테스트 시나리오: 복합 Expression도 변환 후 올바르게 필터링해야 한다
    [Fact]
    public void TranslatedExpression_ShouldFilterDbModels_WhenCombinedSpec()
    {
        // Arrange
        var spec = new ProductInStockSpec() & new ProductPriceRangeSpec(0, 10_000);
        var compositeExpr = SpecificationExpressionResolver.TryResolve(spec);
        var modelExpr = _map.Translate(compositeExpr!);
        var dbModels = new List<ProductDbModel>
        {
            new("USB 케이블", 3_000, 200, "전자제품"),
            new("기계식 키보드", 89_000, 30, "전자제품"),
            new("만년필", 120_000, 0, "문구류"),
        };

        // Act
        var actual = dbModels.AsQueryable().Where(modelExpr).ToList();

        // Assert
        actual.Count.ShouldBe(1);
        actual[0].ProductName.ShouldBe("USB 케이블");
    }
}
