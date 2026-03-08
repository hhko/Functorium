using FinalContact;

Console.WriteLine("=== Part 1~3 통합: 최종 Contact ===\n");

// 1. 이메일만 있는 Contact 생성
Console.WriteLine("--- 이메일 전용 Contact ---");
var emailOnly = Contact.Create("HyungHo", "Ko", "user@example.com", "J");
emailOnly.Match(
    Succ: contact =>
    {
        Console.WriteLine($"Contact: {contact}");

        // 2. 이메일 인증 전이 (Unverified → Verified)
        Console.WriteLine("\n--- 이메일 인증 전이 ---");
        if (contact.ContactInfo is ContactInfo.EmailOnly eo)
        {
            var verifyResult = EmailVerificationState.Verify(eo.EmailState, DateTime.Now);
            verifyResult.Match(
                Succ: s => Console.WriteLine($"인증 성공: {s}"),
                Fail: e => Console.WriteLine($"인증 실패: {e.Message}"));

            // 3. 이미 인증된 상태에서 재인증 시도
            if (verifyResult.IsSucc)
            {
                var verified = verifyResult.Match(
                    Succ: v => v,
                    Fail: _ => throw new InvalidOperationException());
                var reVerify = EmailVerificationState.Verify(verified, DateTime.Now);
                reVerify.Match(
                    Succ: s => Console.WriteLine($"재인증 성공: {s}"),
                    Fail: e => Console.WriteLine($"재인증 실패: {e.Message}"));
            }
        }
    },
    Fail: error => Console.WriteLine($"실패: {error.Message}"));

// 4. 우편 주소만 있는 Contact 생성
Console.WriteLine("\n--- 우편 전용 Contact ---");
var postalOnly = Contact.CreateWithPostal("Jane", "Doe", "456 Oak Ave", "Chicago", "IL", "60601");
postalOnly.Match(
    Succ: contact => Console.WriteLine($"Contact: {contact}"),
    Fail: error => Console.WriteLine($"실패: {error.Message}"));

// 5. 이메일 + 우편 모두 있는 Contact 생성
Console.WriteLine("\n--- 이메일 + 우편 Contact ---");
var both = Contact.CreateWithEmailAndPostal(
    "Bob", "Smith", "bob@example.com",
    "789 Pine Rd", "Springfield", "IL", "62701");
both.Match(
    Succ: contact =>
    {
        Console.WriteLine($"Contact: {contact}");

        // 6. ContactInfo 패턴 매칭
        Console.WriteLine("\n--- ContactInfo 패턴 매칭 ---");
        var description = contact.ContactInfo switch
        {
            ContactInfo.EmailOnly eo => $"이메일만: {eo.EmailState}",
            ContactInfo.PostalOnly po => $"우편만: {po.Address}",
            ContactInfo.EmailAndPostal ep => $"이메일({ep.EmailState}) + 우편({ep.Address})",
            _ => throw new InvalidOperationException()
        };
        Console.WriteLine(description);
    },
    Fail: error => Console.WriteLine($"실패: {error.Message}"));
