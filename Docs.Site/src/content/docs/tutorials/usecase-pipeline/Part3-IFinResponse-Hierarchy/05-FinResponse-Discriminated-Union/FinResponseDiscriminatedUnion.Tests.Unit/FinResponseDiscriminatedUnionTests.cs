using LanguageExt.Common;
using Shouldly;

namespace FinResponseDiscriminatedUnion.Tests.Unit;

public class FinResponseDiscriminatedUnionTests
{
    [Fact]
    public void Succ_ReturnsSucc_WhenValueProvided()
    {
        // Arrange & Act
        FinResponse<string> actual = FinResponse.Succ("Hello");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ShouldBeOfType<FinResponse<string>.Succ>();
    }

    [Fact]
    public void Fail_ReturnsFail_WhenErrorProvided()
    {
        // Arrange & Act
        FinResponse<string> actual = FinResponse.Fail<string>(Error.New("error"));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.ShouldBeOfType<FinResponse<string>.Fail>();
    }

    [Fact]
    public void Match_ReturnsSuccValue_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);

        // Act
        var actual = sut.Match(Succ: v => v * 2, Fail: _ => 0);

        // Assert
        actual.ShouldBe(84);
    }

    [Fact]
    public void Map_TransformsValue_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(10);

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
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));

        // Act
        var actual = sut.Map(v => v.ToString());

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Bind_ChainsOperations_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(5);

        // Act
        var actual = sut.Bind(v =>
            v > 0 ? FinResponse.Succ(v * 2) : FinResponse.Fail<int>(Error.New("negative")));

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
        FinResponse<int> sut = FinResponse.Succ(5);

        // Act
        var actual = from v in sut select v * 2;

        // Assert
        actual.Match(v => v, _ => 0).ShouldBe(10);
    }

    [Fact]
    public void LinqSelectMany_ChainsOperations_WhenAllSucc()
    {
        // Arrange
        FinResponse<int> a = FinResponse.Succ(3);
        FinResponse<int> b = FinResponse.Succ(4);

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
        FinResponse<string> sut = FinResponse.Fail<string>(Error.New("test error"));

        // Act & Assert
        (sut is IFinResponseWithError).ShouldBeTrue();
    }

    [Fact]
    public void SuccDoesNotImplementIFinResponseWithError_WhenSucc()
    {
        // Arrange
        FinResponse<string> sut = FinResponse.Succ("Hello");

        // Act & Assert
        (sut is IFinResponseWithError).ShouldBeFalse();
    }

    // --- 값 추출 패턴 ---

    [Fact]
    public void ThrowIfFail_ReturnsValue_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);

        // Act
        var actual = sut.ThrowIfFail();

        // Assert
        actual.ShouldBe(42);
    }

    [Fact]
    public void ThrowIfFail_Throws_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));

        // Act & Assert
        Should.Throw<ErrorException>(() => sut.ThrowIfFail());
    }

    [Fact]
    public void IfFail_ReturnsSuccValue_WhenSuccWithFunc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);

        // Act
        var actual = sut.IfFail(err => -1);

        // Assert
        actual.ShouldBe(42);
    }

    [Fact]
    public void IfFail_ReturnsFallback_WhenFailWithFunc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));

        // Act
        var actual = sut.IfFail(err => -1);

        // Assert
        actual.ShouldBe(-1);
    }

    [Fact]
    public void IfFail_ReturnsSuccValue_WhenSuccWithValue()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);

        // Act
        var actual = sut.IfFail(-1);

        // Assert
        actual.ShouldBe(42);
    }

    [Fact]
    public void IfFail_ReturnsFallback_WhenFailWithValue()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));

        // Act
        var actual = sut.IfFail(-1);

        // Assert
        actual.ShouldBe(-1);
    }

    [Fact]
    public void IfFail_InvokesAction_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));
        var invoked = false;

        // Act
        sut.IfFail(_ => invoked = true);

        // Assert
        invoked.ShouldBeTrue();
    }

    [Fact]
    public void IfFail_DoesNotInvokeAction_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);
        var invoked = false;

        // Act
        sut.IfFail(_ => invoked = true);

        // Assert
        invoked.ShouldBeFalse();
    }

    [Fact]
    public void IfSucc_InvokesAction_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);
        var captured = 0;

        // Act
        sut.IfSucc(v => captured = v);

        // Assert
        captured.ShouldBe(42);
    }

    [Fact]
    public void IfSucc_DoesNotInvokeAction_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));
        var invoked = false;

        // Act
        sut.IfSucc(_ => invoked = true);

        // Assert
        invoked.ShouldBeFalse();
    }

    [Fact]
    public void MatchVoid_InvokesSuccAction_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);
        var captured = 0;

        // Act
        sut.Match(Succ: v => captured = v, Fail: _ => { });

        // Assert
        captured.ShouldBe(42);
    }

    [Fact]
    public void MatchVoid_InvokesFailAction_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("test"));
        var invoked = false;

        // Act
        sut.Match(Succ: _ => { }, Fail: _ => invoked = true);

        // Assert
        invoked.ShouldBeTrue();
    }

    // --- 에러 트랙 연산 ---

    [Fact]
    public void MapFail_TransformsError_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("original"));

        // Act
        var actual = sut.MapFail(e => Error.New("mapped"));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(_ => "", e => e.Message).ShouldBe("mapped");
    }

    [Fact]
    public void MapFail_PreservesValue_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);

        // Act
        var actual = sut.MapFail(e => Error.New("mapped"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(42);
    }

    [Fact]
    public void BiMap_TransformsSuccValue_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(10);

        // Act
        var actual = sut.BiMap(v => v.ToString(), e => Error.New("mapped"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("10");
    }

    [Fact]
    public void BiMap_TransformsError_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("original"));

        // Act
        var actual = sut.BiMap(v => v.ToString(), e => Error.New("mapped"));

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(_ => "", e => e.Message).ShouldBe("mapped");
    }

    [Fact]
    public void BiBind_InvokesSuccFunc_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(10);

        // Act
        var actual = sut.BiBind(
            v => FinResponse.Succ(v.ToString()),
            e => FinResponse.Fail<string>(e));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("10");
    }

    [Fact]
    public void BiBind_InvokesFailFunc_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));

        // Act
        var actual = sut.BiBind(
            v => FinResponse.Succ(v.ToString()),
            _ => FinResponse.Succ("recovered"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("recovered");
    }

    [Fact]
    public void BindFail_PreservesSucc_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);

        // Act
        var actual = sut.BindFail(_ => FinResponse.Succ(0));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(42);
    }

    [Fact]
    public void BindFail_AppliesRecovery_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));

        // Act
        var actual = sut.BindFail(_ => FinResponse.Succ(0));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(0);
    }

    // --- Boolean 및 Choice 연산자 ---

    [Fact]
    public void BooleanOperator_SupportsIfPattern_WhenSucc()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Succ(42);

        // Act & Assert
        if (sut)
            true.ShouldBeTrue(); // Succ → true 분기
        else
            true.ShouldBeFalse(); // 도달하지 않아야 함
    }

    [Fact]
    public void BooleanOperator_SupportsIfPattern_WhenFail()
    {
        // Arrange
        FinResponse<int> sut = FinResponse.Fail<int>(Error.New("error"));

        // Act & Assert
        if (sut)
            true.ShouldBeFalse(); // 도달하지 않아야 함
        else
            true.ShouldBeTrue(); // Fail → false 분기
    }

    [Fact]
    public void ChoiceOperator_ReturnsLhs_WhenLhsIsSucc()
    {
        // Arrange
        FinResponse<int> lhs = FinResponse.Succ(1);
        FinResponse<int> rhs = FinResponse.Succ(2);

        // Act
        var actual = lhs | rhs;

        // Assert
        actual.ThrowIfFail().ShouldBe(1);
    }

    [Fact]
    public void ChoiceOperator_ReturnsRhs_WhenLhsIsFail()
    {
        // Arrange
        FinResponse<int> lhs = FinResponse.Fail<int>(Error.New("error"));
        FinResponse<int> rhs = FinResponse.Succ(2);

        // Act
        var actual = lhs | rhs;

        // Assert
        actual.ThrowIfFail().ShouldBe(2);
    }
}
