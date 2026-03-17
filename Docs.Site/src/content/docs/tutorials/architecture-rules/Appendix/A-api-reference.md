---
title: "API 레퍼런스"
---

이 부록은 Functorium ArchitectureRules의 전체 API를 한눈에 참조할 수 있도록 정리한 레퍼런스입니다. 튜토리얼 학습 중 특정 메서드의 사용법이나 파라미터를 빠르게 확인할 때 활용하세요.

## ClassValidator

`TypeValidator<Class, ClassValidator>`를 상속하며, 클래스 수준의 아키텍처 규칙을 검증합니다.

### 가시성 규칙

| 메서드 | 설명 |
|--------|------|
| `RequirePublic()` | public 클래스여야 함 |
| `RequireInternal()` | internal 클래스여야 함 |

### 수정자 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireSealed()` | sealed 클래스여야 함 |
| `RequireNotSealed()` | sealed가 아니어야 함 |
| `RequireStatic()` | static 클래스여야 함 |
| `RequireNotStatic()` | static이 아니어야 함 |
| `RequireAbstract()` | abstract 클래스여야 함 |
| `RequireNotAbstract()` | abstract가 아니어야 함 |

### 타입 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireRecord()` | record 타입이어야 함 |
| `RequireNotRecord()` | record가 아니어야 함 |

### 어트리뷰트 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireAttribute(string attributeName)` | 지정된 어트리뷰트가 있어야 함 |

### 상속/인터페이스 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireInherits(Type baseType)` | 특정 기반 클래스를 상속해야 함 |
| `RequireImplements(Type interfaceType)` | 특정 인터페이스를 구현해야 함 |
| `RequireImplementsGenericInterface(string name)` | 제네릭 인터페이스를 구현해야 함 |

### 생성자 규칙

| 메서드 | 설명 |
|--------|------|
| `RequirePrivateAnyParameterlessConstructor()` | private 무매개변수 생성자가 있어야 함 |
| `RequireAllPrivateConstructors()` | 모든 생성자가 private이어야 함 |

### 속성/필드 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireProperty(string propertyName)` | 특정 속성이 존재해야 함 |
| `RequireNoPublicSetters()` | public setter가 없어야 함 |
| `RequireNoInstanceFields()` | 인스턴스 필드가 없어야 함 |
| `RequireOnlyPrimitiveProperties(params string[])` | 원시 타입 속성만 허용 |

### 중첩 클래스 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireNestedClass(string name, Action<ClassValidator>?)` | 중첩 클래스가 있어야 함 (선택적 검증) |
| `RequireNestedClassIfExists(string name, Action<ClassValidator>?)` | 존재할 경우만 검증 |

### 불변성 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireImmutable()` | ImmutabilityRule 적용 (6차원 불변성 검증) |

## InterfaceValidator

`TypeValidator<Interface, InterfaceValidator>`를 상속하며, 인터페이스 수준의 규칙을 검증합니다.

TypeValidator에서 상속받은 메서드를 사용합니다 (네이밍, 메서드 검증, 의존성 등).

## MethodValidator

메서드 수준의 시그니처 검증을 수행합니다.

### 가시성/수정자 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireVisibility(Visibility)` | 특정 가시성이어야 함 |
| `RequireStatic()` | static 메서드여야 함 |
| `RequireNotStatic()` | static이 아니어야 함 |
| `RequireVirtual()` | virtual 메서드여야 함 |
| `RequireNotVirtual()` | virtual이 아니어야 함 |
| `RequireExtensionMethod()` | 확장 메서드여야 함 |

### 반환 타입 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireReturnType(Type)` | 특정 반환 타입이어야 함 (open generic 지원) |
| `RequireReturnTypeOfDeclaringClass()` | 선언 클래스 타입을 반환해야 함 |
| `RequireReturnTypeOfDeclaringTopLevelClass()` | 최상위 클래스 타입을 반환해야 함 |
| `RequireReturnTypeContaining(string)` | 반환 타입 이름에 문자열이 포함되어야 함 |

### 파라미터 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireParameterCount(int)` | 정확한 파라미터 수 |
| `RequireParameterCountAtLeast(int)` | 최소 파라미터 수 |
| `RequireFirstParameterTypeContaining(string)` | 첫 번째 파라미터 타입에 문자열 포함 |
| `RequireAnyParameterTypeContaining(string)` | 임의의 파라미터 타입에 문자열 포함 |

