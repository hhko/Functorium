using BooleanBlindness;

namespace BooleanBlindness;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Boolean Blindness ===\n");

        // 유효한 상태들
        var emailOnly = new ContactInfo { EmailAddress = "user@example.com" };
        Console.WriteLine($"이메일만: email={emailOnly.EmailAddress}, postal={emailOnly.PostalAddress ?? "없음"}");

        var postalOnly = new ContactInfo { PostalAddress = "123 Main St, Springfield, IL 62701" };
        Console.WriteLine($"우편만: email={postalOnly.EmailAddress ?? "없음"}, postal={postalOnly.PostalAddress}");

        // 불법 상태 — 컴파일러는 이를 허용함
        var illegal = new ContactInfo();
        Console.WriteLine($"\n불법 상태: email={illegal.EmailAddress ?? "없음"}, postal={illegal.PostalAddress ?? "없음"}");
        Console.WriteLine("컴파일러는 이 불법 상태를 허용합니다!");
    }
}
