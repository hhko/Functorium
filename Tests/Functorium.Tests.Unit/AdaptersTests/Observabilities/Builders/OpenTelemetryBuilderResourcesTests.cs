using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Builders;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class OpenTelemetryBuilderResourcesTests
{
    [Fact]
    public void CreateResourceAttributes_Always_ContainsAllRequiredAttributes()
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
        attributes.ShouldContainKey("service.name");
        attributes["service.name"].ShouldBe("OrderService");

        attributes.ShouldContainKey("service.version");

        attributes.ShouldContainKey("service.namespace");
        attributes["service.namespace"].ShouldBe("MyCompany.Production");
    }

    [Fact]
    public void CreateResourceAttributes_ShouldReturn_ExactlyThreeAttributes()
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
        attributes.Count.ShouldBe(3);
    }
}
