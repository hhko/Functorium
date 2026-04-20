using Functorium.Applications.Linq;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;
using static LanguageExt.Prelude;

namespace Functorium.Tests.Unit.ApplicationsTests.Linq;

/// <summary>
/// FinT → Fin SelectMany 확장 메서드 테스트
/// FinT 체인 중간에 Fin 결과를 체이닝하는 시나리오 테스트
/// </summary>
[Trait(nameof(UnitTest), UnitTest.Functorium_Applications)]
public class FinTToFinSelectManyTests
{
    #region Success Cases

    [Fact]
    public async Task SelectMany_ReturnsSuccess_WhenBothFinTAndFinSucceed()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(10));
        Func<int, Fin<string>> finSelector = x => Fin.Succ($"Value: {x}");

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in finSelector(a)
            select b;

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("Value: 10");
    }

    [Fact]
    public async Task SelectMany_ReturnsSuccess_WhenChainedWithMultipleOperations()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(5));

        // Act
        FinT<IO, int> result =
            from a in finT
            from b in Fin.Succ(a * 2)  // 10
            from c in Fin.Succ(b + 3)  // 13
            select c;

        Fin<int> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(13);
    }

    [Fact]
    public async Task SelectMany_ProjectsBothValues_WhenUsingProjector()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(10));

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in Fin.Succ(20)
            select $"{a}+{b}={a + b}";

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("10+20=30");
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task SelectMany_ReturnsFail_WhenFinTFails()
    {
        // Arrange
        Error error = Error.New("FinT failed");
        FinT<IO, int> finT = FinT.Fail<IO, int>(error);

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in Fin.Succ($"Value: {a}")
            select b;

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: e => e.Message.ShouldBe("FinT failed"));
    }

    [Fact]
    public async Task SelectMany_ReturnsFail_WhenFinSelectorFails()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(10));
        Error error = Error.New("Fin selector failed");

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in Fin.Fail<string>(error)
            select b;

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: e => e.Message.ShouldBe("Fin selector failed"));
    }

    #endregion

    #region Real-world Scenario: Domain Logic

    [Fact]
    public async Task SelectMany_WorksWithDomainLogic_WhenDeductingStock()
    {
        // Arrange - 재고 차감 시나리오 시뮬레이션
        var product = new TestProduct(100);
        FinT<IO, TestProduct> getProduct = FinT.lift<IO, TestProduct>(Fin.Succ(product));

        // Act
        FinT<IO, int> result =
            from p in getProduct
            from _ in p.DeductStock(30)  // Fin<Unit> 반환
            select p.Stock;

        Fin<int> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(70);
    }

    [Fact]
    public async Task SelectMany_ReturnsFail_WhenDomainLogicFails()
    {
        // Arrange - 재고 부족 시나리오
        var product = new TestProduct(50);
        FinT<IO, TestProduct> getProduct = FinT.lift<IO, TestProduct>(Fin.Succ(product));

        // Act
        FinT<IO, int> result =
            from p in getProduct
            from _ in p.DeductStock(100)  // 재고 부족으로 실패
            select p.Stock;

        Fin<int> actual = await result.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: e => e.Message.ShouldContain("Insufficient stock"));
    }

    private sealed class TestProduct
    {
        public int Stock { get; private set; }

        public TestProduct(int stock) => Stock = stock;

        public Fin<LanguageExt.Unit> DeductStock(int quantity)
        {
            if (quantity > Stock)
                return Fin.Fail<LanguageExt.Unit>(Error.New($"Insufficient stock. Current: {Stock}, Requested: {quantity}"));

            Stock -= quantity;
            return Fin.Succ(LanguageExt.Unit.Default);
        }
    }

    #endregion
}

/// <summary>
/// FinT → IO SelectMany 확장 메서드 테스트
/// FinT 체인 중간에 순수 IO 효과를 체이닝하는 시나리오 테스트
/// </summary>
[Trait(nameof(UnitTest), UnitTest.Functorium_Applications)]
public class FinTToIOSelectManyTests
{
    #region Success Cases

