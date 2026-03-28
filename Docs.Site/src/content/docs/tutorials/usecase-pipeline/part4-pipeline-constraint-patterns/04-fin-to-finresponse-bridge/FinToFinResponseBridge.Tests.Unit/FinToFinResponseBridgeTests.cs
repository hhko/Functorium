namespace FinToFinResponseBridge.Tests.Unit;

public class FinToFinResponseBridgeTests
{
    [Fact]
    public void DirectConversion_ReturnsSucc_WhenFinIsSucc()
    {
        // Act
        var actual = BridgeExamples.DirectConversion();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void MappedConversion_ReturnsLength_WhenFinIsSucc()
    {
        // Act
        var actual = BridgeExamples.MappedConversion();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(5);
    }

    [Fact]
    public void FactoryConversion_ReturnsNewValue_WhenFinIsSucc()
    {
        // Act
        var actual = BridgeExamples.FactoryConversion();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("Deleted successfully");
    }

    [Fact]
    public void FailConversion_ReturnsFail_WhenFinIsFail()
    {
        // Act
        var actual = BridgeExamples.FailConversion();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CustomConversion_ReturnsTransformedSucc_WhenFinIsSucc()
    {
        // Act
        var actual = BridgeExamples.CustomConversion();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("Value is 42");
    }
}
