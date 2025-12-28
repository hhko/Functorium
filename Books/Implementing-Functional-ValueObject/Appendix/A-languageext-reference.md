# 부록 A. LanguageExt 주요 타입 참조

> **부록** | [← 이전: 5.4 일정/예약 도메인](../05-domain-examples/04-scheduling-domain.md) | [목차](../README.md) | [다음: B. 프레임워크 타입 선택 가이드 →](B-type-selection-guide.md)

---

## 개요

LanguageExt 라이브러리의 핵심 타입들을 빠르게 참조할 수 있는 가이드입니다.

---

## Fin<T> - 결과 타입

### 기본 사용법

```csharp
// 성공 생성
Fin<int> success = 42;
Fin<int> success2 = FinSucc(42);

// 실패 생성
Fin<int> failure = Error.New("오류 발생");
Fin<int> failure2 = FinFail<int>(Error.New("오류"));

// 결과 확인
if (result.IsSucc) { /* 성공 */ }
if (result.IsFail) { /* 실패 */ }
```

### Match - 패턴 매칭

```csharp
var message = result.Match(
    Succ: value => $"성공: {value}",
    Fail: error => $"실패: {error.Message}"
);
```

### Map - 값 변환

```csharp
Fin<int> number = 10;
Fin<string> text = number.Map(n => n.ToString());
// 결과: "10"
```

### Bind - 체이닝

```csharp
Fin<int> Parse(string s) =>
    int.TryParse(s, out var n) ? n : Error.New("파싱 실패");

Fin<int> result = Fin<string>.Succ("42")
    .Bind(Parse)
    .Map(n => n * 2);
// 결과: 84
```

### IfFail - 기본값

```csharp
int value = result.IfFail(0);
int value2 = result.IfFail(error => -1);
```

---

## Validation<Error, T> - 검증 타입

### 기본 사용법

```csharp
// 성공 생성
Validation<Error, string> valid = Success<Error, string>("값");

// 실패 생성
Validation<Error, string> invalid = Fail<Error, string>(Error.New("오류"));
```

### Apply - 병렬 검증

```csharp
var result = (
    ValidateName(name),
    ValidateAge(age),
    ValidateEmail(email)
).Apply((n, a, e) => new User(n, a, e));

// 모든 검증 오류가 수집됨
```

### 단일 필드 검증

```csharp
Validation<Error, string> ValidateName(string name) =>
    string.IsNullOrEmpty(name)
        ? Fail<Error, string>(Error.New("이름은 필수입니다"))
        : Success<Error, string>(name);
```

### 오류 수집

```csharp
result.Match(
    Succ: value => Console.WriteLine($"성공: {value}"),
    Fail: errors =>
    {
        foreach (var error in errors)
            Console.WriteLine($"오류: {error.Message}");
    }
);
```

---

## Option<T> - 선택적 값

### 기본 사용법

```csharp
// 값이 있는 경우
Option<int> some = Some(42);
Option<int> some2 = 42; // 암시적 변환

// 값이 없는 경우
Option<int> none = None;
```

### Match

```csharp
string message = option.Match(
    Some: value => $"값: {value}",
    None: () => "값 없음"
);
```

### Map과 Bind

```csharp
Option<int> result = Some(10)
    .Map(n => n * 2)
    .Bind(n => n > 0 ? Some(n) : None);
```

### 기본값

```csharp
int value = option.IfNone(0);
int value2 = option.IfNone(() => GetDefaultValue());
```

---

## Either<L, R> - 이진 선택

### 기본 사용법

```csharp
// Right (성공 값)
Either<string, int> right = Right<string, int>(42);

// Left (오류 값)
Either<string, int> left = Left<string, int>("오류");
```

### Match

```csharp
string result = either.Match(
    Right: value => $"성공: {value}",
    Left: error => $"실패: {error}"
);
```

---

## Error 타입

### 생성

```csharp
// 기본 오류
var error = Error.New("오류 메시지");

// 코드와 메시지
var error2 = Error.New("ERR001", "오류 메시지");

// 예외로부터
var error3 = Error.New(exception);

// 내부 오류 포함
var error4 = Error.New("외부 오류", Error.New("내부 오류"));
```

### 속성

