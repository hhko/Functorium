using ComparableValueObjectComposite.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ComparableValueObjectComposite;

/// <summary>
/// 6. 비교 가능한 복합 값 객체 데모 - ComparableValueObject
/// 
/// 이 데모는 ComparableValueObject의 특징을 보여줍니다:
/// - 복잡한 검증 로직을 가진 값 객체
/// - 비교 기능 자동 제공
/// - 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 6. 비교 가능한 복합 값 객체 - ComparableValueObject ===");
        Console.WriteLine("부모 클래스: ComparableValueObject");
        Console.WriteLine("예시: Address (주소) - Street + City + PostalCode 조합");
        Console.WriteLine();

        try
        {
            DemonstrateComparableValueObjectComposite();
            Console.WriteLine("✅ 데모가 성공적으로 완료되었습니다!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 데모 실행 중 오류가 발생했습니다: {ex.Message}");
            Console.WriteLine($"상세 정보: {ex}");
        }
    }

    /// <summary>
    /// ComparableValueObject 복합 값 객체 데모
    /// </summary>
    private static void DemonstrateComparableValueObjectComposite()
    {
        Console.WriteLine("📋 특징:");
        Console.WriteLine("   ✅ 복잡한 검증 로직을 가진 값 객체");
        Console.WriteLine("   ✅ 비교 기능 자동 제공");
        Console.WriteLine("   ✅ 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현");
        Console.WriteLine("   ✅ Street + City + PostalCode = Address");
        Console.WriteLine();

        // 성공 케이스
        Console.WriteLine("🔍 성공 케이스:");
        var address1 = Address.Create("강남대로 123", "서울시", "12345");
        var address2 = Address.Create("테헤란로 456", "서울시", "67890");
        var address3 = Address.Create("강남대로 123", "서울시", "12345");

        if (address1.IsSucc && address2.IsSucc && address3.IsSucc)
        {
            var a1 = address1.Match(Succ: x => x, Fail: _ => default!);
            var a2 = address2.Match(Succ: x => x, Fail: _ => default!);
            var a3 = address3.Match(Succ: x => x, Fail: _ => default!);

            Console.WriteLine($"   ✅ Address: {a1}");
            Console.WriteLine($"     - Street: {a1.Street}");
            Console.WriteLine($"     - City: {a1.City}");
            Console.WriteLine($"     - PostalCode: {a1.PostalCode}");
            Console.WriteLine();
            Console.WriteLine($"   ✅ Address: {a2}");
            Console.WriteLine($"     - Street: {a2.Street}");
            Console.WriteLine($"     - City: {a2.City}");
            Console.WriteLine($"     - PostalCode: {a2.PostalCode}");
            Console.WriteLine();
            Console.WriteLine($"   ✅ Address: {a3}");
            Console.WriteLine($"     - Street: {a3.Street}");
            Console.WriteLine($"     - City: {a3.City}");
            Console.WriteLine($"     - PostalCode: {a3.PostalCode}");
            Console.WriteLine();

            // 동등성 비교 데모
            Console.WriteLine("📊 동등성 비교:");
            Console.WriteLine($"   {a1} == {a2} = {a1 == a2}");
            Console.WriteLine($"   {a1} == {a3} = {a1 == a3}");
            Console.WriteLine();

            // 비교 기능 데모
            Console.WriteLine("📊 비교 기능 (IComparable<T>):");
            Console.WriteLine($"   {a1} < {a2} = {a1 < a2}");
            Console.WriteLine($"   {a1} <= {a2} = {a1 <= a2}");
            Console.WriteLine($"   {a1} > {a2} = {a1 > a2}");
            Console.WriteLine($"   {a1} >= {a2} = {a1 >= a2}");
            Console.WriteLine();

            // 해시코드 데모
            Console.WriteLine("🔢 해시코드:");
            Console.WriteLine($"   {a1}.GetHashCode() = {a1.GetHashCode()}");
            Console.WriteLine($"   {a3}.GetHashCode() = {a3.GetHashCode()}");
            Console.WriteLine($"   동일한 값의 해시코드가 같은가? {a1.GetHashCode() == a3.GetHashCode()}");
            Console.WriteLine();

            // 구성 요소 분석
            Console.WriteLine("🔍 구성 요소 분석:");
            Console.WriteLine($"   Street: {a1.Street} (ComparableSimpleValueObject<string>)");
            Console.WriteLine($"   City: {a1.City} (ComparableSimpleValueObject<string>)");
            Console.WriteLine($"   PostalCode: {a1.PostalCode} (ComparableSimpleValueObject<string>)");
            Console.WriteLine($"   Address: {a1} (ComparableValueObject - 비교 가능한 복합 값 객체)");
        }

        Console.WriteLine();

        // 실패 케이스
        Console.WriteLine("❌ 실패 케이스:");
        var invalidAddress1 = Address.Create("", "서울시", "12345");
        var invalidAddress2 = Address.Create("강남대로 123", "서울시", "1234");
        var invalidAddress3 = Address.Create("강남대로 123", "", "12345");

        if (invalidAddress1.IsFail)
        {
            var error = invalidAddress1.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   Address(\"\", \"서울시\", \"12345\"): {error.Message}");
        }

        if (invalidAddress2.IsFail)
        {
            var error = invalidAddress2.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   Address(\"강남대로 123\", \"서울시\", \"1234\"): {error.Message}");
        }

        if (invalidAddress3.IsFail)
        {
            var error = invalidAddress3.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   Address(\"강남대로 123\", \"\", \"12345\"): {error.Message}");
        }

        Console.WriteLine();

        // 정렬 데모
        Console.WriteLine("📈 정렬 데모:");
        var addresses = new[] 
        { 
            ("테헤란로 456", "서울시", "67890"), 
            ("강남대로 123", "서울시", "12345"), 
            ("종로 789", "서울시", "34567"), 
            ("명동길 321", "서울시", "23456") 
        }
            .Select(a => Address.Create(a.Item1, a.Item2, a.Item3))
            .Where(result => result.IsSucc)
            .Select(result => result.Match(Succ: x => x, Fail: _ => default!))
            .OrderBy(a => a)
            .ToArray();

        Console.WriteLine("   정렬된 Address 목록:");
        foreach (var address in addresses)
        {
            Console.WriteLine($"     {address}");
        }

        Console.WriteLine();

        // 복합 값 객체의 특징 설명
        Console.WriteLine("💡 비교 가능한 복합 값 객체의 특징:");
        Console.WriteLine("   - Street, City, PostalCode는 각각 독립적인 비교 가능한 값 객체");
        Console.WriteLine("   - Address는 이 세 값 객체를 조합하여 더 복잡한 도메인 개념 표현");
        Console.WriteLine("   - 각 구성 요소는 자체적인 검증 로직과 비교 기능을 가짐");
        Console.WriteLine("   - 전체 Address는 구성 요소들의 조합으로 동등성 비교와 정렬 기능 제공");
    }
}