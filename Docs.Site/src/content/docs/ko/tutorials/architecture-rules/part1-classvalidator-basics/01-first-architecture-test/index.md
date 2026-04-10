---
title: "첫 번째 아키텍처 테스트"
---
## 개요

`Employee` 클래스가 `public sealed`인지 매번 코드 리뷰에서 눈으로 확인하고 있나요? 클래스가 10개일 때는 가능하지만, 50개, 100개로 늘어나면 놓치는 것은 시간 문제입니다. 이 장에서는 이런 수동 확인을 **실행 가능한 테스트로** 바꾸는 방법을 배웁니다.

> **"아키텍처 테스트의 첫 걸음은 간단합니다. '이 클래스는 public sealed이어야 한다'를 코드로 표현하면 됩니다."**

## 학습 목표

### 핵심 학습 목표
1. **아키텍처 테스트의 기본 구조 이해**
   - `ArchLoader`로 어셈블리를 로드하여 `Architecture` 객체 생성
   - `ArchRuleDefinition.Classes()`로 검증 대상 클래스 선택
2. **ClassValidator를** 사용한 클래스 규칙 검증
   - `RequirePublic()`: 클래스가 `public`인지 검증
   - `RequireSealed()`: 클래스가 `sealed`인지 검증
3. **여러 규칙의 체이닝**
   - `RequirePublic().RequireSealed()`처럼 여러 규칙을 하나의 검증에서 결합

### 실습을 통해 확인할 내용
- `Employee` 클래스의 가시성(`public`)과 한정자(`sealed`)를 자동으로 검증
- 단일 규칙 테스트와 복합 규칙 체이닝의 차이

## 프로젝트 구조

```
01-First-Architecture-Test/
├── FirstArchitectureTest/                    # 메인 프로젝트
│   ├── Domains/
│   │   └── Employee.cs                       # 검증 대상 도메인 클래스
│   ├── Program.cs
│   └── FirstArchitectureTest.csproj
├── FirstArchitectureTest.Tests.Unit/         # 테스트 프로젝트
│   ├── ArchitectureTests.cs                  # 아키텍처 테스트
│   ├── FirstArchitectureTest.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## 검증 대상 코드

### Employee.cs

```csharp
namespace FirstArchitectureTest.Domains;

public sealed class Employee
{
    public string Name { get; }
    public string Department { get; }

    private Employee(string name, string department)
    {
        Name = name;
        Department = department;
    }

    public static Employee Create(string name, string department)
        => new(name, department);
}
```

`Employee` 클래스는 다음 설계 원칙을 따릅니다:
- **`public sealed`**: 외부에 공개하되 상속을 금지하여 불변 계약을 보호합니다.
- **`private` 생성자 + 정적 팩토리 메서드**: 객체 생성을 통제합니다.

## 테스트 코드 설명

### Architecture 로드

```csharp
public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(FirstArchitectureTest.Domains.Employee).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(FirstArchitectureTest.Domains.Employee).Namespace!;
}
```

`ArchLoader`는 **ArchUnitNET**의 핵심 클래스로, 지정한 어셈블리의 타입 정보를 분석하여 `Architecture` 객체를 생성합니다. 이 객체는 모든 아키텍처 검증의 기반이 됩니다.

### 단일 규칙 검증

```csharp
[Fact]
public void DomainClasses_ShouldBe_Public()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Visibility Rule");
}
```

검증은 네 단계로 이루어집니다:

1. **대상 선택** -- `ArchRuleDefinition.Classes()`로 클래스 검증을 시작합니다
2. **필터링** -- `.That().ResideInNamespace(...)`로 특정 네임스페이스의 클래스만 선택합니다
3. **규칙 적용** -- `.ValidateAllClasses(...)`에서 **ClassValidator를** 사용하여 각 클래스에 `RequirePublic()` 규칙을 적용합니다
4. **결과 확인** -- `.ThrowIfAnyFailures(...)`로 위반 사항이 있으면 예외를 발생시킵니다

### 규칙 체이닝

```csharp
[Fact]
public void DomainClasses_ShouldBe_PublicAndSealed()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic()
            .RequireSealed(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Public Sealed Rule");
}
```

**ClassValidator는** 플루언트 API를 제공하여 여러 규칙을 체이닝할 수 있습니다. `RequirePublic().RequireSealed()`는 하나의 검증 안에서 두 가지 조건을 모두 확인합니다.

## 한눈에 보는 정리

다음 표는 이 장에서 사용한 핵심 구성요소를 요약합니다.

### 아키텍처 테스트 구성요소

| 구성요소 | 역할 |
|----------|------|
| **ArchLoader** | 어셈블리를 분석하여 Architecture 객체 생성 |
| **ArchRuleDefinition** | 검증 대상(클래스, 인터페이스 등) 선택의 시작점 |
| **ValidateAllClasses** | 선택된 모든 클래스에 ClassValidator 규칙 적용 |
| **ClassValidator** | 클래스별 규칙을 정의하는 플루언트 API |
| **ThrowIfAnyFailures** | 위반 사항이 있으면 상세 메시지와 함께 예외 발생 |
| **verbose: true** | 검증 대상 클래스 목록을 콘솔에 출력 |

## FAQ

### Q1: `verbose: true`는 어떤 역할을 하나요?
**A**: 검증 대상으로 선택된 클래스 목록을 콘솔에 출력합니다. 디버깅 시 "예상한 클래스가 검증 대상에 포함되었는가"를 확인하는 데 유용합니다. 프로덕션 CI에서는 `false`로 설정하여 출력을 줄일 수 있습니다.

### Q2: 규칙을 위반하면 어떤 메시지가 출력되나요?
**A**: `ThrowIfAnyFailures`는 위반한 클래스 이름, 위반한 규칙, 규칙명(`"Domain Class Public Sealed Rule"`)을 포함한 상세 메시지를 출력합니다. 예를 들어 `Employee`가 `sealed`가 아니면 "`Employee` failed: RequireSealed"와 같은 메시지가 표시됩니다.

### Q3: 체이닝과 별도 테스트의 차이는 무엇인가요?
**A**: `RequirePublic().RequireSealed()` 체이닝은 두 규칙을 **하나의 검증 단위로** 묶습니다. 하나라도 실패하면 해당 클래스의 모든 위반 사항이 함께 보고됩니다. 반면 별도 테스트로 분리하면 각 규칙의 성공/실패를 독립적으로 추적할 수 있습니다. 일반적으로 관련된 규칙은 체이닝하고, 독립적인 규칙은 별도 테스트로 분리합니다.

### Q4: `@class`에서 `@`는 왜 필요한가요?
**A**: `class`는 C#의 예약어입니다. 변수명으로 사용하려면 `@` 접두사를 붙여 컴파일러에게 "이것은 예약어가 아니라 식별자"라는 것을 알려줘야 합니다. `@class` 대신 `c`나 `cls` 같은 이름을 써도 되지만, `@class`가 의도를 더 명확하게 전달합니다.

---

다음 장에서는 `public`/`internal` 가시성, `sealed`/`abstract`/`static` 한정자, `record` 타입까지 다양한 클래스 속성을 검증하는 방법을 배웁니다.

→ [2장: 가시성과 한정자](../02-Visibility-And-Modifiers/)