```csharp
string code = error.Code;
string message = error.Message;
Option<Error> inner = error.Inner;
Option<Exception> exception = error.Exception;
```

---

## Unit 타입

### 사용 목적

```csharp
// 반환값이 없는 함수의 결과 타입
Fin<Unit> SaveToDatabase(Data data)
{
    // 저장 로직
    return unit; // 성공
}

// 검증 함수
Fin<Unit> ValidateNotEmpty(string value) =>
    string.IsNullOrEmpty(value)
        ? Error.New("값이 비어있습니다")
        : unit;
```

---

## Prelude 정적 메서드

### 필수 import

```csharp
using static LanguageExt.Prelude;
```

### 자주 사용하는 메서드

```csharp
// Option 생성
Some(42)
None

// Either 생성
Right<L, R>(value)
Left<L, R>(value)

// Validation 생성
Success<Error, T>(value)
Fail<Error, T>(error)

// Fin 생성
FinSucc<T>(value)
FinFail<T>(error)

// Unit 값
unit
```

---

## LINQ 확장

### SelectMany (Bind)

```csharp
var result =
    from x in Fin<int>.Succ(10)
    from y in Fin<int>.Succ(20)
    select x + y;
// 결과: 30
```

### Select (Map)

```csharp
var result =
    from x in Some(10)
    select x * 2;
// 결과: Some(20)
```

### Where (Filter)

```csharp
var result =
    from x in Some(10)
    where x > 5
    select x;
// 결과: Some(10)
```

---

## 컬렉션 확장

### Seq<T>

```csharp
// 불변 시퀀스
var seq = Seq(1, 2, 3, 4, 5);
var head = seq.Head; // Some(1)
var tail = seq.Tail; // Seq(2, 3, 4, 5)
```

### Arr<T>

```csharp
// 불변 배열
var arr = Array(1, 2, 3);
var added = arr.Add(4); // 새 배열 반환
```

### Map<K, V>

```csharp
// 불변 딕셔너리
var map = Map(("a", 1), ("b", 2));
var value = map.Find("a"); // Some(1)
var updated = map.Add("c", 3);
```

---

## 자주 사용하는 패턴

### 체이닝 패턴

```csharp
var result = GetUser(id)
    .Bind(ValidateUser)
    .Bind(UpdateUser)
    .Map(ToResponse);
```

### 병렬 검증 패턴

**방법 1: 튜플 기반 Apply (권장)**

```csharp
var result = (
    ValidateField1(input.Field1),
    ValidateField2(input.Field2),
    ValidateField3(input.Field3)
).Apply((f1, f2, f3) => new Output(f1, f2, f3));
```

**방법 2: fun 기반 개별 Apply**

`fun` 함수는 람다의 타입 추론을 돕는 헬퍼로, Currying을 통해 단계적으로 Apply를 적용합니다.

```csharp
// fun으로 생성자/팩토리를 감싸고 개별 Apply 호출
var result = fun((string f1, string f2, string f3) => new Output(f1, f2, f3))
    .Map(f => Success<Error, Func<string, string, string, Output>>(f))
    .Apply(ValidateField1(input.Field1))
    .Apply(ValidateField2(input.Field2))
    .Apply(ValidateField3(input.Field3));
```

또는 Pure를 사용하여 더 간결하게:

```csharp
var result = Pure<Validation<Error>, Output>(
    fun((string f1, string f2, string f3) => new Output(f1, f2, f3)))
    .Apply(ValidateField1(input.Field1))
    .Apply(ValidateField2(input.Field2))
    .Apply(ValidateField3(input.Field3));
```

| 방법 | 특징 | 사용 시기 |
|------|------|----------|
| 튜플 Apply | 간결하고 직관적 | 대부분의 경우 권장 |
| fun 개별 Apply | Currying 기반, 단계적 적용 | 동적 파라미터 개수, 고급 합성 |

### 옵션 체이닝

```csharp
var result = user
    .Map(u => u.Address)
    .Bind(a => a.City)
    .Map(c => c.Name)
    .IfNone("Unknown");
```

---

## 다음 단계

프레임워크 타입 선택 가이드를 확인합니다.

→ [B. 프레임워크 타입 선택 가이드](B-type-selection-guide.md)
