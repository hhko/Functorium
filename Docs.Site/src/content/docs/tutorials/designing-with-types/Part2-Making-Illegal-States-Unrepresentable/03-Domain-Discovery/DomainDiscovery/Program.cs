using DomainDiscovery;

namespace DomainDiscovery;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 도메인 발견 ===\n");

        ContactMethod[] methods =
        [
            new ContactMethod.Email("user@example.com"),
            new ContactMethod.PostalMail("123 Main St, Springfield, IL 62701"),
            new ContactMethod.Phone("010-1234-5678"),
        ];

        foreach (var method in methods)
        {
            var description = ContactMethodHandler.Describe(method);
            Console.WriteLine(description);
        }

        Console.WriteLine("\n새 연락 방법을 추가하면 switch 식에서 컴파일 에러가 발생합니다.");
    }
}
