---
title: "제약된 타입"
---

## 개요

문자열 길이 제한(`String50`), 음수 불허(`NonNegativeInt`) 같은 **비즈니스 제약을 타입 자체에 인코딩**합니다.

## 학습 목표

1. 제약을 타입에 인코딩하는 이유 이해
2. `String50`, `String100` 같은 길이 제한 타입 구현
3. `NonNegativeInt` 같은 범위 제한 타입 구현

## 핵심 개념: 제약 = 타입

데이터베이스 컬럼이 `VARCHAR(50)`이면, 코드에서도 50자 제한이 타입으로 표현되어야 합니다. `string`은 이 제약을 표현하지 못합니다.

```csharp
public sealed class String50 : SimpleValueObject<string>
{
    private String50(string value) : base(value) { }

    public static Fin<String50> Create(string value) =>
        CreateFromValidation(Validate(value), v => new String50(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<String50>.NotEmpty(value)
            .ThenMaxLength(50);
}
```

## 요약

- 비즈니스 제약은 `string`이 아닌 타입으로 표현해야 합니다
- 제약된 타입은 유효하지 않은 값이 존재할 수 없도록 보장합니다
- `Fin<T>`를 통해 생성 실패를 명시적으로 처리합니다
