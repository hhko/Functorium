using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Incidents;
using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Tests.Unit.Domain.Aggregates;

public class ModelIncidentTests
{
    private static ModelIncident CreateSampleIncident(string severity = "High")
    {
        return ModelIncident.Create(
            ModelDeploymentId.New(),
            AIModelId.New(),
            IncidentSeverity.CreateFromValidated(severity),
            IncidentDescription.Create("Model returned biased results").ThrowIfFail());
    }

    [Fact]
    public void Create_ShouldBeInReportedStatus_AndRaiseEvent()
    {
        // Act
        var sut = CreateSampleIncident();

        // Assert
        sut.Id.ShouldNotBe(default);
        sut.Status.ShouldBe(IncidentStatus.Reported);
        var reportedEvent = sut.DomainEvents.OfType<ModelIncident.ReportedEvent>().ShouldHaveSingleItem();
        reportedEvent.IncidentId.ShouldBe(sut.Id);
        reportedEvent.Severity.ShouldBe(sut.Severity);
        reportedEvent.DeploymentId.ShouldBe(sut.DeploymentId);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var deploymentId = ModelDeploymentId.New();
        var modelId = AIModelId.New();
        var severity = IncidentSeverity.Critical;
        var description = IncidentDescription.Create("Critical failure").ThrowIfFail();

        // Act
        var sut = ModelIncident.Create(deploymentId, modelId, severity, description);

        // Assert
        sut.DeploymentId.ShouldBe(deploymentId);
        sut.ModelId.ShouldBe(modelId);
        sut.Severity.ShouldBe(IncidentSeverity.Critical);
        ((string)sut.Description).ShouldBe("Critical failure");
        sut.ResolutionNote.IsNone.ShouldBeTrue();
        sut.ResolvedAt.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Investigate_ShouldTransitionToInvestigating()
    {
        // Arrange
        var sut = CreateSampleIncident();
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Investigate();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(IncidentStatus.Investigating);
        sut.DomainEvents.OfType<ModelIncident.InvestigatingEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Investigate_FromResolved_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleIncident();
        sut.Investigate();
        sut.Resolve(ResolutionNote.Create("Fixed").ThrowIfFail());

        // Act
        var actual = sut.Investigate();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Resolve_FromInvestigating_ShouldSetResolutionNote_AndRaiseEvent()
    {
        // Arrange
        var sut = CreateSampleIncident();
        sut.Investigate();
        sut.ClearDomainEvents();
        var resolutionNote = ResolutionNote.Create("Applied bias correction patch").ThrowIfFail();

        // Act
        var actual = sut.Resolve(resolutionNote);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(IncidentStatus.Resolved);
        sut.ResolutionNote.IsSome.ShouldBeTrue();
        sut.ResolutionNote.IfSome(note => ((string)note).ShouldBe("Applied bias correction patch"));
        sut.ResolvedAt.IsSome.ShouldBeTrue();
        var resolvedEvent = sut.DomainEvents.OfType<ModelIncident.ResolvedEvent>().ShouldHaveSingleItem();
        resolvedEvent.ResolutionNote.ShouldBe(resolutionNote);
    }

    [Fact]
    public void Resolve_FromReported_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleIncident();
        var resolutionNote = ResolutionNote.Create("Quick fix").ThrowIfFail();

        // Act
        var actual = sut.Resolve(resolutionNote);

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.Status.ShouldBe(IncidentStatus.Reported);
    }

    [Fact]
    public void Escalate_FromReported_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSampleIncident();
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Escalate();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(IncidentStatus.Escalated);
        sut.DomainEvents.OfType<ModelIncident.EscalatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Escalate_FromInvestigating_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSampleIncident();
        sut.Investigate();
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Escalate();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(IncidentStatus.Escalated);
    }

    [Fact]
    public void Escalate_FromResolved_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleIncident();
        sut.Investigate();
        sut.Resolve(ResolutionNote.Create("Fixed").ThrowIfFail());

        // Act
        var actual = sut.Escalate();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = ModelIncidentId.New();
        var deploymentId = ModelDeploymentId.New();
        var modelId = AIModelId.New();
        var severity = IncidentSeverity.Critical;
        var status = IncidentStatus.Resolved;
        var description = IncidentDescription.CreateFromValidated("Test incident");
        var resolutionNote = ResolutionNote.CreateFromValidated("Fixed the issue");
        var reportedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var resolvedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = ModelIncident.CreateFromValidated(
            id, deploymentId, modelId, severity, status, description,
            Some(resolutionNote), reportedAt, Some(resolvedAt), createdAt, None);

        // Assert
        sut.Id.ShouldBe(id);
        sut.Status.ShouldBe(IncidentStatus.Resolved);
        sut.ResolutionNote.IsSome.ShouldBeTrue();
        sut.ResolvedAt.ShouldBe(Some(resolvedAt));
        sut.DomainEvents.ShouldBeEmpty();
    }
}
