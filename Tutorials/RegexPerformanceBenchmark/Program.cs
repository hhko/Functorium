using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace RegexPerformanceBenchmark;

/// <summary>
/// UsecasePipelineBase.cs의 Regex 캐싱 최적화 성능 비교
///
/// 개선 전: Regex.Match() - 런타임 Regex 컴파일
/// 개선 후: [GeneratedRegex] - AOT 컴파일된 Regex
/// </summary>
[MemoryDiagnoser]
public partial class RegexBenchmark
{
    // 실제 UsecasePipelineBase에서 사용하는 패턴들
    private const string PlusPattern = @"\.([^.+]+)\+";
    private const string BeforePlusPattern = @"^([^+]+)\+";
    private const string AfterLastDotPattern = @"\.([^.+]+)$";

    // 테스트 데이터 - 실제 사용 사례 기반
    private readonly string[] testInputs = [
        "Observability.Adapters.Infrastructure.Repositories.OrderRepository+CreateOrderHandler",
        "Observability.Application.Usecases.CreateOrderCommand",
        "Observability.Adapters.Infrastructure.Repositories.UserRepository",
        "Domain.Entities.Order+OrderValidator",
        "Application.Usecases.ProcessPayment+PaymentProcessor",
        "Infrastructure.Services.EmailService",
        "Domain.ValueObjects.Money",
        "Adapters.External.PaymentGateway+StripeAdapter"
    ];

    // 개선 전 방식: 런타임 Regex 컴파일
    public class LegacyRegexApproach
    {
        [Benchmark(Baseline = true)]
        public int RuntimeCompiledRegex()
        {
            int totalMatches = 0;

            foreach (var input in BenchmarkTestData.TestInputs)
            {
                // PlusPattern: ".xxx+"
                var plusMatch = Regex.Match(input, PlusPattern);
                if (plusMatch.Success)
                    totalMatches++;

                // BeforePlusPattern: "^([^+]+)\+"
                var beforePlusMatch = Regex.Match(input, BeforePlusPattern);
                if (beforePlusMatch.Success)
                    totalMatches++;

                // AfterLastDotPattern: "\.([^.+]+)$"
                var afterDotMatch = Regex.Match(input, AfterLastDotPattern);
                if (afterDotMatch.Success)
                    totalMatches++;
            }

            return totalMatches;
        }
    }

    // 개선 후 방식: AOT 컴파일된 Regex
    public partial class OptimizedRegexApproach
    {
        [GeneratedRegex(PlusPattern, RegexOptions.Compiled)]
        private static partial Regex PlusPatternRegex();

        [GeneratedRegex(BeforePlusPattern, RegexOptions.Compiled)]
        private static partial Regex BeforePlusPatternRegex();

        [GeneratedRegex(AfterLastDotPattern, RegexOptions.Compiled)]
        private static partial Regex AfterLastDotPatternRegex();

        [Benchmark]
        public int AotCompiledRegex()
        {
            int totalMatches = 0;

            foreach (var input in BenchmarkTestData.TestInputs)
            {
                // PlusPattern: ".xxx+"
                var plusMatch = PlusPatternRegex().Match(input);
                if (plusMatch.Success)
                    totalMatches++;

                // BeforePlusPattern: "^([^+]+)\+"
                var beforePlusMatch = BeforePlusPatternRegex().Match(input);
                if (beforePlusMatch.Success)
                    totalMatches++;

                // AfterLastDotPattern: "\.([^.+]+)$"
                var afterDotMatch = AfterLastDotPatternRegex().Match(input);
                if (afterDotMatch.Success)
                    totalMatches++;
            }

            return totalMatches;
        }
    }

    // 실제 UsecasePipelineBase.GetRequestHandler 로직 성능 비교
    public partial class RealWorldScenario
    {
        [Benchmark]
        public int LegacyGetRequestHandler()
        {
            int totalProcessed = 0;

            foreach (var input in BenchmarkTestData.TestInputs)
            {
                var result = GetRequestHandlerLegacy(input);
                if (!string.IsNullOrEmpty(result))
                    totalProcessed++;
            }

            return totalProcessed;
        }

