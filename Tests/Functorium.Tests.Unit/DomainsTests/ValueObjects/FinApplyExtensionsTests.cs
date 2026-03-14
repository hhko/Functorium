using Functorium.Domains.ValueObjects.Validations;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.ValueObjects;

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class FinApplyExtensionsTests
{
    #region 2-Tuple Apply

    [Fact]
    public void Apply_2Tuple_ReturnsSuccess_WhenAllSucceed()
    {
        // Arrange
        Fin<string> v1 = Fin.Succ("test");
        Fin<int> v2 = Fin.Succ(42);

        // Act
        Fin<(string, int)> actual = (v1, v2)
            .Apply((a, b) => (a, b));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var value = actual.ThrowIfFail();
        value.Item1.ShouldBe("test");
        value.Item2.ShouldBe(42);
    }

    [Fact]
    public void Apply_2Tuple_CollectsAllErrors_WhenBothFail()
    {
        // Arrange
        Fin<string> v1 = Fin.Fail<string>(Error.New("Error 1"));
        Fin<int> v2 = Fin.Fail<int>(Error.New("Error 2"));

        // Act
        var actual = (v1, v2).Apply((a, b) => (a, b));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Apply_2Tuple_ReturnsFailure_WhenFirstFails()
    {
        // Arrange
        Fin<string> v1 = Fin.Fail<string>(Error.New("Error 1"));
        Fin<int> v2 = Fin.Succ(42);

        // Act
        var actual = (v1, v2).Apply((a, b) => (a, b));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Apply_2Tuple_ReturnsFailure_WhenSecondFails()
    {
        // Arrange
        Fin<string> v1 = Fin.Succ("test");
        Fin<int> v2 = Fin.Fail<int>(Error.New("Error 2"));

        // Act
        var actual = (v1, v2).Apply((a, b) => (a, b));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 3-Tuple Apply

    [Fact]
    public void Apply_3Tuple_ReturnsSuccess_WhenAllSucceed()
    {
        // Arrange
        Fin<string> v1 = Fin.Succ("test");
        Fin<int> v2 = Fin.Succ(42);
        Fin<decimal> v3 = Fin.Succ(100.5m);

        // Act
        Fin<(string, int, decimal)> actual = (v1, v2, v3)
            .Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var value = actual.ThrowIfFail();
        value.Item1.ShouldBe("test");
        value.Item2.ShouldBe(42);
        value.Item3.ShouldBe(100.5m);
    }

    [Fact]
    public void Apply_3Tuple_CollectsAllErrors_WhenAllFail()
    {
        // Arrange
        Fin<string> v1 = Fin.Fail<string>(Error.New("Error 1"));
        Fin<int> v2 = Fin.Fail<int>(Error.New("Error 2"));
        Fin<decimal> v3 = Fin.Fail<decimal>(Error.New("Error 3"));

        // Act
        var actual = (v1, v2, v3).Apply((a, b, c) => (a, b, c));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 4-Tuple Apply

    [Fact]
    public void Apply_4Tuple_ReturnsSuccess_WhenAllSucceed()
    {
        // Arrange
        Fin<string> v1 = Fin.Succ("test");
        Fin<int> v2 = Fin.Succ(42);
        Fin<decimal> v3 = Fin.Succ(100.5m);
        Fin<string> v4 = Fin.Succ("extra");

        // Act
        Fin<(string, int, decimal, string)> actual = (v1, v2, v3, v4)
            .Apply((a, b, c, d) => (a, b, c, d));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var value = actual.ThrowIfFail();
        value.Item1.ShouldBe("test");
        value.Item2.ShouldBe(42);
        value.Item3.ShouldBe(100.5m);
        value.Item4.ShouldBe("extra");
    }

    [Fact]
    public void Apply_4Tuple_CollectsAllErrors_WhenAllFail()
    {
        // Arrange
        Fin<string> v1 = Fin.Fail<string>(Error.New("Error 1"));
        Fin<int> v2 = Fin.Fail<int>(Error.New("Error 2"));
        Fin<decimal> v3 = Fin.Fail<decimal>(Error.New("Error 3"));
        Fin<string> v4 = Fin.Fail<string>(Error.New("Error 4"));

        // Act
        var actual = (v1, v2, v3, v4).Apply((a, b, c, d) => (a, b, c, d));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 5-Tuple Apply

    [Fact]
    public void Apply_5Tuple_ReturnsSuccess_WhenAllSucceed()
    {
        // Arrange
        Fin<string> v1 = Fin.Succ("test");
        Fin<int> v2 = Fin.Succ(42);
        Fin<decimal> v3 = Fin.Succ(100.5m);
        Fin<string> v4 = Fin.Succ("extra");
        Fin<bool> v5 = Fin.Succ(true);

        // Act
        Fin<(string, int, decimal, string, bool)> actual = (v1, v2, v3, v4, v5)
            .Apply((a, b, c, d, e) => (a, b, c, d, e));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var value = actual.ThrowIfFail();
        value.Item1.ShouldBe("test");
        value.Item2.ShouldBe(42);
        value.Item3.ShouldBe(100.5m);
        value.Item4.ShouldBe("extra");
        value.Item5.ShouldBeTrue();
    }

    [Fact]
    public void Apply_5Tuple_CollectsAllErrors_WhenAllFail()
    {
        // Arrange
        Fin<string> v1 = Fin.Fail<string>(Error.New("Error 1"));
        Fin<int> v2 = Fin.Fail<int>(Error.New("Error 2"));
        Fin<decimal> v3 = Fin.Fail<decimal>(Error.New("Error 3"));
        Fin<string> v4 = Fin.Fail<string>(Error.New("Error 4"));
        Fin<bool> v5 = Fin.Fail<bool>(Error.New("Error 5"));

        // Act
        var actual = (v1, v2, v3, v4, v5).Apply((a, b, c, d, e) => (a, b, c, d, e));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion
}
