using ValueComparability.Demonstrations;

namespace ValueComparability;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 값 객체의 비교 가능성 ===\n");

        // 기본 비교 기능 시연
        ComparabilityTests.DemonstrateBasicComparison();

        // null 비교 시연
        ComparabilityTests.DemonstrateNullComparison();

        // 정렬 시연
        ComparabilityTests.DemonstrateSorting();

        // 컬렉션에서의 비교 시연
        ComparabilityTests.DemonstrateCollectionComparison();

        // 성능 비교 시연
        ComparabilityTests.DemonstratePerformanceComparison();

        // 경계값 시연
        ComparabilityTests.DemonstrateBoundaryValues();


        Console.WriteLine("\n" + new string('=', 50) + "\n");

        Console.WriteLine("=== IEqualityComparer<T> 사용 예제 테스트 ===\n");

        // 기본 IEqualityComparer<T> 시연
        EqualityComparerTests.DemonstrateBasicEqualityComparer();

        // 컬렉션에서 IEqualityComparer<T> 사용 시연
        EqualityComparerTests.DemonstrateCollectionWithEqualityComparer();

        // 대소문자 무시 비교자 시연
        EqualityComparerTests.DemonstrateCaseInsensitiveComparer();

        // Dictionary에서 IEqualityComparer<T> 사용 시연
        EqualityComparerTests.DemonstrateDictionaryWithEqualityComparer();

        // 성능 비교 시연
        EqualityComparerTests.DemonstratePerformanceComparison();
    }
}
