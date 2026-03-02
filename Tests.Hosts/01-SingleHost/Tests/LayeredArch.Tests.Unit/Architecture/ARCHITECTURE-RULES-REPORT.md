# Architecture Rules 검증 보고서

> Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Architecture

## 1. 적용된 검증 규칙 (총 42개 테스트)

### 1-1. Layer Dependency (6개)

| 테스트 | 검증 내용 | Validator |
|--------|-----------|-----------|
| `DomainLayer_ShouldNotDependOn_ApplicationLayer` | Domain → Application 의존 금지 | ArchUnitNET 내장 |
| `DomainLayer_ShouldNotDependOn_AdapterLayer` | Domain → Adapter(Persistence/Infrastructure/Presentation) 의존 금지 | ArchUnitNET 내장 |
| `ApplicationLayer_ShouldNotDependOn_AdapterLayer` | Application → Adapter 의존 금지 | ArchUnitNET 내장 |
| `PresentationAdapter_ShouldNotDependOn_OtherAdapters` | Presentation → Persistence/Infrastructure 의존 금지 | ArchUnitNET 내장 |
| `PersistenceAdapter_ShouldNotDependOn_OtherAdapters` | Persistence → Presentation/Infrastructure 의존 금지 | ArchUnitNET 내장 |
| `InfrastructureAdapter_ShouldNotDependOn_OtherAdapters` | Infrastructure → Presentation/Persistence 의존 금지 | ArchUnitNET 내장 |

### 1-2. Entity / AggregateRoot (8개)

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| `AggregateRoot_ShouldBe_PublicSealedClass` | public sealed, not static | `RequirePublic`, `RequireSealed`, `RequireNotStatic` |
| `AggregateRoot_ShouldInherit_AggregateRootBase` | AggregateRoot<> 상속 | `RequireInherits` |
| `AggregateRoot_ShouldHave_CreateAndCreateFromValidated` | Create/CreateFromValidated 팩토리 메서드 | `RequireMethod`, `RequireVisibility`, `RequireStatic`, `RequireReturnTypeOfDeclaringClass` |
| **`AggregateRoot_ShouldHave_GenerateEntityIdAttribute`** | [GenerateEntityId] 속성 필수 | `RequireAttribute` |
| **`AggregateRoot_ShouldHave_AllPrivateConstructors`** | 모든 생성자 private | `RequireAllPrivateConstructors` |
| `Entity_ShouldBe_PublicSealedClass` | public sealed, not static | `RequirePublic`, `RequireSealed`, `RequireNotStatic` |
| `Entity_ShouldHave_CreateAndCreateFromValidated` | Create/CreateFromValidated 팩토리 메서드 | `RequireMethod`, `RequireVisibility`, `RequireStatic`, `RequireReturnTypeOfDeclaringClass` |
| **`Entity_ShouldHave_AllPrivateConstructors`** | 모든 생성자 private | `RequireAllPrivateConstructors` |

### 1-3. ValueObject (4개)

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| `ValueObject_ShouldBe_PublicSealedWithPrivateConstructors` | public sealed + private 생성자 | `RequirePublic`, `RequireSealed`, `RequireAllPrivateConstructors` |
| `ValueObject_ShouldBe_Immutable` | 불변성 종합 검증 (6차원) | `RequireImmutable` |
| `ValueObject_ShouldHave_CreateFactoryMethod` | Create 정적 팩토리, Fin<> 반환 | `RequireMethod`, `RequireVisibility`, `RequireStatic`, `RequireReturnType` |
| `ValueObject_ShouldHave_ValidateMethod` | Validate 정적 메서드, Validation<,> 반환 | `RequireMethod`, `RequireVisibility`, `RequireStatic`, `RequireReturnType` |

### 1-4. DomainEvent (2개)

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| `DomainEvent_ShouldBe_SealedRecord` | sealed record | `RequireSealed`, `RequireRecord` |
| `DomainEvent_ShouldHave_EventSuffix` | "Event" 접미사 | `RequireNameEndsWith` |

