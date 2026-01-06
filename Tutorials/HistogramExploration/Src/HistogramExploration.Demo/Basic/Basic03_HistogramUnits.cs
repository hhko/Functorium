using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Basic;

/// <summary>
/// Basic03: 단위(Unit)와 설명(Description) 지정
/// 
/// 학습 목표:
/// - UCUM 표준 단위 사용
/// - 설명(description)으로 메트릭 의미 명확화
/// - 다양한 단위 예제
/// </summary>
public static class Basic03_HistogramUnits
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Basic03: Histogram Units and Descriptions");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Basic");

        // 시간 측정: 초 단위 (UCUM 표준)
        Histogram<double> requestDuration = meter.CreateHistogram<double>(
            name: "basic.request.duration",
            unit: "s", // seconds
            description: "HTTP request processing duration");

        // 크기 측정: 바이트 단위
        Histogram<long> responseSize = meter.CreateHistogram<long>(
            name: "basic.response.size",
            unit: "By", // bytes (UCUM 표준)
            description: "HTTP response body size in bytes");

        // 사용자 정의 단위: {requests} (UCUM 표준의 descriptive annotation)
        Histogram<int> queueLength = meter.CreateHistogram<int>(
            name: "basic.queue.length",
            unit: "{requests}", // curly braces = descriptive annotation
            description: "Number of requests waiting in queue");

        Console.WriteLine("Recording measurements with different units...");
        Console.WriteLine();

        Random random = new();

        // 시간 측정 (초)
        for (int i = 0; i < 5; i++)
        {
            double duration = random.NextDouble() * 0.5 + 0.1;
            requestDuration.Record(duration);
            Console.WriteLine($"  Request Duration: {duration * 1000:F2} ms (recorded as {duration:F3} s)");
        }

        Console.WriteLine();

        // 크기 측정 (바이트)
        for (int i = 0; i < 5; i++)
        {
            long size = random.Next(1000, 10000);
            responseSize.Record(size);
            Console.WriteLine($"  Response Size: {size:N0} bytes");
        }

        Console.WriteLine();

        // 큐 길이 측정
        for (int i = 0; i < 5; i++)
        {
            int length = random.Next(0, 20);
            queueLength.Record(length);
            Console.WriteLine($"  Queue Length: {length} requests");
        }

        Console.WriteLine();
        Console.WriteLine("✅ Measurements recorded with proper units!");
        Console.WriteLine();
        Console.WriteLine("💡 UCUM Unit Standards:");
        Console.WriteLine("   - 's' = seconds (time)");
        Console.WriteLine("   - 'By' = bytes (size)");
        Console.WriteLine("   - '{requests}' = descriptive annotation (not a standard unit)");
        Console.WriteLine("   - See: https://ucum.org/ for more unit standards");
        Console.WriteLine();
    }
}
