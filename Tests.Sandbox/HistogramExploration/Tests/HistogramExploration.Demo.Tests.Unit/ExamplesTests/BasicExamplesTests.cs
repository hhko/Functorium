using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Tests.Unit.ExamplesTests;

public class BasicExamplesTests
{
    [Fact]
    public void Basic01_ShouldCreateHistogram()
    {
        // Arrange
        Meter meter = new("Test.Basic");

        // Act
        Histogram<double> histogram = meter.CreateHistogram<double>(
            name: "test.duration",
            unit: "s",
            description: "Test duration");

        // Assert
        histogram.ShouldNotBeNull();
        
        // Record a value
        histogram.Record(0.5);
    }

    [Fact]
    public void Basic02_ShouldRecordWithTags()
    {
        // Arrange
        Meter meter = new("Test.Basic");
        Histogram<double> histogram = meter.CreateHistogram<double>(
            name: "test.duration",
            unit: "s",
            description: "Test duration");

        // Act
        TagList tags = new()
        {
            { "category", "test" },
            { "method", "GET" }
        };
        histogram.Record(0.3, tags);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void Basic03_ShouldCreateHistogramWithUnits()
    {
        // Arrange
        Meter meter = new("Test.Basic");

        // Act
        Histogram<double> durationHistogram = meter.CreateHistogram<double>(
            name: "test.duration",
            unit: "s",
            description: "Duration in seconds");

        Histogram<long> sizeHistogram = meter.CreateHistogram<long>(
            name: "test.size",
            unit: "By",
            description: "Size in bytes");

        // Assert
        durationHistogram.ShouldNotBeNull();
        sizeHistogram.ShouldNotBeNull();
    }

    [Fact]
    public void Basic04_ShouldCalculatePercentiles()
    {
        // Arrange
        var values = new List<double> { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };

        // Act & Assert - Should not throw
        MetricViewer.PrintPercentiles(values, "Test Values");

        // Verify percentile calculation
        var sorted = values.OrderBy(v => v).ToList();
        double p50 = CalculatePercentile(sorted, 50);
        double p95 = CalculatePercentile(sorted, 95);
        double p99 = CalculatePercentile(sorted, 99);

        // Assert
        p50.ShouldBeGreaterThanOrEqualTo(400);
        p50.ShouldBeLessThanOrEqualTo(600);
        p95.ShouldBeGreaterThanOrEqualTo(900);
        p99.ShouldBeGreaterThanOrEqualTo(900);
    }

    private static double CalculatePercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        double index = (percentile / 100.0) * (sortedValues.Count - 1);
        int lowerIndex = (int)Math.Floor(index);
        int upperIndex = (int)Math.Ceiling(index);

        if (lowerIndex == upperIndex)
            return sortedValues[lowerIndex];

        double weight = index - lowerIndex;
        return sortedValues[lowerIndex] * (1 - weight) + sortedValues[upperIndex] * weight;
    }
}
