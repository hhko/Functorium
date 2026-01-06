using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Intermediate;

/// <summary>
/// Intermediate02: 여러 Histogram 동시 사용
/// 
/// 학습 목표:
/// - Meter별 그룹화
/// - 카테고리별 메트릭 관리 (OpenTelemetryMetricRecorder 패턴)
/// - 여러 Histogram을 효율적으로 관리
/// </summary>
public static class Intermediate02_MultipleHistograms
{
    private sealed class MetricsSet
    {
        public required Counter<long> RequestCounter { get; init; }
        public required Histogram<double> DurationHistogram { get; init; }
    }

    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Intermediate02: Multiple Histograms");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        string serviceNamespace = "HistogramExploration";

        // 카테고리별 Meter 및 메트릭 관리 (OpenTelemetryMetricRecorder 패턴)
        Dictionary<string, Meter> meters = new();
        Dictionary<string, MetricsSet> metrics = new();

        string[] categories = { "database", "cache", "external_api" };

        Console.WriteLine("Initializing metrics for categories...");
        Console.WriteLine();

        foreach (var category in categories)
        {
            string categoryLower = category.ToLowerInvariant();

            // 카테고리별 Meter 생성
            Meter meter = new($"{serviceNamespace}.{categoryLower}");
            meters[category] = meter;

            // 카테고리별 메트릭 세트 생성
            MetricsSet metricsSet = new()
            {
                RequestCounter = meter.CreateCounter<long>(
                    name: $"{categoryLower}.requests",
                    description: $"Total number of {category} requests",
                    unit: "{request}"),

                DurationHistogram = meter.CreateHistogram<double>(
                    name: $"{categoryLower}.duration",
                    description: $"Duration of {category} operations in seconds",
                    unit: "s")
            };

            metrics[category] = metricsSet;

            Console.WriteLine($"  ✅ {category}: Meter = {serviceNamespace}.{categoryLower}");
        }

        Console.WriteLine();
        Console.WriteLine("Recording measurements...");
        Console.WriteLine();

        Random random = new();

        // 각 카테고리별로 다른 처리 시간 시뮬레이션
        foreach (var category in categories)
        {
            double baseTime = category switch
            {
                "database" => 0.1,      // 빠름
                "cache" => 0.01,        // 매우 빠름
                "external_api" => 0.5,   // 느림
                _ => 0.2
            };

            for (int i = 0; i < 10; i++)
            {
                double duration = baseTime + random.NextDouble() * 0.2;

                TagList tags = new()
                {
                    { "category", category },
                    { "operation", $"op_{i + 1}" }
                };

                metrics[category].RequestCounter.Add(1, tags);
                metrics[category].DurationHistogram.Record(duration, tags);

                Console.WriteLine($"  {category,-15}: {duration * 1000:F2} ms");
            }
            Console.WriteLine();
        }

        Console.WriteLine("✅ Multiple histograms managed successfully!");
        Console.WriteLine();
        Console.WriteLine("💡 Pattern Benefits:");
        Console.WriteLine("   - Each category has its own Meter (isolation)");
        Console.WriteLine("   - Metrics can be enabled/disabled per category");
        Console.WriteLine("   - Clear namespace organization");
        Console.WriteLine("   - Matches production patterns (e.g., OpenTelemetryMetricRecorder)");
        Console.WriteLine();

        // 정리
        foreach (var meter in meters.Values)
        {
            meter.Dispose();
        }
    }
}