        [Benchmark]
        public int OptimizedGetRequestHandler()
        {
            int totalProcessed = 0;

            foreach (var input in BenchmarkTestData.TestInputs)
            {
                var result = GetRequestHandlerOptimized(input);
                if (!string.IsNullOrEmpty(result))
                    totalProcessed++;
            }

            return totalProcessed;
        }

        // 개선 전 방식 (현재 코드)
        private static string GetRequestHandlerLegacy(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // "+"가 있는 경우: ".xxx+"
            Match plusMatch = Regex.Match(input, PlusPattern);
            if (plusMatch.Success)
            {
                return plusMatch.Groups[1].Value;
            }

            // "+"가 있으나 "."이 없는 경우: "^([^+]+)\+"
            Match beforePlusMatch = Regex.Match(input, BeforePlusPattern);
            if (beforePlusMatch.Success)
            {
                return beforePlusMatch.Groups[1].Value;
            }

            // "+"가 없는 경우: ".xxx$"
            Match afterLastDotMatch = Regex.Match(input, AfterLastDotPattern);
            if (afterLastDotMatch.Success)
            {
                return afterLastDotMatch.Groups[1].Value;
            }

            return string.Empty;
        }

        // 개선 후 방식 ([GeneratedRegex] 적용)
        private static string GetRequestHandlerOptimized(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // "+"가 있는 경우: ".xxx+"
            Match plusMatch = PlusPatternRegex().Match(input);
            if (plusMatch.Success)
            {
                return plusMatch.Groups[1].Value;
            }

            // "+"가 있으나 "."이 없는 경우: "^([^+]+)\+"
            Match beforePlusMatch = BeforePlusPatternRegex().Match(input);
            if (beforePlusMatch.Success)
            {
                return beforePlusMatch.Groups[1].Value;
            }

            // "+"가 없는 경우: ".xxx$"
            Match afterLastDotMatch = AfterLastDotPatternRegex().Match(input);
            if (afterLastDotMatch.Success)
            {
                return afterLastDotMatch.Groups[1].Value;
            }

            return string.Empty;
        }

        [GeneratedRegex(PlusPattern, RegexOptions.Compiled)]
        private static partial Regex PlusPatternRegex();

        [GeneratedRegex(BeforePlusPattern, RegexOptions.Compiled)]
        private static partial Regex BeforePlusPatternRegex();

        [GeneratedRegex(AfterLastDotPattern, RegexOptions.Compiled)]
        private static partial Regex AfterLastDotPatternRegex();
    }
}

// 벤치마크 데이터 공유
public static class BenchmarkTestData
{
    public static readonly string[] TestInputs = [
        "Observability.Adapters.Infrastructure.Repositories.OrderRepository+CreateOrderHandler",
        "Observability.Application.Usecases.CreateOrderCommand",
        "Observability.Adapters.Infrastructure.Repositories.UserRepository",
        "Domain.Entities.Order+OrderValidator",
        "Application.Usecases.ProcessPayment+PaymentProcessor",
        "Infrastructure.Services.EmailService",
        "Domain.ValueObjects.Money",
        "Adapters.External.PaymentGateway+StripeAdapter"
    ];
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Regex 캐싱 최적화 성능 비교 ===");
        Console.WriteLine("UsecasePipelineBase.cs의 [GeneratedRegex] 적용 효과 측정");
        Console.WriteLine();

        // Regex 패턴별 성능 비교
        Console.WriteLine("1. Regex.Match() vs [GeneratedRegex] 비교:");
        var summary1 = BenchmarkRunner.Run<RegexBenchmark.LegacyRegexApproach>();
        var summary2 = BenchmarkRunner.Run<RegexBenchmark.OptimizedRegexApproach>();

        // 실제 사용 사례 성능 비교
        Console.WriteLine("\n2. 실제 GetRequestHandler() 메서드 성능 비교:");
        var summary3 = BenchmarkRunner.Run<RegexBenchmark.RealWorldScenario>();

        Console.WriteLine("\n=== 벤치마크 완료 ===");
        Console.WriteLine("결과 분석:");
        Console.WriteLine("- 개선 전: 매번 Regex 컴파일 오버헤드");
        Console.WriteLine("- 개선 후: AOT 컴파일된 Regex 재사용");
        Console.WriteLine("- 기대 효과: 시작 성능 10-15% 향상, GC 압력 감소");
    }
}