---
title: "값 객체 아키텍처 테스트"
---

## 개요

개발자가 실수로 `sealed` 키워드를 빠뜨리거나, `Create` 메서드 시그니처를 다르게 구현하면 코드 리뷰에서 놓치기 쉽습니다. ArchUnitNET을 활용한 아키텍처 테스트는 01-07번 프로젝트의 모든 값 객체가 규칙을 올바르게 준수하는지 빌드 시마다 자동으로 검증합니다.

## 학습 목표

1. ArchUnitNET으로 값 객체 클래스의 구조적 규칙(sealed, private 생성자, Create/Validate 메서드 등)을 자동 검증할 수 있습니다.
2. AssemblyReference 패턴을 활용하여 다중 어셈블리를 하나의 테스트에서 검증할 수 있습니다.
3. 아키텍처 테스트를 CI/CD 파이프라인에 통합하여 지속적인 품질을 보장할 수 있습니다.

## 왜 필요한가?

대규모 프로젝트에서 모든 값 객체의 구현을 수동으로 검토하는 것은 비현실적입니다. 개발자가 실수로 ValueObject 규칙을 위반하더라도 코드 리뷰에서 놓칠 수 있고, 팀 내에서 서로 다른 방식으로 구현하거나 새로운 개발자가 기존 규칙을 모르고 다른 방식으로 구현할 수 있습니다. 리팩토링 과정에서 의도치 않게 아키텍처 규칙을 위반할 수도 있으며, 이런 변경사항이 즉시 발견되지 않을 수 있습니다.

아키텍처 테스트는 이 문제를 해결합니다. 코드 변경 시마다 자동으로 아키텍처 규칙을 검증하여, 규칙 위반이 커밋되기 전에 발견됩니다.

## 핵심 개념

### 아키텍처 테스트 (Architecture Testing)

아키텍처 테스트는 코드의 구조적 특성을 검증하는 테스트입니다. 클래스의 접근성, 메서드 시그니처, 상속 관계 등을 자동으로 검사하여 아키텍처 규칙 준수를 보장합니다.

ArchUnitNET의 Fluent API로 검증 규칙을 선언적으로 표현합니다.

```csharp
// 아키텍처 테스트 예시
ArchRuleDefinition
    .Classes()
    .That()
    .ImplementInterface(typeof(IValueObject))
    .Should()
    .BeSealed()                    // sealed 클래스여야 함
    .And()
    .HaveMethod("Create")          // Create 메서드가 있어야 함
    .And()
    .HaveMethod("Validate");       // Validate 메서드가 있어야 함
```

### 다중 어셈블리 검증 (Multi-Assembly Validation)

하나의 테스트에서 여러 어셈블리의 클래스들을 동시에 검증할 수 있습니다. 7개 프로젝트의 모든 값 객체를 한 번에 검증하여 전체적인 일관성을 보장합니다.

`BuildArchitecture()`에서 모든 대상 어셈블리를 로드합니다.

```csharp
// 다중 어셈블리 아키텍처 구성
protected static readonly Architecture Architecture = BuildArchitecture();

private static Architecture BuildArchitecture()
{
    List<System.Reflection.Assembly> assemblies = [];

    assemblies.AddRange([
        SimpleValueObject.AssemblyReference.Assembly,
        ComparableSimpleValueObject.AssemblyReference.Assembly,
        ValueObjectPrimitive.AssemblyReference.Assembly,
        // ... 7개 프로젝트 모두 포함
    ]);

    return new ArchLoader()
        .LoadAssemblies(assemblies.ToArray())
        .Build();
}
```

새로운 프로젝트가 추가되면 이 배열에 어셈블리를 추가하기만 하면 됩니다.

### AssemblyReference 패턴 (AssemblyReference Pattern)

각 프로젝트에 `AssemblyReference` 클래스를 두어, 테스트 프로젝트에서 해당 어셈블리를 컴파일 타임에 안전하게 참조할 수 있도록 합니다.

```csharp
// 각 프로젝트의 AssemblyReference.cs
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
```

## 실전 지침

### 각 프로젝트 실행 방법

```bash
# 아키텍처 테스트 실행
cd 03-Patterns/08-Architecture-Test/ArchitectureTest
dotnet test

# 특정 테스트만 실행
dotnet test --filter "ValueObject_ShouldSatisfy_Rules"
```

### 예상 출력 예시

```
=== 아키텍처 테스트 실행 ===

Validating 7 assemblies:
  - SimpleValueObject
  - ComparableSimpleValueObject
  - ValueObjectPrimitive
  - ComparableValueObjectPrimitive
  - ValueObjectComposite
  - ComparableValueObjectComposite
  - TypeSafeEnum

Test passed: ValueObject Rule
```

## 프로젝트 설명

### 프로젝트 구조

```
08-Architecture-Test/
├── ArchitectureTest/                    # 테스트 프로젝트
│   ├── ArchitectureTestBase.cs         # 아키텍처 테스트 베이스 클래스
│   ├── DomainRuleTests.cs              # 도메인 규칙 테스트
│   └── ArchitectureTest.csproj         # 프로젝트 파일
└── README.md                           # 이 문서
```

### 핵심 코드

`ArchitectureTestBase`는 7개 프로젝트의 어셈블리를 로드하여 `Architecture` 인스턴스를 구성합니다.

