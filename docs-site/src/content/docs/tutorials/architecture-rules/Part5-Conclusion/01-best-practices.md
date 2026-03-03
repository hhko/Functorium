---
title: "베스트 프랙티스"
---

## 규칙 설계 원칙

### 규칙은 명확한 이름을 가져야 합니다

`ThrowIfAnyFailures`에 전달하는 규칙 이름은 위반 시 무엇이 잘못되었는지 즉시 알 수 있어야 합니다.

```csharp
// Good: 규칙의 의도가 명확
.ThrowIfAnyFailures("ValueObject Immutability Rule");
.ThrowIfAnyFailures("Entity Factory Method Rule");

// Bad: 무엇을 검증하는지 불명확
.ThrowIfAnyFailures("Rule1");
.ThrowIfAnyFailures("Check");
```

### 한 테스트에 하나의 관심사

관련된 규칙은 하나의 Validator 체인으로 묶되, 서로 다른 관심사는 별도 테스트로 분리합니다.

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

`verbose: true`를 사용하면 위반 시 상세한 디버깅 정보를 얻을 수 있습니다. 개발 중에는 항상 활성화하고, 안정화된 후에도 유지하는 것을 권장합니다.

## 테스트 구성 패턴

### ArchitectureTestBase 패턴

모든 아키텍처 테스트에서 공통으로 사용하는 `Architecture` 객체와 네임스페이스 문자열을 기반 클래스로 추출합니다.

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

레이어별 또는 패턴별로 테스트 파일을 분리합니다:

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

팀 공통 규칙은 `DelegateArchRule`이나 `CompositeArchRule`로 정의하여 여러 테스트에서 재사용합니다:

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

한 번에 모든 규칙을 도입하지 마세요. 다음 순서를 권장합니다:

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

기존 코드에 규칙을 소급 적용하기 어려운 경우, ArchUnitNET의 필터링을 활용합니다:

```csharp
// 특정 네임스페이스의 클래스만 검증 (레거시 제외)
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace("MyApp.Domains.V2")  // 새 코드만
    .ValidateAllClasses(Architecture, @class => @class
        .RequireImmutable())
    .ThrowIfAnyFailures("New Domain Immutability Rule");
```

## 다음 장에서는

관련 학습 자료와 프레임워크 확장 방법을 안내합니다.
