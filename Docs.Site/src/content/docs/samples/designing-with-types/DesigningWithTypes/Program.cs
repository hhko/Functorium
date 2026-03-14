using DesigningWithTypes.Contacts;
using DesigningWithTypes.Contacts.ValueObjects;
using DesigningWithTypes.Contacts.Specifications;
using DesigningWithTypes.Contacts.Services;

var now = DateTime.UtcNow;

Console.WriteLine("=== DesigningWithTypes: DDD 고급 패턴 적용 ===\n");

// 1. 향상된 VO 생성 (null 처리, 정규화)
Console.WriteLine("--- 향상된 VO 생성 ---");
var nullResult = String50.Create(null);
Console.WriteLine($"String50.Create(null): IsFail={nullResult.IsFail}");

var trimResult = String50.Create("  Hello  ").ThrowIfFail();
Console.WriteLine($"String50.Create(\"  Hello  \"): \"{(string)trimResult}\" (Trim 정규화)");

var emailNorm = EmailAddress.Create("User@Example.COM").ThrowIfFail();
Console.WriteLine($"EmailAddress.Create(\"User@Example.COM\"): \"{(string)emailNorm}\" (소문자 정규화)");

// 2. Aggregate 생성 → CreatedEvent
Console.WriteLine("\n--- Contact 생성 ---");
var name = PersonalName.Create("HyungHo", "Ko", "J").ThrowIfFail();
var email = EmailAddress.Create("user@example.com").ThrowIfFail();
var contact = Contact.Create(name, email, now);
Console.WriteLine($"Contact: {contact}");
Console.WriteLine($"ID: {contact.Id}");
Console.WriteLine($"EmailValue: {contact.EmailValue}");
Console.WriteLine($"이벤트: {contact.DomainEvents[0].GetType().Name}");

// 3. VerifyEmail → EmailVerifiedEvent
Console.WriteLine("\n--- 이메일 인증 ---");
contact.VerifyEmail(now).ThrowIfFail();
Console.WriteLine($"이벤트 수: {contact.DomainEvents.Count}");
Console.WriteLine($"이벤트: {contact.DomainEvents[1].GetType().Name}");

// 4. UpdateName → NameUpdatedEvent
Console.WriteLine("\n--- 이름 변경 ---");
var newName = PersonalName.Create("Gildong", "Hong").ThrowIfFail();
contact.UpdateName(newName, now).ThrowIfFail();
Console.WriteLine($"변경 후: {contact.Name}");
Console.WriteLine($"이벤트: {contact.DomainEvents[2].GetType().Name}");
var nameEvent = (Contact.NameUpdatedEvent)contact.DomainEvents[2];
Console.WriteLine($"Old: {nameEvent.OldName}, New: {nameEvent.NewName}");

// 5. AddNote (자식 엔티티)
Console.WriteLine("\n--- 메모 추가 ---");
var noteContent = NoteContent.Create("첫 번째 메모입니다").ThrowIfFail();
contact.AddNote(noteContent, now).ThrowIfFail();
Console.WriteLine($"메모 수: {contact.Notes.Count}");
Console.WriteLine($"메모 내용: {(string)contact.Notes[0].Content}");
Console.WriteLine($"이벤트: {contact.DomainEvents[3].GetType().Name}");

// 6. RemoveNote (멱등)
Console.WriteLine("\n--- 메모 제거 ---");
var noteId = contact.Notes[0].Id;
contact.RemoveNote(noteId, now);
Console.WriteLine($"메모 수: {contact.Notes.Count}");
Console.WriteLine($"이벤트: {contact.DomainEvents[4].GetType().Name}");

// 다시 제거 시도 (멱등)
contact.RemoveNote(noteId, now);
Console.WriteLine($"중복 제거 후 이벤트 수: {contact.DomainEvents.Count} (변화 없음)");

// 7. Specification 사용
Console.WriteLine("\n--- Specification ---");
var emailSpec = new ContactEmailSpec(email);
Console.WriteLine($"ContactEmailSpec.IsSatisfiedBy: {emailSpec.IsSatisfiedBy(contact)}");