### 1-5. DomainService (5개)

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| `DomainService_ShouldBe_PublicSealed` | public sealed | `RequirePublic`, `RequireSealed` |
| `DomainService_ShouldBe_Stateless` | 인스턴스 필드 없음 | `RequireNoInstanceFields` |
| `DomainService_ShouldNotDependOn_IObservablePort` | IObservablePort 의존 금지 | `RequireNoDependencyOn` |
| **`DomainService_PublicMethods_ShouldReturn_Fin`** | 모든 public instance 메서드가 Fin 반환 | `RequireAllMethods(filter, ...)`, `RequireReturnTypeContaining` |
| **`DomainService_ShouldNotBe_Record`** | record 타입 금지 | `RequireNotRecord` |

### 1-6. Adapter (4개)

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| `Adapter_ShouldHave_VirtualMethods` | 모든 메서드 virtual | `RequireAllMethods`, `RequireVirtual` |
| `Adapter_ShouldHave_RequestCategoryProperty` | RequestCategory 프로퍼티 필수 | `RequireProperty` |
| `Adapter_ShouldHave_GenerateObservablePortAttribute` | [GenerateObservablePort] 속성 필수 | `RequireAttribute` |
| **`Adapter_ShouldNotBe_Sealed`** | sealed 금지 (Pipeline 상속) | `RequireNotSealed` |

### 1-7. Port Interface (3개) - 신규

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| **`RepositoryPort_ShouldFollow_NamingConvention`** | "I" 접두사 네이밍 | `RequireNameStartsWith` |
| **`RepositoryPort_ShouldImplement_IObservablePort`** | IObservablePort 구현 | `RequireImplements` |
| **`RepositoryPort_Methods_ShouldReturn_FinT`** | 모든 메서드 FinT 반환 | `RequireAllMethods`, `RequireReturnTypeContaining` |

### 1-8. CQRS (1개)

| 테스트 | 검증 내용 | Validator |
|--------|-----------|-----------|
| `QueryUsecase_ShouldNotDependOn_IRepository` | Query → Repository 의존 금지 | ArchUnitNET 내장 |

### 1-9. Usecase (4개)

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| `Command_ShouldHave_ValidatorNestedClass` | Command.Validator 중첩 클래스 | `RequireNestedClassIfExists`, `RequireSealed`, `RequireImplementsGenericInterface` |
| `Command_ShouldHave_UsecaseNestedClass` | Command.Usecase 중첩 클래스 | `RequireNestedClass`, `RequireSealed`, `RequireImplementsGenericInterface` |
| `Query_ShouldHave_ValidatorNestedClass` | Query.Validator 중첩 클래스 | `RequireNestedClassIfExists`, `RequireSealed`, `RequireImplementsGenericInterface` |
| `Query_ShouldHave_UsecaseNestedClass` | Query.Usecase 중첩 클래스 | `RequireNestedClass`, `RequireSealed`, `RequireImplementsGenericInterface` |

### 1-10. DTO / Mapper (5개)

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| `PersistenceMapper_ShouldBe_InternalStaticWithMethods` | internal static, ToModel/ToDomain 확장 메서드 | `RequireInternal`, `RequireStatic`, `RequireMethod`, `RequireExtensionMethod` |
| `PersistenceModel_ShouldBe_PublicPocoClass` | public, not sealed, primitive 프로퍼티만 | `RequirePublic`, `RequireNotSealed`, `RequireOnlyPrimitiveProperties` |
| `ApplicationUsecase_ShouldHave_NestedRequestResponse` | Command/Query의 Request/Response 중첩 record | `RequireNestedClass`, `RequireRecord`, `RequireOnlyPrimitiveProperties`, `RequireImplementsGenericInterface` |
| `SharedApplicationDto_ShouldBe_SealedRecordWithPrimitives` | sealed record, primitive만 | `RequireSealed`, `RequireRecord`, `RequireOnlyPrimitiveProperties` |
| `PresentationEndpoint_ShouldHave_SealedRecordDtos` | Endpoint의 Request/Response 중첩 record | `RequireNestedClassIfExists`, `RequireRecord`, `RequireOnlyPrimitiveProperties` |

### 1-11. Specification (3개)

| 테스트 | 검증 내용 | 사용 메서드 |
|--------|-----------|-------------|
| `Specification_ShouldBe_PublicSealed` | public sealed | `RequirePublic`, `RequireSealed` |
| `Specification_ShouldInherit_SpecificationBase` | Specification<> 상속 | `RequireInherits` |
| `Specification_ShouldResideIn_DomainLayer` | Domain 레이어 배치 | ArchUnitNET 내장 |

