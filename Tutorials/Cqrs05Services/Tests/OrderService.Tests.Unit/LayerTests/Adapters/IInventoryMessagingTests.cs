using OrderService.Adapters.Messaging;
using Cqrs05Services.Messages;
using Shouldly;
using Xunit;

namespace OrderService.Tests.Unit.LayerTests.Adapters;

/// <summary>
/// IInventoryMessaging 인터페이스 정의 검증 테스트
/// </summary>
public sealed class IInventoryMessagingTests
{
    [Fact]
    public void IInventoryMessaging_ShouldHaveCheckInventoryMethod()
    {
        // Arrange & Act
        var method = typeof(IInventoryMessaging).GetMethod("CheckInventory");

        // Assert
        method.ShouldNotBeNull();
        method!.ReturnType.Name.ShouldContain("FinT");
        method.GetParameters().Length.ShouldBe(1);
        method.GetParameters()[0].ParameterType.ShouldBe(typeof(CheckInventoryRequest));
    }

    [Fact]
    public void IInventoryMessaging_ShouldHaveReserveInventoryMethod()
    {
        // Arrange & Act
        var method = typeof(IInventoryMessaging).GetMethod("ReserveInventory");

        // Assert
        method.ShouldNotBeNull();
        method!.ReturnType.Name.ShouldContain("FinT");
        method.GetParameters().Length.ShouldBe(1);
        method.GetParameters()[0].ParameterType.ShouldBe(typeof(ReserveInventoryCommand));
    }

    [Fact]
    public void IInventoryMessaging_ShouldInheritFromIAdapter()
    {
        // Arrange & Act
        var baseType = typeof(IInventoryMessaging).GetInterfaces();

        // Assert
        baseType.ShouldContain(t => t.Name == "IAdapter");
    }
}

