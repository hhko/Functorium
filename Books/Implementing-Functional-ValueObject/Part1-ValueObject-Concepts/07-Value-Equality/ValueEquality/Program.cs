using ValueEquality.ValueObjects;

namespace ValueEquality;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 값 객체의 동등성 ===\n");

        // 1. 기본 동등성 테스트
        DemonstrateBasicEquality();

        // 2. 참조 동등성 vs 값 동등성 비교
        DemonstrateReferenceVsValueEquality();

        // 3. null과의 동등성 테스트
        DemonstrateNullEquality();

        // 4. 해시 코드 테스트
        DemonstrateHashCode();

        // 5. 컬렉션에서의 동등성 테스트
        DemonstrateCollectionEquality();

        // 6. 성능 비교 테스트
        DemonstratePerformanceComparison();
    }

    /// <summary>
    /// 기본 동등성 시연
    /// </summary>
    private static void DemonstrateBasicEquality()
    {
        Console.WriteLine("=== 기본 동등성 테스트 ===");

        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(5)
                     from c in Denominator.Create(10)
                     select (a, b, c);

        result.Match(
            Succ: values =>
            {
                var (a, b, c) = values;
                Console.WriteLine($"a = {a}, b = {b}, c = {c}");
                Console.WriteLine($"a == b: {a == b}"); // true (값이 같음)
                Console.WriteLine($"a == c: {a == c}"); // false (값이 다름)
                Console.WriteLine($"a.Equals(b): {a.Equals(b)}"); // true
                Console.WriteLine($"a.Equals(c): {a.Equals(c)}"); // false
            },
            Fail: error => Console.WriteLine($"생성 실패: {error}")
        );
    }

    /// <summary>
    /// 참조 동등성 vs 값 동등성 비교 시연
    /// </summary>
    private static void DemonstrateReferenceVsValueEquality()
    {
        Console.WriteLine("\n=== 참조 동등성(ReferenceEquals) vs 값 동등성(Equals) ===");

        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(5)
                     select (a, b);

        result.Match(
            Succ: values =>
            {
                var (a, b) = values;
                Console.WriteLine($"a = {a}, b = {b}");
                Console.WriteLine($"ReferenceEquals(a, b): {ReferenceEquals(a, b)}"); // false (다른 객체)
                Console.WriteLine($"a == b: {a == b}"); // true (값이 같음)
                Console.WriteLine($"a.Equals(b): {a.Equals(b)}"); // true (값이 같음)
            },
            Fail: error => Console.WriteLine($"생성 실패: {error}")
        );
    }

    /// <summary>
    /// null과의 동등성 시연
    /// </summary>
    private static void DemonstrateNullEquality()
    {
        Console.WriteLine("\n=== null과의 동등성 테스트 ===");

        var result = from a in Denominator.Create(5)
                     select a;

        result.Match(
            Succ: a =>
            {
                Console.WriteLine($"a = {a}");
                Console.WriteLine($"a == null: {a == null}"); // false
                Console.WriteLine($"null == a: {null == a}"); // false
                Console.WriteLine($"a.Equals(null): {a.Equals((object?)null)}"); // false
                Console.WriteLine($"null == null: {null == null}"); // true
            },
            Fail: error => Console.WriteLine($"a 생성 실패: {error}")
        );
    }

    /// <summary>
    /// 해시 코드 시연
    /// </summary>
    private static void DemonstrateHashCode()
    {
        Console.WriteLine("\n=== 해시 코드 테스트 ===");

        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(5)
                     from c in Denominator.Create(10)
                     select (a, b, c);

        result.Match(
            Succ: values =>
            {
                var (a, b, c) = values;
                Console.WriteLine($"a = {a}, b = {b}, c = {c}");
                Console.WriteLine($"a.GetHashCode(): {a.GetHashCode()}");
                Console.WriteLine($"b.GetHashCode(): {b.GetHashCode()}");
                Console.WriteLine($"c.GetHashCode(): {c.GetHashCode()}");
                Console.WriteLine($"a.GetHashCode() == b.GetHashCode(): {a.GetHashCode() == b.GetHashCode()}"); // true
                Console.WriteLine($"a.GetHashCode() == c.GetHashCode(): {a.GetHashCode() == c.GetHashCode()}"); // false
            },
            Fail: error => Console.WriteLine($"생성 실패: {error}")
        );
    }

    /// <summary>
    /// 컬렉션에서의 동등성 시연
    /// </summary>
    private static void DemonstrateCollectionEquality()
    {
        Console.WriteLine("\n=== 컬렉션에서의 동등성 테스트 ===");

        var values = new[] { 5, 10, 5, 15, 10 };

        // LINQ 표현식을 사용하여 모든 값 생성
        var allResults = values.Select(v => Denominator.Create(v)).ToList();

        // 성공한 생성 결과만 필터링
        var validValues = allResults
            .Where(result => result.IsSucc)
            .Select(result => result.Match(Succ: x => x, Fail: _ => throw new InvalidOperationException()))
            .ToList();

        Console.WriteLine($"원본 값들: [{string.Join(", ", values)}]");
        Console.WriteLine($"Denominator 값들: [{string.Join(", ", validValues)}]");

        // HashSet 테스트 - 중복 제거
        var hashSet = new System.Collections.Generic.HashSet<Denominator>(validValues);
        Console.WriteLine($"HashSet (중복 제거): [{string.Join(", ", hashSet)}]");

        // Dictionary 테스트
        var dictionary = new Dictionary<Denominator, string>();
        foreach (var value in validValues)
        {
            dictionary[value] = $"Value_{value}";
        }
        Console.WriteLine($"Dictionary 키 개수: {dictionary.Count}");

        // 동일한 값으로 키 검색 - LINQ 표현식 사용
        var searchResult = from key in Denominator.Create(5)
                           select key;

        searchResult.Match(
            Succ: key =>
            {
                if (dictionary.TryGetValue(key, out var value))
                {
                    Console.WriteLine($"키 {key}로 검색된 값: {value}");
                }
            },
            Fail: error => Console.WriteLine($"검색 키 생성 실패: {error}")
        );
    }

    /// <summary>
    /// 성능 비교 시연
    /// </summary>
    private static void DemonstratePerformanceComparison()
    {
        Console.WriteLine("\n=== 성능 비교 테스트(1,000,000개) ===");

        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(5)
                     select (a, b);

        result.Match(
            Succ: values =>
            {
                var (a, b) = values;
                const int iterations = 1_000_000;

                // IEquatable<T> 사용 (타입 안전)
                var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    var result = a.Equals(b);
                }
                stopwatch1.Stop();

                // Object.Equals 사용 (박싱 발생)
                var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    var result = a.Equals((object)b);
                }
                stopwatch2.Stop();

                Console.WriteLine($"IEquatable<T> 사용: {stopwatch1.ElapsedMilliseconds}ms");
                Console.WriteLine($"Object.Equals 사용: {stopwatch2.ElapsedMilliseconds}ms");
                Console.WriteLine($"성능 차이: {stopwatch2.ElapsedMilliseconds - stopwatch1.ElapsedMilliseconds}ms");
            },
            Fail: error => Console.WriteLine($"생성 실패: {error}")
        );
    }
}
