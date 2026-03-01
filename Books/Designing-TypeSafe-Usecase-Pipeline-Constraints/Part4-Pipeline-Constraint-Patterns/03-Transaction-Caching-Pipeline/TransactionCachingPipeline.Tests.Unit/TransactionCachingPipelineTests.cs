using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace TransactionCachingPipeline.Tests.Unit;

public class TransactionCachingPipelineTests
{
    [Fact]
    public void Execute_CommitsTransaction_WhenCommandSucceeds()
    {
        // Arrange
        var sut = new SimpleTransactionPipeline<FinResponse<string>>();

        // Act
        var actual = sut.Execute(
            isCommand: true,
            handler: () => FinResponse.Succ("OK"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Actions.ShouldContain("Begin transaction");
        sut.Actions.ShouldContain("Commit");
    }

    [Fact]
    public void Execute_RollbacksTransaction_WhenCommandFails()
    {
        // Arrange
        var sut = new SimpleTransactionPipeline<FinResponse<string>>();

        // Act
        var actual = sut.Execute(
            isCommand: true,
            handler: () => FinResponse.Fail<string>(Error.New("error")));

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.Actions.ShouldContain("Begin transaction");
        sut.Actions.ShouldContain("Rollback");
    }

    [Fact]
    public void Execute_SkipsTransaction_WhenNotCommand()
    {
        // Arrange
        var sut = new SimpleTransactionPipeline<FinResponse<string>>();

        // Act
        var actual = sut.Execute(
            isCommand: false,
            handler: () => FinResponse.Succ("OK"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Actions.ShouldContain("Skip: not a command");
        sut.Actions.ShouldNotContain("Begin transaction");
    }

    [Fact]
    public void GetOrExecute_ReturnsHandlerResult_WhenNotCacheable()
    {
        // Arrange
        var sut = new SimpleCachingPipeline<FinResponse<string>>();

        // Act
        var actual = sut.GetOrExecute(
            cacheKey: "key",
            isCacheable: false,
            handler: () => FinResponse.Succ("OK"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void GetOrExecute_ReturnsCachedResult_WhenCalledTwice()
    {
        // Arrange
        var sut = new SimpleCachingPipeline<FinResponse<string>>();
        var callCount = 0;
        FinResponse<string> Handler()
        {
            callCount++;
            return FinResponse.Succ("OK");
        }

        // Act
        sut.GetOrExecute("key", isCacheable: true, Handler);
        var actual = sut.GetOrExecute("key", isCacheable: true, Handler);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        callCount.ShouldBe(1);
    }

    [Fact]
    public void GetOrExecute_DoesNotCacheFail_WhenHandlerFails()
    {
        // Arrange
        var sut = new SimpleCachingPipeline<FinResponse<string>>();
        var callCount = 0;
        FinResponse<string> FailHandler()
        {
            callCount++;
            return FinResponse.Fail<string>(Error.New("error"));
        }

        // Act
        sut.GetOrExecute("key", isCacheable: true, FailHandler);
        sut.GetOrExecute("key", isCacheable: true, FailHandler);

        // Assert
        callCount.ShouldBe(2);
    }
}
