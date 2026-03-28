using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Shared;

/// <summary>
/// 메트릭 값을 콘솔에 출력하는 유틸리티
/// </summary>
public static class MetricViewer
{
    /// <summary>
    /// 백분위수 계산 및 표시
    /// </summary>
    public static void PrintPercentiles(List<double> values, string label = "Values")
    {
        if (values.Count == 0)
        {
            Console.WriteLine($"{label}: No values");
            return;
        }

        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;

        Console.WriteLine($"\n{label} ({count} measurements):");
        Console.WriteLine($"  P50 (Median): {CalculatePercentile(sorted, 50):F3} ms");
        Console.WriteLine($"  P90:          {CalculatePercentile(sorted, 90):F3} ms");
        Console.WriteLine($"  P95:          {CalculatePercentile(sorted, 95):F3} ms");
        Console.WriteLine($"  P99:          {CalculatePercentile(sorted, 99):F3} ms");
        Console.WriteLine($"  Min:          {sorted[0]:F3} ms");
        Console.WriteLine($"  Max:          {sorted[count - 1]:F3} ms");
        Console.WriteLine($"  Mean:         {sorted.Average():F3} ms");
    }

    /// <summary>
    /// 백분위수 계산
    /// </summary>
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

    /// <summary>
    /// 버킷 분포 시각화
    /// values는 밀리초 단위, bucketBoundaries는 초 단위
    /// </summary>
    public static void PrintBucketDistribution(List<double> values, double[] bucketBoundaries, string label = "Distribution")
    {
        if (values.Count == 0)
        {
            Console.WriteLine($"{label}: No values");
            return;
        }

        var buckets = new int[bucketBoundaries.Length + 1];
        int overflow = 0;

        foreach (var valueMs in values)
        {
            // 밀리초를 초로 변환
            double valueSeconds = valueMs / 1000.0;
            bool placed = false;
            for (int i = 0; i < bucketBoundaries.Length; i++)
            {
                if (valueSeconds < bucketBoundaries[i])
                {
                    buckets[i]++;
                    placed = true;
                    break;
                }
            }
            if (!placed)
            {
                overflow++;
            }
        }

        Console.WriteLine($"\n{label} ({values.Count} measurements):");
        Console.WriteLine($"  Bucket Boundaries: [{string.Join(", ", bucketBoundaries.Select(b => $"{b * 1000:F0}ms"))}]");
        Console.WriteLine();

        for (int i = 0; i < bucketBoundaries.Length; i++)
        {
            string range = i == 0
                ? $"[0ms - {bucketBoundaries[i] * 1000:F0}ms)"
                : $"[{bucketBoundaries[i - 1] * 1000:F0}ms - {bucketBoundaries[i] * 1000:F0}ms)";
            int count = buckets[i];
            double percentage = (double)count / values.Count * 100;
            string bar = new string('█', (int)(percentage / 2)); // 간단한 바 차트
            Console.WriteLine($"  {range,-25} {count,5} ({percentage,5:F1}%) {bar}");
        }

        if (overflow > 0)
        {
            double overflowPercentage = (double)overflow / values.Count * 100;
            string bar = new string('█', (int)(overflowPercentage / 2));
            Console.WriteLine($"  [≥{bucketBoundaries[bucketBoundaries.Length - 1] * 1000:F0}ms]     {overflow,5} ({overflowPercentage,5:F1}%) {bar}");
        }
    }

    /// <summary>
    /// 두 버킷 설정의 비교 결과 출력
    /// </summary>
    public static void PrintComparison(
        List<double> values,
        double[] badBuckets,
        double[] goodBuckets,
        double sloThresholdMs)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine($"SLO Threshold: {sloThresholdMs}ms");
        Console.WriteLine($"Total Measurements: {values.Count}");
        Console.WriteLine(new string('=', 80));

        // 실제 백분위수 계산
        var sorted = values.OrderBy(v => v).ToList();
        double actualP95 = CalculatePercentile(sorted, 95);
        double actualP99 = CalculatePercentile(sorted, 99);

        Console.WriteLine($"\n📊 Actual Percentiles (from raw data):");
        Console.WriteLine($"   P95: {actualP95:F2} ms");
        Console.WriteLine($"   P99: {actualP99:F2} ms");

        // 나쁜 버킷 분석
        Console.WriteLine($"\n❌ Bad Buckets: [{string.Join(", ", badBuckets.Select(b => $"{b * 1000:F0}ms"))}]");
        AnalyzeBucketAlignment(badBuckets, sloThresholdMs);
        PrintBucketDistribution(values, badBuckets, "Bad Bucket Distribution");

        // 좋은 버킷 분석
        Console.WriteLine($"\n✅ Good Buckets: [{string.Join(", ", goodBuckets.Select(b => $"{b * 1000:F0}ms"))}]");
        AnalyzeBucketAlignment(goodBuckets, sloThresholdMs);
        PrintBucketDistribution(values, goodBuckets, "Good Bucket Distribution");

        // 결론
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("💡 Key Insight:");
        Console.WriteLine($"   When SLO threshold ({sloThresholdMs}ms) aligns with bucket boundary,");
        Console.WriteLine($"   you can accurately determine if measurements exceed the threshold.");
        Console.WriteLine($"   Bad buckets: SLO threshold is NOT at a boundary → inaccurate measurement");
        Console.WriteLine($"   Good buckets: SLO threshold IS at a boundary (0.5s = 500ms) → accurate measurement");
        Console.WriteLine(new string('=', 80));
    }

    /// <summary>
    /// 버킷이 SLO 임계값과 정렬되어 있는지 분석
    /// </summary>
    private static void AnalyzeBucketAlignment(double[] buckets, double sloThresholdMs)
    {
        double sloThresholdSeconds = sloThresholdMs / 1000.0;
        bool aligned = buckets.Contains(sloThresholdSeconds);

        if (aligned)
        {
            Console.WriteLine($"   ✅ SLO threshold ({sloThresholdMs}ms = {sloThresholdSeconds}s) is ALIGNED with bucket boundary");
        }
        else
        {
            // 가장 가까운 버킷 찾기
            double closestBucket = buckets.OrderBy(b => Math.Abs(b - sloThresholdSeconds)).First();
            double difference = Math.Abs(closestBucket - sloThresholdSeconds) * 1000;
            Console.WriteLine($"   ❌ SLO threshold ({sloThresholdMs}ms = {sloThresholdSeconds}s) is NOT aligned");
            Console.WriteLine($"      Closest bucket: {closestBucket * 1000:F0}ms (difference: {difference:F0}ms)");
            Console.WriteLine($"      This causes inaccurate P95/P99 calculations!");
        }
    }
}
