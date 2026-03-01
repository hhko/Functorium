using LanguageExt.Common;
using Shouldly;

namespace FinResponseDiscriminatedUnion.Tests.Unit;

public class FinResponseDiscriminatedUnionTests
{
    [Fact]
    public void Succ_ReturnsSucc_WhenValueProvided()
    {
        // Arrange & Act
        FinResponse<string> actual = FinResponseFactory.Succ("Hello");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ShouldBeOfType<FinResponse<string>.Succ>();
    }

    [Fact]
    public void Fail_ReturnsFail_WhenErrorProvided()
    {
        // Arrange & Act
        FinResponse<string> actual = FinResponseFactory.Fail<string>(Error.New("error"));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.ShouldBeOfType<FinResponse<string>.Fail>();
    }

    [Fact]
    public void Match_ReturnsSuccValue_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponseFactory.Succ(42);

        // Act
        var actual = sut.Match(Succ: v => v * 2, Fail: _ => 0);

        // Assert
        actual.ShouldBe(84);
    }

    [Fact]
    public void Map_TransformsValue_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponseFactory.Succ(10);

        // Act
        var actual = sut.Map(v => v.ToString());

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(v => v, _ => "").ShouldBe("10");
    }

    [Fact]
    public void Map_PropagatesFail_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponseFactory.Fail<int>(Error.New("error"));

        // Act
        var actual = sut.Map(v => v.ToString());

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Bind_ChainsOperations_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponseFactory.Succ(5);

        // Act
        var actual = sut.Bind(v =>
            v > 0 ? FinResponseFactory.Succ(v * 2) : FinResponseFactory.Fail<int>(Error.New("negative")));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(v => v, _ => 0).ShouldBe(10);
    }

    [Fact]
    public void ImplicitConversion_CreatesSucc_WhenValueAssigned()
    {
        // Act
        FinResponse<string> actual = "Hello";

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ImplicitConversion_CreatesFail_WhenErrorAssigned()
    {
        // Act
        FinResponse<string> actual = Error.New("error");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFail_ReturnsFail_WhenCalledViaFactory()
    {
        // Act - CRTP: static abstract 호출
        var actual = FinResponse<string>.CreateFail(Error.New("error"));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void LinqSelect_TransformsValue_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponseFactory.Succ(5);

        // Act
        var actual = from v in sut select v * 2;

        // Assert
        actual.Match(v => v, _ => 0).ShouldBe(10);
    }

    [Fact]
    public void LinqSelectMany_ChainsOperations_WhenAllSucc()
    {
        // Arrange
        FinResponse<int> a = FinResponseFactory.Succ(3);
        FinResponse<int> b = FinResponseFactory.Succ(4);

        // Act
        var actual = from x in a
                     from y in b
                     select x + y;

        // Assert
        actual.Match(v => v, _ => 0).ShouldBe(7);
    }

    [Fact]
    public void FailImplementsIFinResponseWithError_WhenFail()
    {
        // Arrange
        FinResponse<string> sut = FinResponseFactory.Fail<string>(Error.New("test error"));

        // Act & Assert
        (sut is IFinResponseWithError).ShouldBeTrue();
    }

    [Fact]
    public void SuccDoesNotImplementIFinResponseWithError_WhenSucc()
    {
        // Arrange
        FinResponse<string> sut = FinResponseFactory.Succ("Hello");

        // Act & Assert
        (sut is IFinResponseWithError).ShouldBeFalse();
    }
}
