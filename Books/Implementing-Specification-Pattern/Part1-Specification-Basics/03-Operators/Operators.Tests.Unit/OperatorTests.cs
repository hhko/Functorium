using Operators;
using Operators.Specifications;

namespace Operators.Tests.Unit;

/// <summary>
/// 연산자 오버로딩이 메서드 호출과 동일한 결과를 내는지 검증
/// </summary>
[Trait("Part1-Operators", "OperatorTests")]
public class OperatorTests
{
    private static readonly Product[] Products =
    [
        new("노트북", 1_200_000m, 5, "전자제품"),
        new("마우스", 25_000m, 0, "전자제품"),
        new("키보드", 89_000m, 3, "주변기기"),
        new("모니터", 350_000m, 2, "전자제품"),
        new("USB 케이블", 5_000m, 10, "주변기기"),
    ];

    private readonly InStockSpec _inStock = new();
    private readonly PriceRangeSpec _affordable = new(10_000m, 100_000m);
    private readonly CategorySpec _electronics = new("전자제품");

    // 테스트 시나리오: & 연산자는 And() 메서드와 동일한 결과를 반환해야 한다
    [Fact]
    public void AndOperator_ShouldProduceSameResult_AsAndMethod()
    {
        // Arrange
        var methodSpec = _inStock.And(_affordable);
        var operatorSpec = _inStock & _affordable;

        // Act & Assert
        foreach (var product in Products)
        {
            operatorSpec.IsSatisfiedBy(product)
                .ShouldBe(methodSpec.IsSatisfiedBy(product),
                    $"Product '{product.Name}' 결과가 다릅니다");
        }
    }

    // 테스트 시나리오: | 연산자는 Or() 메서드와 동일한 결과를 반환해야 한다
    [Fact]
    public void OrOperator_ShouldProduceSameResult_AsOrMethod()
    {
        // Arrange
        var methodSpec = _electronics.Or(_affordable);
        var operatorSpec = _electronics | _affordable;

        // Act & Assert
        foreach (var product in Products)
        {
            operatorSpec.IsSatisfiedBy(product)
                .ShouldBe(methodSpec.IsSatisfiedBy(product),
                    $"Product '{product.Name}' 결과가 다릅니다");
        }
    }

    // 테스트 시나리오: ! 연산자는 Not() 메서드와 동일한 결과를 반환해야 한다
    [Fact]
    public void NotOperator_ShouldProduceSameResult_AsNotMethod()
    {
        // Arrange
        var methodSpec = _electronics.Not();
        var operatorSpec = !_electronics;

        // Act & Assert
        foreach (var product in Products)
        {
            operatorSpec.IsSatisfiedBy(product)
                .ShouldBe(methodSpec.IsSatisfiedBy(product),
                    $"Product '{product.Name}' 결과가 다릅니다");
        }
    }
}