var otherEmail = EmailAddress.Create("other@example.com").ThrowIfFail();
var otherSpec = new ContactEmailSpec(otherEmail);
Console.WriteLine($"다른 이메일 Spec: {otherSpec.IsSatisfiedBy(contact)}");

var uniqueSpec = new ContactEmailUniqueSpec(email);
Console.WriteLine($"ContactEmailUniqueSpec (전체): {uniqueSpec.IsSatisfiedBy(contact)}");

var uniqueSpecExclude = new ContactEmailUniqueSpec(email, contact.Id);
Console.WriteLine($"ContactEmailUniqueSpec (자기 제외): {uniqueSpecExclude.IsSatisfiedBy(contact)}");

// 8. Domain Service 사용
Console.WriteLine("\n--- Domain Service ---");
var service = new ContactEmailCheckService();
var contacts = Seq.create((contact.Id, contact.EmailValue));

var uniqueResult = service.ValidateEmailUnique(otherEmail, contacts);
Console.WriteLine($"고유 이메일 검증: IsSucc={uniqueResult.IsSucc}");

var dupResult = service.ValidateEmailUnique(email, contacts);
Console.WriteLine($"중복 이메일 검증: IsFail={dupResult.IsFail}");

var selfExcludeResult = service.ValidateEmailUnique(email, contacts, contact.Id);
Console.WriteLine($"자기 제외 검증: IsSucc={selfExcludeResult.IsSucc}");

// 9. Soft Delete → DeletedEvent (멱등)
Console.WriteLine("\n--- Soft Delete ---");
contact.Delete("admin", now);
Console.WriteLine($"DeletedAt: {contact.DeletedAt}");
Console.WriteLine($"DeletedBy: {contact.DeletedBy}");

// 멱등: 다시 삭제 시 이벤트 추가 없음
var eventCountBefore = contact.DomainEvents.Count;
contact.Delete("admin", now);
Console.WriteLine($"멱등 삭제: 이벤트 수 변화 없음 = {contact.DomainEvents.Count == eventCountBefore}");

// 10. 삭제된 Contact에 행위 시도 → AlreadyDeleted 에러
Console.WriteLine("\n--- 삭제된 Contact에 행위 시도 ---");
var updateResult = contact.UpdateName(name, now);
Console.WriteLine($"UpdateName: IsFail={updateResult.IsFail}");

var addNoteResult = contact.AddNote(noteContent, now);
Console.WriteLine($"AddNote: IsFail={addNoteResult.IsFail}");

var verifyResult = contact.VerifyEmail(now);
Console.WriteLine($"VerifyEmail: IsFail={verifyResult.IsFail}");

// 11. Restore → RestoredEvent (멱등)
Console.WriteLine("\n--- Restore ---");
contact.Restore();
Console.WriteLine($"DeletedAt: {contact.DeletedAt}");

// 멱등: 다시 복원 시 이벤트 추가 없음
var eventCountBeforeRestore = contact.DomainEvents.Count;
contact.Restore();
Console.WriteLine($"멱등 복원: 이벤트 수 변화 없음 = {contact.DomainEvents.Count == eventCountBeforeRestore}");

// 12. CreateFromValidated (이벤트 없음)
Console.WriteLine("\n--- CreateFromValidated ---");
var note = ContactNote.Create(NoteContent.Create("복원 메모").ThrowIfFail(), now);
var restored = Contact.CreateFromValidated(
    contact.Id,
    name,
    new ContactInfo.EmailOnly(new EmailVerificationState.Unverified(email)),
    [note],
    now,
    Option<DateTime>.None,
    Option<DateTime>.None,
    Option<string>.None);
Console.WriteLine($"복원된 Contact: {restored}");
Console.WriteLine($"메모 수: {restored.Notes.Count}");
Console.WriteLine($"이벤트 수: {restored.DomainEvents.Count} (이벤트 없음)");
