using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Intermediate;

/// <summary>
/// Intermediate01: 커스텀 버킷 경계 설정
/// 
/// 학습 목표:
/// - 버킷(Bucket)이 무엇인지 이해
/// - InstrumentAdvice API로 커스텀 버킷 설정
/// - 버킷 선택 전략
/// </summary>
public static class Intermediate01_CustomBuckets
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Intermediate01: Custom Bucket Boundaries");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Intermediate");

        // 기본 버킷 (OpenTelemetry 기본값)
        Histogram<double> defaultHistogram = meter.CreateHistogram<double>(
            name: "intermediate.default.duration",
            unit: "s",
            description: "Duration with default buckets");

        // 커스텀 버킷 설정 (InstrumentAdvice API)
        // 중요: 버킷 경계는 오름차순으로 정렬되어야 함
        double[] customBuckets = [0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0];

        Histogram<double> customHistogram = meter.CreateHistogram<double>(
            name: "intermediate.custom.duration",
            unit: "s",
            description: "Duration with custom buckets",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = customBuckets
            });

        Console.WriteLine("Default Buckets (OpenTelemetry):");
        Console.WriteLine("  [0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500, 5000, 7500, 10000] ms");
        Console.WriteLine();

        Console.WriteLine("Custom Buckets:");
        Console.WriteLine($"  [{string.Join(", ", customBuckets.Select(b => $"{b * 1000:F0}ms"))}]");
        Console.WriteLine();

        Console.WriteLine("Recording measurements...");
        Console.WriteLine();

        Random random = new();
        for (int i = 0; i < 30; i++)
        {
            // 0.05초 ~ 2초 사이의 랜덤 값
            double duration = random.NextDouble() * 1.95 + 0.05;

            defaultHistogram.Record(duration);
            customHistogram.Record(duration);

            Console.WriteLine($"  Measurement {i + 1}: {duration * 1000:F2} ms");
        }

        Console.WriteLine();
        Console.WriteLine("✅ Measurements recorded with both histogram configurations!");
        Console.WriteLine();
        Console.WriteLine("💡 Bucket Selection Strategy:");
        Console.WriteLine("   1. Identify your SLO thresholds (e.g., 500ms, 1s)");
        Console.WriteLine("   2. Include those thresholds as bucket boundaries");
        Console.WriteLine("   3. Add more buckets around critical ranges");
        Console.WriteLine("   4. Consider memory usage (more buckets = more memory)");
        Console.WriteLine();
    }
}
