# 값 객체 아키텍처 테스트

## 목차
- [개요](#개요)<br/>
- [학습 목표](#학습-목표)<br/>
- [왜 필요한가?](#왜-필요한가)<br/>
- [핵심 개념](#핵심-개념)<br/>
- [실전 지침](#실전-지침)<br/>
- [프로젝트 설명](#프로젝트-설명)<br/>
- [한눈에 보는 정리](#한눈에-보는-정리)<br/>
- [FAQ](#faq)

## 개요

이 프로젝트는 **ArchUnitNET을 활용한 아키텍처 테스트**를 통해 03-Patterns의 01-07번 프로젝트들이 ValueObject 규칙을 올바르게 준수하고 있는지 자동으로 검증합니다.

> **아키텍처 테스트를 통해 값 객체 구현의 일관성과 품질을 보장하세요.**

## 학습 목표

### **핵심 학습 목표**
1. **아키텍처 테스트 이해**
   - ArchUnitNET을 활용한 구조적 검증
   - ValueObject 규칙 자동 검증
   - 지속적인 아키텍처 품질 보장

2. **실전 적용 능력**
   - 다중 어셈블리 대상 아키텍처 테스트
   - AssemblyReference 패턴 활용
   - CI/CD 파이프라인 통합

### **실습을 통해 확인할 내용**
- **7개 프로젝트 검증**: 01-07번 프로젝트의 모든 값 객체 검증
- **자동화된 품질 보장**: 수동 검토 없이 아키텍처 규칙 준수 확인
- **지속적 통합**: 빌드 파이프라인에서 자동 실행

## 왜 필요한가?

대규모 프로젝트에서는 **아키텍처 일관성 유지**가 매우 중요합니다. 하지만 개발자가 수동으로 모든 클래스의 구현을 검토하는 것은 비현실적입니다.

**첫 번째 문제는 수동 검토의 한계입니다.** 개발자가 실수로 ValueObject 규칙을 위반하더라도, 코드 리뷰 과정에서 놓칠 수 있습니다. 특히 시간에 쫓기는 상황에서는 더욱 그렇습니다.

**두 번째 문제는 일관성 부족입니다.** 팀 내에서 서로 다른 방식으로 ValueObject를 구현하거나, 새로운 개발자가 기존 규칙을 모르고 다른 방식으로 구현할 수 있습니다.

**세 번째 문제는 리팩토링의 위험성입니다.** 기존 코드를 수정할 때 의도치 않게 아키텍처 규칙을 위반할 수 있으며, 이런 변경사항이 즉시 발견되지 않을 수 있습니다.

이러한 문제들을 해결하기 위해 **아키텍처 테스트**를 도입해야 합니다. 아키텍처 테스트를 사용하면 코드 변경 시마다 자동으로 아키텍처 규칙을 검증하여 일관성을 보장할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 3가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 1. 아키텍처 테스트 (Architecture Testing)

**핵심 아이디어는 "코드 구조를 자동으로 검증하여 아키텍처 규칙 준수를 보장"입니다.**

아키텍처 테스트는 코드의 구조적 특성을 검증하는 테스트입니다. **클래스의 접근성, 메서드 시그니처, 상속 관계** 등을 자동으로 검사하여 아키텍처 규칙을 준수하는지 확인합니다.

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

이렇게 하면 **개발자가 실수로 규칙을 위반하더라도 즉시 발견**할 수 있어서, 아키텍처 일관성을 유지할 수 있습니다.

### 2. 다중 어셈블리 검증 (Multi-Assembly Validation)

**핵심 아이디어는 "여러 프로젝트의 값 객체를 한 번에 검증하여 전체적인 일관성 보장"입니다.**

하나의 테스트에서 여러 어셈블리의 클래스들을 동시에 검증할 수 있습니다. 이를 통해 **전체 프로젝트의 아키텍처 일관성**을 보장할 수 있습니다.

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

이 방식의 장점은 **전체 프로젝트의 일관성을 한 번에 검증**할 수 있고, **새로운 프로젝트가 추가되어도 쉽게 확장**할 수 있다는 것입니다.

### 3. AssemblyReference 패턴 (AssemblyReference Pattern)

**핵심 아이디어는 "각 프로젝트에서 자체 어셈블리 참조를 제공하여 테스트에서 활용"입니다.**

각 프로젝트에 `AssemblyReference` 클래스를 두어, 테스트 프로젝트에서 해당 어셈블리를 쉽게 참조할 수 있도록 합니다.

```csharp
// 각 프로젝트의 AssemblyReference.cs
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
```

이 패턴의 장점은 **어셈블리 참조가 명확하고 안전**하며, **컴파일 타임에 참조 오류를 방지**할 수 있다는 것입니다.

## 실전 지침

### 시작하기 전 준비사항

1. **ArchUnitNET 패키지** 이해
2. **AssemblyReference 패턴** 이해
3. **ValueObject 규칙** 숙지

### 학습 방법

1. **테스트 실행**: `dotnet test` 명령으로 아키텍처 테스트 실행
2. **규칙 이해**: 실패하는 테스트를 통해 규칙 위반 사항 파악
3. **수정 적용**: 위반 사항을 수정하여 규칙 준수
4. **확장 학습**: 새로운 아키텍처 규칙 추가 방법 학습

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
                .RequireMethod(IValueObject.CreateMethodName, method => method
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Fin<>)))
                .RequireMethod(IValueObject.CreateFromValidatedMethodName, method => method
                    .RequireVisibility(Visibility.Internal)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod(IValueObject.ValidateMethodName, method => method
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Validation<,>)))
                .RequireImplements(typeof(IEquatable<>));

            // DomainErrors 중첩 클래스 규칙 검증
            @class
                .RequireNestedClassIfExists(IValueObject.DomainErrorsNestedClassName, domainErrors =>
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

| 순서 | 프로젝트 | 검증 내용 | AssemblyReference |
|------|----------|-----------|-------------------|
| **01** | `SimpleValueObject` | 비교 불가능한 단일 값 객체 | ✅ |
| **02** | `ComparableSimpleValueObject` | 비교 가능한 단일 값 객체 | ✅ |
| **03** | `ValueObjectPrimitive` | 비교 불가능한 복합 원시 타입 | ✅ |
| **04** | `ComparableValueObjectPrimitive` | 비교 가능한 복합 원시 타입 | ✅ |
| **05** | `ValueObjectComposite` | 비교 불가능한 복합 값 객체 | ✅ |
| **06** | `ComparableValueObjectComposite` | 비교 가능한 복합 값 객체 | ✅ |
| **07** | `TypeSafeEnum` | 타입 안전한 열거형 | ✅ |

### 검증 규칙

| 규칙 | 설명 | 검증 대상 |
|------|------|-----------|
| **Public 클래스** | 모든 값 객체는 public이어야 함 | 클래스 접근성 |
| **Sealed 클래스** | 상속을 방지하기 위해 sealed이어야 함 | 클래스 수정자 |
| **Private 생성자** | 모든 생성자는 private이어야 함 | 생성자 접근성 |
| **불변성** | 모든 프로퍼티는 읽기 전용이어야 함 | 프로퍼티 수정자 |
| **Create 메서드** | public static Create 메서드가 있어야 함 | 메서드 존재 |
| **Validate 메서드** | public static Validate 메서드가 있어야 함 | 메서드 존재 |
| **IEquatable 구현** | 값 동등성을 위해 IEquatable을 구현해야 함 | 인터페이스 구현 |
| **DomainErrors 중첩 클래스** | 에러 정의를 위한 중첩 클래스가 있어야 함 | 중첩 클래스 구조 |

## FAQ

### Q1: 아키텍처 테스트가 실패하면 어떻게 해야 하나요?
**A**: 아키텍처 테스트가 실패하면 다음 단계를 따르세요:

1. **실패 메시지 분석**: 어떤 규칙을 위반했는지 확인
2. **해당 클래스 수정**: 위반한 규칙에 맞춰 클래스 수정
3. **테스트 재실행**: 수정 후 테스트가 통과하는지 확인

예를 들어, `sealed` 키워드가 누락되었다면 클래스에 `sealed` 키워드를 추가하면 됩니다.

### Q2: 새로운 프로젝트를 추가할 때는 어떻게 하나요?
**A**: 새로운 프로젝트를 추가할 때는 다음 단계를 따르세요:

1. **AssemblyReference.cs 추가**: 새 프로젝트에 `AssemblyReference.cs` 파일 추가
2. **ArchitectureTestBase 수정**: `BuildArchitecture()` 메서드에 새 어셈블리 추가
3. **테스트 실행**: 새 프로젝트가 올바르게 검증되는지 확인

### Q3: 아키텍처 테스트의 성능은 어떤가요?
**A**: 아키텍처 테스트는 일반적으로 **빠르게 실행**됩니다. 7개 프로젝트를 검증하는 데 약 1-2초 정도 소요됩니다. 하지만 프로젝트가 많아질수록 실행 시간이 증가할 수 있으므로, 필요에 따라 테스트를 분할하거나 병렬 실행을 고려할 수 있습니다.

### Q4: CI/CD 파이프라인에 어떻게 통합하나요?
**A**: CI/CD 파이프라인에 통합하는 방법은 다음과 같습니다:

```yaml
# GitHub Actions 예시
- name: Run Architecture Tests
  run: |
    cd 03-Patterns/08-Architecture-Test/ArchitectureTest
    dotnet test
```

이렇게 하면 **모든 PR에서 자동으로 아키텍처 규칙을 검증**하여 일관성을 보장할 수 있습니다.

### Q5: 커스텀 아키텍처 규칙을 추가할 수 있나요?
**A**: 네, 가능합니다. `DomainRuleTests.cs`에 새로운 테스트 메서드를 추가하여 커스텀 규칙을 정의할 수 있습니다:

```csharp
[Fact]
public void CustomRule_ShouldBeSatisfied()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ImplementInterface(typeof(IValueObject))
        .Should()
        .HaveMethod("CustomMethod")  // 커스텀 규칙
        .Check(Architecture);
}
```

### Q6: 아키텍처 테스트와 단위 테스트의 차이점은 무엇인가요?
**A**: 두 테스트는 서로 다른 목적을 가집니다:

**단위 테스트**:
- **목적**: 개별 메서드나 클래스의 동작 검증
- **범위**: 특정 기능의 정확성
- **예시**: `Create` 메서드가 올바른 값을 반환하는지 확인

**아키텍처 테스트**:
- **목적**: 코드 구조와 아키텍처 규칙 준수 검증
- **범위**: 전체 프로젝트의 일관성
- **예시**: 모든 값 객체가 `sealed` 클래스인지 확인

두 테스트를 함께 사용하면 **기능적 정확성과 구조적 일관성**을 모두 보장할 수 있습니다.
