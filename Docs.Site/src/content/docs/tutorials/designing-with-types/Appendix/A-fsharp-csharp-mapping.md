---
title: "F# → C# 패턴 매핑"
---

## 참조표

| F# 패턴 | C# 14 대응 | 예시 |
|---------|-----------|------|
| `type Email = Email of string` | `sealed class Email : SimpleValueObject<string>` | Single-case DU |
| `type ContactInfo = EmailOnly of Email \| PostalOnly of Address` | `abstract record ContactInfo { sealed record EmailOnly... }` | Multi-case DU |
| `match x with` | `x switch { ... }` | 패턴 매칭 |
| `Result<'T, 'E>` | `Fin<T>` (LanguageExt) | 결과 타입 |
| `Validation<'E, 'T>` | `Validation<Error, T>` (LanguageExt) | 검증 타입 |
| `private constructor` | `private ClassName() { }` | 외부 생성 차단 |
| `[<Sealed>]` | `sealed` keyword | 상속 차단 |

## 주요 차이점

### 완전성 검사(Exhaustiveness)

F#에서는 DU의 모든 케이스를 처리하지 않으면 **컴파일 에러가** 발생합니다. C#에서는 sealed record union에 대해 `switch` 식이 **경고를** 발생시킵니다. `_ =>` 기본 케이스를 추가하여 안전성을 확보합니다.

### 불변성

F#에서는 모든 값이 기본적으로 불변입니다. C#에서는 `record`가 불변성을 제공하지만, `class`는 명시적으로 `readonly`를 사용해야 합니다.

### 타입 추론

F#의 Hindley-Milner 타입 추론은 C#보다 강력합니다. C#에서는 타입 매개변수를 명시해야 하는 경우가 더 많습니다.
