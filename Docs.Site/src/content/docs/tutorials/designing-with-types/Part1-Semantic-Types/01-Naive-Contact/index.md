---
title: "나이브 Contact"
---

## 개요

모든 필드가 `string`인 Contact 클래스를 작성하고, 타입 혼동 버그가 컴파일러에게 감지되지 않음을 확인합니다.

## 학습 목표

1. 원시 타입만으로 도메인을 모델링할 때의 문제점 이해
2. 타입 혼동(type confusion) 버그가 컴파일 타임에 감지되지 않음을 체험
3. 타입 주도 설계의 필요성 인식

## 문제: 모든 것이 string

```csharp
public class Contact
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string EmailAddress { get; set; } = "";
}
```

`FirstName`과 `LastName`은 둘 다 `string`입니다. 컴파일러는 이 둘을 구분할 수 없으므로, 실수로 바꿔 넣어도 아무런 경고가 없습니다.

```csharp
// 버그: firstName과 lastName이 뒤바뀜 — 컴파일러는 침묵
var contact = new Contact
{
    FirstName = "Ko",      // 실제로는 성(lastName)
    LastName = "HyungHo",  // 실제로는 이름(firstName)
    EmailAddress = "test@example.com"
};
```

## 핵심 교훈

원시 타입은 **도메인 의미를 전달하지 못합니다.** 다음 장에서 이 문제를 해결합니다.

## 요약

- `string`은 이름, 이메일, 주소를 구분하지 못합니다
- 타입 혼동 버그는 테스트로만 잡을 수 있으며, 컴파일러가 도와주지 않습니다
- 이것이 타입 주도 설계의 출발점입니다
