using LanguageExt;
using ValueComparability.ValueObjects;

namespace ValueComparability.Demonstrations;

/// <summary>
/// Denominator 값 객체의 비교 가능성 테스트
/// </summary>
public static class ComparabilityTests
{
    /// <summary>
    /// Denominator를 안전하게 생성하는 헬퍼 메서드
    /// </summary>
    private static Denominator CreateDenominator(int value)
    {
        var result = from denominator in Denominator.Create(value)
                     select denominator;

        return result.Match(
            Succ: x => x,
            Fail: _ => Denominator.Create(1).Match(
                Succ: x => x,
                Fail: _ => throw new InvalidOperationException($"Failed to create Denominator with value {value}")
            )
        );
    }

    /// <summary>
    /// 기본 비교 기능 시연
    /// </summary>
    public static void DemonstrateBasicComparison()
    {
        Console.WriteLine("=== 기본 비교 기능 테스트 ===");

        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(10)
                     from c in Denominator.Create(5)
                     select (a, b, c);

        result.Match(
            Succ: values =>
            {
                var (a, b, c) = values;
                Console.WriteLine($"a = {a}, b = {b}, c = {c}");
                Console.WriteLine();

                // CompareTo 테스트
                Console.WriteLine("CompareTo 테스트:");
                Console.WriteLine($"a.CompareTo(b) = {a.CompareTo(b)}"); // -1 (a < b)
                Console.WriteLine($"b.CompareTo(a) = {b.CompareTo(a)}"); // 1 (b > a)
                Console.WriteLine($"a.CompareTo(c) = {a.CompareTo(c)}"); // 0 (a == c)
                Console.WriteLine();

                // 연산자 테스트
                Console.WriteLine("연산자 테스트:");
                Console.WriteLine($"a < b: {a < b}");   // true
                Console.WriteLine($"a <= b: {a <= b}"); // true
                Console.WriteLine($"a > b: {a > b}");   // false
                Console.WriteLine($"a >= b: {a >= b}"); // false
                Console.WriteLine($"a == c: {a == c}"); // true
                Console.WriteLine($"a != b: {a != b}"); // true
                Console.WriteLine();
            },
            Fail: error => Console.WriteLine($"생성 실패: {error.Message}")
        );
    }

    /// <summary>
    /// null 비교 시연
    /// </summary>
    public static void DemonstrateNullComparison()
    {
        Console.WriteLine("=== null 비교 테스트 ===");

        var result = from a in Denominator.Create(5)
                     select a;

        result.Match(
            Succ: a =>
            {
                Denominator? nullValue = null;
                Console.WriteLine($"a = {a}, nullValue = null");
                Console.WriteLine();

                // null과의 비교
                Console.WriteLine("null과의 비교:");
                Console.WriteLine($"a.CompareTo(null) = {a.CompareTo(null)}"); // 1 (null보다 큼)
                Console.WriteLine($"a > null: {a > null}"); // true
                Console.WriteLine($"a >= null: {a >= null}"); // true
                Console.WriteLine($"a < null: {a < null}"); // false
                Console.WriteLine($"a <= null: {a <= null}"); // false
                Console.WriteLine($"a == null: {a == null}"); // false
                Console.WriteLine($"a != null: {a != null}"); // true
                Console.WriteLine();

                // null과 null 비교
                Console.WriteLine("null과 null 비교:");
                Console.WriteLine($"null == null: {nullValue == null}"); // true
                Console.WriteLine($"null != null: {nullValue != null}"); // false
                Console.WriteLine();
            },
            Fail: error => Console.WriteLine($"생성 실패: {error.Message}")
        );
    }

    /// <summary>
    /// 정렬 시연
    /// </summary>
    public static void DemonstrateSorting()
    {
        Console.WriteLine("=== 정렬 테스트 ===");

        List<Denominator> values = [
            CreateDenominator(10),
            CreateDenominator(3),
            CreateDenominator(7),
            CreateDenominator(1),
            CreateDenominator(15)
        ];

        Console.WriteLine("정렬 전:");
        foreach (var value in values)
        {
            Console.Write($"{value} ");
        }
        Console.WriteLine();

        // 오름차순 정렬
        values.Sort();
        Console.WriteLine("오름차순 정렬 후:");
        foreach (var value in values)
        {
            Console.Write($"{value} ");
        }
        Console.WriteLine();

        // 내림차순 정렬
        values.Sort((a, b) => b.CompareTo(a));
        Console.WriteLine("내림차순 정렬 후:");
        foreach (var value in values)
        {
            Console.Write($"{value} ");
        }
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// 컬렉션에서의 비교 시연
    /// </summary>
    public static void DemonstrateCollectionComparison()
    {
        Console.WriteLine("=== 컬렉션에서의 비교 테스트 ===");

        List<Denominator> denominators = [
            CreateDenominator(5),
            CreateDenominator(2),
            CreateDenominator(8),
            CreateDenominator(1),
            CreateDenominator(3)
        ];

        Console.WriteLine("원본 리스트:");
        foreach (var d in denominators)
        {
            Console.Write($"{d} ");
        }
        Console.WriteLine();

        // 최소값 찾기
        var min = denominators.Min();
        Console.WriteLine($"최소값: {min}");

        // 최대값 찾기
        var max = denominators.Max();
        Console.WriteLine($"최대값: {max}");

        // 범위 찾기
        var range = denominators.Max()! - denominators.Min()!;
        Console.WriteLine($"범위: {range}");
        Console.WriteLine();
    }

    /// <summary>
    /// 성능 비교 시연
    /// </summary>
    public static void DemonstratePerformanceComparison()
    {
        Console.WriteLine("=== 성능 비교 테스트 ===");

        var denominators = Enumerable.Range(1, 10000)
            .Select(i => CreateDenominator(i))
            .ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 정렬 성능 테스트
        stopwatch.Restart();
        denominators.Sort();
        stopwatch.Stop();

        Console.WriteLine($"10,000개 Denominator 정렬 시간: {stopwatch.ElapsedMilliseconds}ms");

        // 검색 성능 테스트
        stopwatch.Restart();
        var target = CreateDenominator(5000);
        var found = denominators.BinarySearch(target);
        stopwatch.Stop();

        Console.WriteLine($"이진 검색 시간: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"찾은 인덱스: {found}");
        Console.WriteLine();
    }

    /// <summary>
    /// 경계값 시연
    /// </summary>
    public static void DemonstrateBoundaryValues()
    {
        Console.WriteLine("=== 경계값 테스트 ===");

        var minValue = CreateDenominator(int.MinValue);
        var maxValue = CreateDenominator(int.MaxValue);
        var negativeValue = CreateDenominator(-100);
        var positiveValue = CreateDenominator(100);

        Console.WriteLine($"최소값: {minValue}");
        Console.WriteLine($"최대값: {maxValue}");
        Console.WriteLine($"음수값: {negativeValue}");
        Console.WriteLine($"양수값: {positiveValue}");
        Console.WriteLine();

        // 음수와 양수 비교
        Console.WriteLine("음수와 양수 비교:");
        Console.WriteLine($"음수 < 양수: {negativeValue < positiveValue}"); // true
        Console.WriteLine($"음수 > 양수: {negativeValue > positiveValue}"); // false
        Console.WriteLine();

        // 최소값과 최대값 비교
        Console.WriteLine("최소값과 최대값 비교:");
        Console.WriteLine($"최소값 < 최대값: {minValue < maxValue}"); // true
        Console.WriteLine($"최소값 > 최대값: {minValue > maxValue}"); // false
        Console.WriteLine();
    }
}
