# 부록 C. 용어집

> **부록** | [← 이전: B. 타입 선택 가이드](B-type-selection-guide.md) | [목차](../README.md) | [다음: D. 참고 자료 →](D-references.md)

---

## A

### Apply (어플라이)
여러 검증을 병렬로 실행하고 모든 오류를 수집하는 함수형 패턴. `Validation<Error, T>` 타입과 함께 사용.

```csharp
(ValidateA(), ValidateB(), ValidateC())
    .Apply((a, b, c) => new Result(a, b, c));
```

### Always Valid (항상 유효)
값 객체가 생성된 이후에는 항상 유효한 상태를 유지하는 패턴. 팩토리 메서드에서 검증 후 생성.

---

## B

### Bind (바인드)
모나드의 핵심 연산. 성공 값을 다른 연산에 전달하고 실패 시 단락(short-circuit). `SelectMany`와 동일.

```csharp
result.Bind(value => NextOperation(value));
```

---

## C

### ComparableValueObject
`IComparable<T>` 인터페이스를 구현한 값 객체. 정렬과 비교 연산 지원.

### CQRS (Command Query Responsibility Segregation)
명령(쓰기)과 조회(읽기)의 책임을 분리하는 아키텍처 패턴.

---

## D

### Domain Error (도메인 오류)
비즈니스 규칙 위반을 나타내는 오류. 예외가 아닌 `Error` 타입으로 표현.

```csharp
internal static class DomainErrors
{
    public static readonly Error InvalidEmail =
        Error.New("Email.Invalid", "유효하지 않은 이메일 형식입니다.");
}
```

### DDD (Domain-Driven Design)
도메인 모델을 중심으로 소프트웨어를 설계하는 방법론.

---

## E

### Entity (엔티티)
고유 식별자를 가지며 생명주기 동안 변경될 수 있는 도메인 객체. 값 객체와 대비되는 개념.

### Error Type (오류 타입)
LanguageExt의 `Error` 타입. 코드와 메시지를 포함한 구조화된 오류 정보.

---

## F

### Factory Method (팩토리 메서드)
객체 생성을 캡슐화하는 정적 메서드. 값 객체에서는 검증을 포함한 `Create` 메서드.

### Fin<T>
LanguageExt의 결과 타입. 성공(Succ) 또는 실패(Fail)를 표현.

```csharp
Fin<User> result = User.Create(name, email);
```

### Functor (펑터)
`Map` 연산을 지원하는 타입. 컨테이너 내부의 값을 변환.

---

## G

### GetEqualityComponents
ValueObject의 동등성 비교에 사용되는 구성 요소를 반환하는 메서드.

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return Property1;
    yield return Property2;
}
```

---

## I

### Immutability (불변성)
객체가 생성된 후 상태가 변경되지 않는 특성. 값 객체의 핵심 원칙.

### IValueObject
값 객체임을 나타내는 마커 인터페이스.

---

## L

### LanguageExt
C#용 함수형 프로그래밍 라이브러리. `Fin<T>`, `Option<T>`, `Validation<E, T>` 등 제공.

### LINQ Expression
`from`, `select`, `where` 등의 쿼리 구문. 모나드 연산과 결합 가능.

```csharp
var result =
    from a in GetA()
    from b in GetB(a)
    select Combine(a, b);
```

---

## M

### Map (맵)
펑터/모나드의 변환 연산. 내부 값에 함수를 적용하고 결과를 같은 컨테이너로 래핑.

```csharp
Fin<int> number = 10;
Fin<string> text = number.Map(n => n.ToString());
```

### Match (매치)
패턴 매칭. 성공/실패, Some/None 등의 경우에 따라 다른 로직 실행.

```csharp
result.Match(
    Succ: value => HandleSuccess(value),
    Fail: error => HandleError(error)
);
```

### Monad (모나드)
`Bind` 연산을 지원하는 타입. `Map`과 `Bind`를 모두 지원.

---

## O

### Option<T>
값이 있거나 없을 수 있는 타입. null 대신 사용.

```csharp
Option<User> user = Some(new User());
Option<User> noUser = None;
```

### Operator Overloading (연산자 오버로딩)
`+`, `-`, `==`, `implicit` 등의 연산자를 커스텀 구현.

---

## P

### Prelude
LanguageExt의 정적 헬퍼 메서드 모음. `using static LanguageExt.Prelude;`로 사용.

### Pure Function (순수 함수)
부수 효과 없이 입력에 대해 항상 같은 출력을 반환하는 함수.

---

## R

### Railway Oriented Programming (철도 지향 프로그래밍)
성공/실패 경로를 철도 선로에 비유한 함수형 오류 처리 패턴.

```
   성공 경로 ─────────────────────────▶
                ↘        ↘
   실패 경로 ────▶────────▶───────────▶
```

---

## S

### Sealed Class (봉인 클래스)
상속이 금지된 클래스. 값 객체는 sealed로 선언하는 것이 권장됨.

### Short-Circuit (단락)
Bind 체인에서 실패 발생 시 이후 연산을 건너뛰는 동작.

### SimpleValueObject<T>
단일 값을 래핑하는 기본 값 객체 클래스.

### SmartEnum
행위와 속성을 가진 열거형. `Ardalis.SmartEnum` 라이브러리 기반.

### Success-Driven Development (성공 주도 개발)
예외 대신 명시적 결과 타입으로 성공 경로를 중심으로 개발하는 방법론.

---

## U

### Unit
반환값이 없음을 나타내는 타입. `void` 대신 사용하여 함수형 조합 가능.

```csharp
Fin<Unit> SaveData(data) => unit;
```

---

## V

### Validation<Error, T>
병렬 검증과 오류 수집을 지원하는 타입. Apply 패턴과 함께 사용.

### Value Equality (값 동등성)
참조가 아닌 값으로 동등성을 판단. `Equals`, `GetHashCode` 구현 필요.

### Value Object (값 객체)
식별자 없이 값으로만 정의되는 불변 객체. DDD의 핵심 빌딩 블록.

**특성:**
- 불변성 (Immutability)
- 값 동등성 (Value Equality)
- 자체 검증 (Self-Validation)
- 부수 효과 없음 (Side-Effect Free)

---

## 다음 단계

참고 자료를 확인합니다.

→ [D. 참고 자료](D-references.md)
