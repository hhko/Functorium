# 부록 C: FAQ

## 일반

### Q: 아키텍처 테스트가 단위 테스트를 대체할 수 있나요?

**아닙니다.** 아키텍처 테스트와 단위 테스트는 서로 다른 관심사를 검증합니다. 아키텍처 테스트는 코드의 **구조(structure)를** 검증하고, 단위 테스트는 코드의 **동작(behavior)을** 검증합니다. 두 종류의 테스트를 함께 사용하세요.

### Q: 아키텍처 테스트를 CI/CD에 포함해야 하나요?

**네, 반드시 포함하세요.** 아키텍처 테스트의 가장 큰 가치는 자동화된 검증입니다. `dotnet test`에 포함되므로 별도 설정 없이 CI 파이프라인에서 실행됩니다.

### Q: 아키텍처 테스트가 빌드 시간에 얼마나 영향을 주나요?

`ArchLoader`의 어셈블리 로딩이 가장 비용이 큰 부분입니다. `static readonly`로 캐싱하면 일반적으로 **수백 밀리초** 수준입니다. 단위 테스트 실행 시간에 큰 영향을 주지 않습니다.

## ClassValidator

### Q: `RequireSealed()`를 사용하면 abstract 클래스가 실패하나요?

**네.** abstract 클래스는 sealed가 될 수 없으므로 실패합니다. abstract 기반 클래스를 필터에서 제외하세요:

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreNotAbstract()  // abstract 클래스 제외
    .ValidateAllClasses(Architecture, @class => @class
        .RequireSealed())
    .ThrowIfAnyFailures("Rule");
```

### Q: static 클래스에 `RequireSealed()`를 적용하면 어떻게 되나요?

C#의 static 클래스는 IL 수준에서 `abstract sealed`로 컴파일됩니다. `RequireStatic()`을 사용하면 이를 올바르게 감지합니다. static 클래스에는 `RequireStatic()`을 사용하고, `RequireSealed()`는 일반 클래스에 사용하세요.

### Q: `RequireImmutable()`이 검증하는 6차원이 구체적으로 무엇인가요?

| 차원 | 검증 내용 | 위반 예시 |
|------|----------|----------|
| 쓰기 가능성 | 비정적 멤버가 불변 | `public int X { get; set; }` |
| 생성자 | public 생성자 없음 | `public MyClass() { }` |
| 속성 | public setter 없음 | `public string Name { get; set; }` |
| 필드 | public 비정적 필드 없음 | `public int Count;` |
| 컬렉션 | 가변 컬렉션 금지 | `public List<int> Items { get; }` |
| 메서드 | 허용 목록 외 메서드 금지 | `public void Mutate() { }` |

## MethodValidator

### Q: `RequireReturnType(typeof(Fin<>))`에서 open generic은 어떻게 매칭하나요?

`typeof(Fin<>)`는 open generic 타입입니다. MethodValidator는 반환 타입의 generic 정의를 추출하여 비교합니다. 예를 들어, `Fin<Email>`의 generic 정의는 `Fin<>`이므로 매칭됩니다.

### Q: `RequireMethod`와 `RequireMethodIfExists`의 차이는?

- **`RequireMethod`:** 메서드가 반드시 존재해야 합니다. 없으면 위반입니다.
- **`RequireMethodIfExists`:** 메서드가 있을 때만 검증합니다. 없어도 위반이 아닙니다.

선택적 메서드(예: Validator 중첩 클래스 안의 특정 메서드)에는 `RequireMethodIfExists`를 사용하세요.

### Q: 확장 메서드를 어떻게 검증하나요?

```csharp
@class.RequireAllMethods(
    m => m.IsStatic == true,  // static 메서드만 필터
    m => m.RequireExtensionMethod());
```

`RequireExtensionMethod()`는 `[ExtensionAttribute]`가 있는지 확인합니다.

## 커스텀 규칙

### Q: `DelegateArchRule`과 `IArchRule` 직접 구현 중 어느 것을 사용해야 하나요?

| 상황 | 권장 |
|------|------|
| 간단한 단일 조건 | `DelegateArchRule` |
| 복잡한 로직 (여러 메서드) | `IArchRule` 직접 구현 |
| 상태가 필요한 경우 | `IArchRule` 직접 구현 |
| 빠른 프로토타이핑 | `DelegateArchRule` |

### Q: `CompositeArchRule`은 AND 논리인가요, OR 논리인가요?

**AND 논리입니다.** 모든 하위 규칙의 위반을 수집합니다. OR 논리가 필요하면 `DelegateArchRule`로 직접 구현하세요.

## 트러블슈팅

### Q: `ArchLoader`가 어셈블리를 찾지 못합니다

테스트 프로젝트에서 대상 프로젝트를 `ProjectReference`로 참조하고 있는지 확인하세요. 빌드가 성공해야 어셈블리가 생성됩니다.

```xml
<ProjectReference Include="..\TargetProject\TargetProject.csproj" />
```

### Q: 테스트가 "No types found" 오류를 발생시킵니다

1. 네임스페이스 문자열이 정확한지 확인하세요
2. `typeof().Namespace!` 대신 하드코딩된 문자열을 사용했다면 오타가 없는지 확인하세요
3. 대상 클래스가 해당 네임스페이스에 실제로 존재하는지 확인하세요

### Q: `RequireImplements`가 generic 인터페이스를 인식하지 못합니다

closed generic 인터페이스(예: `IRepository<Order>`)에는 `RequireImplements(typeof(IRepository<Order>))`를, open generic(예: `IRepository<>`)에는 `RequireImplementsGenericInterface("IRepository")`를 사용하세요.

### Q: Record 타입이 `RequireImmutable()` 검증에 실패합니다

C#의 positional record는 기본적으로 `init` setter가 있는 속성을 생성합니다. `init` setter도 setter로 간주되어 불변성 검증에 영향을 줄 수 있습니다. 도메인 record에는 private 생성자와 팩토리 메서드를 사용하세요.
