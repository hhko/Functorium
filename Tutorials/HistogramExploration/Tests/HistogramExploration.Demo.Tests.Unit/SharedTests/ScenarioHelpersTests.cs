namespace HistogramExploration.Demo.Tests.Unit.SharedTests;

public class ScenarioHelpersTests
{
    [Fact]
    public void GenerateRandomLatencyMs_ShouldReturnValueInRange()
    {
        // Arrange
        double minMs = 100;
        double maxMs = 500;

        // Act
        double latency = ScenarioHelpers.GenerateRandomLatencyMs(minMs, maxMs);

        // Assert
        latency.ShouldBeGreaterThanOrEqualTo(minMs);
        latency.ShouldBeLessThan(maxMs);
    }

    [Fact]
    public void GenerateNormalLatencyMs_ShouldReturnValue()
    {
        // Arrange
        double meanMs = 300;
        double stdDevMs = 50;

        // Act
        double latency = ScenarioHelpers.GenerateNormalLatencyMs(meanMs, stdDevMs);

        // Assert
        latency.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GenerateSloFocusedLatencies_ShouldGenerateCorrectCount()
    {
        // Arrange
        int count = 100;
        double sloThresholdMs = 500;
        double spreadMs = 100;

        // Act
        var latencies = ScenarioHelpers.GenerateSloFocusedLatencies(count, sloThresholdMs, spreadMs);

        // Assert
        latencies.ShouldNotBeNull();
        latencies.Count.ShouldBe(count);
        latencies.All(l => l >= 0).ShouldBeTrue();
    }

    [Fact]
    public void GenerateRealisticLatencies_ShouldGenerateCorrectCount()
    {
        // Arrange
        int count = 50;

        // Act
        var latencies = ScenarioHelpers.GenerateRealisticLatencies(count);

        // Assert
        latencies.ShouldNotBeNull();
        latencies.Count.ShouldBe(count);
        latencies.All(l => l >= 0).ShouldBeTrue();
    }
}