## TypeValidator (공통 기반)

ClassValidator와 InterfaceValidator가 상속하는 공통 기반 클래스입니다.

### 네이밍 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireNameStartsWith(string prefix)` | 이름이 접두사로 시작해야 함 |
| `RequireNameEndsWith(string suffix)` | 이름이 접미사로 끝나야 함 |
| `RequireNameMatching(string regex)` | 이름이 정규식과 일치해야 함 |

### 의존성 규칙

| 메서드 | 설명 |
|--------|------|
| `RequireNoDependencyOn(string typeNameContains)` | 특정 타입에 의존하지 않아야 함 |

### 메서드 검증

| 메서드 | 설명 |
|--------|------|
| `RequireMethod(string name, Action<MethodValidator>)` | 특정 메서드를 검증 |
| `RequireMethodIfExists(string name, Action<MethodValidator>)` | 존재할 경우만 검증 |
| `RequireAllMethods(Action<MethodValidator>)` | 모든 메서드를 검증 |
| `RequireAllMethods(Func<MethodMember, bool>, Action<MethodValidator>)` | 필터된 메서드를 검증 |

### 규칙 합성

| 메서드 | 설명 |
|--------|------|
| `Apply(IArchRule<TType> rule)` | 커스텀 규칙 적용 |

## 진입점 (ArchitectureValidationEntryPoint)

| 확장 메서드 | 설명 |
|------------|------|
| `ValidateAllClasses(Architecture, Action<ClassValidator>, bool verbose)` | `IObjectProvider<Class>` 확장 |
| `ValidateAllInterfaces(Architecture, Action<InterfaceValidator>, bool verbose)` | `IObjectProvider<Interface>` 확장 |

반환 타입: `ValidationResultSummary`

| 메서드 | 설명 |
|--------|------|
| `ThrowIfAnyFailures(string ruleName)` | 위반 시 `ArchitectureViolationException` 발생 |

## 커스텀 규칙

### IArchRule&lt;TType&gt;

```csharp
public interface IArchRule<in TType> where TType : IType
{
    string Description { get; }
    IReadOnlyList<RuleViolation> Validate(TType target, Architecture architecture);
}
```

### DelegateArchRule&lt;TType&gt;

```csharp
// 람다 기반 커스텀 규칙
var rule = new DelegateArchRule<Class>(
    "Rule description",
    (target, architecture) => {
        // 검증 로직
        return violations; // IReadOnlyList<RuleViolation>
    });
```

### CompositeArchRule&lt;TType&gt;

```csharp
// 여러 규칙을 AND로 합성
var composite = new CompositeArchRule<Class>(rule1, rule2, rule3);
// Description: "rule1 AND rule2 AND rule3"
```

### RuleViolation

```csharp
public sealed record RuleViolation(
    string TargetName,    // 위반 대상 타입의 전체 이름
    string RuleName,      // 규칙 이름
    string Description);  // 위반 설명
```

## ImmutabilityRule 6차원 검증

| 차원 | 검증 내용 |
|------|----------|
| **쓰기 가능성** | 비정적 멤버가 불변이어야 함 |
| **생성자** | public 생성자가 없어야 함 |
| **속성** | public setter가 없어야 함 |
| **필드** | public 비정적 필드가 없어야 함 |
| **컬렉션** | 가변 컬렉션 타입 금지 (List, Dictionary 등) |
| **메서드** | public 비정적 메서드는 허용 목록만 가능 |

허용되는 메서드: `Equals`, `GetHashCode`, `ToString`, `Create`, `Validate`, 연산자, Getter 메서드

## Architecture Test Suites

사전 구축된 테스트 스위트로, 상속만으로 검증된 아키텍처 규칙을 즉시 적용합니다.

### DomainArchitectureTestSuite

도메인 레이어의 DDD 전술 패턴을 검증하는 21개 테스트를 제공합니다.

