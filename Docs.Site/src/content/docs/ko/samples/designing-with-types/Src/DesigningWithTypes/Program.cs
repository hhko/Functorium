using DesigningWithTypes.AggregateRoots.Contacts;
using DesigningWithTypes.AggregateRoots.Contacts.ValueObjects;
using DesigningWithTypes.AggregateRoots.Contacts.Specifications;
using DesigningWithTypes.AggregateRoots.Contacts.Services;

var now = DateTime.UtcNow;

Console.WriteLine("=== DesigningWithTypes: DDD 고급 패턴 적용 ===\n");

// === 정상 시나리오 ===

// 시나리오 1. 이메일만 등록 후 인증
Console.WriteLine("--- 시나리오 1. 이메일만 등록 후 인증 ---");
var name = PersonalName.Create("HyungHo", "Ko", "J").ThrowIfFail();
var email = EmailAddress.Create("user@example.com").ThrowIfFail();
var contact = Contact.Create(name, email, now);
Console.WriteLine($"Contact: {contact}");
Console.WriteLine($"ID: {contact.Id}");
Console.WriteLine($"EmailValue: {contact.EmailValue}");
Console.WriteLine($"이벤트: {contact.DomainEvents[0].GetType().Name}");

contact.VerifyEmail(now).ThrowIfFail();
Console.WriteLine($"이벤트 수: {contact.DomainEvents.Count}");
Console.WriteLine($"이벤트: {contact.DomainEvents[1].GetType().Name}");

// 시나리오 2. 우편 주소만 등록
Console.WriteLine("\n--- 시나리오 2. 우편 주소만 등록 ---");
var postal = PostalAddress.Create("123 Main St", "Springfield", "IL", "62704").ThrowIfFail();
var postalContact = Contact.Create(name, postal, now);
Console.WriteLine($"ContactInfo 타입: {postalContact.ContactInfo.GetType().Name}");
Console.WriteLine($"이벤트: {postalContact.DomainEvents[0].GetType().Name}");

// 시나리오 3. 이메일과 우편 주소 모두 등록
Console.WriteLine("\n--- 시나리오 3. 이메일과 우편 주소 모두 등록 ---");
var bothEmail = EmailAddress.Create("both@example.com").ThrowIfFail();
var bothContact = Contact.Create(name, bothEmail, postal, now);
Console.WriteLine($"ContactInfo 타입: {bothContact.ContactInfo.GetType().Name}");
Console.WriteLine($"이벤트: {bothContact.DomainEvents[0].GetType().Name}");

// 시나리오 4. 이름 변경
Console.WriteLine("\n--- 시나리오 4. 이름 변경 ---");
var newName = PersonalName.Create("Gildong", "Hong").ThrowIfFail();
contact.UpdateName(newName, now).ThrowIfFail();
Console.WriteLine($"변경 후: {contact.Name}");
Console.WriteLine($"이벤트: {contact.DomainEvents[2].GetType().Name}");
var nameEvent = (Contact.NameUpdatedEvent)contact.DomainEvents[2];
Console.WriteLine($"Old: {nameEvent.OldName}, New: {nameEvent.NewName}");

// 시나리오 5. 메모 추가/제거
Console.WriteLine("\n--- 시나리오 5. 메모 추가/제거 ---");
var noteContent = NoteContent.Create("첫 번째 메모입니다").ThrowIfFail();
contact.AddNote(noteContent, now).ThrowIfFail();
Console.WriteLine($"메모 수: {contact.Notes.Count}");
Console.WriteLine($"메모 내용: {(string)contact.Notes[0].Content}");
Console.WriteLine($"이벤트: {contact.DomainEvents[3].GetType().Name}");

var noteId = contact.Notes[0].Id;
contact.RemoveNote(noteId, now);
Console.WriteLine($"메모 수: {contact.Notes.Count}");
Console.WriteLine($"이벤트: {contact.DomainEvents[4].GetType().Name}");

