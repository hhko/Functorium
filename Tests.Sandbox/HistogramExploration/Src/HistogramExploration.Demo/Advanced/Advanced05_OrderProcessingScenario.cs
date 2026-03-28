// using System.Diagnostics;
// using System.Diagnostics.Metrics;
// using Functorium.Adapters.Observabilities.Configurations;
// using HistogramExploration.Demo.Shared;

// namespace HistogramExploration.Demo.Advanced;

// /// <summary>
// /// Advanced05: 주문 처리 시간 측정 (SLO 정렬)
// /// 
// /// 학습 목표:
// /// - Functorium의 UsecaseMetricsPipeline 패턴 적용
// /// - SLO 위반 감지 및 알림
// /// - 실제 프로덕션 사용 예제
// /// </summary>
// public static class Advanced05_OrderProcessingScenario
// {
//     public static void Run()
//     {
//         Console.WriteLine("=".PadRight(80, '='));
//         Console.WriteLine("Advanced05: Order Processing Scenario (SLO-Aligned)");
//         Console.WriteLine("=".PadRight(80, '='));
//         Console.WriteLine();

//         Meter meter = new("HistogramExploration.Advanced");

//         // Functorium의 SLO 정렬 버킷 사용
//         double[] sloBuckets = SloConfiguration.DefaultHistogramBuckets;

//         Histogram<double> orderProcessingDuration = meter.CreateHistogram<double>(
//             name: "advanced.order.processing.duration",
//             unit: "s",
//             description: "Order processing duration with SLO-aligned buckets",
//             advice: new InstrumentAdvice<double>
//             {
//                 HistogramBucketBoundaries = sloBuckets
//             });

//         // SLO 목표값 (Command 기본값)
//         double sloP95Ms = 500;  // 500ms
//         double sloP99Ms = 1000; // 1000ms

//         Console.WriteLine("SLO Configuration:");
//         Console.WriteLine($"   P95 Target: ≤ {sloP95Ms}ms");
//         Console.WriteLine($"   P99 Target: ≤ {sloP99Ms}ms");
//         Console.WriteLine();

//         Console.WriteLine("Simulating order processing...");
//         Console.WriteLine();

//         string[] orderTypes = { "standard", "express", "premium" };
//         var allLatencies = new List<double>();
//         var p95Violations = new List<double>();
//         var p99Violations = new List<double>();

//         Random random = new();

//         for (int i = 0; i < 200; i++)
//         {
//             string orderType = orderTypes[random.Next(orderTypes.Length)];

//             // 주문 타입에 따라 다른 처리 시간
//             double baseTime = orderType switch
//             {
//                 "standard" => 0.2,  // 표준 주문
//                 "express" => 0.15, // 빠른 배송
//                 "premium" => 0.3,  // 프리미엄 (추가 검증)
//                 _ => 0.2
//             };

//             double duration = baseTime + random.NextDouble() * 0.4;
//             double durationMs = duration * 1000;

//             TagList tags = new()
//             {
//                 { "order.type", orderType },
//                 { "order.id", $"ORD-{i + 1:D6}" }
//             };

//             orderProcessingDuration.Record(duration, tags);
//             allLatencies.Add(durationMs);

//             // SLO 위반 감지
//             if (durationMs > sloP99Ms)
//             {
//                 p99Violations.Add(durationMs);
//             }
//             else if (durationMs > sloP95Ms)
//             {
//                 p95Violations.Add(durationMs);
//             }

//             if (i < 20 || durationMs > sloP95Ms)
//             {
//                 string status = durationMs > sloP99Ms ? "❌ P99 VIOLATION" :
//                                durationMs > sloP95Ms ? "⚠️  P95 VIOLATION" : "✅ OK";
//                 Console.WriteLine($"  {orderType,-10} {durationMs,6:F2} ms {status}");
//             }
//         }

//         Console.WriteLine();
//         Console.WriteLine("📊 Order Processing Analysis:");
//         MetricViewer.PrintPercentiles(allLatencies, "Order Processing Durations");

//         Console.WriteLine();
//         Console.WriteLine("🚨 SLO Violation Summary:");
//         Console.WriteLine($"   P95 Violations (> {sloP95Ms}ms): {p95Violations.Count} orders");
//         Console.WriteLine($"   P99 Violations (> {sloP99Ms}ms): {p99Violations.Count} orders");

//         if (p95Violations.Any())
//         {
//             Console.WriteLine($"   P95 Violation Rate: {(double)p95Violations.Count / allLatencies.Count * 100:F2}%");
//         }

//         if (p99Violations.Any())
//         {
//             Console.WriteLine($"   P99 Violation Rate: {(double)p99Violations.Count / allLatencies.Count * 100:F2}%");
//         }

//         Console.WriteLine();
//         Console.WriteLine("💡 Functorium Pattern:");
//         Console.WriteLine("   - UsecaseMetricsPipeline automatically records duration");
//         Console.WriteLine("   - SloConfiguration provides SLO-aligned buckets");
//         Console.WriteLine("   - SLO violations can trigger alerts");
//         Console.WriteLine("   - Bucket alignment ensures accurate P95/P99 calculations");
//         Console.WriteLine();
//     }
// }
