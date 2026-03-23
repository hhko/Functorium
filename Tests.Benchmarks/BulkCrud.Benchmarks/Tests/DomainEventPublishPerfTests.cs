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
    public async Task IndividualPublish_1K_Events_Completes_Under_Threshold()
    {
        // Arrange
        var products = TestDataGenerator.GenerateProducts(1_000);
        var collector = new DomainEventCollector();
        var noOpPublisher = new NoOpPublisher();
        var noOpServiceProvider = new NoOpServiceProvider();
        var publisher = new DomainEventPublisher(noOpPublisher, collector, noOpServiceProvider);

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
    public async Task IndividualPublish_10K_Events_Completes_Under_Threshold()
    {
        // Arrange
        var products = TestDataGenerator.GenerateProducts(10_000);
        var collector = new DomainEventCollector();
        var noOpPublisher = new NoOpPublisher();
        var noOpServiceProvider = new NoOpServiceProvider();
        var publisher = new DomainEventPublisher(noOpPublisher, collector, noOpServiceProvider);

        collector.TrackRange(products);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await publisher.PublishTrackedEvents().Run().RunAsync();
        sw.Stop();

        // Assert
        result.IsSucc.ShouldBeTrue();
        sw.ElapsedMilliseconds.ShouldBeLessThan(2_000);
    }

    [Fact]
    public void DeleteRange_EachAggregate_Raises_DeletedEvent()
    {
        // Arrange
        var collector = new DomainEventCollector();
        var products = TestDataGenerator.GenerateProducts(100);

        // Act — DeleteRange 시나리오: 각 Aggregate가 DeletedEvent 발행
        foreach (var product in products)
        {
            product.Delete("system");
            collector.Track(product);
        }

        // Assert — 각 Aggregate에서 개별 DeletedEvent 발생
        var trackedAggregates = collector.GetTrackedAggregates();
        trackedAggregates.Count.ShouldBe(100);

        var allEvents = trackedAggregates
            .SelectMany(a => a.DomainEvents)
            .OfType<Product.DeletedEvent>()
            .ToList();
        allEvents.Count.ShouldBe(100);
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

    private sealed class NoOpServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
