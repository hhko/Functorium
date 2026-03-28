using LanguageExt;
using LanguageExt.Common;
using ValueObjectComposite.ValueObjects;

namespace ValueObjectComposite;

/// <summary>
/// 5. 비교 불가능한 복합 값 객체 데모 - ValueObject
/// 
/// 이 데모는 ValueObject의 특징을 보여줍니다:
/// - 복잡한 검증 로직을 가진 값 객체
/// - 동등성 비교만 제공
/// - 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 5. 비교 불가능한 복합 값 객체 - ValueObject ===");
        Console.WriteLine("부모 클래스: ValueObject");
        Console.WriteLine("예시: Email (이메일 주소) - EmailLocalPart + EmailDomain 조합");
        Console.WriteLine();

        try
        {
            DemonstrateValueObjectComposite();
            Console.WriteLine("✅ 데모가 성공적으로 완료되었습니다!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 데모 실행 중 오류가 발생했습니다: {ex.Message}");
            Console.WriteLine($"상세 정보: {ex}");
        }
    }

    /// <summary>
    /// ValueObject 복합 값 객체 데모
    /// </summary>
    private static void DemonstrateValueObjectComposite()
    {
        Console.WriteLine("📋 특징:");
        Console.WriteLine("   ✅ 복잡한 검증 로직을 가진 값 객체");
        Console.WriteLine("   ✅ 동등성 비교만 제공");
        Console.WriteLine("   ✅ 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현");
        Console.WriteLine("   ✅ EmailLocalPart + EmailDomain = Email");
        Console.WriteLine();

        // 성공 케이스
        Console.WriteLine("🔍 성공 케이스:");
        var email1 = Email.Create("user@example.com");
        var email2 = Email.Create("user@example.com");
        var email3 = Email.Create("admin@test.org");

        if (email1.IsSucc && email2.IsSucc && email3.IsSucc)
        {
            var e1 = email1.Match(Succ: x => x, Fail: _ => default!);
            var e2 = email2.Match(Succ: x => x, Fail: _ => default!);
            var e3 = email3.Match(Succ: x => x, Fail: _ => default!);

            Console.WriteLine($"   ✅ Email: {e1}");
            Console.WriteLine($"     - LocalPart: {e1.LocalPart}");
            Console.WriteLine($"     - Domain: {e1.Domain}");
            Console.WriteLine();
            Console.WriteLine($"   ✅ Email: {e2}");
            Console.WriteLine($"     - LocalPart: {e2.LocalPart}");
            Console.WriteLine($"     - Domain: {e2.Domain}");
            Console.WriteLine();
            Console.WriteLine($"   ✅ Email: {e3}");
            Console.WriteLine($"     - LocalPart: {e3.LocalPart}");
            Console.WriteLine($"     - Domain: {e3.Domain}");
            Console.WriteLine();

            // 동등성 비교 데모
            Console.WriteLine("📊 동등성 비교:");
            Console.WriteLine($"   {e1} == {e2} = {e1 == e2}");
            Console.WriteLine($"   {e1} == {e3} = {e1 == e3}");
            Console.WriteLine();

            // 해시코드 데모
            Console.WriteLine("🔢 해시코드:");
            Console.WriteLine($"   {e1}.GetHashCode() = {e1.GetHashCode()}");
            Console.WriteLine($"   {e2}.GetHashCode() = {e2.GetHashCode()}");
            Console.WriteLine($"   동일한 값의 해시코드가 같은가? {e1.GetHashCode() == e2.GetHashCode()}");
            Console.WriteLine();

            // 구성 요소 분석
            Console.WriteLine("🔍 구성 요소 분석:");
            Console.WriteLine($"   EmailLocalPart: {e1.LocalPart} (SimpleValueObject<string>)");
            Console.WriteLine($"   EmailDomain: {e1.Domain} (SimpleValueObject<string>)");
            Console.WriteLine($"   Email: {e1} (ValueObject - 복합 값 객체)");
        }

        Console.WriteLine();

        // 실패 케이스
        Console.WriteLine("❌ 실패 케이스:");
        var invalidEmail1 = Email.Create("invalid-email");
        var invalidEmail2 = Email.Create("@example.com");
        var invalidEmail3 = Email.Create("user@");

        if (invalidEmail1.IsFail)
        {
            var error = invalidEmail1.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   Email(\"invalid-email\"): {error.Message}");
        }

        if (invalidEmail2.IsFail)
        {
            var error = invalidEmail2.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   Email(\"@example.com\"): {error.Message}");
        }

        if (invalidEmail3.IsFail)
        {
            var error = invalidEmail3.Match(Succ: _ => default!, Fail: x => x);
            Console.WriteLine($"   Email(\"user@\"): {error.Message}");
        }

        Console.WriteLine();

        // 복합 값 객체의 특징 설명
        Console.WriteLine("💡 복합 값 객체의 특징:");
        Console.WriteLine("   - EmailLocalPart와 EmailDomain은 각각 독립적인 값 객체");
        Console.WriteLine("   - Email은 이 두 값 객체를 조합하여 더 복잡한 도메인 개념 표현");
        Console.WriteLine("   - 각 구성 요소는 자체적인 검증 로직을 가짐");
        Console.WriteLine("   - 전체 Email은 구성 요소들의 조합으로 동등성 비교");
    }
}