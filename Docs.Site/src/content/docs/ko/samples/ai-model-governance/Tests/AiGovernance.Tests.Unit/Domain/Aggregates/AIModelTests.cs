using AiGovernance.Domain.AggregateRoots.Models;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.Aggregates;

public class AIModelTests
{
    private static AIModel CreateSampleModel(
        string name = "GPT-4",
        string version = "1.0.0",
        string purpose = "Natural language processing",
        string riskTier = "Minimal")
    {
        return AIModel.Create(
            ModelName.Create(name).ThrowIfFail(),
            ModelVersion.Create(version).ThrowIfFail(),
            ModelPurpose.Create(purpose).ThrowIfFail(),
            RiskTier.CreateFromValidated(riskTier));
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Act
        var sut = CreateSampleModel(
            name: "GPT-4",
            version: "2.0.0",
            purpose: "Text generation",
            riskTier: "Limited");

        // Assert
        sut.Id.ShouldNotBe(default);
        ((string)sut.Name).ShouldBe("GPT-4");
        ((string)sut.Version).ShouldBe("2.0.0");
        ((string)sut.Purpose).ShouldBe("Text generation");
        ((string)sut.RiskTier).ShouldBe("Limited");
    }

    [Fact]
    public void Create_ShouldPublishRegisteredEvent()
    {
        // Act
        var sut = CreateSampleModel();

        // Assert
        var registeredEvent = sut.DomainEvents.OfType<AIModel.RegisteredEvent>().ShouldHaveSingleItem();
        registeredEvent.ModelId.ShouldBe(sut.Id);
        registeredEvent.Name.ShouldBe(sut.Name);
        registeredEvent.Version.ShouldBe(sut.Version);
        registeredEvent.Purpose.ShouldBe(sut.Purpose);
        registeredEvent.RiskTier.ShouldBe(sut.RiskTier);
    }

