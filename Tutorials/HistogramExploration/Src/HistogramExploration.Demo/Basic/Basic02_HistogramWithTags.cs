using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HistogramExploration.Demo.Basic;

/// <summary>
/// Basic02: 태그를 사용한 다차원 메트릭
/// 
/// 학습 목표:
/// - 태그(Tags)를 사용하여 메트릭을 분류하는 방법
/// - TagList 구조체 사용 (Functorium 패턴)
/// - 같은 Histogram으로 여러 차원의 데이터 측정
/// </summary>
public static class Basic02_HistogramWithTags
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Basic02: Histogram with Tags");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        Meter meter = new("HistogramExploration.Basic");

        Histogram<double> orderProcessingHistogram = meter.CreateHistogram<double>(
            name: "basic.order.processing_time",
            unit: "s",
            description: "Order processing time by product category");

        Console.WriteLine("Recording measurements with tags...");
        Console.WriteLine();

        Random random = new();

        // 다양한 제품 카테고리별로 처리 시간 기록
        string[] categories = { "electronics", "clothing", "books", "food" };
        string[] paymentMethods = { "credit_card", "paypal", "cash" };

        foreach (var category in categories)
        {
            foreach (var paymentMethod in paymentMethods)
            {
                // 카테고리와 결제 방법에 따라 다른 처리 시간 시뮬레이션
                double baseTime = category switch
                {
                    "electronics" => 0.3,
                    "clothing" => 0.2,
                    "books" => 0.15,
                    "food" => 0.1,
                    _ => 0.2
                };

                double processingTime = baseTime + random.NextDouble() * 0.1;

                // TagList 사용 (Functorium 패턴)
                TagList tags = new()
                {
                    { "product.category", category },
                    { "payment.method", paymentMethod }
                };

                orderProcessingHistogram.Record(processingTime, tags);

                Console.WriteLine($"  {category,-12} / {paymentMethod,-12}: {processingTime * 1000:F2} ms");
            }
        }

        Console.WriteLine();
        Console.WriteLine("✅ Measurements recorded with tags!");
        Console.WriteLine();
        Console.WriteLine("💡 Tip: Tags allow you to filter and aggregate metrics:");
        Console.WriteLine("   - Filter by category: product.category=\"electronics\"");
        Console.WriteLine("   - Filter by payment: payment.method=\"credit_card\"");
        Console.WriteLine("   - Combine filters for detailed analysis");
        Console.WriteLine();
    }
}
