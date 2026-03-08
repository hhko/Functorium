namespace StateAsUnion;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Union 상태 ===\n");

        EmailVerificationState unverified = new EmailVerificationState.Unverified("user@example.com");
        EmailVerificationState verified = new EmailVerificationState.Verified("user@example.com", DateTime.Now);

        PrintState(unverified);
        PrintState(verified);

        Console.WriteLine("\n'미인증인데 인증일이 있는' 상태는 타입으로 표현할 수 없습니다!");
    }

    static void PrintState(EmailVerificationState state)
    {
        var description = state switch
        {
            EmailVerificationState.Unverified u => $"미인증: {u.Email}",
            EmailVerificationState.Verified v => $"인증: {v.Email}, 인증일={v.VerifiedAt:yyyy-MM-dd}",
            _ => throw new InvalidOperationException()
        };
        Console.WriteLine(description);
    }
}
