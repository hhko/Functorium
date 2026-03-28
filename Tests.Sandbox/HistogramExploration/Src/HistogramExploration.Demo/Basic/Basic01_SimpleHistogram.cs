using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Basic;

/// <summary>
/// Basic01: 가장 기본적인 Histogram 생성 및 기록
/// 
/// 학습 목표:
/// - Histogram이 무엇인지 이해
/// - CreateHistogram으로 Histogram 생성
/// - Record 메서드로 값 기록
/// </summary>
public static class Basic01_SimpleHistogram
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Basic01: Simple Histogram");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // Meter 생성: 메트릭 그룹의 이름
        Meter meter = new("HistogramExploration.Basic");

        // Histogram 생성
        // - name: 메트릭 이름
        // - unit: 단위 (초 단위)
        // - description: 설명
        Histogram<double> durationHistogram = meter.CreateHistogram<double>(
            name: "basic.request.duration",
            unit: "s",
            description: "Request processing duration in seconds");

        Console.WriteLine("Recording measurements...");
        Console.WriteLine();

        // 랜덤 지연시간 시뮬레이션
        Random random = new();
        for (int i = 0; i < 20; i++)
        {
            // 0.1초 ~ 0.5초 사이의 랜덤 값
            double durationSeconds = random.NextDouble() * 0.4 + 0.1;
            durationHistogram.Record(durationSeconds);

            Console.WriteLine($"  Measurement {i + 1}: {durationSeconds * 1000:F2} ms");
        }

        Console.WriteLine();
        Console.WriteLine("✅ Histogram created and measurements recorded!");
        Console.WriteLine();
        Console.WriteLine("💡 Tip: Use 'dotnet-counters monitor' to view metrics:");
        Console.WriteLine("   dotnet-counters monitor -n HistogramExploration.Demo --counters HistogramExploration.Basic");
        Console.WriteLine();
    }
}
