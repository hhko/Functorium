using ValueEquality.ValueObjects;

namespace ValueEquality.Tests.Unit;

/// <summary>
/// 값 기반 동등성의 통합 테스트
/// 
/// 테스트 목적:
/// 1. 컬렉션에서의 동등성 동작 검증
/// 2. HashSet과 Dictionary에서의 올바른 동작 검증
/// 3. 성능 비교 테스트
/// 4. LINQ 표현식을 활용한 모나드 체이닝 테스트
/// </summary>
[Trait("Concept-07-Value-Equality", "EqualityIntegrationTests")]
public class EqualityIntegrationTests
{
    // 테스트 시나리오: HashSet에서 값 기반 동등성을 사용하여 중복 제거가 올바르게 동작해야 한다
    [Fact]
    public void HashSet_ShouldRemoveDuplicates_WhenUsingValueEquality()
    {
        // Arrange
        var values = new[] { 5, 10, 5, 15, 10 };
        var denominators = values.Select(v => Denominator.Create(v)).ToList();
        var validDenominators = denominators
            .Where(result => result.IsSucc)
            .Select(result => result.Match(Succ: x => x, Fail: _ => throw new InvalidOperationException()))
            .ToList();

        // Act
        var hashSet = new System.Collections.Generic.HashSet<Denominator>(validDenominators);

        // Assert
        hashSet.Count.ShouldBe(3); // 5, 10, 15 (중복 제거)
        hashSet.ShouldContain(d => (int)d == 5);
        hashSet.ShouldContain(d => (int)d == 10);
        hashSet.ShouldContain(d => (int)d == 15);
    }

    // 테스트 시나리오: Dictionary에서 값 기반 동등성을 사용하여 키 검색이 올바르게 동작해야 한다
    [Fact]
    public void Dictionary_ShouldFindKey_WhenUsingValueEquality()
    {
        // Arrange
        var values = new[] { 5, 10, 15 };
        var denominators = values.Select(v => Denominator.Create(v)).ToList();
        var validDenominators = denominators
            .Where(result => result.IsSucc)
            .Select(result => result.Match(Succ: x => x, Fail: _ => throw new InvalidOperationException()))
            .ToList();

        var dictionary = new System.Collections.Generic.Dictionary<Denominator, string>();
        foreach (var denominator in validDenominators)
        {
            dictionary[denominator] = $"Value_{denominator}";
        }

        // Act
        var searchResult = Denominator.Create(5);
        var found = searchResult.Match(
            Succ: key => dictionary.TryGetValue(key, out var value) ? value : null,
            Fail: _ => null
        );

        // Assert
        found.ShouldNotBeNull();
        found.ShouldBe("Value_5");
    }

    // 테스트 시나리오: LINQ 표현식을 사용하여 여러 Denominator 생성과 비교가 올바르게 동작해야 한다
    [Fact]
    public void LinqExpression_ShouldWorkCorrectly_WhenCreatingMultipleDenominators()
    {
        // Arrange
        var result = from a in Denominator.Create(5)
                     from b in Denominator.Create(5)
                     from c in Denominator.Create(10)
                     select (a, b, c);

        // Act & Assert
        result.Match(
            Succ: values =>
            {
                var (a, b, c) = values;
                (a == b).ShouldBeTrue();        // 같은 값
                (a == c).ShouldBeFalse();       // 다른 값
                a.Equals(b).ShouldBeTrue();     // 같은 값
                a.Equals(c).ShouldBeFalse();    // 다른 값
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    //// 테스트 시나리오: IEquatable<T>와 Object.Equals의 성능 차이를 확인해야 한다
    //[Fact]
    //public void Performance_ShouldBeBetter_WhenUsingIEquatable()
    //{
    //    // Arrange
    //    var result = from a in Denominator.Create(5)
    //                 from b in Denominator.Create(5)
    //                 select (a, b);

    //    result.Match(
    //        Succ: values =>
    //        {
    //            var (a, b) = values;
    //            const int iterations = 100_000;

    //            // IEquatable<T> 사용 (타입 안전)
    //            var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
    //            for (int i = 0; i < iterations; i++)
    //            {
    //                var result = a.Equals(b);
    //            }
    //            stopwatch1.Stop();

    //            // Object.Equals 사용 (박싱 발생)
    //            var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
    //            for (int i = 0; i < iterations; i++)
    //            {
    //                var result = a.Equals((object)b);
    //            }
    //            stopwatch2.Stop();

    //            // Assert
    //            stopwatch1.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(stopwatch2.ElapsedMilliseconds);
    //        },
    //        Fail: error => throw new Exception($"생성 실패: {error.Message}")
    //    );
    //}

    // 테스트 시나리오: 해시 코드가 일관되게 동작해야 한다
    [Theory]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(-5, -5)]
    public void GetHashCode_ShouldBeConsistent_WhenValuesAreEqual(int value1, int value2)
    {
        // Arrange
        var result = from a in Denominator.Create(value1)
                     from b in Denominator.Create(value2)
                     select (a, b);

        // Act & Assert
        result.Match(
            Succ: values =>
            {
                var (a, b) = values;
                a.GetHashCode().ShouldBe(b.GetHashCode());
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }
}
