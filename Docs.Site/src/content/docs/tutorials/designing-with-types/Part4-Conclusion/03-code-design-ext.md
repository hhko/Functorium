---
title: "코드 설계 (확장)"
---

## 타입 설계에서 DDD Aggregate 구현으로

[코드 설계](./02-code-design/)가 타입 설계 의사결정을 C# 구현 패턴으로 매핑했다면, 이 문서는 DDD Aggregate 패턴으로의 **확장**을 다룹니다. 04-DDD-Contact의 기본 DDD 패턴 위에 실무에서 필요한 수명 관리, 자식 엔티티, Specification, Domain Service 패턴을 추가합니다.

| 설계 의사결정 | C# 구현 패턴 | 적용 |
|---|---|---|
| Nullable 입력 + 정규화 | `string?` + `NotNull` → `ThenNormalize` | String50, EmailAddress, NoteContent |
| VO 계층 일관성 | `sealed class : ValueObject` + `GetEqualityComponents` | PersonalName, PostalAddress |
| Aggregate 식별 + 수명 관리 | `AggregateRoot<TId>` + `IAuditable` | Contact |
| Aggregate 내 자식 엔티티 | `Entity<TId>` + private 컬렉션 관리 | ContactNote |
| 논리 삭제 + 복원 | `ISoftDeletableWithUser` + 멱등 Delete/Restore | Contact |
| 실패 가능 vs 멱등 행위 | `Fin<Unit>` vs `Contact` 반환 | Aggregate 메서드 |
| Aggregate 가드 + 상태 전이 위임 | Aggregate가 가드 후 `EmailVerificationState.Verify`에 위임 | Contact.VerifyEmail |
| 시간 주입 | 모든 행위 메서드가 `DateTime`을 매개변수로 수신 | Create, UpdateName, Delete 등 |
| 쿼리 가능한 도메인 상태 | 투영 속성 (projection property) | EmailValue |
| 도메인 쿼리 사양 | `ExpressionSpecification<T>` | ContactEmailSpec, ContactEmailUniqueSpec |
| 교차 Aggregate 검증 | `IDomainService` + 인메모리 검증 | ContactEmailCheckService |
| 영속성 추상화 | `IRepository<T, TId>` + 커스텀 메서드 | IContactRepository |
| 투영 속성 동기화 | `ContactInfo` setter에서 `EmailValue` 자동 갱신 | Contact.ContactInfo |
| ORM 복원 | `CreateFromValidated` (검증/이벤트 없음) | Contact, PersonalName, PostalAddress |

## 향상된 VO 검증

04에서는 `string` 입력을 받고 `NotEmpty`부터 시작합니다. 05에서는 `string?`을 받고 `NotNull` → `ThenNormalize` 체인으로 null 안전성과 정규화를 동시에 해결합니다.

```csharp
// 04 패턴: string 입력, NotEmpty부터 시작
public static Validation<Error, string> Validate(string value) =>
    ValidationRules<String50>.NotEmpty(value)
        .ThenMaxLength(50);

// 05 패턴: string? 입력, NotNull + 정규화
public static Validation<Error, string> Validate(string? value) =>
    ValidationRules<String50>
        .NotNull(value)
        .ThenNotEmpty()
        .ThenMaxLength(MaxLength)
        .ThenNormalize(v => v.Trim());
```

`EmailAddress`는 추가로 소문자 정규화를 적용합니다: `.ThenNormalize(v => v.Trim().ToLowerInvariant())`. 이메일 비교 시 대소문자 불일치 문제를 입력 시점에 제거합니다.

## 복합 VO → ValueObject 상속

04에서 `PersonalName`과 `PostalAddress`는 `sealed record`로 구현됩니다. 간결하지만 `String50`, `EmailAddress` 등 단일 VO가 모두 `SimpleValueObject<string>`을 상속하는데, 복합 VO만 plain record로 남으면 VO 계층에 참여하지 않습니다.

05에서는 `ValueObject` 추상 클래스를 상속하여 계층 일관성을 확보합니다.

