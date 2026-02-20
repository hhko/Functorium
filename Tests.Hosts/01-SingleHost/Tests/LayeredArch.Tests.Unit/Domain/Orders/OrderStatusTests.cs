using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Orders;

public class OrderStatusTests
{
    [Theory]
    [InlineData("Pending", "Confirmed", true)]
    [InlineData("Pending", "Cancelled", true)]
    [InlineData("Confirmed", "Shipped", true)]
    [InlineData("Confirmed", "Cancelled", true)]
    [InlineData("Shipped", "Delivered", true)]
    [InlineData("Pending", "Shipped", false)]
    [InlineData("Pending", "Delivered", false)]
    [InlineData("Confirmed", "Pending", false)]
    [InlineData("Confirmed", "Delivered", false)]
    [InlineData("Shipped", "Pending", false)]
    [InlineData("Shipped", "Cancelled", false)]
    [InlineData("Delivered", "Pending", false)]
    [InlineData("Delivered", "Cancelled", false)]
    [InlineData("Cancelled", "Pending", false)]
    [InlineData("Cancelled", "Confirmed", false)]
    public void CanTransitionTo_ShouldReturnExpected(string from, string to, bool expected)
    {
        // Arrange
        var fromStatus = OrderStatus.CreateFromValidated(from);
        var toStatus = OrderStatus.CreateFromValidated(to);

        // Act
        var actual = fromStatus.CanTransitionTo(toStatus);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public void Create_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = OrderStatus.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidValue()
    {
        // Act
        var actual = OrderStatus.Create("Invalid");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public void Validate_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = OrderStatus.Validate(value);

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        ((string)actual.ToFin().ThrowIfFail()).ShouldBe(value);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenInvalidValue(string? value)
    {
        // Act
        var actual = OrderStatus.Validate(value!);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public void CreateFromValidated_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = OrderStatus.CreateFromValidated(value);

        // Assert
        ((string)actual).ShouldBe(value);
    }

    [Fact]
    public void CreateFromValidated_ShouldThrow_WhenInvalidValue()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => OrderStatus.CreateFromValidated("Invalid"));
    }
}
