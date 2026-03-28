using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Advanced;

/// <summary>
/// Advanced01: InstrumentAdvice API 상세 설명
/// 
/// 학습 목표:
/// - InstrumentAdvice<T> API 이해
/// - 권장 버킷 경계 설정
/// - OpenTelemetry SDK 통합
/// </summary>
public static class Advanced01_InstrumentAdvice
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Advanced01: InstrumentAdvice API");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Advanced");

        // InstrumentAdvice를 사용하여 권장 버킷 경계 설정
        // 중요: .NET 9.0.0+ System.Diagnostics.DiagnosticSource 패키지 필요
        double[] recommendedBuckets = [0.01, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0];

        Histogram<double> histogram = meter.CreateHistogram<double>(
            name: "advanced.request.duration",
            unit: "s",
            description: "Request duration with InstrumentAdvice buckets",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = recommendedBuckets
            });

        Console.WriteLine("InstrumentAdvice API:");
        Console.WriteLine("   - Allows instrumentation authors to specify recommended bucket boundaries");
        Console.WriteLine("   - Collection tools (e.g., OpenTelemetry SDK) can use these recommendations");
        Console.WriteLine("   - Provides better default experience for users");
        Console.WriteLine();

        Console.WriteLine("Recommended Buckets:");
        Console.WriteLine($"   [{string.Join(", ", recommendedBuckets.Select(b => $"{b * 1000:F0}ms"))}]");
        Console.WriteLine();

        Console.WriteLine("Recording measurements...");
        Console.WriteLine();

        Random random = new();
        for (int i = 0; i < 50; i++)
        {
            double duration = random.NextDouble() * 3.0 + 0.05;
            histogram.Record(duration);
            Console.WriteLine($"  Measurement {i + 1}: {duration * 1000:F2} ms");
        }

        Console.WriteLine();
        Console.WriteLine("✅ Measurements recorded with InstrumentAdvice!");
        Console.WriteLine();
        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   1. InstrumentAdvice is a recommendation, not a requirement");
        Console.WriteLine("   2. Collection tools may choose to use or ignore these recommendations");
        Console.WriteLine("   3. OpenTelemetry .NET SDK 1.10.0+ supports InstrumentAdvice");
        Console.WriteLine("   4. Useful for library authors who want to provide good defaults");
        Console.WriteLine();
    }
}