// 다시 제거 시도 (멱등)
contact.RemoveNote(noteId, now);
Console.WriteLine($"중복 제거 후 이벤트 수: {contact.DomainEvents.Count} (변화 없음)");

// 시나리오 6. 논리 삭제 후 복원
Console.WriteLine("\n--- 시나리오 6. 논리 삭제 ---");
contact.Delete("admin", now);
Console.WriteLine($"DeletedAt: {contact.DeletedAt}");
Console.WriteLine($"DeletedBy: {contact.DeletedBy}");

// 멱등: 다시 삭제 시 이벤트 추가 없음
var eventCountBefore = contact.DomainEvents.Count;
contact.Delete("admin", now);
Console.WriteLine($"멱등 삭제: 이벤트 수 변화 없음 = {contact.DomainEvents.Count == eventCountBefore}");

// === 거부 시나리오 ===

// 시나리오 7. 연락 수단 없이 등록 (거부)
// Contact.Create()는 EmailAddress 또는 PostalAddress를 필수로 요구하므로
// 연락 수단 없는 Contact는 타입 시스템에 의해 생성 자체가 불가능합니다.
Console.WriteLine("\n--- 시나리오 7. 연락 수단 없이 등록 (거부) ---");
Console.WriteLine("Contact.Create()는 EmailAddress 또는 PostalAddress를 필수로 요구");
Console.WriteLine("→ 타입 시스템에 의해 컴파일 타임에 방지됩니다.");

// 시나리오 8. 인증된 이메일 재인증 (거부)
Console.WriteLine("\n--- 시나리오 8. 인증된 이메일 재인증 (거부) ---");
var reVerifyResult = contact.VerifyEmail(now);
Console.WriteLine($"재인증 시도: IsFail={reVerifyResult.IsFail}");

// 시나리오 9. 삭제된 연락처 수정 (거부)
Console.WriteLine("\n--- 시나리오 9. 삭제된 연락처 수정 (거부) ---");
var updateResult = contact.UpdateName(name, now);
Console.WriteLine($"UpdateName: IsFail={updateResult.IsFail}");

var addNoteResult = contact.AddNote(noteContent, now);
Console.WriteLine($"AddNote: IsFail={addNoteResult.IsFail}");

var verifyResult = contact.VerifyEmail(now);
Console.WriteLine($"VerifyEmail: IsFail={verifyResult.IsFail}");

// 시나리오 6. 복원
Console.WriteLine("\n--- 시나리오 6. 복원 ---");
contact.Restore();
Console.WriteLine($"DeletedAt: {contact.DeletedAt}");

// 멱등: 다시 복원 시 이벤트 추가 없음
var eventCountBeforeRestore = contact.DomainEvents.Count;
contact.Restore();
Console.WriteLine($"멱등 복원: 이벤트 수 변화 없음 = {contact.DomainEvents.Count == eventCountBeforeRestore}");

// === 거부 시나리오 (계속) ===

// 시나리오 10. 중복 이메일 등록 (거부)
Console.WriteLine("\n--- 시나리오 10. 중복 이메일 등록 (거부) ---");
var repo = new DemoContactRepository([contact]);
var service = new ContactEmailCheckService(repo);

// Service가 내부에서 ContactEmailUniqueSpec 생성 → Repository.Exists 호출 → 결과 해석
var dupResult = await service.ValidateEmailUnique(email).Run().RunAsync();
Console.WriteLine($"중복 이메일 검증: IsFail={dupResult.IsFail}");

var otherEmail = EmailAddress.Create("other@example.com").ThrowIfFail();
var uniqueResult = await service.ValidateEmailUnique(otherEmail).Run().RunAsync();
Console.WriteLine($"고유 이메일 검증: IsSucc={uniqueResult.IsSucc}");

// 자기 제외: Service가 ContactEmailUniqueSpec(email, contact.Id)를 내부 생성
var selfResult = await service.ValidateEmailUnique(email, contact.Id).Run().RunAsync();
Console.WriteLine($"자기 제외 검증: IsSucc={selfResult.IsSucc}");

