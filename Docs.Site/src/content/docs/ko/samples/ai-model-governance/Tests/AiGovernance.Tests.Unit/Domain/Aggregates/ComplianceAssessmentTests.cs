using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Models;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.Aggregates;

public class ComplianceAssessmentTests
{
    private static ComplianceAssessment CreateSampleAssessment(string riskTier = "Minimal")
    {
        return ComplianceAssessment.Create(
            AIModelId.New(),
            ModelDeploymentId.New(),
            RiskTier.CreateFromValidated(riskTier));
    }

    [Fact]
    public void Create_WithMinimalRisk_ShouldGenerate3Criteria()
    {
        // Act
        var sut = CreateSampleAssessment(riskTier: "Minimal");

        // Assert
        sut.Criteria.Count.ShouldBe(3);
        sut.Status.ShouldBe(AssessmentStatus.Initiated);
    }

    [Fact]
    public void Create_WithLimitedRisk_ShouldGenerate3Criteria()
    {
        // Act
        var sut = CreateSampleAssessment(riskTier: "Limited");

        // Assert
        sut.Criteria.Count.ShouldBe(3);
    }

    [Fact]
    public void Create_WithHighRisk_ShouldGenerate6Criteria()
    {
        // Act
        var sut = CreateSampleAssessment(riskTier: "High");

        // Assert
        sut.Criteria.Count.ShouldBe(6);
    }

    [Fact]
    public void Create_WithUnacceptableRisk_ShouldGenerate7Criteria()
    {
        // Act
        var sut = CreateSampleAssessment(riskTier: "Unacceptable");

        // Assert
        sut.Criteria.Count.ShouldBe(7);
    }

    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Act
        var sut = CreateSampleAssessment(riskTier: "High");

        // Assert
        var createdEvent = sut.DomainEvents.OfType<ComplianceAssessment.CreatedEvent>().ShouldHaveSingleItem();
        createdEvent.AssessmentId.ShouldBe(sut.Id);
        createdEvent.CriteriaCount.ShouldBe(6);
    }

    [Fact]
    public void EvaluateCriterion_ShouldUpdateResult_AndTransitionToInProgress()
    {
        // Arrange
        var sut = CreateSampleAssessment();
        sut.ClearDomainEvents();
        var criterionId = sut.Criteria[0].Id;

        // Act
        var actual = sut.EvaluateCriterion(criterionId, CriterionResult.Pass, None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(AssessmentStatus.InProgress);
        sut.Criteria[0].Result.IsSome.ShouldBeTrue();
        sut.DomainEvents.OfType<ComplianceAssessment.CriterionEvaluatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void EvaluateCriterion_WithNotes_ShouldSetNotes()
    {
        // Arrange
        var sut = CreateSampleAssessment();
        var criterionId = sut.Criteria[0].Id;

        // Act
        var actual = sut.EvaluateCriterion(criterionId, CriterionResult.Pass, Some("Looks good"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Criteria[0].Notes.ShouldBe(Some("Looks good"));
    }

    [Fact]
    public void EvaluateCriterion_ShouldFail_WhenCriterionNotFound()
    {
        // Arrange
        var sut = CreateSampleAssessment();
        var fakeCriterionId = AssessmentCriterionId.New();

        // Act
        var actual = sut.EvaluateCriterion(fakeCriterionId, CriterionResult.Pass, None);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Complete_AllPassing_ShouldSetStatusPassed()
    {
        // Arrange
        var sut = CreateSampleAssessment(riskTier: "Minimal");
        foreach (var criterion in sut.Criteria)
        {
            sut.EvaluateCriterion(criterion.Id, CriterionResult.Pass, None);
        }
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Complete();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(AssessmentStatus.Passed);
        sut.OverallScore.IsSome.ShouldBeTrue();
        sut.OverallScore.IfSome(score => ((int)score).ShouldBe(100));
        var completedEvent = sut.DomainEvents.OfType<ComplianceAssessment.CompletedEvent>().ShouldHaveSingleItem();
        completedEvent.Status.ShouldBe(AssessmentStatus.Passed);
    }

    [Fact]
    public void Complete_AllFailing_ShouldSetStatusFailed()
    {
        // Arrange
        var sut = CreateSampleAssessment(riskTier: "Minimal");
        foreach (var criterion in sut.Criteria)
        {
            sut.EvaluateCriterion(criterion.Id, CriterionResult.Fail, None);
        }
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Complete();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(AssessmentStatus.Failed);
        sut.OverallScore.IsSome.ShouldBeTrue();
        sut.OverallScore.IfSome(score => ((int)score).ShouldBe(0));
    }

    [Fact]
    public void Complete_MixedResults_ShouldSetStatusRequiresRemediation()
    {
        // Arrange — High risk generates 6 criteria
        var sut = CreateSampleAssessment(riskTier: "High");

        // Pass 3, Fail 3 → 50% score (between 40 and 70)
        for (var i = 0; i < sut.Criteria.Count; i++)
        {
            var result = i < 3 ? CriterionResult.Pass : CriterionResult.Fail;
            sut.EvaluateCriterion(sut.Criteria[i].Id, result, None);
        }
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Complete();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(AssessmentStatus.RequiresRemediation);
    }

    [Fact]
    public void Complete_WithNotApplicable_ShouldExcludeFromScore()
    {
        // Arrange — Minimal risk generates 3 criteria
        var sut = CreateSampleAssessment(riskTier: "Minimal");

        // 2 Pass, 1 NotApplicable → 100% (only applicable count)
        sut.EvaluateCriterion(sut.Criteria[0].Id, CriterionResult.Pass, None);
        sut.EvaluateCriterion(sut.Criteria[1].Id, CriterionResult.Pass, None);
        sut.EvaluateCriterion(sut.Criteria[2].Id, CriterionResult.NotApplicable, None);
        sut.ClearDomainEvents();

        // Act
        var actual = sut.Complete();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.Status.ShouldBe(AssessmentStatus.Passed);
        sut.OverallScore.IfSome(score => ((int)score).ShouldBe(100));
    }

    [Fact]
    public void Complete_ShouldFail_WhenNotAllCriteriaEvaluated()
    {
        // Arrange
        var sut = CreateSampleAssessment(riskTier: "Minimal");
        // Only evaluate one criterion out of three
        sut.EvaluateCriterion(sut.Criteria[0].Id, CriterionResult.Pass, None);

        // Act
        var actual = sut.Complete();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = ComplianceAssessmentId.New();
        var modelId = AIModelId.New();
        var deploymentId = ModelDeploymentId.New();
        var score = AssessmentScore.CreateFromValidated(85);
        var status = AssessmentStatus.Passed;
        var criteria = new[]
        {
            AssessmentCriterion.CreateFromValidated(
                AssessmentCriterionId.New(), "Test", "Description",
                Some(CriterionResult.Pass), None, Some(DateTime.UtcNow))
        };
        var assessedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = ComplianceAssessment.CreateFromValidated(
            id, modelId, deploymentId, Some(score), status, criteria,
            assessedAt, createdAt, None);

        // Assert
        sut.Id.ShouldBe(id);
        sut.Status.ShouldBe(AssessmentStatus.Passed);
        sut.Criteria.Count.ShouldBe(1);
        sut.DomainEvents.ShouldBeEmpty();
    }
}
