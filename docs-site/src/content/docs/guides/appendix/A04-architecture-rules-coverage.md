---
title: "아키텍처 규칙 검증 커버리지"
---

가이드 문서에 정의된 구현 규칙들이 `ArchitectureRules` 프레임워크를 통해 어디까지 자동 검증 가능한지 체계적으로 매핑한 문서입니다.

## 목차

- [요약](#요약)
- [1. ArchitectureRules API 요약](#1-architecturerules-api-요약)
- [2. 규칙 커버리지 매트릭스](#2-규칙-커버리지-매트릭스)
- [3. 검증 불가 규칙 분석](#3-검증-불가-규칙-분석)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// ClassValidator로 직접 검증
ArchRuleDefinition.Classes().That()
    .ImplementInterface(typeof(IValueObject))
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic().RequireSealed().RequireImmutable())
    .ThrowIfAnyFailures("ValueObject Rule");

// DelegateArchRule로 커스텀 규칙
var rule = new DelegateArchRule<Class>(
    "No infrastructure suffixes",
    (target, _) => target.Name.EndsWith("Dto")
        ? [new RuleViolation(target.FullName, "Naming", "...")]
        : []);

// CompositeArchRule로 규칙 합성
var composite = new CompositeArchRule<Class>(
    new ImmutabilityRule(), namingRule, dependencyRule);

// ArchUnitNET 필터로 레이어 의존성 검증
Types().That().ResideInNamespace(DomainNamespace)
    .Should().NotDependOnAnyTypesThat()
    .ResideInNamespace(ApplicationNamespace)
    .Check(Architecture);
```

### 주요 절차

**1. 규칙 검증 가능 여부 판별:**
1. ClassValidator/InterfaceValidator/MethodValidator API에 1:1 매핑되는가? → ✅ 직접 지원
2. `IArchRule<T>` / `DelegateArchRule<T>`로 구현 가능한가? → 🔧 커스텀 규칙
3. ArchUnitNET의 `ArchRuleDefinition` 필터로 검증 가능한가? → ⚠️ ArchUnitNET 필터
4. 어셈블리 메타데이터/IL 수준에서 판별 불가능한가? → ❌ 검증 불가

**2. 새 아키텍처 규칙 추가:**
1. 가이드 문서에서 규칙 식별
2. 이 매트릭스에서 검증 가능 여부 확인
3. 적합한 API 선택 (✅ > 🔧 > ⚠️ 순)
4. 테스트 클래스에 규칙 추가

### 주요 개념

**1. 커버리지 통계**

| 검증 상태 | 기호 | 규칙 수 | 비율 |
|----------|------|---------|------|
| 직접 지원 | ✅ | 55 | 57% |
| 커스텀 규칙 | 🔧 | 9 | 9% |
| ArchUnitNET 필터 | ⚠️ | 10 | 10% |
| 검증 불가 | ❌ | 23 | 24% |
| **합계** | | **97** | **100%** |

**2. 검증 가능 합계:** 74건 (76%) — 프레임워크로 자동 검증 가능

**3. ArchitectureRules API 구성**

| 컴포넌트 | 역할 |
|----------|------|
| `ClassValidator` | 클래스 수준 규칙 (visibility, modifier, inheritance, constructor, method, property, naming) |
| `InterfaceValidator` | 인터페이스 수준 규칙 (naming, inheritance, method) |
| `MethodValidator` | 메서드 수준 규칙 (visibility, static, return type, parameter) |
| `IArchRule<T>` | 커스텀 규칙 확장 인터페이스 |
| `DelegateArchRule<T>` | 람다 기반 커스텀 규칙 |
| `CompositeArchRule<T>` | 여러 규칙의 AND 합성 |
| `ImmutabilityRule` | 불변성 종합 검증 (6가지 차원) |




## 1. ArchitectureRules API 요약

### ClassValidator

| 카테고리 | 메서드 | 설명 |
|----------|--------|------|
| Visibility | `RequirePublic()`, `RequireInternal()` | 접근 제한자 |
| Modifier | `RequireSealed()`, `RequireNotSealed()` | sealed 여부 |
| Modifier | `RequireStatic()`, `RequireNotStatic()` | static 여부 |
| Modifier | `RequireAbstract()`, `RequireNotAbstract()` | abstract 여부 |
| Type | `RequireRecord()`, `RequireNotRecord()` | record 여부 |
| Type | `RequireAttribute(string)` | 특정 어트리뷰트 존재 |
| Inheritance | `RequireInherits(Type)` | 기본 클래스 상속 |
| Inheritance | `RequireImplements(Type)` | 인터페이스 구현 |
| Inheritance | `RequireImplementsGenericInterface(string)` | 제네릭 인터페이스 구현 |
| Constructor | `RequireAllPrivateConstructors()` | 모든 생성자 private |
| Constructor | `RequirePrivateAnyParameterlessConstructor()` | 매개변수 없는 private 생성자 |
| Property | `RequireNoPublicSetters()` | public setter 금지 |
| Property | `RequireOnlyPrimitiveProperties(params string[])` | 원시 타입 프로퍼티만 허용 |
| Property | `RequireProperty(string)` | 특정 프로퍼티 존재 |
| Field | `RequireNoInstanceFields()` | 인스턴스 필드 금지 |
| Method | `RequireMethod(string, Action<MethodValidator>)` | 특정 메서드 검증 |
| Method | `RequireAllMethods(Action<MethodValidator>)` | 모든 메서드 검증 |
| Method | `RequireAllMethods(Func<MethodMember, bool>, Action<MethodValidator>)` | 필터링된 메서드 검증 |
| Method | `RequireMethodIfExists(string, Action<MethodValidator>)` | 메서드 존재 시 검증 |
| Naming | `RequireNameStartsWith(string)` | 이름 접두사 |
| Naming | `RequireNameEndsWith(string)` | 이름 접미사 |
| Naming | `RequireNameMatching(string)` | 정규식 패턴 매칭 |
| Dependency | `RequireNoDependencyOn(string)` | 특정 타입 의존 금지 |
| Nested | `RequireNestedClass(string, Action<ClassValidator>?)` | 중첩 클래스 필수 |
| Nested | `RequireNestedClassIfExists(string, Action<ClassValidator>?)` | 중첩 클래스 존재 시 검증 |
| Immutability | `RequireImmutable()` | 불변성 종합 검증 |
| Composition | `Apply(IArchRule<TType>)` | 커스텀 규칙 적용 |

### MethodValidator

| 카테고리 | 메서드 | 설명 |
|----------|--------|------|
| Visibility | `RequireVisibility(Visibility)` | 접근 제한자 |
| Modifier | `RequireStatic()`, `RequireNotStatic()` | static 여부 |
| Modifier | `RequireExtensionMethod()` | 확장 메서드 여부 |
| Modifier | `RequireVirtual()`, `RequireNotVirtual()` | virtual 여부 |
| Return | `RequireReturnType(Type)` | 반환 타입 검증 |
| Return | `RequireReturnTypeOfDeclaringClass()` | 선언 클래스 반환 |
| Return | `RequireReturnTypeOfDeclaringTopLevelClass()` | 최상위 클래스 반환 |
| Return | `RequireReturnTypeContaining(string)` | 반환 타입 이름 포함 |
| Parameter | `RequireParameterCount(int)` | 매개변수 개수 |
| Parameter | `RequireParameterCountAtLeast(int)` | 최소 매개변수 개수 |
| Parameter | `RequireFirstParameterTypeContaining(string)` | 첫 번째 매개변수 타입 |
| Parameter | `RequireAnyParameterTypeContaining(string)` | 임의 매개변수 타입 |

### 확장 포인트

| 타입 | 용도 | 예시 |
|------|------|------|
| `IArchRule<TType>` | 커스텀 규칙 인터페이스 | `ImmutabilityRule : IArchRule<Class>` |
| `DelegateArchRule<TType>` | 람다 기반 인라인 규칙 | 인프라 접미사 금지, 인프라 네임스페이스 의존 금지 |
| `CompositeArchRule<TType>` | 여러 규칙 AND 합성 | `ImmutabilityRule` + 네이밍 + 의존성 = ValueObject 핵심 규칙 |




## 2. 규칙 커버리지 매트릭스

검증 가능 상태 범례:

| 기호 | 상태 | 설명 |
|------|------|------|
| ✅ | 직접 지원 | ClassValidator / InterfaceValidator / MethodValidator 기존 API로 검증 |
| 🔧 | 커스텀 규칙 | `IArchRule<T>` 또는 `DelegateArchRule<T>`로 구현 가능 |
| ⚠️ | ArchUnitNET 필터 | `ArchRuleDefinition` 필터 + `Should()` 체인으로 검증 |
| ❌ | 검증 불가 | 사유 명시 |

### 2.1 Domain Layer 규칙

#### Value Object

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `public sealed` 클래스 | 05a | ✅ | `RequirePublic()` + `RequireSealed()` |
| 불변성 (6가지 차원) | 05a | ✅ | `RequireImmutable()` (`ImmutabilityRule`) |
| 모든 생성자 private | 05a | ✅ | `RequireAllPrivateConstructors()` |
| `Create()` 팩토리 (public static, `Fin<T>` 반환) | 05a | ✅ | `RequireMethod("Create", m => m.RequireStatic().RequireReturnType(typeof(Fin<>)))` |
| `Validate()` 메서드 (public static, `Validation<,>` 반환) | 05a | ✅ | `RequireMethod("Validate", m => m.RequireStatic().RequireReturnType(typeof(Validation<,>)))` |
| `IValueObject` 인터페이스 구현 | 05a | ⚠️ | ArchUnitNET 필터: `.ImplementInterface(typeof(IValueObject))` |
| `IEquatable<>` 구현 | 05a | ✅ | `RequireImplements(typeof(IEquatable<>))` |
| `SimpleValueObject<T>` 또는 `ValueObject` 상속 | 05a | ✅ | `RequireInherits(typeof(SimpleValueObject<>))` |
| 중첩 `DomainErrors` 클래스 규칙 | 08b | ✅ | `RequireNestedClassIfExists("DomainErrors", ...)` |
| implicit operator 정의 | 05a | 🔧 | `DelegateArchRule`: `op_Implicit` 메서드 존재 확인 |
| `ValidationRules<T>` 사용 | 05a | ❌ | 소스 코드 분석 필요 (IL에서 호출 패턴 추적 불가) |
| `CreateFromValidation()` 패턴 사용 | 05a | ❌ | 소스 코드 분석 필요 |
| 인프라 접미사 금지 (Dto, ViewModel 등) | 04 | 🔧 | `DelegateArchRule`: 금지 접미사 패턴 매칭 |
| 인프라 네임스페이스 의존 금지 | 04 | 🔧 | `DelegateArchRule`: 의존성 네임스페이스 검사 |

#### Entity / Aggregate Root

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `public sealed` 클래스 | 06b | ✅ | `RequirePublic()` + `RequireSealed()` |
| Not static | 06b | ✅ | `RequireNotStatic()` |
| `AggregateRoot<TId>` 상속 | 06b | ✅ | `RequireInherits(typeof(AggregateRoot<>))` |
| `Entity<TId>` 상속 (일반 Entity) | 06b | ✅ | `RequireInherits(typeof(Entity<>))` |
| `[GenerateEntityId]` 어트리뷰트 | 06b | ✅ | `RequireAttribute("GenerateEntityId")` |
| 모든 생성자 private | 06b | ✅ | `RequireAllPrivateConstructors()` |
| 매개변수 없는 private 생성자 (ORM) | 06b | ✅ | `RequirePrivateAnyParameterlessConstructor()` |
| `Create()` 팩토리 (선언 클래스 반환) | 06b | ✅ | `RequireMethod("Create", m => m.RequireStatic().RequireReturnTypeOfDeclaringClass())` |
| `CreateFromValidated()` 팩토리 | 06b | ✅ | `RequireMethod("CreateFromValidated", m => m.RequireStatic().RequireReturnTypeOfDeclaringClass())` |
| `Create()`에서 `AddDomainEvent()` 호출 | 06b | ❌ | 소스 코드 분석 필요 (메서드 본문 검증 불가) |
| 가변 컬렉션 직접 노출 금지 | 06b | 🔧 | `DelegateArchRule`: public 프로퍼티의 반환 타입에서 `List<>` 등 탐지 |
| `IConcurrencyAware` 구현 (선택적) | 06c | ✅ | `RequireImplements(typeof(IConcurrencyAware))` |
| 인프라 접미사 금지 | 04 | 🔧 | `DelegateArchRule`: 금지 접미사 패턴 매칭 |
| 인프라 네임스페이스 의존 금지 | 04 | 🔧 | `DelegateArchRule`: 의존성 네임스페이스 검사 |

#### Domain Event

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `sealed record` | 07 | ✅ | `RequireSealed()` + `RequireRecord()` |
| 이름이 "Event"로 끝남 | 07 | ✅ | `RequireNameEndsWith("Event")` |
| `DomainEvent` 기반 클래스 상속 | 07 | ✅ | `RequireInherits(typeof(DomainEvent))` |
| AggregateRoot 내부에 중첩 정의 | 07 | 🔧 | `DelegateArchRule`: 타입의 `DeclaringType` 검사 |
| 과거 시제 이름 (Created, Shipped 등) | 07 | ❌ | 의미론적 분석 필요 (자연어 시제 판별 불가) |

#### Domain Service

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `public sealed` 클래스 | 09 | ✅ | `RequirePublic()` + `RequireSealed()` |
| `IDomainService` 구현 | 09 | ⚠️ | ArchUnitNET 필터: `.ImplementInterface(typeof(IDomainService))` |
| Stateless (인스턴스 필드 없음) | 09 | ✅ | `RequireNoInstanceFields()` |
| `IObservablePort` 의존 금지 | 09 | ✅ | `RequireNoDependencyOn("IObservablePort")` |
| Public 메서드 `Fin` 반환 | 09 | ✅ | `RequireAllMethods(filter, m => m.RequireReturnTypeContaining("Fin"))` |
| Record 금지 | 09 | ✅ | `RequireNotRecord()` |
| 순수 함수, I/O 없음 | 09 | ❌ | 런타임 동작 검증 (정적 분석 한계) |

#### Specification

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `public sealed` 클래스 | 10 | ✅ | `RequirePublic()` + `RequireSealed()` |
| `Specification<T>` 상속 | 10 | ✅ | `RequireInherits(typeof(Specification<>))` |
| Domain 레이어에 위치 | 10 | ⚠️ | ArchUnitNET: `.Should().ResideInNamespaceMatching()` |
| `ToExpression()` 오버라이드 | 10 | 🔧 | `DelegateArchRule`: 메서드 존재 및 virtual override 확인 |

#### Error (Domain)

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| 중첩 `DomainErrors` 클래스 (internal sealed) | 08b | ✅ | `RequireNestedClassIfExists("DomainErrors", n => n.RequireInternal().RequireSealed())` |
| DomainErrors 메서드 (public static, Error 반환) | 08b | ✅ | `.RequireAllMethods(m => m.RequireStatic().RequireReturnType(typeof(Error)))` |
| `DomainError.For<T>()` 팩토리 패턴 사용 | 08a | ❌ | 소스 코드 분석 필요 (메서드 본문의 호출 패턴) |
| 에러 코드 형식 (`DomainErrors.Type.Name`) | 08a | ❌ | 런타임 검증 필요 (단위 테스트로 검증) |
| Custom 에러 타입이 sealed record | 08b | 🔧 | `DelegateArchRule`: 특정 네임스페이스의 Error 파생 타입 검사 |

### 2.2 Application Layer 규칙

#### Usecase

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| Command 클래스 sealed | 11 | ✅ | `RequireSealed()` |
| Command 내부 Request (sealed record, `ICommandRequest`) | 11 | ✅ | `RequireNestedClass("Request", n => n.RequireSealed().RequireRecord().RequireImplementsGenericInterface("ICommandRequest"))` |
| Command 내부 Response (sealed record) | 11 | ✅ | `RequireNestedClass("Response", n => n.RequireSealed().RequireRecord())` |
| Command 내부 Usecase (sealed, `ICommandUsecase`) | 11 | ✅ | `RequireNestedClass("Usecase", n => n.RequireSealed().RequireImplementsGenericInterface("ICommandUsecase"))` |
| Query 클래스 sealed | 11 | ✅ | `RequireSealed()` |
| Query 내부 Request (sealed record, `IQueryRequest`) | 11 | ✅ | `RequireNestedClass("Request", n => n.RequireSealed().RequireRecord().RequireImplementsGenericInterface("IQueryRequest"))` |
| Query 내부 Response (sealed record) | 11 | ✅ | `RequireNestedClass("Response", n => n.RequireSealed().RequireRecord())` |
| Query 내부 Usecase (sealed, `IQueryUsecase`) | 11 | ✅ | `RequireNestedClass("Usecase", n => n.RequireSealed().RequireImplementsGenericInterface("IQueryUsecase"))` |
| Request/Response는 원시 타입 프로퍼티만 허용 | 11 | ✅ | `RequireOnlyPrimitiveProperties()` |
| Usecase는 `ValueTask<FinResponse<T>>` 반환 | 11 | ❌ | 인터페이스가 강제하므로 별도 검증 불필요 (컴파일러 보장) |
| `FinT<IO, T>` LINQ 체인 사용 | 11 | ❌ | 소스 코드 분석 필요 |

#### CQRS

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| Query Usecase가 IRepository에 의존 금지 | 11 | ⚠️ | ArchUnitNET: `.Should().NotDependOnAnyTypesThat().HaveNameEndingWith("Repository")` |
| Command 이름이 "Command"로 끝남 | 11 | ⚠️ | ArchUnitNET 필터: `.HaveNameEndingWith("Command")` |
| Query 이름이 "Query"로 끝남 | 11 | ⚠️ | ArchUnitNET 필터: `.HaveNameEndingWith("Query")` |

#### Validator

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| Validator가 `AbstractValidator` 상속 | 14a | ✅ | `RequireImplementsGenericInterface("AbstractValidator")` |
| Sealed 클래스 | 14a | ✅ | `RequireSealed()` |
| Usecase 내부에 중첩 (선택적) | 11 | ✅ | `RequireNestedClassIfExists("Validator", ...)` |

#### Error (Application)

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `ApplicationError.For<T>()` 팩토리 패턴 사용 | 08b | ❌ | 소스 코드 분석 필요 |
| 에러 코드 형식 (`ApplicationErrors.Usecase.Name`) | 08b | ❌ | 런타임 검증 필요 |
| Shared Application DTO (sealed record, primitive) | 17 | ✅ | `RequireSealed()` + `RequireRecord()` + `RequireOnlyPrimitiveProperties()` |

### 2.3 Adapter Layer 규칙

#### Port 인터페이스

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| Repository 네이밍 (`I` 접두사) | 12 | ✅ | `RequireNameStartsWith("I")` (InterfaceValidator) |
| Repository가 `IObservablePort` 구현 | 12 | ✅ | `RequireImplements(typeof(IObservablePort))` |
| Repository 메서드가 `FinT` 반환 | 12 | ✅ | `RequireAllMethods(m => m.RequireReturnTypeContaining("FinT"))` |
| Domain Port는 Domain 레이어에 위치 | 12 | ⚠️ | ArchUnitNET: `.Should().ResideInNamespaceMatching()` |
| Application Port는 Application 레이어에 위치 | 12 | ⚠️ | ArchUnitNET: `.Should().ResideInNamespaceMatching()` |

#### Adapter 구현

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `[GenerateObservablePort]` 어트리뷰트 | 13 | ✅ | `RequireAttribute("GenerateObservablePort")` |
| 메서드가 virtual | 13 | ✅ | `RequireAllMethods(m => m.RequireVirtual())` |
| `RequestCategory` 프로퍼티 존재 | 13 | ✅ | `RequireProperty("RequestCategory")` |
| Not sealed (Observable 상속 허용) | 13 | ✅ | `RequireNotSealed()` |
| `AdapterError.For<T>()` 팩토리 사용 | 08c | ❌ | 소스 코드 분석 필요 |

#### Persistence (Mapper / Model)

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| Mapper는 `internal static` 클래스 | 13 | ✅ | `RequireInternal()` + `RequireStatic()` |
| `ToModel()` 확장 메서드 | 13 | ✅ | `RequireMethod("ToModel", m => m.RequireStatic().RequireExtensionMethod())` |
| `ToDomain()` 확장 메서드 | 13 | ✅ | `RequireMethod("ToDomain", m => m.RequireStatic().RequireExtensionMethod())` |
| Model은 public, not sealed, POCO | 13 | ✅ | `RequirePublic()` + `RequireNotSealed()` + `RequireOnlyPrimitiveProperties()` |
| EF Core Configuration (FluentAPI) | 13 | ❌ | 소스 코드 분석 필요 |

#### Presentation (Endpoint)

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| Endpoint는 sealed | 13 | ✅ | `RequireSealed()` |
| 중첩 Request (sealed record, primitive) | 13 | ✅ | `RequireNestedClassIfExists("Request", ...)` |
| 중첩 Response (sealed record, primitive) | 13 | ✅ | `RequireNestedClassIfExists("Response", ...)` |
| FastEndpoints 상속 | 13 | ❌ | 제네릭 타입 매칭 복잡 (다양한 Endpoint 기반 클래스) |

#### Pipeline / DI

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| 등록 메서드 네이밍 (`RegisterAdapter{Category}`) | 14a | ❌ | 확장 메서드 네이밍은 정적 분석 가능하나, 관례 수준 |
| 미들웨어 등록 순서 | 14a | ❌ | 런타임 동작 검증 (코드 실행 순서) |
| `OptionsConfigurator<T>` 패턴 사용 | 14a | ❌ | 소스 코드 분석 필요 |

#### Error (Adapter)

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `AdapterError.For<T>()` 팩토리 패턴 사용 | 08c | ❌ | 소스 코드 분석 필요 |
| 에러 코드 형식 (`AdapterErrors.Adapter.Name`) | 08c | ❌ | 런타임 검증 필요 |

### 2.4 Testing 규칙

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| 테스트 프로젝트 폴더가 소스 구조를 미러링 | 15a | ❌ | 파일/디렉토리 구조 검증 (어셈블리 메타데이터 범위 밖) |
| 테스트 네이밍: `{Method}_{ShouldBehavior}_{Condition}` | 15a | ❌ | 테스트 메서드 이름은 IL에 있지만, 의미론적 패턴 검증 어려움 |
| AAA (Arrange-Act-Assert) 패턴 | 15a | ❌ | 소스 코드 분석 필요 (코드 구조/주석 검증 불가) |
| `xunit.runner.json` 병렬 설정 | 15a | ❌ | 설정 파일 검증 (JSON 파일 파싱 필요) |
| Shouldly assertion 사용 | 15a | ❌ | 소스 코드 분석 필요 (특정 라이브러리 호출 추적) |

### 2.5 Observability 규칙

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| Signal 접두사 (Logging, Tracing, Metrics) | 18b | 🔧 | `DelegateArchRule`: 클래스 이름 패턴 매칭 |
| Component 접미사 (Logger, Span, Metric) | 18b | 🔧 | `DelegateArchRule`: 클래스 이름 패턴 매칭 |
| Logger 메서드 네이밍 (`Log{Context}{Phase}{Status}`) | 18b | ❌ | 소스 코드 분석 필요 (`LoggerMessage` 어트리뷰트 파싱) |
| `[LoggerMessage]` 어트리뷰트 사용 | 18b | ❌ | 메서드 수준 어트리뷰트 검증은 가능하나 partial 메서드 제약 |

### 2.6 프로젝트 구조 & 설정 규칙

#### 레이어 의존성

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| Domain → Application 의존 금지 | 01 | ⚠️ | ArchUnitNET: `Types().Should().NotDependOnAnyTypesThat()` |
| Domain → Adapter 의존 금지 | 01 | ⚠️ | ArchUnitNET: `Types().Should().NotDependOnAnyTypesThat()` |
| Application → Adapter 의존 금지 | 01 | ⚠️ | ArchUnitNET: `Types().Should().NotDependOnAnyTypesThat()` |
| Adapter 간 교차 의존 금지 | 01 | ⚠️ | ArchUnitNET: `Types().Should().NotDependOnAnyTypesThat()` |

#### 프로젝트 파일

| 규칙 | 출처 | 검증 가능 | 사용 API / 불가 사유 |
|------|------|----------|---------------------|
| `AssemblyReference.cs` 존재 | 01 | ❌ | 파일 존재 확인 (어셈블리 메타데이터 범위 밖) |
| `Using.cs` 존재 | 01 | ❌ | 파일 존재 확인 |
| `csproj` 의존성 방향 | 01 | ❌ | 설정 파일 검증 (MSBuild XML 파싱 필요) |
| 네임스페이스 패턴 준수 | 01 | ⚠️ | ArchUnitNET: `.Should().ResideInNamespaceMatching()` |




## 3. 검증 불가 규칙 분석

### 3.1 소스 코드 분석 필요

IL(Intermediate Language) 수준에서는 메서드 본문의 로직 흐름을 추적할 수 없습니다. ArchUnitNET은 어셈블리 메타데이터(타입, 메서드 시그니처, 의존성)를 분석하지만, **메서드 내부에서 어떤 패턴을 사용하는지**는 판별할 수 없습니다.

| 검증 불가 규칙 | 대안 |
|---------------|------|
| `ValidationRules<T>` 사용 여부 | 코드 리뷰, Roslyn Analyzer |
| `DomainError.For<T>()` 팩토리 패턴 사용 | 단위 테스트에서 에러 코드 검증 |
| `Create()`에서 `AddDomainEvent()` 호출 | 단위 테스트에서 이벤트 발행 검증 |
| AAA 패턴, Shouldly 사용 | 코드 리뷰, Roslyn Analyzer |
| `FinT<IO, T>` LINQ 체인 사용 | 컴파일러가 타입 안전성 보장 |
| `[LoggerMessage]` 어트리뷰트 사용 | 로그 스냅샷 테스트로 간접 검증 |

### 3.2 파일/디렉토리 구조 검증

어셈블리 메타데이터에는 파일 시스템 구조 정보가 포함되지 않습니다.

| 검증 불가 규칙 | 대안 |
|---------------|------|
| 테스트 폴더가 소스 구조를 미러링 | 빌드 스크립트, CI 검증 |
| `AssemblyReference.cs` 존재 | 빌드 스크립트, CI 검증 |
| `Using.cs` 존재 | 빌드 스크립트, CI 검증 |

### 3.3 설정 파일 검증

`csproj`, `JSON`, `xunit.runner.json` 등의 설정 파일은 어셈블리 메타데이터 범위 밖입니다.

| 검증 불가 규칙 | 대안 |
|---------------|------|
| `csproj` 의존성 방향 | MSBuild 타겟, CI 검증 |
| `xunit.runner.json` 병렬 설정 | CI 검증 |
| EF Core FluentAPI 설정 | 통합 테스트에서 마이그레이션 검증 |

### 3.4 런타임 동작 검증

정적 분석으로는 코드 실행 순서나 런타임 동작을 검증할 수 없습니다.

| 검증 불가 규칙 | 대안 |
|---------------|------|
| Domain Service의 순수 함수/I/O 없음 | 코드 리뷰, 의존성 분석으로 간접 추론 |
| 미들웨어 등록 순서 | 통합 테스트 |
| 에러 코드 형식 일관성 | 단위 테스트의 `ShouldFailWithErrorCode()` |




## 트러블슈팅

### 커스텀 규칙에서 타입 정보가 부족할 때

**원인:** `DelegateArchRule<Class>`의 `target` 파라미터는 ArchUnitNET의 `Class` 타입으로, .NET Reflection의 `Type`과 다릅니다.

**해결:** `target.Dependencies`, `target.Members`, `target.Name`, `target.FullName` 등 ArchUnitNET이 제공하는 속성을 사용하세요. 필요한 경우 `Architecture` 파라미터에서 추가 타입을 조회할 수 있습니다.

### ArchUnitNET 필터에서 하위 네임스페이스가 포함되지 않을 때

**원인:** `ResideInNamespace()`는 정확한 네임스페이스만 매칭합니다.

**해결:** `ResideInNamespaceMatching($@"{BaseNamespace}\..*")`으로 정규식 기반 매칭을 사용하세요.

### `RequireAllMethods()`에서 Object 상속 메서드가 포함될 때

**원인:** `Equals`, `GetHashCode`, `ToString` 등 `System.Object`에서 상속된 메서드가 검증 대상에 포함됩니다.

**해결:** 필터 오버로드를 사용하세요:
```csharp
@class.RequireAllMethods(
    m => m.Visibility == Visibility.Public && m.MethodForm == MethodForm.Normal,
    method => method.RequireReturnTypeContaining("Fin"));
```




## FAQ

### Q1. 커버리지 76%의 의미는 무엇인가요?

가이드에 정의된 97개 구현 규칙 중 74개가 ArchitectureRules 프레임워크로 자동 검증 가능합니다. 나머지 24%는 소스 코드 분석, 파일 구조 검증, 런타임 동작 등 정적 분석의 한계를 넘어서는 규칙이며, 단위 테스트, 코드 리뷰, CI 검증 등으로 보완해야 합니다.

### Q2. ✅ 직접 지원과 🔧 커스텀 규칙의 차이는 무엇인가요?

✅ 직접 지원은 `ClassValidator.RequireSealed()`처럼 기존 API를 호출하면 되는 것이고, 🔧 커스텀 규칙은 `DelegateArchRule<T>` 또는 `IArchRule<T>` 구현을 직접 작성해야 하는 것입니다. 커스텀 규칙은 프로젝트별로 작성하며 재사용 가능합니다.

### Q3. ⚠️ ArchUnitNET 필터와 ✅ 직접 지원은 어떻게 다른가요?

⚠️ ArchUnitNET 필터는 `ArchRuleDefinition.Types().Should().NotDependOnAnyTypesThat()` 같은 ArchUnitNET 자체 DSL을 사용하는 것이고, ✅ 직접 지원은 `ValidateAllClasses()` 이후 `ClassValidator` 콜백에서 사용하는 Functorium 자체 API입니다. 둘 다 테스트 코드에서 사용하지만 API 레이어가 다릅니다.

### Q4. 검증 불가 규칙은 어떻게 보완하나요?

| 카테고리 | 보완 방법 |
|----------|----------|
| 소스 코드 분석 | Roslyn Analyzer, 코드 리뷰 |
| 파일/디렉토리 구조 | CI 빌드 스크립트, `dotnet new` 템플릿 |
| 설정 파일 | CI 검증 스크립트 |
| 런타임 동작 | 단위 테스트, 통합 테스트 |

### Q5. 새 규칙을 추가할 때 이 문서를 어떻게 활용하나요?

1. 가이드 문서에 새 규칙을 정의합니다
2. 이 매트릭스에서 해당 규칙의 검증 가능 여부를 판별합니다
3. ✅/🔧/⚠️이면 아키텍처 테스트 클래스에 추가합니다
4. ❌이면 대안(단위 테스트, CI 등)을 선택합니다
5. 이 문서의 매트릭스를 업데이트합니다

### Q6. CompositeArchRule은 언제 사용하나요?

여러 규칙을 AND로 합성하여 재사용할 때 사용합니다. 예를 들어, ValueObject의 핵심 규칙(불변성 + 네이밍 + 의존성)을 하나의 `CompositeArchRule`로 합성하면, 여러 테스트에서 일관되게 적용할 수 있습니다.

```csharp
private static readonly CompositeArchRule<Class> s_valueObjectCoreRule = new(
    new ImmutabilityRule(),
    s_domainNamingRule,
    s_noInfrastructureDependencyRule);
```

### Q7. SingleHost 예제에는 어떤 아키텍처 테스트가 있나요?

`Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Architecture/`에 13개 테스트 클래스가 구현되어 있습니다. [16-testing-library.md](./16-testing-library.md)의 "SingleHost 아키텍처 테스트 인벤토리"를 참조하세요.

---

## 참고 문서

- [16-testing-library.md](./16-testing-library.md) — Functorium.Testing 라이브러리 가이드 (ArchitectureRules API 상세)
- [01-project-structure.md](./01-project-structure.md) — 프로젝트 구조 (레이어 의존성, 네임스페이스)
- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) — DDD 전술적 설계 개요
- [05a-value-objects.md](./05a-value-objects.md) — 값 객체 규칙
- [06b-entity-aggregate-core.md](./06b-entity-aggregate-core.md) — Entity/Aggregate 핵심 패턴
- [07-domain-events.md](./07-domain-events.md) — 도메인 이벤트
- [08a-error-system.md](./08a-error-system.md) — 에러 시스템
- [09-domain-services.md](./09-domain-services.md) — 도메인 서비스
- [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) — Usecase와 CQRS
- [12-ports.md](./12-ports.md) — Port 아키텍처
- [13-adapters.md](./13-adapters.md) — Adapter 구현
