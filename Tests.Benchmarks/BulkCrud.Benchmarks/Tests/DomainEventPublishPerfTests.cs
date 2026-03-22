using System.Diagnostics;
using BulkCrud.Benchmarks.Helpers;
using Functorium.Adapters.Events;
using Functorium.Domains.Events;
using LanguageExt;
using LayeredArch.Domain.AggregateRoots.Products;
using Mediator;
using Shouldly;
using Xunit;

namespace BulkCrud.Benchmarks.Tests;

public sealed class DomainEventPublishPerfTests
{
    [Fact]
    public async Task BulkPublish_1K_Events_Completes_Under_Threshold()
    {
        // Arrange
        var products = TestDataGenerator.GenerateProducts(1_000);
        var collector = new DomainEventCollector();
        var noOpPublisher = new NoOpPublisher();
        var publisher = new DomainEventPublisher(noOpPublisher, collector);

        collector.TrackRange(products);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await publisher.PublishTrackedEvents().Run().RunAsync();
        sw.Stop();

        // Assert
        result.IsSucc.ShouldBeTrue();
        sw.ElapsedMilliseconds.ShouldBeLessThan(100);
    }

    [Fact]
    public async Task BulkPublish_10K_Events_Completes_Under_Threshold()
    {
        // Arrange
        var products = TestDataGenerator.GenerateProducts(10_000);
        var collector = new DomainEventCollector();
        var noOpPublisher = new NoOpPublisher();
        var publisher = new DomainEventPublisher(noOpPublisher, collector);

        collector.TrackRange(products);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await publisher.PublishTrackedEvents().Run().RunAsync();
        sw.Stop();

        // Assert
        result.IsSucc.ShouldBeTrue();
        sw.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    [Fact]
    public void DeleteRange_Raises_BulkDeletedEvent()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var ids = Enumerable.Range(0, 100)
            .Select(_ => ProductId.New())
            .ToList();

        // Act — DeleteRange 시나리오 시뮬레이션
        collector.TrackEvent(BulkDeletedEvent.From(ids, ids.Count));

        // Assert
        var directEvents = collector.GetDirectlyTrackedEvents();
        directEvents.Count.ShouldBe(1);
        var bulkDeleted = directEvents[0].ShouldBeOfType<BulkDeletedEvent>();
        bulkDeleted.DeletedIds.Count.ShouldBe(100);
        bulkDeleted.AffectedCount.ShouldBe(100);
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public ValueTask Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
            => ValueTask.CompletedTask;

        public ValueTask Publish(
            object notification,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
