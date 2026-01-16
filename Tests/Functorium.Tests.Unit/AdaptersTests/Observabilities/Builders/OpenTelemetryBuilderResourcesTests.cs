using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Builders;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class OpenTelemetryBuilderResourcesTests
{
    [Fact]
    public void CreateResourceAttributes_Always_ContainsServiceNameAndVersion()
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "OrderService"
        };

        // Act
        var attributes = OpenTelemetryBuilder.CreateResourceAttributes(options);

        // Assert
        attributes.ShouldContainKey("service.name");
        attributes["service.name"].ShouldBe("OrderService");
        attributes.ShouldContainKey("service.version");
    }

    [Fact]
    public void CreateResourceAttributes_WithServiceNamespace_IncludesServiceNamespace()
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "OrderService",
            ServiceNamespace = "MyCompany.Production"
        };

        // Act
        var attributes = OpenTelemetryBuilder.CreateResourceAttributes(options);

        // Assert
        attributes.ShouldContainKey("service.namespace");
        attributes["service.namespace"].ShouldBe("MyCompany.Production");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateResourceAttributes_WithEmptyServiceNamespace_ExcludesServiceNamespace(string serviceNamespace)
    {
        // Arrange
        var options = new OpenTelemetryOptions
        {
            ServiceName = "OrderService",
            ServiceNamespace = serviceNamespace
        };

        // Act
        var attributes = OpenTelemetryBuilder.CreateResourceAttributes(options);

        // Assert
        attributes.ShouldNotContainKey("service.namespace");
    }
}
