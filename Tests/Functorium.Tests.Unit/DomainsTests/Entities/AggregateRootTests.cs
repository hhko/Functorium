using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.Entities;

/// <summary>
/// 테스트용 도메인 이벤트
/// </summary>
public sealed record TestDomainEvent : DomainEvent
{
    public string Message { get; init; }

    public TestDomainEvent(string message) : base()
    {
        Message = message;
    }

    public TestDomainEvent(string message, string? correlationId)
        : base(correlationId)
    {
        Message = message;
    }

    public TestDomainEvent(string message, string? correlationId, string? causationId)
        : base(correlationId, causationId)
    {
        Message = message;
    }
}

/// <summary>
/// 또 다른 테스트용 도메인 이벤트
/// </summary>
public sealed record AnotherTestDomainEvent : DomainEvent
{
    public int Value { get; init; }

    public AnotherTestDomainEvent(int value) : base()
    {
        Value = value;
    }
}

/// <summary>
/// 테스트용 Aggregate Root
/// </summary>
public sealed class TestAggregateRoot : AggregateRoot<TestEntityId>
{
    public string Name { get; private set; }

    private TestAggregateRoot(TestEntityId id, string name) : base(id)
    {
        Name = name;
    }

    public static TestAggregateRoot Create(TestEntityId id, string name)
    {
        var aggregate = new TestAggregateRoot(id, name);
        aggregate.AddDomainEvent(new TestDomainEvent($"Created: {name}"));
        return aggregate;
    }

    public void UpdateName(string newName)
    {
        Name = newName;
        AddDomainEvent(new TestDomainEvent($"Updated: {newName}"));
    }

    public void AddEvent(IDomainEvent domainEvent)
    {
        AddDomainEvent(domainEvent);
    }

    public void RemoveEvent(IDomainEvent domainEvent)
    {
        RemoveDomainEvent(domainEvent);
    }
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class AggregateRootTests
{
    #region DomainEvents Tests

    [Fact]
    public void DomainEvents_IsEmpty_WhenNoEventsAdded()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");

        // Act
        var actual = aggregate.DomainEvents;

        // Assert
        actual.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_ContainsEvent_WhenEventAdded()
    {
        // Arrange
        var aggregate = TestAggregateRoot.Create(TestEntityId.New(), "Test");

        // Act
        var actual = aggregate.DomainEvents;

        // Assert
        actual.Count.ShouldBe(1);
        actual[0].ShouldBeOfType<TestDomainEvent>();
    }

    [Fact]
    public void DomainEvents_ContainsMultipleEvents_WhenMultipleEventsAdded()
    {
        // Arrange
        var aggregate = TestAggregateRoot.Create(TestEntityId.New(), "Test");

        // Act
        aggregate.UpdateName("Updated");
        var actual = aggregate.DomainEvents;

        // Assert
        actual.Count.ShouldBe(2);
    }

    [Fact]
    public void DomainEvents_IsReadOnly_WhenAccessed()
    {
        // Arrange
        var aggregate = TestAggregateRoot.Create(TestEntityId.New(), "Test");

        // Act
        var actual = aggregate.DomainEvents;

        // Assert
        actual.ShouldBeAssignableTo<IReadOnlyList<IDomainEvent>>();
    }

    #endregion

    #region AddDomainEvent Tests

    [Fact]
    public void AddDomainEvent_AddsEvent_WhenCalled()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var domainEvent = new TestDomainEvent("Test Event");

        // Act
        aggregate.AddEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Count.ShouldBe(1);
        aggregate.DomainEvents[0].ShouldBe(domainEvent);
    }

    [Fact]
    public void AddDomainEvent_AddsDifferentEventTypes_WhenCalled()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Test");
        var event2 = new AnotherTestDomainEvent(42);

        // Act
        aggregate.AddEvent(event1);
        aggregate.AddEvent(event2);

