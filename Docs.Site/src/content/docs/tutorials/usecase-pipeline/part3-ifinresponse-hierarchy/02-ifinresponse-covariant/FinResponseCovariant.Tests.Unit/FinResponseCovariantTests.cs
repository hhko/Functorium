using Shouldly;

namespace FinResponseCovariant.Tests.Unit;

public class FinResponseCovariantTests
{
    [Fact]
    public void Assign_Succeeds_WhenStringResponseAssignedToObjectResponse()
    {
        // Arrange
        IFinResponse<string> stringResponse = CovariantResponse<string>.Succ("Hello");

        // Act - 공변성: IFinResponse<string> → IFinResponse<object>
        IFinResponse<object> objectResponse = stringResponse;

        // Assert
        objectResponse.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Assign_Succeeds_WhenStringResponseAssignedToNonGenericIFinResponse()
    {
        // Arrange
        IFinResponse<string> stringResponse = CovariantResponse<string>.Succ("Hello");

        // Act
        IFinResponse nonGeneric = stringResponse;

        // Assert
        nonGeneric.IsSucc.ShouldBeTrue();
    }
}
