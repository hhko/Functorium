using ComparableValueObjectPrimitive.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ComparableValueObjectPrimitive;

/// <summary>
/// 4. 비교 가능한 복합 primitive 값 객체 데모 - ComparableValueObject
/// 
/// 이 데모는 ComparableValueObject의 특징을 보여줍니다:
/// - 여러 primitive 값을 조합
/// - 비교 기능 자동 제공
/// - 날짜 범위의 유효성 검증
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 4. 비교 가능한 복합 primitive 값 객체 - ComparableValueObject ===");
        Console.WriteLine("부모 클래스: ComparableValueObject");
        Console.WriteLine("예시: DateRange (날짜 범위)");
        Console.WriteLine();

        try
        {
            DemonstrateComparableValueObjectPrimitive();
            Console.WriteLine("✅ 데모가 성공적으로 완료되었습니다!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 데모 실행 중 오류가 발생했습니다: {ex.Message}");
            Console.WriteLine($"상세 정보: {ex}");
        }
    }

    /// <summary>
    /// ComparableValueObject primitive 복합 값 객체 데모
    /// </summary>
    private static void DemonstrateComparableValueObjectPrimitive()
    {
        Console.WriteLine("📋 특징:");
        Console.WriteLine("   ✅ 여러 primitive 값을 조합");
        Console.WriteLine("   ✅ 비교 기능 자동 제공");
        Console.WriteLine("   ✅ 날짜 범위의 유효성 검증");
        Console.WriteLine();

        // 성공 케이스
        Console.WriteLine("🔍 성공 케이스:");
        var range1 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30));
        var range2 = DateRange.Create(new DateTime(2024, 7, 1), new DateTime(2024, 12, 31));
        var range3 = DateRange.Create(new DateTime(2024, 1, 1), new DateTime(2024, 6, 30));

        if (range1.IsSucc && range2.IsSucc && range3.IsSucc)
        {
            var r1 = range1.Match(Succ: x => x, Fail: _ => default!);
            var r2 = range2.Match(Succ: x => x, Fail: _ => default!);
            var r3 = range3.Match(Succ: x => x, Fail: _ => default!);

            Console.WriteLine($"   ✅ DateRange: {r1}");
            Console.WriteLine($"     - StartDate: {r1.StartDate:yyyy-MM-dd}");
            Console.WriteLine($"     - EndDate: {r1.EndDate:yyyy-MM-dd}");
            Console.WriteLine();
            Console.WriteLine($"   ✅ DateRange: {r2}");
            Console.WriteLine($"     - StartDate: {r2.StartDate:yyyy-MM-dd}");
            Console.WriteLine($"     - EndDate: {r2.EndDate:yyyy-MM-dd}");
            Console.WriteLine();
            Console.WriteLine($"   ✅ DateRange: {r3}");
            Console.WriteLine($"     - StartDate: {r3.StartDate:yyyy-MM-dd}");
            Console.WriteLine($"     - EndDate: {r3.EndDate:yyyy-MM-dd}");
            Console.WriteLine();

            // 동등성 비교 데모
            Console.WriteLine("📊 동등성 비교:");
            Console.WriteLine($"   {r1} == {r2} = {r1 == r2}");
            Console.WriteLine($"   {r1} == {r3} = {r1 == r3}");
            Console.WriteLine();

            // 비교 기능 데모
            Console.WriteLine("📊 비교 기능 (IComparable<T>):");
            Console.WriteLine($"   {r1} < {r2} = {r1 < r2}");
            Console.WriteLine($"   {r1} <= {r2} = {r1 <= r2}");
            Console.WriteLine($"   {r1} > {r2} = {r1 > r2}");
            Console.WriteLine($"   {r1} >= {r2} = {r1 >= r2}");
            Console.WriteLine();

            // 해시코드 데모
            Console.WriteLine("🔢 해시코드:");
            Console.WriteLine($"   {r1}.GetHashCode() = {r1.GetHashCode()}");
            Console.WriteLine($"   {r3}.GetHashCode() = {r3.GetHashCode()}");
            Console.WriteLine($"   동일한 값의 해시코드가 같은가? {r1.GetHashCode() == r3.GetHashCode()}");
        }

        Console.WriteLine();

        // 실패 케이스
        Console.WriteLine("❌ 실패 케이스:");
        var invalidRange = DateRange.Create(new DateTime(2024, 12, 31), new DateTime(2024, 1, 1));

        if (invalidRange.IsFail)
        {
            var error = invalidRange.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   DateRange(2024-12-31, 2024-01-01): {error.Message}");
        }

        Console.WriteLine();

        // 정렬 데모
        Console.WriteLine("📈 정렬 데모:");
        var ranges = new[]
        {
            (new DateTime(2024, 6, 1), new DateTime(2024, 6, 30)),
            (new DateTime(2024, 1, 1), new DateTime(2024, 3, 31)),
            (new DateTime(2024, 9, 1), new DateTime(2024, 12, 31)),
            (new DateTime(2024, 4, 1), new DateTime(2024, 5, 31))
        }
        .Select(r => DateRange.Create(r.Item1, r.Item2))
        .Where(result => result.IsSucc)
        .Select(result => result.Match(Succ: x => x, Fail: _ => default!))
        .OrderBy(r => r)
        .ToArray();

        Console.WriteLine("   정렬된 DateRange 목록:");
        foreach (var range in ranges)
        {
            Console.WriteLine($"     {range}");
        }

        Console.WriteLine();

        // primitive 조합의 특징 설명
        Console.WriteLine("💡 비교 가능한 primitive 조합 값 객체의 특징:");
        Console.WriteLine("   - 여러 primitive 타입(DateTime 등)을 조합");
        Console.WriteLine("   - 각 primitive 값에 대한 개별 검증 로직");
        Console.WriteLine("   - 동등성 비교와 비교 기능 모두 제공");
        Console.WriteLine("   - 정렬과 크기 비교가 가능한 복잡한 도메인 개념 표현");
    }
}