---

## 2. 이번 개선에서 새로 추가된 검증 능력

### 2-1. MethodValidator 신규 메서드

| 메서드 | 용도 | 테스트 활용 |
|--------|------|-------------|
| `RequireNotStatic()` | 인스턴스 메서드 강제 | - (가용, 미사용) |
| `RequireNotVirtual()` | virtual 금지 | - (가용, 미사용) |
| `RequireParameterCount(int)` | 정확한 파라미터 수 검증 | - (가용, 미사용) |
| `RequireParameterCountAtLeast(int)` | 최소 파라미터 수 검증 | - (가용, 미사용) |
| `RequireFirstParameterTypeContaining(string)` | 첫 파라미터 타입 검증 | - (가용, 미사용) |
| `RequireAnyParameterTypeContaining(string)` | 임의 파라미터 타입 포함 검증 | - (가용, 미사용) |

### 2-2. ClassValidator 신규 메서드

| 메서드 | 용도 | 테스트 활용 |
|--------|------|-------------|
| `RequireAbstract()` | 추상 클래스 강제 | - (가용, 미사용) |
| `RequireNotAbstract()` | 구체 클래스 강제 | - (가용, 미사용) |
| `RequireNotRecord()` | record 금지 | `DomainService_ShouldNotBe_Record` |
| `RequireNoPublicSetters()` | public setter 금지 | - (가용, 미사용) |
| `RequireAllMethods(Func<MethodMember, bool>, Action<MethodValidator>)` | 필터 기반 메서드 검증 | `DomainService_PublicMethods_ShouldReturn_Fin` |

### 2-3. InterfaceValidator (완전 신규)

| 메서드 | 용도 | 테스트 활용 |
|--------|------|-------------|
| `RequireNameStartsWith(string)` | 접두사 네이밍 검증 | `RepositoryPort_ShouldFollow_NamingConvention` |
| `RequireNameEndsWith(string)` | 접미사 네이밍 검증 | - (가용, 미사용) |
| `RequireNameMatching(string)` | 정규식 네이밍 검증 | - (가용, 미사용) |
| `RequireImplements(Type)` | 인터페이스 구현 검증 | `RepositoryPort_ShouldImplement_IObservablePort` |
| `RequireImplementsGenericInterface(string)` | 제네릭 인터페이스 구현 검증 | - (가용, 미사용) |
| `RequireMethod(string, Action<MethodValidator>)` | 특정 메서드 검증 | - (가용, 미사용) |
| `RequireAllMethods(Action<MethodValidator>)` | 전체 메서드 검증 | `RepositoryPort_Methods_ShouldReturn_FinT` |
| `RequireAllMethods(Func, Action)` | 필터 기반 메서드 검증 | - (가용, 미사용) |
| `RequireMethodIfExists(string, Action)` | 조건부 메서드 검증 | - (가용, 미사용) |
| `RequireProperty(string)` | 프로퍼티 존재 검증 | - (가용, 미사용) |
| `RequireNoDependencyOn(string)` | 의존성 금지 검증 | - (가용, 미사용) |

### 2-4. ArchitectureValidationEntryPoint 확장

| 메서드 | 용도 |
|--------|------|
| `ValidateAllInterfaces(...)` | 인터페이스 집합에 대한 검증 규칙 일괄 적용 |
| `ValidateAllInterfaces(..., bool verbose)` | 검증 대상 로깅 포함 |

---

## 3. 기능적 제약으로 적용하지 못한 검증 규칙

### 3-1. ArchUnitNET v0.13.x 제한

| 제약 | 영향 | 우회 방법 |
|------|------|-----------|
| `Interfaces().That().ResideInNamespace()`가 하위 네임스페이스 미포함 | Port 인터페이스를 네임스페이스로 필터링 불가 | `HaveNameEndingWith` 등 이름 기반 필터로 우회 |
| `MethodMember`에 `Parameters` 프로퍼티 미노출 | 메서드 파라미터 직접 접근 불가 | `System.Reflection`으로 런타임 리졸브 (`ResolveReflectionMethod`) |
| `IsVirtual` 속성이 항상 false 반환 (버그) | virtual 검증 불가 | `System.Reflection`으로 우회 (기존 방식 유지) |