    [Fact]
    public async Task SelectMany_ReturnsSuccess_WhenBothFinTAndIOSucceed()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(10));

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in IO.lift(() => $"Timestamp: {a}")
            select b;

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("Timestamp: 10");
    }

    [Fact]
    public async Task SelectMany_ProjectsBothValues_WhenUsingProjector()
    {
        // Arrange
        FinT<IO, string> finT = FinT.lift<IO, string>(Fin.Succ("Hello"));

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in IO.lift(() => " World")
            select a + b;

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("Hello World");
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task SelectMany_ReturnsFail_WhenFinTFails()
    {
        // Arrange
        Error error = Error.New("FinT failed");
        FinT<IO, int> finT = FinT.Fail<IO, int>(error);

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in IO.lift(() => $"Value: {a}")
            select b;

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: e => e.Message.ShouldBe("FinT failed"));
    }

    #endregion

    #region Real-world Scenario: Timestamp/GUID Generation

    [Fact]
    public async Task SelectMany_WorksWithTimestampGeneration()
    {
        // Arrange
        FinT<IO, string> finT = FinT.lift<IO, string>(Fin.Succ("Event"));

        // Act
        FinT<IO, (string EventName, DateTime Timestamp)> result =
            from eventName in finT
            from timestamp in IO.lift(() => DateTime.UtcNow)
            select (eventName, timestamp);

        Fin<(string EventName, DateTime Timestamp)> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var tuple = actual.ThrowIfFail();
        tuple.EventName.ShouldBe("Event");
        tuple.Timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task SelectMany_WorksWithGuidGeneration()
    {
        // Arrange
        FinT<IO, string> finT = FinT.lift<IO, string>(Fin.Succ("Entity"));

        // Act
        FinT<IO, (string Name, Guid Id)> result =
            from name in finT
            from id in IO.lift(() => Guid.NewGuid())
            select (name, id);

        Fin<(string Name, Guid Id)> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var tuple = actual.ThrowIfFail();
        tuple.Name.ShouldBe("Entity");
        tuple.Id.ShouldNotBe(Guid.Empty);
    }

    #endregion
}

/// <summary>
/// FinT → Validation SelectMany 확장 메서드 테스트
/// FinT 체인 중간에 Validation 결과를 체이닝하는 시나리오 테스트
/// </summary>
[Trait(nameof(UnitTest), UnitTest.Functorium_Applications)]
public class FinTToValidationSelectManyTests
{
    #region Success Cases

    [Fact]
    public async Task SelectMany_ReturnsSuccess_WhenBothFinTAndValidationSucceed()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(10));

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in Success<Error, string>($"Valid: {a}")
            select b;

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe("Valid: 10");
    }

    [Fact]
    public async Task SelectMany_ReturnsSuccess_WhenChainedWithMultipleValidations()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(5));

        // Act
        FinT<IO, int> result =
            from a in finT
            from b in ValidatePositive(a)
            from c in ValidateMax(b, 100)
            select c;

        Fin<int> actual = await result.Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(5);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task SelectMany_ReturnsFail_WhenFinTFails()
    {
        // Arrange
        Error error = Error.New("FinT failed");
        FinT<IO, int> finT = FinT.Fail<IO, int>(error);

        // Act
        FinT<IO, string> result =
            from a in finT
            from b in Success<Error, string>($"Valid: {a}")
            select b;

        Fin<string> actual = await result.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: e => e.Message.ShouldBe("FinT failed"));
    }

    [Fact]
    public async Task SelectMany_ReturnsFail_WhenValidationFails()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(-5));

        // Act
        FinT<IO, int> result =
            from a in finT
            from b in ValidatePositive(a)
            select b;

        Fin<int> actual = await result.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: e => e.Message.ShouldContain("must be positive"));
    }

    [Fact]
    public async Task SelectMany_ReturnsFail_WhenSecondValidationFails()
    {
        // Arrange
        FinT<IO, int> finT = FinT.lift<IO, int>(Fin.Succ(150));

        // Act
        FinT<IO, int> result =
            from a in finT
            from b in ValidatePositive(a)
            from c in ValidateMax(b, 100)
            select c;

        Fin<int> actual = await result.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: e => e.Message.ShouldContain("must be at most"));
    }

    #endregion

    #region Helper Methods

    private static Validation<Error, int> ValidatePositive(int value)
    {
        return value > 0
            ? Success<Error, int>(value)
            : Fail<Error, int>(Error.New($"Value {value} must be positive"));
    }

    private static Validation<Error, int> ValidateMax(int value, int max)
    {
        return value <= max
            ? Success<Error, int>(value)
            : Fail<Error, int>(Error.New($"Value {value} must be at most {max}"));
    }

    #endregion
}