```csharp
public sealed class PersonalName : ValueObject
{
    public String50 FirstName { get; }
    public String50 LastName { get; }
    public string? MiddleInitial { get; }

    private PersonalName(String50 firstName, String50 lastName, string? middleInitial)
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleInitial = middleInitial;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
        if (MiddleInitial is not null)
            yield return MiddleInitial;
    }

    public static Fin<PersonalName> Create(
        string? firstName, string? lastName, string? middleInitial = null)
    {
        return from first in String50.Create(firstName)
               from last in String50.Create(lastName)
               select new PersonalName(first, last, middleInitial);
    }

    public static PersonalName CreateFromValidated(
        String50 firstName, String50 lastName, string? middleInitial = null) =>
        new(firstName, lastName, middleInitial);
}
```

| 항목 | `sealed record` (04) | `sealed class : ValueObject` (05) |
|---|---|---|
| 동등성 | 컴파일러 자동 생성 | `GetEqualityComponents()` 명시 구현 |
| 불변성 | `required init` | private 생성자 + `{ get; }` |
| VO 계층 | 참여하지 않음 | `ValueObject` 계층 참여 |
| ORM 호환 | 프록시 미지원 | 프록시 타입 자동 처리 |
| 해시코드 | 컴파일러 생성 | 캐시된 해시코드 |

`ContactInfo`와 `EmailVerificationState`는 Discriminated Union(`abstract record` + sealed 케이스)이므로 변경하지 않습니다. `ValueObject` 클래스 상속 시 record의 패턴 매칭과 구조적 동등성을 잃기 때문입니다.

## Aggregate Root + Entity 수명

`Contact`는 `AggregateRoot<ContactId>`를 상속하고 `IAuditable`을 구현합니다.

```csharp
public sealed class Contact : AggregateRoot<ContactId>, IAuditable, ISoftDeletableWithUser
{
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }
    // ...
}
```

도메인 이벤트는 Aggregate 상태 변경마다 발행됩니다: `CreatedEvent`, `NameUpdatedEvent`, `EmailVerifiedEvent`, `NoteAddedEvent`, `NoteRemovedEvent`, `DeletedEvent`, `RestoredEvent`.

모든 행위 메서드는 `DateTime`을 매개변수로 받아 도메인 내부에서 `DateTime.UtcNow`를 직접 호출하지 않습니다. 이를 통해 테스트에서 시간을 결정적으로 제어할 수 있고, 같은 트랜잭션 내 일관된 타임스탬프를 보장합니다.

**이중 팩토리 패턴으로** 도메인 생성과 ORM 복원을 분리합니다.

| 팩토리 | 용도 | 검증 | 이벤트 |
|---|---|---|---|
| `Create(name, email, createdAt)` | 도메인 생성 | 이미 검증된 VO 수신 | `CreatedEvent` 발행 |
| `CreateFromValidated(id, name, ...)` | ORM 복원 | 없음 (DB 데이터 신뢰) | 없음 |

## 자식 엔티티 + 컬렉션 관리

`ContactNote`는 `Entity<ContactNoteId>`를 상속하는 자식 엔티티입니다. 독립적 ID를 가지지만 Aggregate 경계를 벗어나지 않습니다.

```csharp
/// 생성 후 변경 불가(immutable)한 엔티티로, 식별자 기반 삭제(RemoveNote)를
/// 지원하기 위해 Entity로 모델링합니다.
[GenerateEntityId]
public sealed class ContactNote : Entity<ContactNoteId>
{
    public NoteContent Content { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ContactNote(ContactNoteId id, NoteContent content, DateTime createdAt) : base(id)
    {
        Content = content;
        CreatedAt = createdAt;
    }

    public static ContactNote Create(NoteContent content, DateTime createdAt) =>
        new(ContactNoteId.New(), content, createdAt);
}
```

Aggregate Root가 private 컬렉션으로 자식 엔티티를 관리하고, 외부에는 `IReadOnlyList`만 노출합니다.

```csharp
private readonly List<ContactNote> _notes = [];
public IReadOnlyList<ContactNote> Notes => _notes.AsReadOnly();
```

- **AddNote**: 삭제 가드 → 생성 → `UpdatedAt` 갱신 → `NoteAddedEvent` 발행
- **RemoveNote**: 삭제 가드 → 멱등(없으면 `unit` 반환) → `UpdatedAt` 갱신 → `NoteRemovedEvent` 발행

## Soft Delete

