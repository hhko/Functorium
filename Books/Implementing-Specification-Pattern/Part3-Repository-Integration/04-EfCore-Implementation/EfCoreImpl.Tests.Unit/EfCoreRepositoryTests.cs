using EfCoreImpl;
using EfCoreImpl.Specifications;
using Functorium.Domains.Specifications.Expressions;

namespace EfCoreImpl.Tests.Unit;

/// <summary>
/// SimulatedEfCoreProductRepository 전체 파이프라인 테스트
/// Specification -> Expression 추출 -> PropertyMap 변환 -> Queryable 실행
/// </summary>
[Trait("Part3-EfCoreImpl", "EfCoreRepositoryTests")]
public class EfCoreRepositoryTests
{
    private static readonly List<ProductDbModel> DbModels =
    [
        new("무선 마우스", 15_000, 50, "전자제품"),
        new("기계식 키보드", 89_000, 30, "전자제품"),
        new("USB 케이블", 3_000, 200, "전자제품"),
        new("볼펜 세트", 5_000, 100, "문구류"),
        new("노트", 2_000, 150, "문구류"),
        new("프리미엄 만년필", 120_000, 0, "문구류"),
        new("에르고 의자", 350_000, 5, "가구"),
        new("모니터 암", 45_000, 0, "가구"),
    ];

    private readonly SimulatedEfCoreProductRepository _sut = new(DbModels, ProductPropertyMap.Create());

    // 테스트 시나리오: FindAll은 재고 있는 상품만 반환해야 한다
    [Fact]
    public void FindAll_ShouldReturnInStockProducts_WhenInStockExprSpec()
    {
        // Arrange
        var spec = new InStockExprSpec();

        // Act
        var actual = _sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Stock > 0);
    }

    // 테스트 시나리오: FindAll은 복합 조건도 올바르게 처리해야 한다
    [Fact]
    public void FindAll_ShouldReturnMatchingProducts_WhenCombinedSpec()
    {
        // Arrange
        var spec = new InStockExprSpec() & new PriceRangeExprSpec(0, 10_000);

        // Act
        var actual = _sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Stock > 0 && p.Price <= 10_000);
    }

    // 테스트 시나리오: FindAll은 도메인 Product를 반환해야 한다 (DbModel이 아님)
    [Fact]
    public void FindAll_ShouldReturnDomainProducts_NotDbModels()
    {
        // Arrange
        var spec = new CategoryExprSpec("전자제품");

        // Act
        var actual = _sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Category == "전자제품");
        actual.First().ShouldBeOfType<Product>();
    }

    // 테스트 시나리오: Exists는 조건을 만족하는 상품이 있으면 true를 반환해야 한다
    [Fact]
    public void Exists_ShouldReturnTrue_WhenMatchingProductExists()
    {
        // Arrange
        var spec = new CategoryExprSpec("가구") & new PriceRangeExprSpec(100_000, decimal.MaxValue);

        // Act
        var actual = _sut.Exists(spec);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: Exists는 조건을 만족하는 상품이 없으면 false를 반환해야 한다
    [Fact]
    public void Exists_ShouldReturnFalse_WhenNoMatchingProductExists()
    {
        // Arrange
        var spec = new CategoryExprSpec("가구") & new PriceRangeExprSpec(0, 10_000);

        // Act
        var actual = _sut.Exists(spec);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: 새로운 Specification을 추가해도 Repository 코드 변경이 불필요해야 한다
    [Fact]
    public void FindAll_ShouldWorkWithNewSpec_WithoutRepositoryChange()
    {
        // Arrange - 새로운 Specification: 5만원 이상 전자제품
        var spec = new CategoryExprSpec("전자제품") & new PriceRangeExprSpec(50_000, decimal.MaxValue);

        // Act
        var actual = _sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Category == "전자제품" && p.Price >= 50_000);
    }
}
