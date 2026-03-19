---
title: "베스트 프랙티스"
---

## 개요

아키텍처 규칙을 만들었다면, 다음 과제는 팀 전체가 이를 지속적으로 운영하는 것입니다. 규칙이 코드와 함께 성장하지 않으면, 시간이 지날수록 "늘 실패하는 테스트"로 전락하여 무시되기 시작합니다. 이 장에서는 규칙의 설계, 테스트 구성, 성능, 팀 도입 전략까지 — 아키텍처 테스트를 실무에서 살아있게 유지하는 방법을 정리합니다.

> **"좋은 아키텍처 규칙은 위반 메시지만 읽어도 무엇이 잘못되었는지 알 수 있습니다."**

## Suite 상속부터 시작하세요

새 프로젝트에서 아키텍처 테스트를 도입할 때는 `DomainArchitectureTestSuite`와 `ApplicationArchitectureTestSuite`를 상속하는 것부터 시작하세요. 두 개의 프로퍼티만 오버라이드하면 25개의 검증된 규칙이 즉시 적용됩니다. 프로젝트 고유 규칙은 Suite 위에 `[Fact]` 메서드로 추가합니다.

```csharp
// 1. Suite 상속으로 25개 규칙 즉시 확보
public sealed class DomainArchTests : DomainArchitectureTestSuite { ... }
public sealed class AppArchTests : ApplicationArchitectureTestSuite { ... }

// 2. 프로젝트 고유 규칙 추가
// 3. ArchUnitNET 네이티브 API로 레이어 의존성 규칙 추가
```

자세한 사용법은 [Part 4-05 아키텍처 테스트 스위트](../Part4-Real-World-Patterns/05-Architecture-Test-Suites/)를 참조하세요.

## 규칙 설계 원칙

### 규칙은 명확한 이름을 가져야 합니다

`ThrowIfAnyFailures`에 전달하는 규칙 이름은 위반 시 무엇이 잘못되었는지 즉시 알 수 있어야 합니다. 모호한 이름은 위반을 수정하는 개발자에게 추가 조사 비용을 전가합니다.

```csharp
// Good: 규칙의 의도가 명확
.ThrowIfAnyFailures("ValueObject Immutability Rule");
.ThrowIfAnyFailures("Entity Factory Method Rule");

// Bad: 무엇을 검증하는지 불명확
.ThrowIfAnyFailures("Rule1");
.ThrowIfAnyFailures("Check");
```

### 한 테스트에 하나의 관심사

관련된 규칙은 하나의 Validator 체인으로 묶되, 서로 다른 관심사는 별도 테스트로 분리합니다. 관심사를 섞으면 하나가 실패했을 때 나머지를 확인할 수 없습니다.

```csharp
// Good: "가시성" 관심사를 하나의 테스트로
[Fact]
public void Entity_ShouldBe_PublicSealed()
{
    // RequirePublic() + RequireSealed() = 가시성 관심사
}

// Good: "팩토리 메서드" 관심사는 별도 테스트로
[Fact]
public void Entity_ShouldHave_CreateFactoryMethod()
{
    // RequireMethod("Create", ...) = 팩토리 메서드 관심사
}
```

### verbose 모드 활용

`verbose: true`를 사용하면 위반 시 상세한 디버깅 정보를 얻을 수 있습니다. 개발 중에는 항상 활성화하고, 안정화된 후에도 유지하는 것을 권장합니다. verbose 모드의 오버헤드는 무시할 수 있는 수준이며, 위반 원인을 추적하는 시간을 크게 줄여줍니다.

## 테스트 구성 패턴

### ArchitectureTestBase 패턴

모든 아키텍처 테스트에서 공통으로 사용하는 `Architecture` 객체와 네임스페이스 문자열을 기반 클래스로 추출합니다. 이렇게 하면 어셈블리 로딩 코드의 중복을 제거하고, 네임스페이스가 변경될 때 한 곳만 수정하면 됩니다.

```csharp
public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Domain.AssemblyReference).Assembly,
                typeof(Application.AssemblyReference).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(Domain.AssemblyReference).Namespace!;
}
```

**핵심 포인트:**
- `static readonly`로 선언하여 어셈블리 로딩을 한 번만 수행합니다
- `typeof().Namespace!`로 네임스페이스 문자열을 안전하게 추출합니다
- 문자열 하드코딩 대신 리플렉션을 사용하면 네임스페이스 변경 시 컴파일 에러로 감지됩니다

### 테스트 파일 분류

레이어별 또는 패턴별로 테스트 파일을 분리합니다. 파일 이름만으로 어떤 규칙이 들어있는지 알 수 있어야 합니다.

```txt
Architecture/
├── ArchitectureTestBase.cs          # 공통 설정
├── EntityArchitectureRuleTests.cs   # Entity 규칙
├── ValueObjectArchitectureRuleTests.cs  # ValueObject 규칙
├── UsecaseArchitectureRuleTests.cs  # Usecase 규칙
├── DtoArchitectureRuleTests.cs      # DTO 규칙
└── LayerDependencyArchitectureRuleTests.cs  # 레이어 의존성 규칙
```

### 커스텀 규칙 재사용

팀 공통 규칙은 `DelegateArchRule`이나 `CompositeArchRule`로 정의하여 여러 테스트에서 재사용합니다. 규칙이 한 곳에 정의되면 변경 시 모든 테스트에 일관되게 반영됩니다.

```csharp
// 공유 규칙을 static readonly 필드로 정의
private static readonly DelegateArchRule<Class> s_domainNamingRule = new(
    "Forbids infrastructure suffixes",
    (target, _) => { /* 검증 로직 */ });

private static readonly CompositeArchRule<Class> s_entityCoreRule = new(
    new ImmutabilityRule(),
    s_domainNamingRule);
```

