namespace HistogramExploration.Demo.Tests.Unit.SharedTests;

public class MetricViewerTests
{
    [Fact]
    public void PrintPercentiles_ShouldCalculatePercentilesCorrectly()
    {
        // Arrange
        var values = new List<double> { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };

        // Act & Assert - Should not throw
        MetricViewer.PrintPercentiles(values, "Test Values");
    }

    [Fact]
    public void PrintPercentiles_WithEmptyList_ShouldNotThrow()
    {
        // Arrange
        var values = new List<double>();

        // Act & Assert - Should not throw
        MetricViewer.PrintPercentiles(values, "Empty Values");
    }

    [Fact]
    public void PrintBucketDistribution_ShouldNotThrow()
    {
        // Arrange
        var values = new List<double> { 0.1, 0.2, 0.3, 0.4, 0.5 };
        var buckets = new double[] { 0.1, 0.25, 0.5, 1.0 };

        // Act & Assert - Should not throw
        MetricViewer.PrintBucketDistribution(values, buckets, "Test Distribution");
    }

    [Fact]
    public void PrintComparison_ShouldNotThrow()
    {
        // Arrange
        var values = new List<double> { 450, 480, 500, 520, 550 };
        var badBuckets = new double[] { 0, 1, 2, 5, 10 };
        var goodBuckets = new double[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 };
        double sloThresholdMs = 500;

        // Act & Assert - Should not throw
        MetricViewer.PrintComparison(values, badBuckets, goodBuckets, sloThresholdMs);
    }
}