`ISoftDeletableWithUser` 인터페이스로 논리 삭제와 삭제자 추적을 구현합니다.

```csharp
public Option<DateTime> DeletedAt { get; private set; }
public Option<string> DeletedBy { get; private set; }

public Contact Delete(string deletedBy, DateTime now)
{
    if (DeletedAt.IsSome) return this;  // 멱등

    DeletedAt = now;
    DeletedBy = deletedBy;
    AddDomainEvent(new DeletedEvent(Id, deletedBy));
    return this;
}

public Contact Restore()
{
    if (DeletedAt.IsNone) return this;  // 멱등

    DeletedAt = Option<DateTime>.None;
    DeletedBy = Option<string>.None;
    AddDomainEvent(new RestoredEvent(Id));
    return this;
}
```

삭제된 Contact에 `UpdateName`, `AddNote`, `RemoveNote`, `VerifyEmail`을 시도하면 `AlreadyDeleted` 오류를 반환합니다.

## Aggregate 메서드 반환 타입 설계

| 메서드 | 반환 타입 | 이유 |
|---|---|---|
| `UpdateName`, `VerifyEmail`, `AddNote`, `RemoveNote` | `Fin<Unit>` | 삭제 상태에서 `AlreadyDeleted` 오류 반환 가능 |
| `Delete`, `Restore` | `Contact` | 항상 멱등, fluent chaining 지원 |

**설계 원칙**: 실패 가능한 행위는 `Fin<Unit>`으로 실패를 명시하고, 항상 멱등인 행위는 `Contact`를 반환하여 체이닝을 지원합니다.

## Aggregate 가드 + 상태 전이 위임

02-code-design에서 `EmailVerificationState.Verify`는 독립적인 전이 함수였습니다. 05에서는 `EmailVerificationState`가 자신의 전이 규칙(`Unverified → Verified`)을 소유하고, `Contact.VerifyEmail`은 Aggregate 수준 가드(삭제 상태, 이메일 존재 여부) 후 전이를 위임합니다.

```csharp
// EmailVerificationState — 자신의 전이 규칙을 소유
public Fin<Verified> Verify(DateTime verifiedAt) => this switch
{
    Unverified u => new Verified(u.Email, verifiedAt),
    Verified => Fin.Fail<Verified>(
        DomainError.For<EmailVerificationState>(
            new DomainErrorType.InvalidTransition(
                FromState: "Verified", ToState: "Verified"),
            ToString()!,
            "이미 인증된 이메일입니다")),
    _ => throw new InvalidOperationException()
};

// Contact.VerifyEmail — Aggregate 가드 후 전이 위임
public Fin<Unit> VerifyEmail(DateTime verifiedAt)
{
    if (DeletedAt.IsSome)
        return DomainError.For<Contact>(new AlreadyDeleted(), ...);

    var emailState = ContactInfo switch
    {
        ContactInfo.EmailOnly eo => (EmailVerificationState?)eo.EmailState,
        ContactInfo.EmailAndPostal ep => ep.EmailState,
        _ => null
    };

    if (emailState is null)
        return DomainError.For<Contact>(new NoEmailToVerify(), ...);

    // 상태 전이를 EmailVerificationState에 위임
    return emailState.Verify(verifiedAt).Map(verified =>
    {
        ContactInfo = ContactInfo switch
        {
            ContactInfo.EmailOnly => new ContactInfo.EmailOnly(verified),
            ContactInfo.EmailAndPostal ep => new ContactInfo.EmailAndPostal(verified, ep.Address),
            _ => throw new InvalidOperationException()
        };
        UpdatedAt = verifiedAt;
        AddDomainEvent(new EmailVerifiedEvent(Id, verified.Email, verifiedAt));
        return unit;
    });
}
```

이 구조로 상태 객체는 자신의 전이 규칙을 캡슐화하고, Aggregate는 불변식 가드와 이벤트 발행을 담당합니다.

## 투영 속성 + 동기화

`ContactInfo` union 내부의 이메일을 Specification의 Expression Tree에서 직접 쿼리할 수 없습니다. `string? EmailValue` 투영 속성으로 flat하게 노출하여 이 문제를 해결합니다.

`ContactInfo` 속성의 setter에서 `EmailValue`를 자동 동기화하여, `ContactInfo`를 변경하는 모든 지점에서 투영 속성이 일관되게 유지됩니다.

