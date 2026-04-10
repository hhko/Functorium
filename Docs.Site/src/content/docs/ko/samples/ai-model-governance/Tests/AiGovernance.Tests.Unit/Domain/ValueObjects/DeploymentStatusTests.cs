using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class DeploymentStatusTests
{
    [Theory]
    [InlineData("Draft", "PendingReview", true)]
    [InlineData("PendingReview", "Active", true)]
    [InlineData("PendingReview", "Rejected", true)]
    [InlineData("Active", "Quarantined", true)]
    [InlineData("Active", "Decommissioned", true)]
    [InlineData("Quarantined", "Active", true)]
    [InlineData("Quarantined", "Decommissioned", true)]
    [InlineData("Draft", "Active", false)]
    [InlineData("Draft", "Quarantined", false)]
    [InlineData("Active", "Draft", false)]
    [InlineData("Active", "PendingReview", false)]
    [InlineData("Decommissioned", "Active", false)]
    [InlineData("Rejected", "Active", false)]
    public void CanTransitionTo_ShouldReturnExpected(string from, string to, bool expected)
    {
        // Arrange
        var fromStatus = DeploymentStatus.CreateFromValidated(from);
        var toStatus = DeploymentStatus.CreateFromValidated(to);

        // Act
        var actual = fromStatus.CanTransitionTo(toStatus);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("PendingReview")]
    [InlineData("Active")]
    [InlineData("Quarantined")]
    [InlineData("Decommissioned")]
    [InlineData("Rejected")]
    public void Create_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = DeploymentStatus.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidValue()
    {
        // Act
        var actual = DeploymentStatus.Create("Invalid");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldThrow_WhenInvalidValue()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => DeploymentStatus.CreateFromValidated("Invalid"));
    }
}
