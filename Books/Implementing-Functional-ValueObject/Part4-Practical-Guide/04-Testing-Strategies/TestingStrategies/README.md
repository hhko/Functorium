# 4.4 테스트 전략 🔴

> **Part 4: 실전 가이드** | [← 이전: 4.3 CQRS 통합](../../03-CQRS-Integration/CqrsIntegration/README.md) | [목차](../../../README.md) | [다음: Part 5 도메인별 실전 예제 →](../../../05-domain-examples/01-Ecommerce-Domain/EcommerceDomain/README.md)

---

## 개요

값 객체의 테스트 전략을 학습합니다. 단위 테스트 패턴, 테스트 헬퍼, 아키텍처 테스트를 다룹니다.

---

## 학습 목표

- 값 객체 생성 테스트 패턴
- 동등성 테스트 패턴
- 비교 가능성 테스트 패턴
- `Fin<T>` 테스트 헬퍼 활용

---

## 실행 방법

```bash
cd Books/Functional-ValueObject/04-practical-guide/04-Testing-Strategies/TestingStrategies
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
- [ ] 에러 메시지 검증

### 동등성 테스트
- [ ] 같은 값 → 동등
- [ ] 다른 값 → 비동등
- [ ] 해시코드 일관성
- [ ] == / != 연산자

### 비교 테스트 (해당 시)
- [ ] CompareTo 정확성
- [ ] <, >, <=, >= 연산자
- [ ] 정렬 동작

### 불변성 테스트
- [ ] 연산 후 원본 변경 없음

---

## 다음 단계

Part 5에서 도메인별 실전 예제를 학습합니다.

→ [5.1 이커머스 도메인](../../../05-domain-examples/01-Ecommerce-Domain/EcommerceDomain/README.md)