## 성능 고려사항

### ArchLoader 캐싱

`ArchLoader().LoadAssemblies().Build()`는 리플렉션 기반으로 동작하므로 비용이 큽니다. **테스트 클래스 간에 `Architecture` 객체를 공유하세요:**

```csharp
// Good: static readonly로 한 번만 로딩
protected static readonly Architecture Architecture = ...;

// Bad: 매 테스트마다 새로 로딩
[Fact]
public void Test()
{
    var arch = new ArchLoader().LoadAssemblies(...).Build(); // 느림!
}
```

### 필요한 어셈블리만 로딩

검증하지 않는 어셈블리는 로딩하지 마세요. 불필요한 어셈블리 로딩은 시작 시간을 증가시킵니다.

## 팀 도입 전략

### 점진적 도입

한 번에 모든 규칙을 도입하면 팀의 저항을 초래합니다. 이해하기 쉽고 효과가 큰 규칙부터 시작하여, 팀이 가치를 체감한 후 점진적으로 확장하세요.

1. **레이어 의존성 규칙부터:** 가장 이해하기 쉽고 효과가 큽니다
2. **가시성/수정자 규칙:** `RequirePublic()`, `RequireSealed()` 등 단순 규칙 추가
3. **네이밍 규칙:** 팀 컨벤션을 코드로 강제
4. **메서드 시그니처 규칙:** 팩토리 메서드 패턴 등 심화 규칙
5. **커스텀 규칙:** 팀 고유의 규칙을 `DelegateArchRule`로 정의

### 새 규칙 추가 워크플로

```txt
1. 코드 리뷰에서 반복 지적 사항 발견
2. 팀 회의에서 규칙으로 합의
3. 아키텍처 테스트로 구현 → 기존 코드에서 위반 확인
4. 위반 코드 수정
5. CI에 통합하여 자동 검증
```

### 기존 코드와의 공존

기존 코드에 규칙을 소급 적용하기 어려운 경우, ArchUnitNET의 필터링을 활용합니다. 새 코드에만 규칙을 적용하고, 레거시 코드는 별도 계획으로 점진 수정하세요.

```csharp
// 특정 네임스페이스의 클래스만 검증 (레거시 제외)
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace("MyApp.Domains.V2")  // 새 코드만
    .ValidateAllClasses(Architecture, @class => @class
        .RequireImmutable())
    .ThrowIfAnyFailures("New Domain Immutability Rule");
```

## 한눈에 보는 정리

| 영역 | 베스트 프랙티스 | 핵심 이유 |
|------|----------------|----------|
| **규칙 이름** | 위반 내용을 설명하는 명확한 이름 사용 | 위반 메시지만으로 문제 파악 가능 |
| **관심사 분리** | 테스트 하나에 관심사 하나 | 실패 원인 격리 용이 |
| **verbose 모드** | 항상 `verbose: true` 유지 | 디버깅 시간 단축 |
| **Architecture 캐싱** | `static readonly`로 한 번만 로딩 | 리플렉션 비용 절감 |
| **어셈블리 범위** | 필요한 어셈블리만 로딩 | 시작 시간 최소화 |
| **커스텀 규칙 재사용** | `DelegateArchRule`/`CompositeArchRule` 활용 | 규칙 변경 시 일관 반영 |
| **점진적 도입** | 레이어 의존성 → 가시성 → 네이밍 → 심화 순서 | 팀 수용성 확보 |
| **레거시 공존** | 네임스페이스 필터로 새 코드만 검증 | 기존 코드 안정성 유지 |

## FAQ

### Q1: 규칙이 너무 많아지면 관리가 어렵지 않나요?

**A:** 규칙 수 자체보다 구조가 중요합니다. `ArchitectureTestBase`로 공통 설정을 추출하고, 패턴별로 테스트 파일을 분리하며, `CompositeArchRule`로 관련 규칙을 묶으면 수십 개의 규칙도 체계적으로 관리할 수 있습니다. 규칙이 50개를 넘는다면 카테고리별 폴더 분리를 고려하세요.

### Q2: 새 규칙을 추가할 때 기준은 무엇인가요?

**A:** "코드 리뷰에서 같은 지적이 3번 이상 반복되는가?"가 좋은 기준입니다. 반복되는 지적은 사람이 기억에 의존하고 있다는 신호이며, 아키텍처 테스트로 자동화할 가치가 있습니다. 팀 회의에서 합의된 규칙만 추가하세요.

### Q3: 아키텍처 테스트가 실패하면 빌드를 막아야 하나요?

**A:** 네, CI에서 반드시 막아야 합니다. "경고만 남기고 통과"시키면 위반이 누적되어 규칙의 신뢰성이 사라집니다. 새 규칙 도입 초기에 기존 위반이 많다면, 네임스페이스 필터로 새 코드에만 적용하면서 점진적으로 범위를 넓히세요.

### Q4: 팀원이 아키텍처 테스트의 가치를 느끼지 못하면 어떻게 하나요?

**A:** 가장 효과적인 설득은 "실제 사고 방지" 사례입니다. 레이어 의존성 규칙 하나만 도입해도 도메인 레이어에 인프라 의존성이 침투하는 것을 즉시 잡을 수 있습니다. 작은 성공 사례를 만든 후 팀에 공유하세요.

---

다음 장에서는 관련 학습 자료와 프레임워크 확장 방법을 안내합니다.

→ [2장: 다음 단계](02-next-steps.md)
