using CompositeTypes.ValueObjects;

namespace CompositeTypes;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 복합 타입 ===\n");

        var name = PersonalName.Create("HyungHo", "Ko", "J");
        name.Match(
            Succ: n => Console.WriteLine($"이름: {n}"),
            Fail: error => Console.WriteLine($"실패: {error.Message}"));

        var address = PostalAddress.Create("123 Main St", "Springfield", "IL", "62701");
        address.Match(
            Succ: a => Console.WriteLine($"주소: {a}"),
            Fail: error => Console.WriteLine($"실패: {error.Message}"));
    }
}