/// <summary>
/// Validation 다중 에러 보존 테스트.
/// LanguageExt Validation은 applicative(Apply)로 여러 에러를 누적하도록 설계되어 있다.
/// FinT LINQ 확장은 이 누적된 에러를 ManyErrors로 보존해야 한다 (errors.Head로 유실 금지).
/// </summary>
[Trait(nameof(UnitTest), UnitTest.Functorium_Applications)]
public class ValidationErrorPreservationTests
{
    private static Validation<Error, int> FailWithTwo() =>
        Fail<Error, int>(Seq(Error.New("error-1"), Error.New("error-2")));

    private static Validation<Error, int> FailWithThree() =>
        Fail<Error, int>(Seq(Error.New("error-1"), Error.New("error-2"), Error.New("error-3")));

    [Fact]
    public async Task ValidationToFinT_PreservesAllErrors_WhenMultipleErrorsPresent()
    {
        // Arrange — Validation은 3개 에러를 담고 있음
        // Act — Validation → FinT SelectMany(체이닝)
        FinT<IO, int> pipeline =
            from a in FailWithThree()
            from b in FinT.lift<IO, int>(Fin.Succ(a * 2))
            select b;

        Fin<int> actual = await pipeline.Run().RunAsync();

        // Assert — 모든 에러가 ManyErrors로 보존되어야 함
        actual.IsFail.ShouldBeTrue();
        Error err = actual.Match(Succ: _ => throw new Xunit.Sdk.XunitException("expected fail"), Fail: e => e);
        err.ShouldBeOfType<ManyErrors>();
        ((ManyErrors)err).Errors.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ValidationToFinT_PreservesAllErrors_WithTwoErrors()
    {
        // Arrange — 2개 에러 케이스
        // Act — 동일 경로, 에러 수만 다름
        FinT<IO, int> pipeline =
            from a in FailWithTwo()
            from b in FinT.lift<IO, int>(Fin.Succ(a + 1))
            select b;

        Fin<int> actual = await pipeline.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        Error err = actual.Match(Succ: _ => throw new Xunit.Sdk.XunitException("expected fail"), Fail: e => e);
        err.ShouldBeOfType<ManyErrors>();
        ((ManyErrors)err).Errors.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FinTToValidation_PreservesAllErrors_WhenValidationHasMultipleErrors()
    {
        // Arrange — FinT 체인 중간에서 다중 에러를 가진 Validation이 실패
        FinT<IO, int> seed = FinT.lift<IO, int>(Fin.Succ(10));

        // Act
        FinT<IO, int> pipeline =
            from a in seed
            from b in FailWithThree()
            select b;

        Fin<int> actual = await pipeline.Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        Error err = actual.Match(Succ: _ => throw new Xunit.Sdk.XunitException("expected fail"), Fail: e => e);
        err.ShouldBeOfType<ManyErrors>();
        ((ManyErrors)err).Errors.Count.ShouldBe(3);
    }
}
