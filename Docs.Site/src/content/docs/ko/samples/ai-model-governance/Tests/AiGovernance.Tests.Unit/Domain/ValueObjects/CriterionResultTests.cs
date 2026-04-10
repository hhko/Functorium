using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class CriterionResultTests
{
    [Theory]
    [InlineData("Pass")]
    [InlineData("Fail")]
    [InlineData("NotApplicable")]
    public void Create_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = CriterionResult.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidValue()
    {
        // Act
        var actual = CriterionResult.Create("Invalid");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldThrow_WhenInvalidValue()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => CriterionResult.CreateFromValidated("Invalid"));
    }
}
