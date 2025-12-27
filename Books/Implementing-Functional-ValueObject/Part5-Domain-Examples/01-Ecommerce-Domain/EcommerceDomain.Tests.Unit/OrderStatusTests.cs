namespace EcommerceDomain.Tests.Unit;

/// <summary>
/// OrderStatus 값 객체 테스트 (SmartEnum)
///
/// 학습 목표:
/// 1. SmartEnum 상태 값 검증
/// 2. 상태 전이(TransitionTo) 검증
/// 3. 상태별 속성 검증 (CanCancel)
/// </summary>
[Trait("Part5-Ecommerce-Domain", "OrderStatusTests")]
public class OrderStatusTests
{
    #region 상태 값 테스트

    [Fact]
    public void Pending_HasCorrectProperties()
    {
        // Act & Assert
        OrderStatus.Pending.Value.ShouldBe("PENDING");
        OrderStatus.Pending.DisplayName.ShouldBe("대기중");
        OrderStatus.Pending.CanCancel.ShouldBeTrue();
    }

    [Fact]
    public void Shipped_HasCorrectProperties()
    {
        // Act & Assert
        OrderStatus.Shipped.Value.ShouldBe("SHIPPED");
        OrderStatus.Shipped.DisplayName.ShouldBe("배송중");
        OrderStatus.Shipped.CanCancel.ShouldBeFalse();
    }

    #endregion

    #region 상태 전이 테스트

    [Fact]
    public void TransitionTo_ReturnsSuccess_WhenTransitionIsValid()
    {
        // Arrange
        var status = OrderStatus.Pending;

        // Act
        var actual = status.TransitionTo(OrderStatus.Confirmed);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: s => s.ShouldBe(OrderStatus.Confirmed),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void TransitionTo_ReturnsFail_WhenAlreadyCancelled()
    {
        // Arrange
        var status = OrderStatus.Cancelled;

        // Act
        var actual = status.TransitionTo(OrderStatus.Shipped);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void TransitionTo_ReturnsFail_WhenAlreadyDelivered()
    {
        // Arrange
        var status = OrderStatus.Delivered;

        // Act
        var actual = status.TransitionTo(OrderStatus.Cancelled);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void TransitionTo_ReturnsFail_WhenRevertingToPending()
    {
        // Arrange
        var status = OrderStatus.Confirmed;

        // Act
        var actual = status.TransitionTo(OrderStatus.Pending);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 상태 흐름 테스트

    [Fact]
    public void StatusFlow_FromPendingToDelivered_IsValid()
    {
        // Arrange
        var status = OrderStatus.Pending;

        // Act & Assert - Pending -> Confirmed
        var confirmed = status.TransitionTo(OrderStatus.Confirmed);
        confirmed.IsSucc.ShouldBeTrue();

        // Confirmed -> Shipped
        var shipped = confirmed.Match(s => s, _ => null!).TransitionTo(OrderStatus.Shipped);
        shipped.IsSucc.ShouldBeTrue();

        // Shipped -> Delivered
        var delivered = shipped.Match(s => s, _ => null!).TransitionTo(OrderStatus.Delivered);
        delivered.IsSucc.ShouldBeTrue();
    }

    #endregion
}
