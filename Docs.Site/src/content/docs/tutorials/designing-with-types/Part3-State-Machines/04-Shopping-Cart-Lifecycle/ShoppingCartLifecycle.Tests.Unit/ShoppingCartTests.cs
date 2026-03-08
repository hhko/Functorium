namespace ShoppingCartLifecycle.Tests.Unit;

/// <summary>
/// 쇼핑 카트 라이프사이클 테스트
///
/// 테스트 목적:
/// 1. 유효한 전체 라이프사이클 (Empty → Active → Paid)
/// 2. 상태 건너뛰기 불가 (Empty → Paid 불가)
/// 3. 무효 전이 Fin.Fail 반환
/// </summary>
[Trait("Part3-StateMachines", "04-ShoppingCartLifecycle")]
public class ShoppingCartTests
{
    [Fact]
    public void AddItem_ReturnsActive_WhenEmpty()
    {
        // Arrange
        ShoppingCart cart = new ShoppingCart.Empty();

        // Act
        var actual = ShoppingCart.AddItem(cart, "Widget");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: c => c.ShouldBeOfType<ShoppingCart.Active>(),
            Fail: _ => throw new Exception("실패"));
    }

    [Fact]
    public void AddItem_ReturnsActiveWithMoreItems_WhenActive()
    {
        // Arrange
        ShoppingCart cart = new ShoppingCart.Active(["Widget"]);

        // Act
        var actual = ShoppingCart.AddItem(cart, "Gadget");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: c =>
            {
                var active = c.ShouldBeOfType<ShoppingCart.Active>();
                active.Items.Count.ShouldBe(2);
            },
            Fail: _ => throw new Exception("실패"));
    }

    [Fact]
    public void AddItem_ReturnsFail_WhenPaid()
    {
        // Arrange
        ShoppingCart cart = new ShoppingCart.Paid(["Widget"], 29.99m, DateTime.UtcNow);

        // Act
        var actual = ShoppingCart.AddItem(cart, "Gadget");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Pay_ReturnsPaid_WhenActive()
    {
        // Arrange
        ShoppingCart cart = new ShoppingCart.Active(["Widget"]);

        // Act
        var actual = ShoppingCart.Pay(cart, 29.99m);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: c =>
            {
                var paid = c.ShouldBeOfType<ShoppingCart.Paid>();
                paid.Amount.ShouldBe(29.99m);
            },
            Fail: _ => throw new Exception("실패"));
    }

    [Fact]
    public void Pay_ReturnsFail_WhenEmpty()
    {
        // Arrange
        ShoppingCart cart = new ShoppingCart.Empty();

        // Act
        var actual = ShoppingCart.Pay(cart, 29.99m);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Pay_ReturnsFail_WhenAlreadyPaid()
    {
        // Arrange
        ShoppingCart cart = new ShoppingCart.Paid(["Widget"], 29.99m, DateTime.UtcNow);

        // Act
        var actual = ShoppingCart.Pay(cart, 9.99m);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void FullLifecycle_EmptyToActiveToPlaid_Succeeds()
    {
        // Arrange
        ShoppingCart cart = new ShoppingCart.Empty();

        // Act — Empty → Active
        var addResult = ShoppingCart.AddItem(cart, "Widget");
        addResult.IsSucc.ShouldBeTrue();

        // Act — Active → Paid
        var payResult = addResult.Match(
            Succ: c => ShoppingCart.Pay(c, 29.99m),
            Fail: _ => throw new Exception("실패"));

        // Assert
        payResult.IsSucc.ShouldBeTrue();
        payResult.Match(
            Succ: c => c.ShouldBeOfType<ShoppingCart.Paid>(),
            Fail: _ => throw new Exception("실패"));
    }

    [Fact]
    public void RemoveAllItems_ReturnsEmpty_WhenActive()
    {
        // Arrange
        ShoppingCart cart = new ShoppingCart.Active(["Widget"]);

        // Act
        var actual = ShoppingCart.RemoveAllItems(cart);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: c => c.ShouldBeOfType<ShoppingCart.Empty>(),
            Fail: _ => throw new Exception("실패"));
    }
}
