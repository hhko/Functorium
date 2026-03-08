using WrappedPrimitives.ValueObjects;

namespace WrappedPrimitives;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 래핑된 원시 타입 ===\n");

        var emailResult = EmailAddress.Create("user@example.com");
        emailResult.Match(
            Succ: email => Console.WriteLine($"이메일: {email}"),
            Fail: error => Console.WriteLine($"실패: {error.Message}"));

        var zipResult = ZipCode.Create("12345");
        zipResult.Match(
            Succ: zip => Console.WriteLine($"우편번호: {zip}"),
            Fail: error => Console.WriteLine($"실패: {error.Message}"));

        Console.WriteLine("\n이제 EmailAddress와 ZipCode를 바꿔 넣으면 컴파일 에러가 발생합니다.");
    }
}
