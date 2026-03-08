namespace NaiveContact;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 나이브 Contact ===\n");

        // 정상 케이스
        var correct = Contact.Create("HyungHo", "Ko", "test@example.com");
        Console.WriteLine($"정상: {correct.FirstName} {correct.LastName}");

        // 버그: firstName과 lastName이 뒤바뀜 — 컴파일러는 침묵
        var swapped = Contact.Create("Ko", "HyungHo", "test@example.com");
        Console.WriteLine($"뒤바뀜: {swapped.FirstName} {swapped.LastName}");
        Console.WriteLine("\n컴파일러는 두 경우 모두 아무 경고 없이 통과합니다.");
    }
}
