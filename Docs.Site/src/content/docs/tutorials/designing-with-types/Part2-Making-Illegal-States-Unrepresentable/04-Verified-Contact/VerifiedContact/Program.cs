using VerifiedContact;

namespace VerifiedContact;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 검증된 Contact ===\n");

        var result = Contact.Create("HyungHo", "Ko", "user@example.com");
        result.Match(
            Succ: contact => Console.WriteLine($"Contact: {contact}"),
            Fail: error => Console.WriteLine($"실패: {error.Message}"));

        Console.WriteLine("\nraw string에서 type-safe Contact까지 변환 완료!");
    }
}
