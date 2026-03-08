---
title: "Contact 도메인 소개"
---

## 나이브한 Contact

이 튜토리얼 전체에서 사용할 도메인은 **연락처(Contact)** 관리입니다. 가장 단순한 형태로 시작합니다.

```csharp
public class Contact
{
    public string FirstName { get; set; }
    public string MiddleInitial { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Address1 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
}
```

이 코드에는 아무런 컴파일 에러가 없습니다. 하지만 다음 질문에 답할 수 없습니다.

- `FirstName`과 `LastName`을 바꿔 넣으면? 컴파일러는 침묵합니다.
- `EmailAddress`가 빈 문자열이면? 런타임까지 알 수 없습니다.
- `IsEmailVerified`가 `true`인데 `EmailAddress`가 없다면? 불법 상태입니다.
- 이메일과 우편 주소 중 최소 하나는 있어야 한다면? 타입이 이를 강제하지 않습니다.

## 진화 로드맵

이 튜토리얼에서 Contact는 세 단계를 거쳐 진화합니다.

### Part 1: 시맨틱 타입

```
string FirstName  →  PersonalName { FirstName, LastName, MiddleInitial }
string Email      →  EmailAddress (검증된 값 객체)
string Zip        →  ZipCode (제약된 타입)
```

### Part 2: 불가능한 상태 표현 불가

```
string? Email + string? Address  →  ContactInfo (EmailOnly | PostalOnly | EmailAndPostal)
```

### Part 3: 상태 기계

```
bool IsEmailVerified  →  EmailVerificationState (Unverified | Verified)
```

매 장마다 이전 장의 **구체적 버그를** 해결하며, 최종적으로 "잘못된 Contact가 존재할 수 없는" 타입 구조에 도달합니다.