    [Fact]
    public void ClassifyRisk_ShouldChangeRiskTier_AndRaiseEvent()
    {
        // Arrange
        var sut = CreateSampleModel(riskTier: "Minimal");
        sut.ClearDomainEvents();
        var oldRiskTier = sut.RiskTier;

        // Act
        var actual = sut.ClassifyRisk(RiskTier.High);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)sut.RiskTier).ShouldBe("High");
        var riskEvent = sut.DomainEvents.OfType<AIModel.RiskClassifiedEvent>().ShouldHaveSingleItem();
        riskEvent.OldRiskTier.ShouldBe(oldRiskTier);
        riskEvent.NewRiskTier.ShouldBe(RiskTier.High);
    }

    [Fact]
    public void ClassifyRisk_ShouldFail_WhenModelIsDeleted()
    {
        // Arrange
        var sut = CreateSampleModel();
        sut.Archive("admin");

        // Act
        var actual = sut.ClassifyRisk(RiskTier.High);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void UpdateVersion_ShouldChangeVersion_AndRaiseEvent()
    {
        // Arrange
        var sut = CreateSampleModel(version: "1.0.0");
        sut.ClearDomainEvents();
        var oldVersion = sut.Version;
        var newVersion = ModelVersion.Create("2.0.0").ThrowIfFail();

        // Act
        var actual = sut.UpdateVersion(newVersion);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)sut.Version).ShouldBe("2.0.0");
        var versionEvent = sut.DomainEvents.OfType<AIModel.VersionUpdatedEvent>().ShouldHaveSingleItem();
        versionEvent.OldVersion.ShouldBe(oldVersion);
        versionEvent.NewVersion.ShouldBe(newVersion);
    }

    [Fact]
    public void UpdateVersion_ShouldFail_WhenModelIsDeleted()
    {
        // Arrange
        var sut = CreateSampleModel();
        sut.Archive("admin");

        // Act
        var actual = sut.UpdateVersion(ModelVersion.Create("2.0.0").ThrowIfFail());

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Update_ShouldUpdateProperties_AndRaiseEvent()
    {
        // Arrange
        var sut = CreateSampleModel();
        sut.ClearDomainEvents();
        var newName = ModelName.Create("Updated Model").ThrowIfFail();
        var newPurpose = ModelPurpose.Create("Updated purpose").ThrowIfFail();

        // Act
        var actual = sut.Update(newName, newPurpose);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)sut.Name).ShouldBe("Updated Model");
        ((string)sut.Purpose).ShouldBe("Updated purpose");
        var updatedEvent = sut.DomainEvents.OfType<AIModel.UpdatedEvent>().ShouldHaveSingleItem();
        updatedEvent.Name.ShouldBe(newName);
        updatedEvent.Purpose.ShouldBe(newPurpose);
    }

    [Fact]
    public void Update_ShouldFail_WhenModelIsDeleted()
    {
        // Arrange
        var sut = CreateSampleModel();
        sut.Archive("admin");

        // Act
        var actual = sut.Update(
            ModelName.Create("New Name").ThrowIfFail(),
            ModelPurpose.Create("New Purpose").ThrowIfFail());

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Archive_ShouldSetDeletedProperties_AndRaiseEvent()
    {
        // Arrange
        var sut = CreateSampleModel();
        sut.ClearDomainEvents();

        // Act
        sut.Archive("admin@test.com");

        // Assert
        sut.DeletedAt.IsSome.ShouldBeTrue();
        sut.DeletedBy.ShouldBe(Some("admin@test.com"));
        var archivedEvent = sut.DomainEvents.OfType<AIModel.ArchivedEvent>().ShouldHaveSingleItem();
        archivedEvent.ModelId.ShouldBe(sut.Id);
        archivedEvent.DeletedBy.ShouldBe("admin@test.com");
    }

    [Fact]
    public void Archive_ShouldBeIdempotent_WhenAlreadyArchived()
    {
        // Arrange
        var sut = CreateSampleModel();
        sut.Archive("admin@test.com");
        sut.ClearDomainEvents();

        // Act
        sut.Archive("other@test.com");

        // Assert
        sut.DomainEvents.ShouldBeEmpty();
        sut.DeletedBy.ShouldBe(Some("admin@test.com"));
    }

    [Fact]
    public void Restore_ShouldClearDeletedProperties_AndRaiseEvent()
    {
        // Arrange
        var sut = CreateSampleModel();
        sut.Archive("admin@test.com");
        sut.ClearDomainEvents();

        // Act
        sut.Restore();

        // Assert
        sut.DeletedAt.IsNone.ShouldBeTrue();
        sut.DeletedBy.IsNone.ShouldBeTrue();
        var restoredEvent = sut.DomainEvents.OfType<AIModel.RestoredEvent>().ShouldHaveSingleItem();
        restoredEvent.ModelId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Restore_ShouldBeIdempotent_WhenNotArchived()
    {
        // Arrange
        var sut = CreateSampleModel();
        sut.ClearDomainEvents();

        // Act
        sut.Restore();

        // Assert
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = AIModelId.New();
        var name = ModelName.Create("Restored Model").ThrowIfFail();
        var version = ModelVersion.Create("1.0.0").ThrowIfFail();
        var purpose = ModelPurpose.Create("Restored purpose").ThrowIfFail();
        var riskTier = RiskTier.High;
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = AIModel.CreateFromValidated(
            id, name, version, purpose, riskTier,
            createdAt, Some(updatedAt), None, None);

        // Assert
        sut.Id.ShouldBe(id);
        ((string)sut.Name).ShouldBe("Restored Model");
        ((string)sut.Version).ShouldBe("1.0.0");
        ((string)sut.RiskTier).ShouldBe("High");
        sut.CreatedAt.ShouldBe(createdAt);
        sut.UpdatedAt.ShouldBe(Some(updatedAt));
        sut.DomainEvents.ShouldBeEmpty();
    }
}
