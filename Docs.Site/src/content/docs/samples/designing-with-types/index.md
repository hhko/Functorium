---
title: "타입으로 도메인 설계하기"
---

## 배경

연락처 관리는 단순해 보이지만, 데이터 유효성, 연락 수단 조합, 이메일 인증 생명주기, 수명 관리 등 실제 비즈니스 규칙이 얽히면 naive한 구현으로는 잘못된 상태를 허용하게 됩니다. 이 샘플은 Eric Evans의 DDD 전술적 패턴과 Functorium의 타입 시스템을 결합하여, 비즈니스 규칙을 도메인 모델의 구조 자체에 녹여 넣는 과정을 보여줍니다.

## Naive 출발점

```csharp
public class Contact
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? MiddleInitial { get; set; }
    public string? EmailAddress { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
}
```

이 구현은 컴파일되고 실행됩니다. 하지만 다음과 같은 잘못된 상태를 허용합니다:
- 100자 이름, 숫자가 아닌 우편번호 — 유효성 검증이 없습니다
- 이메일도 주소도 없는 연락처 — 연락 수단 없는 상태가 가능합니다
- `IsEmailVerified = true`인데 `EmailAddress = null` — 모순 상태입니다
- 인증된 이메일을 `false`로 되돌림 — 단방향 전이가 보장되지 않습니다
- 이름과 이메일이 같은 `string` — 실수로 바꿔 넣어도 컴파일러가 침묵합니다

## 목표

위 문제들을 런타임 검증이 아닌 **타입 시스템**으로 원천 차단합니다:

- **잘못된 값은 생성할 수 없다** — 제약된 값 객체가 생성 시점에 검증을 완료합니다
- **잘못된 상태는 표현할 수 없다** — union type이 허용된 조합만 열거합니다
- **잘못된 전이는 실행할 수 없다** — 타입 안전 상태 머신이 규칙을 강제합니다
- **실패는 무시할 수 없다** — `Fin<T>` 반환이 호출자에게 처리를 강제합니다

DDD 전술적 패턴이 규칙 경계를 정의하고, Functorium의 함수형 타입이 이를 컴파일러 수준에서 강제합니다.

## 5단계 여정

이 샘플은 naive한 코드에서 완성된 DDD 도메인 모델까지 5단계를 거칩니다. 각 단계가 이전 단계의 산출물을 입력으로 받아 다음 의사결정을 도출합니다.

| 단계 | 핵심 질문 | 입력 | 산출물 | 문서 |
|------|----------|------|--------|------|
| 1. 요구사항 | 무엇을 해야 하는가? | 도메인 전문가 | 비즈니스 규칙 + 시나리오 | [비즈니스 요구사항](./domain/00-business-requirements/) |
| 2. 설계 의사결정 | 어떤 불변식을 어떻게 보장하는가? | 비즈니스 규칙 | 불변식 유형별 타입 전략 | [타입 설계 의사결정](./domain/01-type-design-decisions/) |
| 3. 코드 설계 | 어떤 C#/Functorium 패턴인가? | 타입 전략 | 구현 패턴 매핑 | [코드 설계](./domain/02-code-design/) |
| 4. 구현 | 코드로 어떻게 실현하는가? | 패턴 매핑 | 도메인 모델 소스 | [구현 결과](./domain/03-implementation-results/) |
| 5. 검증 | 규칙이 보장되는가? | 비즈니스 규칙 + 코드 | 단위 테스트 (114개) | `Tests/DesigningWithTypes.Tests.Unit/` |

## 적용된 DDD 빌딩 블록

| DDD 개념 | Functorium 타입 | 적용 |
|----------|----------------|------|
| Value Object | `SimpleValueObject<T>`, `ValueObject` | String50, EmailAddress, StateCode, ZipCode, PersonalName, PostalAddress, NoteContent |
| Discriminated Union | `UnionValueObject` + `[UnionType]` (Match/Switch 자동 생성) | ContactInfo, EmailVerificationState |
| Entity | `Entity<TId>` | ContactNote |
| Aggregate Root | `AggregateRoot<TId>` | Contact |
| Domain Event | `DomainEvent` | CreatedEvent, NameUpdatedEvent, EmailVerifiedEvent 등 7종 |
| Specification | `ExpressionSpecification<T>` | ContactEmailSpec, ContactEmailUniqueSpec |
| Domain Service | `IDomainService` | ContactEmailCheckService |
| Repository | `IRepository<T, TId>` | IContactRepository |

## 프로젝트 구조

```
samples/designing-with-types/
├── Directory.Build.props              # 빌드 설정 (net10.0, C# 14)
├── Directory.Build.targets            # 루트 상속 차단
├── designing-with-types.slnx          # 솔루션 파일
├── domain/                            # 도메인 설계 문서
│   ├── 00-business-requirements.md    # 1단계: 비즈니스 요구사항
│   ├── 01-type-design-decisions.md    # 2단계: 타입 설계 의사결정
│   ├── 02-code-design.md              # 3단계: 코드 설계
│   └── 03-implementation-results.md   # 4단계: 구현 결과
├── Src/
│   └── DesigningWithTypes/            # 4단계: 구현
│       ├── SharedModels/              # 공유 도메인 요소
│       │   └── ValueObjects/
│       │       └── String50.cs        # 최대 50자 문자열 VO (공유 원시 타입)
│       ├── AggregateRoots/
│       │   └── Contacts/              # Contact Aggregate 경계
│       │       ├── Contact.cs         # Aggregate Root
│       │       ├── ContactNote.cs     # 자식 엔티티
│       │       ├── IContactRepository.cs  # Repository 인터페이스
│       │       ├── ValueObjects/
│       │       │   ├── Simples/       # 원시 타입 래퍼
│       │       │   │   ├── EmailAddress.cs
│       │       │   │   ├── StateCode.cs
│       │       │   │   ├── ZipCode.cs
│       │       │   │   └── NoteContent.cs
│       │       │   ├── Composites/    # 여러 VO 조합
│       │       │   │   ├── PersonalName.cs
│       │       │   │   └── PostalAddress.cs
│       │       │   └── Unions/        # Discriminated Union
│       │       │       ├── ContactInfo.cs
│       │       │       └── EmailVerificationState.cs
│       │       ├── Specifications/    # 쿼리 사양
│       │       │   ├── ContactEmailSpec.cs
│       │       │   └── ContactEmailUniqueSpec.cs
│       │       └── Services/          # 도메인 서비스
│       │           └── ContactEmailCheckService.cs
│       └── Program.cs                 # 데모
└── Tests/
    └── DesigningWithTypes.Tests.Unit/ # 5단계: 검증 (114개 테스트)
        └── Domain/
            ├── SharedModels/
            │   └── ValueObjectTests.cs
            ├── Contacts/
            │   ├── ContactTests.cs
            │   ├── ContactNoteTests.cs
            │   ├── PersonalNameTests.cs
            │   ├── PostalAddressTests.cs
            │   ├── ContactInfoTests.cs
            │   ├── EmailVerificationStateTests.cs
            │   ├── NoteContentTests.cs
            │   └── ContactSpecificationTests.cs
            └── Services/
                └── ContactEmailCheckServiceTests.cs
```

## 실행 방법

```bash
# 빌드
dotnet build Docs.Site/src/content/docs/samples/designing-with-types/designing-with-types.slnx

# 테스트
dotnet test --solution Docs.Site/src/content/docs/samples/designing-with-types/designing-with-types.slnx

# 데모 실행
dotnet run --project Docs.Site/src/content/docs/samples/designing-with-types/Src/DesigningWithTypes/DesigningWithTypes.csproj
```
