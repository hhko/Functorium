using System.Diagnostics.Metrics;
using HistogramExploration.Demo.Shared;

namespace HistogramExploration.Demo.Advanced;

/// <summary>
/// Advanced06: 버킷 정렬의 영향 (핵심 개념 설명)
/// 
/// 학습 목표:
/// - "P95/P99 계산 정확도 향상" 개념 이해
/// - "SLO 임계값(예: 500ms) 정확히 측정 가능" 개념 이해
/// - 버킷 경계가 SLO 임계값과 정렬되지 않았을 때의 문제점
/// - Functorium의 DefaultHistogramBuckets가 왜 0.5초와 1초를 포함하는지 이해
/// </summary>
public static class Advanced06_BucketAlignmentImpact
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Advanced06: Bucket Alignment Impact");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();
        Console.WriteLine("이 예제는 다음 개념을 설명합니다:");
        Console.WriteLine("  - P95/P99 계산 정확도 향상");
        Console.WriteLine("  - SLO 임계값(예: 500ms) 정확히 측정 가능");
        Console.WriteLine();

        // 시나리오 설정
        double sloThresholdMs = 500; // SLO 목표: P95 ≤ 500ms
        int measurementCount = 100;

        Console.WriteLine($"📋 Scenario:");
        Console.WriteLine($"   SLO Threshold: P95 ≤ {sloThresholdMs}ms");
        Console.WriteLine($"   Measurements: {measurementCount}");
        Console.WriteLine();

        // SLO 임계값 근처에 집중된 실제 데이터 생성
        // 대부분의 값이 450-550ms 범위에 있음
        var actualLatencies = ScenarioHelpers.GenerateSloFocusedLatencies(
            measurementCount,
            sloThresholdMs,
            spreadMs: 100);

        // 나쁜 버킷: SLO 임계값과 정렬되지 않음
        // [0, 1, 2, 5, 10]초 → 500ms(0.5초)가 버킷 경계에 없음!
        double[] badBuckets = [0, 1, 2, 5, 10]; // 초 단위

        // 좋은 버킷: SLO 임계값과 정렬됨
        // Functorium의 DefaultHistogramBuckets 사용
        double[] goodBuckets = [0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10];

        Console.WriteLine("🔍 Creating histograms with different bucket configurations...");
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Advanced");

        // 나쁜 버킷으로 Histogram 생성
        Histogram<double> badHistogram = meter.CreateHistogram<double>(
            name: "advanced.bad_buckets.duration",
            unit: "s",
            description: "Duration with misaligned buckets",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = badBuckets
            });

        // 좋은 버킷으로 Histogram 생성
        Histogram<double> goodHistogram = meter.CreateHistogram<double>(
            name: "advanced.good_buckets.duration",
            unit: "s",
            description: "Duration with SLO-aligned buckets",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = goodBuckets
            });

        Console.WriteLine("📊 Recording measurements...");
        Console.WriteLine();

        // 실제 지연시간을 두 Histogram에 기록
        foreach (var latencyMs in actualLatencies)
        {
            double latencySeconds = latencyMs / 1000.0;
            badHistogram.Record(latencySeconds);
            goodHistogram.Record(latencySeconds);
        }

        Console.WriteLine("✅ Measurements recorded!");
        Console.WriteLine();

        // 비교 결과 출력
        MetricViewer.PrintComparison(actualLatencies, badBuckets, goodBuckets, sloThresholdMs);

        Console.WriteLine();
        Console.WriteLine("📚 Additional Explanation:");
        Console.WriteLine();
        Console.WriteLine("왜 버킷 정렬이 중요한가?");
        Console.WriteLine();
        Console.WriteLine("1. Histogram은 버킷 단위로 값을 집계합니다.");
        Console.WriteLine("   - 각 버킷은 특정 범위의 값들을 그룹화합니다");
        Console.WriteLine("   - 예: [0-1초) 버킷에는 0초 이상 1초 미만의 모든 값이 포함됩니다");
        Console.WriteLine();
        Console.WriteLine("2. 백분위수 계산은 버킷 분포를 기반으로 합니다.");
        Console.WriteLine("   - P95를 계산하려면 각 버킷에 몇 개의 값이 있는지 알아야 합니다");
        Console.WriteLine("   - 버킷 경계가 측정하려는 값(SLO 임계값) 근처에 없으면 부정확합니다");
        Console.WriteLine();
        Console.WriteLine("3. SLO 임계값을 버킷 경계로 설정하면:");
        Console.WriteLine("   - 해당 임계값을 정확히 초과하는 요청 수를 알 수 있습니다");
        Console.WriteLine("   - P95/P99 계산이 더 정확해집니다");
        Console.WriteLine("   - SLO 위반 여부를 신뢰할 수 있게 판단할 수 있습니다");
        Console.WriteLine();
        Console.WriteLine("4. Functorium의 DefaultHistogramBuckets:");
        Console.WriteLine("   - 0.5초(500ms) = Command SLO P95 목표값");
        Console.WriteLine("   - 1초(1000ms) = Command SLO P99 목표값");
        Console.WriteLine("   - 0.2초(200ms) = Query SLO P95 목표값");
        Console.WriteLine("   - 0.5초(500ms) = Query SLO P99 목표값");
        Console.WriteLine("   → 모든 주요 SLO 임계값이 버킷 경계로 포함되어 있습니다!");
        Console.WriteLine();
    }
}
