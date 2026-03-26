using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;
using AiGovernance.Domain.SharedModels.Services;

namespace AiGovernance.Tests.Unit.Domain.Services;

public class RiskClassificationServiceTests
{
    private readonly RiskClassificationService _sut = new();

    [Theory]
    [InlineData("Social scoring system")]
    [InlineData("Real-time surveillance monitoring")]
    public void ClassifyByPurpose_ShouldReturnUnacceptable_WhenPurposeContainsUnacceptableKeyword(string purpose)
    {
        // Arrange
        var modelPurpose = ModelPurpose.Create(purpose).ThrowIfFail();

        // Act
        var actual = _sut.ClassifyByPurpose(modelPurpose);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(RiskTier.Unacceptable);
    }

    [Theory]
    [InlineData("Hiring decision support")]
    [InlineData("Credit risk assessment")]
    [InlineData("Medical diagnosis assistant")]
    [InlineData("Biometric identification")]
    public void ClassifyByPurpose_ShouldReturnHigh_WhenPurposeContainsHighKeyword(string purpose)
    {
        // Arrange
        var modelPurpose = ModelPurpose.Create(purpose).ThrowIfFail();

        // Act
        var actual = _sut.ClassifyByPurpose(modelPurpose);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(RiskTier.High);
    }

    [Theory]
    [InlineData("Sentiment analysis tool")]
    [InlineData("Product recommendation engine")]
    [InlineData("Emotion detection system")]
    public void ClassifyByPurpose_ShouldReturnLimited_WhenPurposeContainsLimitedKeyword(string purpose)
    {
        // Arrange
        var modelPurpose = ModelPurpose.Create(purpose).ThrowIfFail();

        // Act
        var actual = _sut.ClassifyByPurpose(modelPurpose);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(RiskTier.Limited);
    }

    [Theory]
    [InlineData("Generic chatbot")]
    [InlineData("Text summarization")]
    [InlineData("Code completion assistant")]
    public void ClassifyByPurpose_ShouldReturnMinimal_WhenPurposeContainsNoKeyword(string purpose)
    {
        // Arrange
        var modelPurpose = ModelPurpose.Create(purpose).ThrowIfFail();

        // Act
        var actual = _sut.ClassifyByPurpose(modelPurpose);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(RiskTier.Minimal);
    }

    [Fact]
    public void ClassifyByPurpose_ShouldBeCaseInsensitive()
    {
        // Arrange
        var modelPurpose = ModelPurpose.Create("SOCIAL SCORING system").ThrowIfFail();

        // Act
        var actual = _sut.ClassifyByPurpose(modelPurpose);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ShouldBe(RiskTier.Unacceptable);
    }
}