// === API 데모 ===

// VO 생성 (null 처리, 정규화)
Console.WriteLine("\n--- API 데모: VO 생성 ---");
var nullResult = String50.Create(null);
Console.WriteLine($"String50.Create(null): IsFail={nullResult.IsFail}");

var trimResult = String50.Create("  Hello  ").ThrowIfFail();
Console.WriteLine($"String50.Create(\"  Hello  \"): \"{(string)trimResult}\" (Trim 정규화)");

var emailNorm = EmailAddress.Create("User@Example.COM").ThrowIfFail();
Console.WriteLine($"EmailAddress.Create(\"User@Example.COM\"): \"{(string)emailNorm}\" (소문자 정규화)");

// Specification: 쿼리 가능한 도메인 규칙
Console.WriteLine("\n--- API 데모: Specification ---");
var emailSpec = new ContactEmailSpec(email);
Console.WriteLine($"ContactEmailSpec.IsSatisfiedBy(동일 이메일): {emailSpec.IsSatisfiedBy(contact)}");

var otherSpec = new ContactEmailSpec(otherEmail);
Console.WriteLine($"ContactEmailSpec.IsSatisfiedBy(다른 이메일): {otherSpec.IsSatisfiedBy(contact)}");

// ContactEmailUniqueSpec: 자기 제외 로직은 Specification이 단일 소유
var uniqueSpec = new ContactEmailUniqueSpec(email);
Console.WriteLine($"ContactEmailUniqueSpec(제외 없음): {uniqueSpec.IsSatisfiedBy(contact)}");

var uniqueSpecExclude = new ContactEmailUniqueSpec(email, contact.Id);
Console.WriteLine($"ContactEmailUniqueSpec(자기 제외): {uniqueSpecExclude.IsSatisfiedBy(contact)}");

// CreateFromValidated (이벤트 없음)
Console.WriteLine("\n--- API 데모: CreateFromValidated ---");
var note = ContactNote.Create(NoteContent.Create("복원 메모").ThrowIfFail(), now);
var restored = Contact.CreateFromValidated(
    contact.Id, name,
    new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(email)),
    [note], now,
    Option<DateTime>.None, Option<DateTime>.None, Option<string>.None);
Console.WriteLine($"복원된 Contact: {restored}");
Console.WriteLine($"메모 수: {restored.Notes.Count}");
Console.WriteLine($"이벤트 수: {restored.DomainEvents.Count} (이벤트 없음)");

// === file-scoped 스텁 Repository ===

file sealed class DemoContactRepository(IReadOnlyList<Contact> contacts) : IContactRepository
{
    public string RequestCategory => "Demo";

    public FinT<IO, bool> Exists(Specification<Contact> spec) =>
        FinT.lift(IO.pure(Fin.Succ(contacts.Any(spec.IsSatisfiedBy))));

    public FinT<IO, Contact> Create(Contact aggregate) => throw new NotImplementedException();
    public FinT<IO, Contact> GetById(ContactId id) => throw new NotImplementedException();
    public FinT<IO, Contact> Update(Contact aggregate) => throw new NotImplementedException();
    public FinT<IO, int> Delete(ContactId id) => throw new NotImplementedException();
    public FinT<IO, int> CreateRange(IReadOnlyList<Contact> aggregates) => throw new NotImplementedException();
    public FinT<IO, Seq<Contact>> GetByIds(IReadOnlyList<ContactId> ids) => throw new NotImplementedException();
    public FinT<IO, int> UpdateRange(IReadOnlyList<Contact> aggregates) => throw new NotImplementedException();
    public FinT<IO, int> DeleteRange(IReadOnlyList<ContactId> ids) => throw new NotImplementedException();
    public FinT<IO, int> Count(Specification<Contact> spec) => throw new NotImplementedException();
    public FinT<IO, int> DeleteBy(Specification<Contact> spec) => throw new NotImplementedException();
}
