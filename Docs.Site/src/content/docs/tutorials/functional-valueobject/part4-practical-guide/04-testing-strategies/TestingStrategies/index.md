---
title: "Testing Strategies"
---
## Overview

value object의 테스트 전략을 학습합니다. 단위 테스트 패턴, 테스트 헬퍼, 아키텍처 테스트를 다룹니다.

---

## Learning Objectives

- value object 생성 테스트 패턴
- 동등성 테스트 패턴
- comparability 테스트 패턴
- `Fin<T>` 테스트 헬퍼 활용

---

## 실행 방법

```bash
cd Docs/tutorials/Functional-ValueObject/04-practical-guide/04-Testing-Strategies/TestingStrategies
dotnet run
```

---

## 예상 출력

```
=== 값 객체 테스트 전략 ===

1. 생성 테스트 패턴
────────────────────────────────────────
   [유효한 입력 테스트] user@example.com → PASS
   [유효하지 않은 입력 테스트] invalid-email → PASS
   [에러 메시지 검증] '@' 포함 → PASS
   [경계값 테스트] 빈 문자열/null → PASS

2. 동등성 테스트 패턴
────────────────────────────────────────
   [같은 값 동등성] email1 == email2 → PASS
   [다른 값 비동등성] email1 != email3 → PASS
   [해시코드 일관성] hash(email1) == hash(email2) → PASS
   [연산자 테스트] == 및 != → PASS

3. 비교 가능성 테스트 패턴
────────────────────────────────────────
   [CompareTo 테스트] 20 < 25 < 30 → PASS
   [비교 연산자 테스트] < 연산자 → PASS
   [정렬 테스트] 정렬 후 순서 → PASS

4. 테스트 헬퍼 사용
────────────────────────────────────────
   [ShouldBeSuccess 헬퍼] → PASS
   [ShouldBeFail 헬퍼] → PASS
   [GetSuccessValue 헬퍼] → PASS
   [GetFailError 헬퍼] → PASS
```

---

## 핵심 코드 설명

### 1. Fin<T> 테스트 헬퍼

```csharp
public static class FinTestExtensions
{
    public static void ShouldBeSuccess<T>(this Fin<T> fin)
    {
        if (fin.IsFail)
        {
            var message = fin.Match(_ => "", e => e.Message);
            throw new Exception($"Expected Succ but got Fail: {message}");
        }
    }

    public static void ShouldBeFail<T>(this Fin<T> fin)
    {
        if (fin.IsSucc)
            throw new Exception("Expected Fail but got Succ");
    }

    public static T GetSuccessValue<T>(this Fin<T> fin) => ...;
    public static Error GetFailError<T>(this Fin<T> fin) => ...;
}
```

### 2. 테스트 패턴 예시

```csharp
// 생성 테스트
[Fact]
public void Create_WithValidEmail_ShouldSucceed()
{
    var result = Email.Create("user@example.com");

    result.ShouldBeSuccess();
    var email = result.GetSuccessValue();
    ((string)email).ShouldBe("user@example.com");
}

// 동등성 테스트
[Fact]
public void Equals_WithSameValue_ShouldBeTrue()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");

    email1.ShouldBe(email2);
    email1.GetHashCode().ShouldBe(email2.GetHashCode());
}

// 비교 테스트
[Fact]
public void Sort_ShouldOrderByValue()
{
    var ages = new[] { Age.CreateFromValidated(30), Age.CreateFromValidated(10) };

    Array.Sort(ages);

    ages[0].Value.ShouldBe(10);
    ages[1].Value.ShouldBe(30);
}
```

---

## 테스트 체크리스트

### 생성 테스트
- [ ] 유효한 입력 → 성공
- [ ] 유효하지 않은 입력 → 실패
- [ ] 경계값 테스트 (빈 문자열, null, 최소/최대값)
- [ ] error message 검증

### 동등성 테스트
- [ ] 같은 값 → 동등
- [ ] 다른 값 → 비동등
- [ ] 해시코드 일관성
- [ ] == / != 연산자

### 비교 테스트 (해당 시)
- [ ] CompareTo 정확성
- [ ] <, >, <=, >= 연산자
- [ ] 정렬 동작

### immutability 테스트
- [ ] 연산 후 원본 변경 없음

## FAQ

### Q1: `Fin<T>` 테스트 헬퍼는 왜 필요한가요?
**A**: `Fin<T>`의 성공/실패를 직접 검증하려면 `Match`를 호출하고 조건을 확인하는 반복 코드가 필요합니다. `ShouldBeSuccess()`, `ShouldBeFail()` 같은 헬퍼를 사용하면 테스트 코드가 간결해지고, 실패 시 error message도 명확하게 출력됩니다.

### Q2: value object 테스트에서 경계값 테스트가 중요한 이유는 무엇인가요?
**A**: value object의 `Create` 메서드는 유효성 검증의 마지막 방어선입니다. 빈 문자열, `null`, 최소/최대값 같은 경계 조건에서 검증이 정확히 동작하는지 확인하지 않으면, 유효하지 않은 값이 시스템에 들어올 수 있습니다.

### Q3: 동등성 테스트에서 해시코드 일관성까지 검증해야 하나요?
**A**: 네. C#의 `Dictionary`와 `HashSet`은 `GetHashCode()`를 사용하여 키를 관리합니다. 동등한 두 객체의 해시코드가 다르면 컬렉션에서 올바르게 동작하지 않으므로, `Equals`가 `true`인 경우 해시코드도 반드시 같아야 합니다.

---

## Next Steps

Part 5에서 도메인별 실전 예제를 학습합니다.

→ [5.1 이커머스 도메인](../../../05-domain-examples/01-Ecommerce-Domain/EcommerceDomain/)
