using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class EndpointUrlTests
{
    [Theory]
    [InlineData("https://api.example.com/predict")]
    [InlineData("http://localhost:8080/model")]
    [InlineData("https://ml.internal.corp/v1/inference")]
    public void Create_ShouldSucceed_WhenValueIsValidUrl(string value)
    {
        // Act
        var actual = EndpointUrl.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = EndpointUrl.Create("  https://api.example.com  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("https://api.example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = EndpointUrl.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://file.server.com")]
    [InlineData("just-text")]
    public void Create_ShouldFail_WhenValueIsInvalidUrl(string value)
    {
        // Act
        var actual = EndpointUrl.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
