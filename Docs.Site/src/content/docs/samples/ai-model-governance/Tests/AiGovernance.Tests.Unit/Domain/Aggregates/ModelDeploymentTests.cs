using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Tests.Unit.Domain.Aggregates;

public class ModelDeploymentTests
{
    private static ModelDeployment CreateSampleDeployment()
    {
        return ModelDeployment.Create(
            AIModelId.New(),
            EndpointUrl.Create("https://api.example.com/predict").ThrowIfFail(),
            DeploymentEnvironment.Production,
            DriftThreshold.Create(0.1m).ThrowIfFail());
    }

    [Fact]
    public void Create_ShouldBeInDraftStatus()
    {
        // Act
        var sut = CreateSampleDeployment();

        // Assert
        sut.Status.ShouldBe(DeploymentStatus.Draft);
    }

    [Fact]
    public void Create_ShouldSetProperties_AndRaiseCreatedEvent()
    {
        // Arrange
        var modelId = AIModelId.New();
        var endpointUrl = EndpointUrl.Create("https://api.example.com/predict").ThrowIfFail();
        var environment = DeploymentEnvironment.Staging;
        var driftThreshold = DriftThreshold.Create(0.2m).ThrowIfFail();

        // Act
        var sut = ModelDeployment.Create(modelId, endpointUrl, environment, driftThreshold);

        // Assert
        sut.Id.ShouldNotBe(default);
        sut.ModelId.ShouldBe(modelId);
        sut.EndpointUrl.ShouldBe(endpointUrl);
        sut.Environment.ShouldBe(environment);
        ((decimal)sut.DriftThreshold).ShouldBe(0.2m);
        var createdEvent = sut.DomainEvents.OfType<ModelDeployment.CreatedEvent>().ShouldHaveSingleItem();
        createdEvent.DeploymentId.ShouldBe(sut.Id);
        createdEvent.ModelId.ShouldBe(modelId);
    }

    [Fact]
    public void SubmitForReview_FromDraft_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.ClearDomainEvents();

        // Act
        var actual = sut.SubmitForReview();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.PendingReview);
        sut.DomainEvents.OfType<ModelDeployment.SubmittedForReviewEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void SubmitForReview_FromActive_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.SubmitForReview();
        sut.Activate();

        // Act
        var actual = sut.SubmitForReview();

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Active);
    }

    [Fact]
    public void Activate_FromPendingReview_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.SubmitForReview();
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Activate();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Active);
        sut.DomainEvents.OfType<ModelDeployment.ActivatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Activate_FromDraft_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleDeployment();

        // Act
        var actual = sut.Activate();

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Draft);
    }

    [Fact]
    public void Quarantine_FromActive_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.SubmitForReview();
        sut.Activate();
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Quarantine("Bias detected");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Quarantined);
        var quarantinedEvent = sut.DomainEvents.OfType<ModelDeployment.QuarantinedEvent>().ShouldHaveSingleItem();
        quarantinedEvent.Reason.ShouldBe("Bias detected");
    }

    [Fact]
    public void Quarantine_FromDraft_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleDeployment();

        // Act
        var actual = sut.Quarantine("Bias detected");

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Draft);
    }

    [Fact]
    public void Remediate_FromQuarantined_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.SubmitForReview();
        sut.Activate();
        sut.Quarantine("Issue found");
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Remediate();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Active);
        sut.DomainEvents.OfType<ModelDeployment.RemediatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Remediate_FromDraft_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleDeployment();

        // Act
        var actual = sut.Remediate();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Decommission_FromActive_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.SubmitForReview();
        sut.Activate();
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Decommission();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Decommissioned);
        sut.DomainEvents.OfType<ModelDeployment.DecommissionedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Decommission_FromQuarantined_ShouldSucceed()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.SubmitForReview();
        sut.Activate();
        sut.Quarantine("Issue");
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Decommission();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Decommissioned);
    }

    [Fact]
    public void Decommission_FromDecommissioned_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.SubmitForReview();
        sut.Activate();
        sut.Decommission();

        // Act
        var actual = sut.Decommission();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Decommission_FromDraft_ShouldFail()
    {
        // Arrange
        var sut = CreateSampleDeployment();

        // Act
        var actual = sut.Decommission();

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.Status.ShouldBe(DeploymentStatus.Draft);
    }

    [Fact]
    public void RecordHealthCheck_ShouldUpdateLastHealthCheckAt()
    {
        // Arrange
        var sut = CreateSampleDeployment();
        sut.LastHealthCheckAt.IsNone.ShouldBeTrue();

        // Act
        sut.RecordHealthCheck();

        // Assert
        sut.LastHealthCheckAt.IsSome.ShouldBeTrue();
        sut.UpdatedAt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = ModelDeploymentId.New();
        var modelId = AIModelId.New();
        var endpointUrl = EndpointUrl.CreateFromValidated("https://api.example.com/predict");
        var environment = DeploymentEnvironment.Production;
        var status = DeploymentStatus.Active;
        var driftThreshold = DriftThreshold.CreateFromValidated(0.15m);
        var deployedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = ModelDeployment.CreateFromValidated(
            id, modelId, endpointUrl, environment, status, driftThreshold,
            None, deployedAt, createdAt, Some(updatedAt));

        // Assert
        sut.Id.ShouldBe(id);
        sut.ModelId.ShouldBe(modelId);
        sut.Status.ShouldBe(DeploymentStatus.Active);
        sut.CreatedAt.ShouldBe(createdAt);
        sut.UpdatedAt.ShouldBe(Some(updatedAt));
        sut.DomainEvents.ShouldBeEmpty();
    }
}