### 3-2. 현재 테스트 대상 부재로 미적용

다음 규칙은 프레임워크에 구현되어 **사용 가능 상태**이지만, 현재 LayeredArch 프로젝트에 해당하는 대상이 없거나 테스트 케이스가 불필요하여 미적용:

| 규칙 | 적용 시나리오 | 미적용 사유 |
|------|--------------|-------------|
| `RequireNotStatic()` (MethodValidator) | Entity 커맨드 메서드가 인스턴스여야 할 때 | 현재 커맨드 메서드가 없는 Entity 구조 |
| `RequireNotVirtual()` (MethodValidator) | DomainService 메서드 virtual 금지 | DomainService가 sealed이므로 자동 보장 |
| `RequireParameterCount(int)` | VO Create(1개 primitive) 검증 | VO별 파라미터 수가 다양하여 일괄 적용 곤란 |
| `RequireParameterCountAtLeast(int)` | CreateFromValidated(최소 EntityId) 검증 | Entity별 파라미터 구성이 다양 |
| `RequireFirstParameterTypeContaining(string)` | CreateFromValidated 첫 파라미터 EntityId 검증 | Entity별 EntityId 타입이 다양 |
| `RequireAnyParameterTypeContaining(string)` | Port 메서드 CancellationToken 포함 검증 | Repository Port 메서드에 CancellationToken이 없는 설계 |
| `RequireAbstract()` | 베이스 클래스 패턴 검증 | 현재 테스트 대상의 추상 클래스 검증 불필요 |
| `RequireNotAbstract()` | 구체 클래스 강제 | `.AreNotAbstract()` fluent 필터로 대체 |
| `RequireNoPublicSetters()` | Entity/VO public setter 금지 | `RequireImmutable()`이 이미 포괄적으로 검증 |

### 3-3. 구조적 한계로 미구현

| 검증 규칙 | 제약 설명 |
|-----------|-----------|
| Application Port 인터페이스(IQuery 등) 메서드 시그니처 검증 | `ResideInNamespace` 미동작으로 Application 네임스페이스 필터링 불가. 이름 패턴도 일관적이지 않아 단일 규칙으로 표현 곤란 |
| 인터페이스 제네릭 타입 파라미터 검증 | ArchUnitNET이 제네릭 타입 파라미터 정보를 노출하지 않음 |
| 메서드 반환 타입의 제네릭 인자 검증 (예: `Fin<Product>` 에서 Product 추출) | FullName 문자열 매칭만 가능, 타입 안전한 제네릭 인자 접근 불가 |

---

## 4. 파일 변경 요약

### 수정된 프레임워크 파일

| 파일 | 변경 내용 |
|------|-----------|
| `MethodValidator.cs` | 생성자 `List<string>` 직접 수신으로 변경, `ResolveReflectionMethod` 공통화, 6개 신규 메서드 |
| `ClassValidator.cs` | MethodValidator 호출부 `_failures` 전달, 5개 신규 메서드 |
| `InterfaceValidator.cs` | **신규** - Port 인터페이스 검증 클래스 (11개 메서드) |
| `ArchitectureValidationEntryPoint.cs` | `ValidateAllInterfaces` 확장 메서드 추가 |
| `ValidationResultSummary.cs` | `Interface` 대상 `ProcessValidationResult` 오버로드 추가 |

### 수정/추가된 테스트 파일

| 파일 | 변경 내용 |
|------|-----------|
| `EntityArchitectureRuleTests.cs` | +3 테스트 (GenerateEntityId, AggregateRoot/Entity private 생성자) |
| `AdapterArchitectureRuleTests.cs` | +1 테스트 (NotSealed) |
| `DomainServiceArchitectureRuleTests.cs` | +2 테스트 (PublicMethods Fin 반환, NotRecord) |
| `PortInterfaceArchitectureRuleTests.cs` | **신규** +3 테스트 (네이밍, IObservablePort, FinT 반환) |

### 테스트 결과

- **총 334개 테스트 실행, 333 성공, 1 실패** (기존 `Adapter_ShouldHave_VirtualMethods` - `ExternalPricingApiService.HandleHttpError`가 non-virtual)
- **신규 추가 9개 테스트 모두 통과**
