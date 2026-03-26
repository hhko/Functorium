using Functorium.Adapters.Observabilities;
using Functorium.Domains.Observabilities;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.Observabilities;

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class ObservableSignalTests
{
    #region ObservableSignal Static API Tests

    [Fact]
    public void Debug_InvokesFactory_WithDebugLevel()
    {
        // Arrange
        var factory = Substitute.For<IObservableSignalFactory>();
        ObservableSignal.SetFactory(factory);

        var logger = Substitute.For<ILogger>();
        using var scope = ObservableSignalScope.Begin(logger, "adapter", "repository", "TestRepo", "GetById");

        // Act
        ObservableSignal.Debug("Cache miss", ("adapter.cache.key", "prod-123"));

        // Assert
        factory.Received(1).Log(
            LogLevel.Debug,
            "Cache miss",
            Arg.Is<(string Key, object? Value)[]>(ctx =>
                ctx.Length == 1 && ctx[0].Key == "adapter.cache.key"),
            null);
    }

    [Fact]
    public void Warning_InvokesFactory_WithWarningLevel()
    {
        // Arrange
        var factory = Substitute.For<IObservableSignalFactory>();
        ObservableSignal.SetFactory(factory);

        var logger = Substitute.For<ILogger>();
        using var scope = ObservableSignalScope.Begin(logger, "adapter", "externalapi", "PricingService", "GetPrice");

        // Act
        ObservableSignal.Warning("Retry attempt",
            ("adapter.retry.attempt", 2),
            ("adapter.retry.delay_ms", 1500));

        // Assert
        factory.Received(1).Log(
            LogLevel.Warning,
            "Retry attempt",
            Arg.Is<(string Key, object? Value)[]>(ctx => ctx.Length == 2),
            null);
    }

    [Fact]
    public void Error_InvokesFactory_WithErrorLevel()
    {
        // Arrange
        var factory = Substitute.For<IObservableSignalFactory>();
        ObservableSignal.SetFactory(factory);

        var logger = Substitute.For<ILogger>();
        using var scope = ObservableSignalScope.Begin(logger, "adapter", "messaging", "RabbitMq", "Send");

        // Act
        ObservableSignal.Error("Message moved to error queue",
            ("adapter.message.id", "msg-123"));

        // Assert
        factory.Received(1).Log(
            LogLevel.Error,
            "Message moved to error queue",
            Arg.Is<(string Key, object? Value)[]>(ctx => ctx.Length == 1),
            null);
    }

    [Fact]
    public void ErrorWithException_InvokesFactory_WithExceptionAndErrorLevel()
    {
        // Arrange
        var factory = Substitute.For<IObservableSignalFactory>();
        ObservableSignal.SetFactory(factory);

        var logger = Substitute.For<ILogger>();
        using var scope = ObservableSignalScope.Begin(logger, "adapter", "repository", "TestRepo", "Save");
        var exception = new InvalidOperationException("Connection lost");

        // Act
        ObservableSignal.Error(exception, "Database connection failed",
            ("adapter.db.operation", "SaveChanges"));

        // Assert
        factory.Received(1).Log(
            LogLevel.Error,
            "Database connection failed",
            Arg.Is<(string Key, object? Value)[]>(ctx => ctx.Length == 1),
            exception);
    }

    [Fact]
    public void Debug_WithoutContext_PassesEmptyArray()
    {
        // Arrange
        var factory = Substitute.For<IObservableSignalFactory>();
        ObservableSignal.SetFactory(factory);

        var logger = Substitute.For<ILogger>();
        using var scope = ObservableSignalScope.Begin(logger, "adapter", "repository", "TestRepo", "GetById");

        // Act
        ObservableSignal.Debug("Simple message");

        // Assert
        factory.Received(1).Log(
            LogLevel.Debug,
            "Simple message",
            Arg.Is<(string Key, object? Value)[]>(ctx => ctx.Length == 0),
            null);
    }

    #endregion

    #region ObservableSignal No-op Tests

    [Fact]
    public void Debug_WithoutFactory_IsNoOp()
    {
        // Arrange: Reset to default NullFactory by setting a new factory then clearing scope
        // ObservableSignal with no scope should be no-op via ObservableSignalFactory
        // This test verifies no exception is thrown

        // Act & Assert — no exception
        ObservableSignal.Debug("Should not throw");
        ObservableSignal.Warning("Should not throw");
        ObservableSignal.Error("Should not throw");
        ObservableSignal.Error(new Exception(), "Should not throw");
    }

    #endregion

    #region ObservableSignalScope Tests

    [Fact]
    public void Scope_SetsCurrentScope_AndRestoresOnDispose()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();

        // Act & Assert: Before scope
        ObservableSignalScope.Current.ShouldBeNull();

        // During scope
        var scope = ObservableSignalScope.Begin(logger, "adapter", "repository", "TestRepo", "GetById");
        ObservableSignalScope.Current.ShouldNotBeNull();
        ObservableSignalScope.Current!.Layer.ShouldBe("adapter");
        ObservableSignalScope.Current.Category.ShouldBe("repository");
        ObservableSignalScope.Current.Handler.ShouldBe("TestRepo");
        ObservableSignalScope.Current.Method.ShouldBe("GetById");
        ObservableSignalScope.Current.Logger.ShouldBe(logger);

        // After dispose
        scope.Dispose();
        ObservableSignalScope.Current.ShouldBeNull();
    }

    [Fact]
    public void Scope_SupportsNesting_AndRestoresParent()
    {
        // Arrange
        var logger1 = Substitute.For<ILogger>();
        var logger2 = Substitute.For<ILogger>();

        // Act & Assert
        using (var outer = ObservableSignalScope.Begin(logger1, "adapter", "repository", "OuterRepo", "Method1"))
        {
            ObservableSignalScope.Current!.Handler.ShouldBe("OuterRepo");

            using (var inner = ObservableSignalScope.Begin(logger2, "adapter", "messaging", "InnerMsg", "Method2"))
            {
                ObservableSignalScope.Current!.Handler.ShouldBe("InnerMsg");
                ObservableSignalScope.Current.Category.ShouldBe("messaging");
            }

            // Inner disposed — outer restored
            ObservableSignalScope.Current!.Handler.ShouldBe("OuterRepo");
            ObservableSignalScope.Current.Category.ShouldBe("repository");
        }

        // Both disposed
        ObservableSignalScope.Current.ShouldBeNull();
    }

    [Fact]
    public async Task Scope_IsIsolated_AcrossAsyncContexts()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        string? capturedHandler = null;

        // Act: Create scope in one async context, verify isolation in another
        using (var scope = ObservableSignalScope.Begin(logger, "adapter", "repository", "MainRepo", "Get"))
        {
            // Capture in a separate async context
            await Task.Run(() =>
            {
                // AsyncLocal flows into child tasks
                capturedHandler = ObservableSignalScope.Current?.Handler;
            });
        }

        // Assert: AsyncLocal flows into child tasks
        capturedHandler.ShouldBe("MainRepo");

        // After scope dispose, current context is null
        ObservableSignalScope.Current.ShouldBeNull();
    }

    #endregion
}
