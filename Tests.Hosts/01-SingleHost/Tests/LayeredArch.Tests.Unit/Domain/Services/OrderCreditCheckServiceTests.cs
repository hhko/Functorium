using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.Services;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Services;

public class OrderCreditCheckServiceTests
{
    private readonly OrderCreditCheckService _sut = new();

    private static Customer CreateSampleCustomer(decimal creditLimit = 5000m)
    {
        return Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(creditLimit).ThrowIfFail());
    }

    private static Order CreateSampleOrder(decimal unitPrice = 100m, int quantity = 1)
    {
        var line = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(quantity).ThrowIfFail(),
            Money.Create(unitPrice).ThrowIfFail()).ThrowIfFail();
        return Order.Create(
            CustomerId.New(),
            [line],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsSuccess_WhenAmountWithinLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(3000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsFail_WhenAmountExceedsLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(6000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsSuccess_WhenAmountEqualsLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(5000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimitWithExistingOrders_ReturnsSuccess_WhenTotalWithinLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var existingOrders = Seq(
            CreateSampleOrder(unitPrice: 1000m),
            CreateSampleOrder(unitPrice: 1500m));
        var newOrderAmount = Money.Create(2000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimitWithExistingOrders(customer, existingOrders, newOrderAmount);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimitWithExistingOrders_ReturnsFail_WhenTotalExceedsLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var existingOrders = Seq(
            CreateSampleOrder(unitPrice: 2000m),
            CreateSampleOrder(unitPrice: 2000m));
        var newOrderAmount = Money.Create(2000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimitWithExistingOrders(customer, existingOrders, newOrderAmount);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimitWithExistingOrders_ReturnsSuccess_WhenNoExistingOrders()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var existingOrders = Seq<Order>();
        var newOrderAmount = Money.Create(3000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimitWithExistingOrders(customer, existingOrders, newOrderAmount);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
