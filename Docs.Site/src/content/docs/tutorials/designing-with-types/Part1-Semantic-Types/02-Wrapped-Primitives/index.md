---
title: "래핑된 원시 타입"
---

## 개요

`string` 대신 `EmailAddress`, `ZipCode`, `StateCode` 타입을 만들어 타입 혼동을 컴파일 타임에 방지합니다.

## 학습 목표

1. 원시 타입을 의미 있는 타입으로 래핑하는 방법 이해
2. `SimpleValueObject<T>`를 사용한 값 객체 생성
3. 타입 안전성이 버그를 컴파일 타임에 방지하는 원리 체험

## 핵심 패턴: Single-case Discriminated Union

F#에서는 `type EmailAddress = EmailAddress of string`으로 한 줄에 래핑합니다. C#에서는 `SimpleValueObject<T>`가 동일한 역할을 합니다.

```csharp
public sealed class EmailAddress : SimpleValueObject<string>
{
    private EmailAddress(string value) : base(value) { }

    public static Fin<EmailAddress> Create(string value) =>
        CreateFromValidation(Validate(value), v => new EmailAddress(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<EmailAddress>.NotEmpty(value);
}
```

## 타입 안전성

이제 `EmailAddress`를 `ZipCode`에 대입하면 **컴파일 에러가** 발생합니다.

```csharp
EmailAddress email = ...;
ZipCode zip = email; // 컴파일 에러!
```

## 요약

- 원시 타입 래핑은 도메인 의미를 타입에 부여합니다
- 컴파일러가 타입 혼동 버그를 방지합니다
- `SimpleValueObject<T>`는 불변성과 값 동등성을 기본 제공합니다
