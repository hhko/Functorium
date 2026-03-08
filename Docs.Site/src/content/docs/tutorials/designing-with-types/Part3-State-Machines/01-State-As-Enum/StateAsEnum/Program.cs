namespace StateAsEnum;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Enum 상태 ===\n");

        // 유효한 상태
        var unverified = new EmailState
        {
            Email = "user@example.com",
            Status = VerificationStatus.Unverified,
            VerifiedAt = null
        };
        Console.WriteLine($"미인증: {unverified.Email}, 인증일={unverified.VerifiedAt?.ToString() ?? "없음"}");

        // 불법 상태 — 미인증인데 인증일이 있음
        var illegal = new EmailState
        {
            Email = "user@example.com",
            Status = VerificationStatus.Unverified,
            VerifiedAt = DateTime.Now // 불법!
        };
        Console.WriteLine($"\n불법 상태: Status={illegal.Status}, VerifiedAt={illegal.VerifiedAt}");
        Console.WriteLine("컴파일러는 이 불법 상태를 허용합니다!");
    }
}
