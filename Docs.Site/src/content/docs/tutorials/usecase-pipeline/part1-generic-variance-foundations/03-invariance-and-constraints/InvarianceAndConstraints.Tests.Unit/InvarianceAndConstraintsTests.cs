using LanguageExt;
using LanguageExt.Common;
using Shouldly;

namespace InvarianceAndConstraints.Tests.Unit;

public class InvarianceAndConstraintsTests
{
    [Fact]
    public void GetAnimals_ReturnsDogs_WhenListDogReturnedAsIEnumerableAnimal()
    {
        // Act
        var actual = InvarianceExamples.GetAnimals().ToList();

        // Assert
        actual.Count.ShouldBe(1);
        actual[0].ShouldBeOfType<Dog>();
    }

    [Fact]
    public void ProcessFin_ReturnsSuccess_WhenFinIsSucc()
    {
        // Arrange
        Fin<string> fin = Fin.Succ("Hello");

        // Act
        var actual = SealedStructConstraint.ProcessFin(fin);

        // Assert
        actual.ShouldBe("Success: Hello");
    }

    [Fact]
    public void ProcessFin_ReturnsFail_WhenFinIsFail()
    {
        // Arrange
        Fin<string> fin = Fin.Fail<string>(Error.New("Something went wrong"));

        // Act
        var actual = SealedStructConstraint.ProcessFin(fin);

        // Assert
        actual.ShouldStartWith("Fail:");
    }

    [Fact]
    public void ProcessResult_ReturnsSuccess_WhenSuccessResultProvided()
    {
        // Arrange
        var sut = new SuccessResult("Data");

        // Act
        var actual = SealedStructConstraint.ProcessResult(sut);

        // Assert
        actual.ShouldBe("Success");
    }

    [Fact]
    public void ProcessResult_ReturnsFail_WhenFailResultProvided()
    {
        // Arrange
        var sut = new FailResult("Error");

        // Act
        var actual = SealedStructConstraint.ProcessResult(sut);

        // Assert
        actual.ShouldBe("Fail");
    }

    [Fact]
    public void IResult_IsSuccIsTrue_WhenSuccessResult()
    {
        // Arrange
        IResult sut = new SuccessResult("Data");

        // Act & Assert
        sut.IsSucc.ShouldBeTrue();
        sut.IsFail.ShouldBeFalse();
    }

    [Fact]
    public void IResult_IsFailIsTrue_WhenFailResult()
    {
        // Arrange
        IResult sut = new FailResult("Error");

        // Act & Assert
        sut.IsSucc.ShouldBeFalse();
        sut.IsFail.ShouldBeTrue();
    }
}