```csharp
// ContactInfo 설정 시 EmailValue 자동 동기화
private ContactInfo _contactInfo = null!;
public ContactInfo ContactInfo
{
    get => _contactInfo;
    private set
    {
        _contactInfo = value;
        EmailValue = ExtractEmail(value);
    }
}

public string? EmailValue { get; private set; }
```

`PostalOnly`인 경우 `null`을 반환합니다. 이 속성은 Specification이 `contact.EmailValue == emailStr` 형태로 Expression Tree에서 사용할 수 있게 합니다.

## Specification

`ExpressionSpecification<Contact>` 기반 쿼리 사양으로, `Expression<Func<T, bool>>`을 반환하여 ORM에서 SQL로 변환할 수 있습니다.

```csharp
// 이메일로 Contact 검색
public sealed class ContactEmailSpec : ExpressionSpecification<Contact>
{
    public EmailAddress Email { get; }
    public ContactEmailSpec(EmailAddress email) => Email = email;

    public override Expression<Func<Contact, bool>> ToExpression()
    {
        string emailStr = Email;
        return contact => contact.EmailValue == emailStr;
    }
}
```

`ContactEmailUniqueSpec`은 `Option<ContactId> ExcludeId`를 받아 자기 자신을 제외한 이메일 고유성 검사를 지원합니다. 새 Contact 생성 시에는 `ExcludeId` 없이, 업데이트 시에는 자기 ID를 제외하고 검사합니다.

## Domain Service

`ContactEmailCheckService`는 상태 없는 이메일 고유성 검증 서비스입니다. Application Layer가 Repository로 기존 Contact 정보를 조회한 뒤, 필요한 최소 데이터만 Domain Service에 전달합니다. Aggregate 전체를 수신하지 않으므로 불필요한 메모리 로딩을 방지합니다.

```csharp
public sealed class ContactEmailCheckService : IDomainService
{
    public sealed record EmailAlreadyInUse : DomainErrorType.Custom;

    public Fin<Unit> ValidateEmailUnique(
        EmailAddress email,
        Seq<(ContactId Id, string? EmailValue)> existingContacts,
        Option<ContactId> excludeId = default)
    {
        var isDuplicate = existingContacts
            .Filter(c => excludeId.Match(id => c.Id != id, () => true))
            .Any(c => c.EmailValue == (string)email);

        if (isDuplicate)
            return DomainError.For<ContactEmailCheckService>(
                new EmailAlreadyInUse(), (string)email,
                "이미 다른 연락처에서 사용 중인 이메일입니다");

        return unit;
    }
}
```

도메인 로직(고유성 판별)은 Domain Service에, 데이터 조회는 Application Layer에 분리합니다.

## Repository Interface

```csharp
public interface IContactRepository : IRepository<Contact, ContactId>
{
    FinT<IO, bool> Exists(Specification<Contact> spec);
}
```

`IRepository<T, TId>` 기본 CRUD에 `Exists` 메서드를 추가하여 Specification 기반 존재 여부 확인을 지원합니다.

## 04 → 05 확장 추적표

| 04-DDD-Contact 패턴 | 05-DDD-Contact-Ext 확장 | 변경 이유 |
|---|---|---|
| `string` 입력 | `string?` + NotNull + ThenNormalize | null 안전성, 정규화 |
| `sealed record` 복합 VO | `sealed class : ValueObject` | VO 계층 일관성, ORM 호환 |
| `DateTime.UtcNow` 직접 호출 | `DateTime` 매개변수 주입 | 테스트 결정성, 트랜잭션 일관성 |
| 독립 전이 함수 | Aggregate 가드 + 상태 전이 위임 | 각 객체가 자기 규칙 소유 |
| 없음 | 자식 엔티티 + 컬렉션 관리 | Aggregate 경계 내 엔티티 |
| 없음 | `IAuditable` + `ISoftDeletableWithUser` | 수명 관리 |
| 없음 | 투영 속성 (`EmailValue`) | Specification 지원 |
| 없음 | `ExpressionSpecification` | 쿼리 사양 |
| 없음 | `IDomainService` | 교차 Aggregate 검증 |
| 없음 | `IRepository` + `Exists` | 영속성 추상화 |
