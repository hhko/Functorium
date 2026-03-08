using UnionTypes;

namespace UnionTypes;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Union Types ===\n");

        ContactInfo[] contacts =
        [
            new ContactInfo.EmailOnly("user@example.com"),
            new ContactInfo.PostalOnly("123 Main St, Springfield, IL"),
            new ContactInfo.EmailAndPostal("user@example.com", "123 Main St, Springfield, IL"),
        ];

        foreach (var contact in contacts)
        {
            var description = contact switch
            {
                ContactInfo.EmailOnly e => $"이메일만: {e.Email}",
                ContactInfo.PostalOnly p => $"우편만: {p.Address}",
                ContactInfo.EmailAndPostal b => $"둘 다: {b.Email}, {b.Address}",
                _ => throw new InvalidOperationException()
            };
            Console.WriteLine(description);
        }

        Console.WriteLine("\n'둘 다 없는' 상태는 타입으로 표현할 수 없습니다!");
    }
}