        // Assert
        aggregate.DomainEvents.Count.ShouldBe(2);
        aggregate.DomainEvents[0].ShouldBeOfType<TestDomainEvent>();
        aggregate.DomainEvents[1].ShouldBeOfType<AnotherTestDomainEvent>();
    }

    #endregion

    #region RemoveDomainEvent Tests

    [Fact]
    public void RemoveDomainEvent_RemovesEvent_WhenEventExists()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var domainEvent = new TestDomainEvent("Test Event");
        aggregate.AddEvent(domainEvent);

        // Act
        aggregate.RemoveEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveDomainEvent_DoesNothing_WhenEventNotExists()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var existingEvent = new TestDomainEvent("Existing");
        var nonExistingEvent = new TestDomainEvent("Non-existing");
        aggregate.AddEvent(existingEvent);

        // Act
        aggregate.RemoveEvent(nonExistingEvent);

        // Assert
        aggregate.DomainEvents.Count.ShouldBe(1);
        aggregate.DomainEvents[0].ShouldBe(existingEvent);
    }

    #endregion

    #region ClearDomainEvents Tests

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents_WhenCalled()
    {
        // Arrange
        var aggregate = TestAggregateRoot.Create(TestEntityId.New(), "Test");
        aggregate.UpdateName("Updated");

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_DoesNothing_WhenNoEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region DomainEvent Record Tests

    [Fact]
    public void DomainEvent_HasOccurredAt_WhenCreated()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new TestDomainEvent("Test");

        // Assert
        var after = DateTimeOffset.UtcNow;
        domainEvent.OccurredAt.ShouldBeGreaterThanOrEqualTo(before);
        domainEvent.OccurredAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void DomainEvent_EqualsAnother_WhenSameValues()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var event1 = new TestDomainEvent("Test") with { OccurredAt = occurredAt, EventId = eventId };
        var event2 = new TestDomainEvent("Test") with { OccurredAt = occurredAt, EventId = eventId };

        // Act
        var actual = event1.Equals(event2);

        // Assert
        actual.ShouldBeTrue();
    }

    #endregion

    #region DomainEvent Metadata Tests

    [Fact]
    public void DomainEvent_HasEventId_WhenCreated()
    {
        // Act
        var domainEvent = new TestDomainEvent("Test");

        // Assert
        domainEvent.EventId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void DomainEvent_HasUniqueEventId_WhenMultipleEventsCreated()
    {
        // Act
        var event1 = new TestDomainEvent("Test1");
        var event2 = new TestDomainEvent("Test2");

        // Assert
        event1.EventId.ShouldNotBe(event2.EventId);
    }

    [Fact]
    public void DomainEvent_HasNullCorrelationId_WhenCreatedWithDefaultConstructor()
    {
        // Act
        var domainEvent = new TestDomainEvent("Test");

        // Assert
        domainEvent.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void DomainEvent_HasNullCausationId_WhenCreatedWithDefaultConstructor()
    {
        // Act
        var domainEvent = new TestDomainEvent("Test");

        // Assert
        domainEvent.CausationId.ShouldBeNull();
    }

    [Fact]
    public void DomainEvent_HasCorrelationId_WhenCreatedWithCorrelationId()
    {
        // Arrange
        var correlationId = "test-correlation-id";

        // Act
        var domainEvent = new TestDomainEvent("Test", correlationId);

        // Assert
        domainEvent.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public void DomainEvent_HasCausationId_WhenCreatedWithBothIds()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        var causationId = "test-causation-id";

        // Act
        var domainEvent = new TestDomainEvent("Test", correlationId, causationId);

        // Assert
        domainEvent.CorrelationId.ShouldBe(correlationId);
        domainEvent.CausationId.ShouldBe(causationId);
    }

    [Fact]
    public void DomainEvent_CanSetMetadataWithWith_WhenUsed()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var correlationId = "new-correlation-id";
        var causationId = "new-causation-id";

        // Act
        var updatedEvent = domainEvent with
        {
            CorrelationId = correlationId,
            CausationId = causationId
        };

        // Assert
        updatedEvent.CorrelationId.ShouldBe(correlationId);
        updatedEvent.CausationId.ShouldBe(causationId);
        updatedEvent.EventId.ShouldBe(domainEvent.EventId);
        updatedEvent.Message.ShouldBe(domainEvent.Message);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void AggregateRoot_InheritsFromEntity_WhenCreated()
    {
        // Arrange & Act
        var aggregate = TestAggregateRoot.Create(TestEntityId.New(), "Test");

        // Assert
        aggregate.ShouldBeAssignableTo<Entity<TestEntityId>>();
        aggregate.ShouldBeAssignableTo<IEntity<TestEntityId>>();
    }

    [Fact]
    public void AggregateRoot_Equals_WhenSameId()
    {
        // Arrange
        var id = TestEntityId.New();
        var aggregate1 = TestAggregateRoot.Create(id, "Test 1");
        var aggregate2 = TestAggregateRoot.Create(id, "Test 2");

        // Act
        var actual = aggregate1.Equals(aggregate2);

        // Assert
        actual.ShouldBeTrue();
    }

    #endregion
}

/// <summary>
/// 이벤트 없이 생성되는 테스트용 Aggregate Root
/// </summary>
public sealed class TestAggregateRootWithoutEvents : AggregateRoot<TestEntityId>
{
    public string Name { get; }

    public TestAggregateRootWithoutEvents(TestEntityId id, string name) : base(id)
    {
        Name = name;
    }

    public void AddEvent(IDomainEvent domainEvent)
    {
        AddDomainEvent(domainEvent);
    }

    public void RemoveEvent(IDomainEvent domainEvent)
    {
        RemoveDomainEvent(domainEvent);
    }
}
