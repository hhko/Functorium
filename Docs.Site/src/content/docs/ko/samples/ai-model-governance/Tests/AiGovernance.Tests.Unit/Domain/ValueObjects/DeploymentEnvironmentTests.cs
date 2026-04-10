using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class DeploymentEnvironmentTests
{
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Create_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = DeploymentEnvironment.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidValue()
    {
        // Act
        var actual = DeploymentEnvironment.Create("Development");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldThrow_WhenInvalidValue()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => DeploymentEnvironment.CreateFromValidated("Invalid"));
    }
}
