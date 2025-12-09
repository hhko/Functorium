using Ardalis.SmartEnum;
using static Functorium.Adapters.Observabilities.OpenTelemetryOptions;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class OtlpCollectorProtocolTests
{
    #region SmartEnum Values Tests

    [Fact]
    public void Grpc_HasCorrectNameAndValue()
    {
        // Arrange & Act
        var sut = OtlpCollectorProtocol.Grpc;

        // Assert
        sut.Name.ShouldBe("Grpc");
        sut.Value.ShouldBe(1);
    }

    [Fact]
    public void HttpProtobuf_HasCorrectNameAndValue()
    {
        // Arrange & Act
        var sut = OtlpCollectorProtocol.HttpProtobuf;

        // Assert
        sut.Name.ShouldBe("HttpProtobuf");
        sut.Value.ShouldBe(2);
    }

    #endregion

    #region SmartEnum List Tests

    [Fact]
    public void List_ContainsTwoProtocols()
    {
        // Arrange & Act
        var actual = SmartEnum<OtlpCollectorProtocol>.List;

        // Assert
        actual.Count.ShouldBe(2);
    }

    [Fact]
    public void List_ContainsGrpcAndHttpProtobuf()
    {
        // Arrange & Act
        var actual = SmartEnum<OtlpCollectorProtocol>.List;

        // Assert
        actual.ShouldContain(OtlpCollectorProtocol.Grpc);
        actual.ShouldContain(OtlpCollectorProtocol.HttpProtobuf);
    }

    #endregion

    #region TryFromName Tests

    [Theory]
    [InlineData("Grpc")]
    [InlineData("HttpProtobuf")]
    public void TryFromName_ReturnsTrue_WhenValidProtocolName(string protocolName)
    {
        // Arrange & Act
        bool actual = SmartEnum<OtlpCollectorProtocol>.TryFromName(protocolName, out var protocol);

        // Assert
        actual.ShouldBeTrue();
        protocol.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("grpc")]
    [InlineData("GRPC")]
    [InlineData("httpprotobuf")]
    [InlineData("InvalidProtocol")]
    [InlineData("")]
    public void TryFromName_ReturnsFalse_WhenInvalidOrCaseMismatchProtocolName(string protocolName)
    {
        // Arrange & Act
        bool actual = SmartEnum<OtlpCollectorProtocol>.TryFromName(protocolName, out var protocol);

        // Assert
        actual.ShouldBeFalse();
        protocol.ShouldBeNull();
    }

    [Theory]
    [InlineData("grpc", true)]
    [InlineData("GRPC", true)]
    [InlineData("httpprotobuf", true)]
    [InlineData("HTTPPROTOBUF", true)]
    public void TryFromName_ReturnsTrue_WhenIgnoreCaseIsTrue(string protocolName, bool ignoreCase)
    {
        // Arrange & Act
        bool actual = SmartEnum<OtlpCollectorProtocol>.TryFromName(protocolName, ignoreCase, out var protocol);

        // Assert
        actual.ShouldBeTrue();
        protocol.ShouldNotBeNull();
    }

    #endregion

    #region TryFromValue Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void TryFromValue_ReturnsTrue_WhenValidProtocolValue(int protocolValue)
    {
        // Arrange & Act
        bool actual = SmartEnum<OtlpCollectorProtocol>.TryFromValue(protocolValue, out var protocol);

        // Assert
        actual.ShouldBeTrue();
        protocol.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public void TryFromValue_ReturnsFalse_WhenInvalidProtocolValue(int protocolValue)
    {
        // Arrange & Act
        bool actual = SmartEnum<OtlpCollectorProtocol>.TryFromValue(protocolValue, out var protocol);

        // Assert
        actual.ShouldBeFalse();
        protocol.ShouldBeNull();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_ReturnsTrue_WhenSameProtocol()
    {
        // Arrange
        var protocol1 = OtlpCollectorProtocol.Grpc;
        var protocol2 = OtlpCollectorProtocol.Grpc;

        // Act
        bool actual = protocol1.Equals(protocol2);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDifferentProtocol()
    {
        // Arrange
        var protocol1 = OtlpCollectorProtocol.Grpc;
        var protocol2 = OtlpCollectorProtocol.HttpProtobuf;

        // Act
        bool actual = protocol1.Equals(protocol2);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void OperatorEquals_ReturnsTrue_WhenSameProtocol()
    {
        // Arrange
        var protocol1 = OtlpCollectorProtocol.Grpc;
        var protocol2 = OtlpCollectorProtocol.Grpc;

        // Act
        bool actual = protocol1 == protocol2;

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void OperatorNotEquals_ReturnsTrue_WhenDifferentProtocol()
    {
        // Arrange
        var protocol1 = OtlpCollectorProtocol.Grpc;
        var protocol2 = OtlpCollectorProtocol.HttpProtobuf;

        // Act
        bool actual = protocol1 != protocol2;

        // Assert
        actual.ShouldBeTrue();
    }

    #endregion
}
