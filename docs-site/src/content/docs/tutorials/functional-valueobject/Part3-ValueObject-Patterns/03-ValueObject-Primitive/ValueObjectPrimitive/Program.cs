using LanguageExt;
using LanguageExt.Common;
using ValueObjectPrimitive.ValueObjects;

namespace ValueObjectPrimitive;

/// <summary>
/// 3. 비교 불가능한 복합 primitive 값 객체 데모 - ValueObject
/// 
/// 이 데모는 ValueObject의 특징을 보여줍니다:
/// - 여러 primitive 값을 조합
/// - 동등성 비교만 제공
/// - 비교 기능은 제공되지 않음 (의도적으로)
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 3. 비교 불가능한 복합 primitive 값 객체 - ValueObject ===");
        Console.WriteLine("부모 클래스: ValueObject");
        Console.WriteLine("예시: Coordinate (2D 좌표)");
        Console.WriteLine();

        try
        {
            DemonstrateValueObjectPrimitive();
            Console.WriteLine("✅ 데모가 성공적으로 완료되었습니다!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 데모 실행 중 오류가 발생했습니다: {ex.Message}");
            Console.WriteLine($"상세 정보: {ex}");
        }
    }

    /// <summary>
    /// ValueObject primitive 복합 값 객체 데모
    /// </summary>
    private static void DemonstrateValueObjectPrimitive()
    {
        Console.WriteLine("📋 특징:");
        Console.WriteLine("   ✅ 여러 primitive 값을 조합");
        Console.WriteLine("   ✅ 동등성 비교만 제공");
        Console.WriteLine("   ✅ 비교 기능은 제공되지 않음 (의도적으로)");
        Console.WriteLine();

        // 성공 케이스
        Console.WriteLine("🔍 성공 케이스:");
        var coord1 = Coordinate.Create(100, 200);
        var coord2 = Coordinate.Create(100, 200);
        var coord3 = Coordinate.Create(300, 400);

        if (coord1.IsSucc && coord2.IsSucc && coord3.IsSucc)
        {
            var c1 = coord1.Match(Succ: x => x, Fail: _ => default!);
            var c2 = coord2.Match(Succ: x => x, Fail: _ => default!);
            var c3 = coord3.Match(Succ: x => x, Fail: _ => default!);

            Console.WriteLine($"   ✅ Coordinate: {c1} (X: {c1.X}, Y: {c1.Y})");
            Console.WriteLine($"   ✅ Coordinate: {c2} (X: {c2.X}, Y: {c2.Y})");
            Console.WriteLine($"   ✅ Coordinate: {c3} (X: {c3.X}, Y: {c3.Y})");
            Console.WriteLine();

            // 동등성 비교 데모
            Console.WriteLine("📊 동등성 비교:");
            Console.WriteLine($"   {c1} == {c2} = {c1 == c2}");
            Console.WriteLine($"   {c1} == {c3} = {c1 == c3}");
            Console.WriteLine();

            // 해시코드 데모
            Console.WriteLine("🔢 해시코드:");
            Console.WriteLine($"   {c1}.GetHashCode() = {c1.GetHashCode()}");
            Console.WriteLine($"   {c2}.GetHashCode() = {c2.GetHashCode()}");
            Console.WriteLine($"   동일한 값의 해시코드가 같은가? {c1.GetHashCode() == c2.GetHashCode()}");
            Console.WriteLine();

            // 비교 기능 없음 데모
            Console.WriteLine("📊 비교 기능:");
            Console.WriteLine($"   비교 기능은 제공되지 않음 (의도적으로)");
            Console.WriteLine($"   정렬이나 크기 비교가 필요한 경우 ComparableValueObject 사용");
        }

        Console.WriteLine();

        // 실패 케이스
        Console.WriteLine("❌ 실패 케이스:");
        var invalidCoord1 = Coordinate.Create(-1, 200);
        var invalidCoord2 = Coordinate.Create(100, 2000);

        if (invalidCoord1.IsFail)
        {
            var error = invalidCoord1.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   Coordinate(-1, 200): {error.Message}");
        }

        if (invalidCoord2.IsFail)
        {
            var error = invalidCoord2.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   Coordinate(100, 2000): {error.Message}");
        }

        Console.WriteLine();

        // primitive 조합의 특징 설명
        Console.WriteLine("💡 primitive 조합 값 객체의 특징:");
        Console.WriteLine("   - 여러 primitive 타입(int, string, decimal 등)을 조합");
        Console.WriteLine("   - 각 primitive 값에 대한 개별 검증 로직");
        Console.WriteLine("   - 동등성 비교만 제공 (비교 기능 없음)");
        Console.WriteLine("   - 복잡한 도메인 개념을 단순한 primitive 조합으로 표현");
    }
}
