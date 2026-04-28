using LanguageExt;
using TransactionPipeline;

namespace TransactionPipeline.Tests.Unit;

public sealed class TransactionPipelineTests
{
    [Fact]
    public async Task SimulateCommandPipeline_SavesAndCommits_WhenHandlerSucceeds()
    {
        // Arrange
        var unitOfWork = new InMemoryUnitOfWork();
        var eventCollector = new SimpleDomainEventCollector();
        var product = Product.Create("노트북", 1_500_000m);
        eventCollector.Track(product);

        // Act
        var result = await TransactionDemo.SimulateCommandPipeline(
            unitOfWork, eventCollector,
            handler: () => Task.FromResult(true));

        // Assert
        result.HandlerSucceeded.ShouldBeTrue();
        result.SavedChanges.ShouldBeTrue();
        result.Committed.ShouldBeTrue();
    }

    [Fact]
    public async Task SimulateCommandPipeline_PublishesEvents_WhenSuccessful()
    {
        // Arrange
        var unitOfWork = new InMemoryUnitOfWork();
        var eventCollector = new SimpleDomainEventCollector();
        var product = Product.Create("노트북", 1_500_000m);
        eventCollector.Track(product);

        // Act
        var result = await TransactionDemo.SimulateCommandPipeline(
            unitOfWork, eventCollector,
            handler: () => Task.FromResult(true));

        // Assert
        result.PublishedEvents.Count.ShouldBe(1);
        result.PublishedEvents[0].ShouldBeOfType<ProductCreatedEvent>();
    }

    [Fact]
    public async Task SimulateCommandPipeline_SkipsSaveAndCommit_WhenHandlerFails()
    {
        // Arrange
        var unitOfWork = new InMemoryUnitOfWork();
        var eventCollector = new SimpleDomainEventCollector();

        // Act
        var result = await TransactionDemo.SimulateCommandPipeline(
            unitOfWork, eventCollector,
            handler: () => Task.FromResult(false));

        // Assert
        result.HandlerSucceeded.ShouldBeFalse();
        result.SavedChanges.ShouldBeFalse();
        result.Committed.ShouldBeFalse();
        result.PublishedEvents.Count.ShouldBe(0);
    }

    [Fact]
    public async Task InMemoryUnitOfWork_TracksSaveState()
    {
        // Arrange
        var unitOfWork = new InMemoryUnitOfWork();

        // Act
        await unitOfWork.SaveChanges().Run().RunAsync();

        // Assert
        unitOfWork.WasSaved.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemoryTransaction_TracksCommitState()
    {
        // Arrange
        var unitOfWork = new InMemoryUnitOfWork();
        var transaction = await unitOfWork.BeginTransactionAsync();

        // Act
        await transaction.CommitAsync();

        // Assert
        var inMemoryTx = transaction.ShouldBeOfType<InMemoryTransaction>();
        inMemoryTx.WasCommitted.ShouldBeTrue();
    }
}