#### ArchitectureTestBase.cs
```csharp
public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = BuildArchitecture();

    private static Architecture BuildArchitecture()
    {
        List<System.Reflection.Assembly> assemblies = [];

        assemblies.AddRange([
            SimpleValueObject.AssemblyReference.Assembly,
            ComparableSimpleValueObject.AssemblyReference.Assembly,
            ValueObjectPrimitive.AssemblyReference.Assembly,
            ComparableValueObjectPrimitive.AssemblyReference.Assembly,
            ValueObjectComposite.AssemblyReference.Assembly,
            ComparableValueObjectComposite.AssemblyReference.Assembly,
            TypeSafeEnum.AssemblyReference.Assembly
        ]);

        return new ArchLoader()
            .LoadAssemblies(assemblies.ToArray())
            .Build();
    }
}
```

`DomainRuleTests`는 `IValueObject`를 구현하는 모든 클래스에 대해 sealed, private 생성자, Create/Validate 메서드 시그니처, 불변성 등을 검증합니다.

#### DomainRuleTests.cs
```csharp
[Fact]
public void ValueObject_ShouldSatisfy_Rules()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ImplementInterface(typeof(IValueObject))
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class =>
        {
            // 값 객체 클래스 규칙 검증
            @class
                .RequirePublic()
                .RequireSealed()
                .RequireAllPrivateConstructors()
                .RequireImmutable()
                .RequireMethod(IValueObject.ArchTestContract.CreateMethodName, method => method
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Fin<>)))
                .RequireMethod(IValueObject.ArchTestContract.CreateFromValidatedMethodName, method => method
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod(IValueObject.ArchTestContract.ValidateMethodName, method => method
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Validation<,>)))
                .RequireImplements(typeof(IEquatable<>));

            // Domain 중첩 클래스 규칙 검증
            @class
                .RequireNestedClassIfExists(IValueObject.ArchTestContract.NestedErrorsClassName, domainErrors =>
                {
                    domainErrors
                        .RequireInternal()
                        .RequireSealed()
                        .RequireAllMethods(method => method
                            .RequireVisibility(Visibility.Public)
                            .RequireStatic()
                            .RequireReturnType(typeof(Error)));
                });
        }, _output)
        .ThrowIfAnyFailures("ValueObject Rule");
}
```

## 한눈에 보는 정리

### 검증 대상 프로젝트

아키텍처 테스트가 검증하는 7개 프로젝트와 각각의 검증 내용입니다.

| 순서 | 프로젝트 | 검증 내용 | AssemblyReference |
|------|----------|-----------|-------------------|
| **01** | `SimpleValueObject` | 비교 불가능한 단일 값 객체 | 포함 |
| **02** | `ComparableSimpleValueObject` | 비교 가능한 단일 값 객체 | 포함 |
| **03** | `ValueObjectPrimitive` | 비교 불가능한 복합 원시 타입 | 포함 |
| **04** | `ComparableValueObjectPrimitive` | 비교 가능한 복합 원시 타입 | 포함 |
| **05** | `ValueObjectComposite` | 비교 불가능한 복합 값 객체 | 포함 |
| **06** | `ComparableValueObjectComposite` | 비교 가능한 복합 값 객체 | 포함 |
| **07** | `TypeSafeEnum` | 타입 안전한 열거형 | 포함 |

### 검증 규칙

각 값 객체 클래스에 적용되는 규칙 목록입니다.

| 규칙 | 설명 |
|------|------|
| **Public 클래스** | 모든 값 객체는 public이어야 함 |
| **Sealed 클래스** | 상속을 방지하기 위해 sealed이어야 함 |
| **Private 생성자** | 모든 생성자는 private이어야 함 |
| **불변성** | 모든 프로퍼티는 읽기 전용이어야 함 |
| **Create 메서드** | public static, `Fin<T>` 반환 |
| **Validate 메서드** | public static, `Validation<,>` 반환 |
| **IEquatable 구현** | 값 동등성을 위해 구현 필수 |

## FAQ

### Q1: 아키텍처 테스트가 실패하면 어떻게 해야 하나요?
**A**: 실패 메시지에서 어떤 규칙을 위반했는지 확인하고, 해당 클래스를 수정합니다. 예를 들어 `sealed` 키워드가 누락되었다면 클래스에 `sealed` 키워드를 추가하고 테스트를 재실행합니다.

### Q2: 새로운 프로젝트를 추가할 때는 어떻게 하나요?
**A**: 새 프로젝트에 `AssemblyReference.cs` 파일을 추가하고, `ArchitectureTestBase`의 `BuildArchitecture()` 메서드에 새 어셈블리를 추가합니다. 테스트를 실행하면 새 프로젝트도 자동으로 검증됩니다.

### Q3: 아키텍처 테스트와 단위 테스트의 차이점은 무엇인가요?
**A**: 단위 테스트는 개별 메서드나 클래스의 동작 정확성을 검증합니다. 아키텍처 테스트는 코드의 구조(클래스 접근성, 메서드 시그니처, 상속 관계 등)가 규칙을 준수하는지 검증합니다. 두 테스트를 함께 사용하면 기능적 정확성과 구조적 일관성을 모두 보장할 수 있습니다.

Part 3에서 프레임워크 기본 클래스를 활용한 값 객체 패턴을 모두 완성했습니다. Part 4에서는 이 값 객체들을 EF Core, CQRS 같은 실전 인프라와 통합하는 방법을 다룹니다.

→ [9장: UnionValueObject](../09-UnionValueObject/)
