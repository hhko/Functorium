using ConstrainedTypes.ValueObjects;

namespace ConstrainedTypes;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 제약된 타입 ===\n");

        var name = String50.Create("고형호");
        name.Match(
            Succ: v => Console.WriteLine($"String50: {v}"),
            Fail: error => Console.WriteLine($"실패: {error.Message}"));

        var tooLong = String50.Create(new string('x', 51));
        tooLong.Match(
            Succ: v => Console.WriteLine($"String50: {v}"),
            Fail: error => Console.WriteLine($"51자 실패: {error.Message}"));

        var age = NonNegativeInt.Create(25);
        age.Match(
            Succ: v => Console.WriteLine($"NonNegativeInt: {v}"),
            Fail: error => Console.WriteLine($"실패: {error.Message}"));

        var negative = NonNegativeInt.Create(-1);
        negative.Match(
            Succ: v => Console.WriteLine($"NonNegativeInt: {v}"),
            Fail: error => Console.WriteLine($"음수 실패: {error.Message}"));
    }
}
