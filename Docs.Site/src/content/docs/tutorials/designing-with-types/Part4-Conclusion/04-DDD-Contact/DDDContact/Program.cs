using DDDContact;

Console.WriteLine("=== 04-DDD-Contact: DDD 패턴 적용 ===\n");

// 1. Value Object 생성 (검증된 VO)
var name = PersonalName.Create("HyungHo", "Ko", "J").ThrowIfFail();
var email = EmailAddress.Create("user@example.com").ThrowIfFail();

// 2. Aggregate 생성 (이메일 전용) → CreatedEvent 발행
Console.WriteLine("--- Contact 생성 (이메일 전용) ---");
var contact = Contact.Create(name, email);
Console.WriteLine($"Contact: {contact}");
Console.WriteLine($"ID: {contact.Id}");
Console.WriteLine($"이벤트 수: {contact.DomainEvents.Count}");
Console.WriteLine($"이벤트: {contact.DomainEvents[0].GetType().Name}");

// 3. 행위 메서드: 이메일 인증 → EmailVerifiedEvent 발행
Console.WriteLine("\n--- 이메일 인증 ---");
var verifyResult = contact.VerifyEmail(DateTime.UtcNow);
verifyResult.Match(
    Succ: _ =>
    {
        Console.WriteLine("인증 성공!");
        Console.WriteLine($"이벤트 수: {contact.DomainEvents.Count}");
        Console.WriteLine($"이벤트: {contact.DomainEvents[1].GetType().Name}");
    },
    Fail: e => Console.WriteLine($"인증 실패: {e.Message}"));

// 4. 이미 인증된 상태에서 재인증 시도 → AlreadyVerified 에러
Console.WriteLine("\n--- 재인증 시도 ---");
var reVerify = contact.VerifyEmail(DateTime.UtcNow);
reVerify.Match(
    Succ: _ => Console.WriteLine("재인증 성공"),
    Fail: e => Console.WriteLine($"재인증 실패: {e.Message}"));

// 5. 우편 전용 Contact 생성
Console.WriteLine("\n--- Contact 생성 (우편 전용) ---");
var postal = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();
var postalContact = Contact.Create(
    PersonalName.Create("Jane", "Doe").ThrowIfFail(),
    postal);
Console.WriteLine($"Contact: {postalContact}");

// 6. 이메일+우편 Contact 생성
Console.WriteLine("\n--- Contact 생성 (이메일 + 우편) ---");
var bothContact = Contact.Create(
    PersonalName.Create("Bob", "Smith").ThrowIfFail(),
    EmailAddress.Create("bob@example.com").ThrowIfFail(),
    postal);
Console.WriteLine($"Contact: {bothContact}");

// 7. CreateFromValidated (ORM 복원 시뮬레이션 — 이벤트 없음)
Console.WriteLine("\n--- CreateFromValidated (이벤트 없음) ---");
var restored = Contact.CreateFromValidated(
    contact.Id,
    name,
    new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(email)),
    DateTime.UtcNow,
    Option<DateTime>.None);
Console.WriteLine($"복원된 Contact: {restored}");
Console.WriteLine($"이벤트 수: {restored.DomainEvents.Count}");

// 8. 우편 전용 Contact에 이메일 인증 시도 → NoEmailToVerify 에러
Console.WriteLine("\n--- 우편 전용 Contact에 이메일 인증 시도 ---");
var noEmailResult = postalContact.VerifyEmail(DateTime.UtcNow);
noEmailResult.Match(
    Succ: _ => Console.WriteLine("인증 성공"),
    Fail: e => Console.WriteLine($"인증 실패: {e.Message}"));
