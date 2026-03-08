using StateTransitions;

namespace StateTransitions;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 상태 전이 ===\n");

        // Unverified → Verified (유효한 전이)
        EmailVerificationState state = new EmailVerificationState.Unverified("user@example.com");
        var result = EmailVerificationState.Verify(state, DateTime.Now);
        result.Match(
            Succ: s => Console.WriteLine($"전이 성공: {s}"),
            Fail: e => Console.WriteLine($"전이 실패: {e.Message}"));

        // Verified → Verified (무효 전이)
        var verified = new EmailVerificationState.Verified("user@example.com", DateTime.Now);
        var invalid = EmailVerificationState.Verify(verified, DateTime.Now);
        invalid.Match(
            Succ: s => Console.WriteLine($"전이 성공: {s}"),
            Fail: e => Console.WriteLine($"전이 실패: {e.Message}"));
    }
}
