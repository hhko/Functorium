using Functorium.Domains.Specifications;
using RepositorySpec;
using RepositorySpec.Specifications;

namespace RepositorySpec.Tests.Unit;

/// <summary>
/// IProductRepository 인터페이스 계약 테스트
/// Specification이 Repository와 올바르게 연동되는지 검증
/// </summary>
[Trait("Part3-RepositorySpec", "RepositorySpecTests")]
public class RepositorySpecTests
{
    // 테스트 시나리오: FindAll은 Specification<Product>을 매개변수로 받아야 한다
    [Fact]
    public void IProductRepository_FindAll_ShouldAcceptSpecification()
    {
        // Arrange
        var method = typeof(IProductRepository).GetMethod(nameof(IProductRepository.FindAll));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(Specification<Product>));
    }

    // 테스트 시나리오: Exists는 Specification<Product>을 매개변수로 받아야 한다
    [Fact]
    public void IProductRepository_Exists_ShouldAcceptSpecification()
    {
        // Arrange
        var method = typeof(IProductRepository).GetMethod(nameof(IProductRepository.Exists));

        // Act
        var parameters = method!.GetParameters();

        // Assert
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(Specification<Product>));
    }

    // 테스트 시나리오: InStockSpec은 재고가 있는 상품만 선택해야 한다
    [Fact]
    public void InStockSpec_ShouldReturnTrue_WhenStockIsPositive()
    {
        // Arrange
        var spec = new ProductInStockSpec();
        var product = new Product("테스트", 1000m, 5, "카테고리");

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: InStockSpec은 재고가 0이면 거부해야 한다
    [Fact]
    public void InStockSpec_ShouldReturnFalse_WhenStockIsZero()
    {
        // Arrange
        var spec = new ProductInStockSpec();
        var product = new Product("테스트", 1000m, 0, "카테고리");

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: PriceRangeSpec은 범위 내 가격만 선택해야 한다
    [Fact]
    public void PriceRangeSpec_ShouldReturnTrue_WhenPriceIsInRange()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(1000m, 5000m);
        var product = new Product("테스트", 3000m, 1, "카테고리");

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: CategorySpec은 해당 카테고리만 선택해야 한다
    [Fact]
    public void CategorySpec_ShouldReturnTrue_WhenCategoryMatches()
    {
        // Arrange
        var spec = new ProductCategorySpec("전자제품");
        var product = new Product("테스트", 1000m, 1, "전자제품");

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: Specification 조합이 올바르게 동작해야 한다
    [Fact]
    public void CombinedSpec_ShouldWork_WithAndOperator()
    {
        // Arrange
        var spec = new ProductInStockSpec() & new ProductCategorySpec("전자제품");
        var matching = new Product("마우스", 15000m, 50, "전자제품");
        var noStock = new Product("모니터", 350000m, 0, "전자제품");
        var wrongCategory = new Product("노트", 2000m, 150, "문구류");

        // Act & Assert
        spec.IsSatisfiedBy(matching).ShouldBeTrue();
        spec.IsSatisfiedBy(noStock).ShouldBeFalse();
        spec.IsSatisfiedBy(wrongCategory).ShouldBeFalse();
    }
}
