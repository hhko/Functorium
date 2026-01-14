// using System.Diagnostics.Metrics;
// using Functorium.Adapters.Observabilities.Configurations;

// namespace HistogramExploration.Demo.Tests.Unit.ExamplesTests;

// public class AdvancedExamplesTests
// {
//     [Fact]
//     public void Advanced02_ShouldUseSloAlignedBuckets()
//     {
//         // Arrange
//         Meter meter = new("Test.Advanced");
//         double[] sloBuckets = SloConfiguration.DefaultHistogramBuckets;

//         // Act
//         Histogram<double> histogram = meter.CreateHistogram<double>(
//             name: "test.duration",
//             unit: "s",
//             description: "Duration with SLO-aligned buckets",
//             advice: new InstrumentAdvice<double>
//             {
//                 HistogramBucketBoundaries = sloBuckets
//             });

//         // Assert
//         histogram.ShouldNotBeNull();
//         sloBuckets.ShouldNotBeNull();
//         sloBuckets.Length.ShouldBeGreaterThan(0);
        
//         // Verify SLO alignment
//         sloBuckets.Contains(0.5).ShouldBeTrue(); // 500ms
//         sloBuckets.Contains(1.0).ShouldBeTrue(); // 1000ms
//     }

//     [Fact]
//     public void Advanced06_BucketAlignment_ShouldShowDifference()
//     {
//         // Arrange
//         var values = new List<double> { 450, 480, 500, 520, 550 };
//         var badBuckets = new double[] { 0, 1, 2, 5, 10 };
//         var goodBuckets = SloConfiguration.DefaultHistogramBuckets;
//         double sloThresholdMs = 500;

//         // Act & Assert
//         // Bad buckets should not contain SLO threshold
//         badBuckets.Contains(sloThresholdMs / 1000.0).ShouldBeFalse();
        
//         // Good buckets should contain SLO threshold
//         goodBuckets.Contains(sloThresholdMs / 1000.0).ShouldBeTrue();
//     }

//     [Fact]
//     public void Advanced06_BucketBoundaries_ShouldBeSorted()
//     {
//         // Arrange
//         var buckets = SloConfiguration.DefaultHistogramBuckets;

//         // Act
//         var sorted = buckets.OrderBy(b => b).ToArray();

//         // Assert
//         buckets.ShouldBe(sorted);
//     }

//     [Fact]
//     public void Advanced06_BucketBoundaries_ShouldBePositive()
//     {
//         // Arrange
//         var buckets = SloConfiguration.DefaultHistogramBuckets;

//         // Act & Assert
//         buckets.All(b => b > 0).ShouldBeTrue();
//     }
// }