#### 추상 프로퍼티 (필수 오버라이드)

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Architecture` | `Architecture` | ArchLoader로 로딩한 어셈블리 아키텍처 |
| `DomainNamespace` | `string` | 도메인 타입이 위치하는 루트 네임스페이스 |

#### 가상 프로퍼티 (선택적 오버라이드)

| 프로퍼티 | 기본값 | 설명 |
|----------|--------|------|
| `ValueObjectExcludeFromFactoryMethods` | `[]` | Create/Validate 팩토리 메서드 검증에서 제외할 ValueObject 타입 |
| `DomainServiceAllowedFieldTypes` | `[]` | DomainService의 `RequireNoInstanceFields`에서 허용할 필드 타입 |

#### 테스트 목록 (21개)

| 카테고리 | 테스트 | 검증 내용 |
|----------|--------|-----------|
| **Entity** | `AggregateRoot_ShouldBe_PublicSealedClass` | public sealed, not static |
| **Entity** | `AggregateRoot_ShouldHave_CreateAndCreateFromValidated` | Create/CreateFromValidated 정적 팩토리 메서드 |
| **Entity** | `AggregateRoot_ShouldHave_GenerateEntityIdAttribute` | `[GenerateEntityId]` 어트리뷰트 |
| **Entity** | `AggregateRoot_ShouldHave_AllPrivateConstructors` | 모든 생성자 private |
| **Entity** | `Entity_ShouldBe_PublicSealedClass` | public sealed, not static (AggregateRoot 제외) |
| **Entity** | `Entity_ShouldHave_CreateAndCreateFromValidated` | Create/CreateFromValidated 정적 팩토리 메서드 |
| **Entity** | `Entity_ShouldHave_AllPrivateConstructors` | 모든 생성자 private |
| **ValueObject** | `ValueObject_ShouldBe_PublicSealedWithPrivateConstructors` | public sealed + private 생성자 |
| **ValueObject** | `ValueObject_ShouldBe_Immutable` | ImmutabilityRule 6차원 불변성 |
| **ValueObject** | `ValueObject_ShouldHave_CreateFactoryMethod` | Create → `Fin<T>` 반환 |
| **ValueObject** | `ValueObject_ShouldHave_ValidateMethod` | Validate → `Validation<Error, T>` 반환 |
| **DomainEvent** | `DomainEvent_ShouldBe_SealedRecord` | sealed record |
| **DomainEvent** | `DomainEvent_ShouldHave_EventSuffix` | "Event" 접미사 |
| **Specification** | `Specification_ShouldBe_PublicSealed` | public sealed |
| **Specification** | `Specification_ShouldInherit_SpecificationBase` | `Specification<T>` 상속 |
| **Specification** | `Specification_ShouldResideIn_DomainLayer` | 도메인 네임스페이스 내 위치 |
| **DomainService** | `DomainService_ShouldBe_PublicSealed` | public sealed |
| **DomainService** | `DomainService_ShouldBe_Stateless` | 인스턴스 필드 없음 (허용 타입 제외) |
| **DomainService** | `DomainService_ShouldNotDependOn_IObservablePort` | IObservablePort 의존 금지 |
| **DomainService** | `DomainService_PublicMethods_ShouldReturn_Fin` | public 인스턴스 메서드 `Fin<T>` 반환 |
| **DomainService** | `DomainService_ShouldNotBe_Record` | record 타입 아님 |

### ApplicationArchitectureTestSuite

애플리케이션 레이어의 Command/Query 구조를 검증하는 4개 테스트를 제공합니다.

#### 추상 프로퍼티 (필수 오버라이드)

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Architecture` | `Architecture` | ArchLoader로 로딩한 어셈블리 아키텍처 |
| `ApplicationNamespace` | `string` | 애플리케이션 타입이 위치하는 루트 네임스페이스 |

#### 테스트 목록 (4개)

| 테스트 | 검증 내용 |
|--------|-----------|
| `Command_ShouldHave_ValidatorNestedClass` | Command에 Validator가 있으면 sealed + `AbstractValidator` 구현 |
| `Command_ShouldHave_UsecaseNestedClass` | Command에 Usecase 필수, sealed + `ICommandUsecase` 구현 |
| `Query_ShouldHave_ValidatorNestedClass` | Query에 Validator가 있으면 sealed + `AbstractValidator` 구현 |
| `Query_ShouldHave_UsecaseNestedClass` | Query에 Usecase 필수, sealed + `IQueryUsecase` 구현 |
