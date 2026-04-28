using LanguageExt;
using LanguageExt.Common;
using Shouldly;
using UnionValueObject.ValueObjects;
using Xunit;
using Unions = Functorium.Domains.ValueObjects.Unions;

namespace UnionValueObject.Tests.Unit;

public sealed class ShapeTests
{
    [Fact]
    public void Match_ReturnsCircleArea_WhenCircle()
    {
        Shape shape = new Shape.Circle(5.0);
        shape.Area.ShouldBe(Math.PI * 25, tolerance: 0.001);
    }

    [Fact]
    public void Match_ReturnsRectangleArea_WhenRectangle()
    {
        Shape shape = new Shape.Rectangle(4.0, 6.0);
        shape.Area.ShouldBe(24.0);
    }

    [Fact]
    public void Match_ReturnsTriangleArea_WhenTriangle()
    {
        Shape shape = new Shape.Triangle(3.0, 4.0);
        shape.Area.ShouldBe(6.0);
    }

    [Fact]
    public void Switch_ExecutesCorrectBranch()
    {
        Shape shape = new Shape.Circle(1.0);
        var result = "";

        shape.Switch(
            circle: _ => result = "circle",
            rectangle: _ => result = "rectangle",
            triangle: _ => result = "triangle");

        result.ShouldBe("circle");
    }

    [Fact]
    public void Is_ReturnsTrueForMatchingCase()
    {
        Shape shape = new Shape.Circle(5.0);
        (shape is Shape.Circle).ShouldBeTrue();
        (shape is Shape.Rectangle).ShouldBeFalse();
    }

    [Fact]
    public void As_ReturnsTypedValueForMatchingCase()
    {
        Shape shape = new Shape.Circle(5.0);
        var circle = shape as Shape.Circle;
        circle.ShouldNotBeNull();
        circle.Radius.ShouldBe(5.0);
    }

    [Fact]
    public void Equality_TwoIdenticalShapes_AreEqual()
    {
        Shape a = new Shape.Circle(5.0);
        Shape b = new Shape.Circle(5.0);
        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentShapes_AreNotEqual()
    {
        Shape a = new Shape.Circle(5.0);
        Shape b = new Shape.Rectangle(5.0, 5.0);
        a.ShouldNotBe(b);
    }
}

public sealed class PaymentMethodTests
{
    [Fact]
    public void CalculateFee_CreditCard_Returns3Percent()
    {
        PaymentMethod method = new PaymentMethod.CreditCard("1234-5678-9012-3456", "12/25");
        method.CalculateFee(10000m).ShouldBe(300m);
    }

    [Fact]
    public void CalculateFee_BankTransfer_ReturnsFixedFee()
    {
        PaymentMethod method = new PaymentMethod.BankTransfer("110-123-456789", "KB");
        method.CalculateFee(10000m).ShouldBe(1000m);
    }

    [Fact]
    public void CalculateFee_Cash_ReturnsZero()
    {
        PaymentMethod method = new PaymentMethod.Cash();
        method.CalculateFee(10000m).ShouldBe(0m);
    }

    [Fact]
    public void DisplayName_CreditCard_ShowsLast4Digits()
    {
        PaymentMethod method = new PaymentMethod.CreditCard("1234-5678-9012-3456", "12/25");
        method.DisplayName.ShouldContain("3456");
    }
}

public sealed class OrderStatusTests
{
    [Fact]
    public void TransitionFrom_ReturnsSuccess_WhenSourceMatches()
    {
        // Arrange
        OrderStatus sut = new OrderStatus.Pending("ORD-001");
        var confirmedAt = new DateTime(2024, 1, 15);

        // Act
        var actual = sut.Confirm(confirmedAt);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var confirmed = actual.ThrowIfFail();
        confirmed.OrderId.ShouldBe("ORD-001");
        confirmed.ConfirmedAt.ShouldBe(confirmedAt);
    }

    [Fact]
    public void TransitionFrom_ReturnsFail_WhenSourceDoesNotMatch()
    {
        // Arrange
        OrderStatus sut = new OrderStatus.Confirmed("ORD-001", new DateTime(2024, 1, 15));

        // Act
        var actual = sut.Confirm(new DateTime(2024, 2, 1));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void TransitionFrom_FailError_ContainsInvalidTransition()
    {
        // Arrange
        OrderStatus sut = new OrderStatus.Confirmed("ORD-001", new DateTime(2024, 1, 15));

        // Act
        var actual = sut.Confirm(new DateTime(2024, 2, 1));

        // Assert
        actual.IsFail.ShouldBeTrue();
        var error = (Error)actual;
        error.Message.ShouldContain("Invalid transition from Confirmed to Confirmed");
    }

    [Fact]
    public void OrderStatus_Implements_IUnionValueObject()
    {
        // Arrange
        OrderStatus sut = new OrderStatus.Pending("ORD-001");

        // Assert
        sut.ShouldBeAssignableTo<Unions.IUnionValueObject>();
    }
}
