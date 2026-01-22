# ArchitectureTest.Tests.Unit

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 `ArchitectureTest` 프로젝트에 구현된 Value Object들이 Framework의 부모 클래스에서 정의한 설계 규칙을 준수하는지 컴파일 타임에 강제화하지 못한 부분을 런타임 테스트를 통해 검증합니다. ArchUnitNET을 활용하여 아키텍처 규칙을 자동화하고, 값 객체의 일관된 설계 패턴을 보장합니다.

## 학습 목표

### **핵심 학습 목표**
1. **아키텍처 테스트 구현**: ArchUnitNET을 활용한 아키텍처 규칙 검증 시스템 구축
2. **값 객체 설계 규칙 강제화**: 컴파일 타임에 강제할 수 없는 설계 규칙을 런타임 테스트로 보장
3. **도메인 규칙 검증**: IValueObject 인터페이스 구현 클래스들의 일관된 설계 패턴 검증

### **실습을 통해 확인할 내용**
- **아키텍처 규칙 검증**: Value Object 클래스들이 필수 메서드와 구조를 갖추고 있는지 확인
- **도메인 에러 규칙**: DomainErrors 중첩 클래스의 올바른 구현 패턴 검증
- **설계 일관성**: 모든 값 객체가 동일한 설계 원칙을 따르는지 자동화된 검증

## 왜 필요한가?

이전 단계인 `ErrorCode`에서는 구조화된 에러 코드 시스템을 통해 도메인 에러를 체계적으로 관리했습니다. 하지만 실제로 값 객체들의 설계 일관성을 보장하려고 할 때 몇 가지 문제가 발생했습니다.

**첫 번째 문제는 컴파일 타임 제약의 한계입니다.** C#의 제네릭 제약이나 인터페이스만으로는 모든 값 객체가 동일한 메서드 시그니처를 갖도록 강제할 수 없습니다. 마치 템플릿 메서드 패턴에서 추상 메서드의 구현을 강제하지만, 메서드의 접근 제한자나 반환 타입까지는 제어할 수 없는 것과 같습니다.

**두 번째 문제는 설계 규칙의 일관성 부족입니다.** 개발자가 실수로 Create 메서드를 private으로 만들거나, DomainErrors 클래스를 누락하는 등의 실수가 발생할 수 있습니다. 이는 마치 디자인 패턴을 적용할 때 일부 클래스에서만 패턴을 따르고 다른 클래스에서는 무시하는 것과 같은 문제입니다.

**세 번째 문제는 수동 검증의 비효율성입니다.** 코드 리뷰나 수동 검사를 통해 설계 규칙을 확인하는 것은 시간이 많이 걸리고 누락이 발생하기 쉽습니다. 이는 마치 정적 분석 도구 없이 코드 품질을 수동으로 검사하는 것과 같은 비효율성입니다.

이러한 문제들을 해결하기 위해 아키텍처 테스트를 도입했습니다. 아키텍처 테스트를 사용하면 컴파일 타임에 강제할 수 없는 설계 규칙을 런타임에 자동으로 검증하여 일관된 코드 품질을 보장할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 3가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 첫 번째 개념: 아키텍처 테스트 프레임워크

아키텍처 테스트는 코드의 구조적 규칙을 자동으로 검증하는 테스트 방식입니다. 이는 마치 정적 분석 도구가 코드의 품질을 검사하는 것처럼, 아키텍처의 일관성을 자동으로 보장합니다.

**핵심 아이디어는 "설계 규칙의 자동화된 검증"입니다.** 컴파일러가 문법 오류를 잡아내는 것처럼, 아키텍처 테스트는 설계 규칙 위반을 자동으로 감지합니다.

예를 들어, 모든 값 객체가 Create 메서드를 public static으로 구현해야 한다는 규칙을 생각해보세요. 이전 방식에서는 코드 리뷰를 통해 수동으로 확인해야 했지만, 아키텍처 테스트를 통해 자동으로 검증할 수 있습니다.

```csharp
// 이전 방식 (수동 검증) - 누락과 실수의 가능성
public class Price : ComparableSimpleValueObject<decimal>
{
    // Create 메서드가 private이거나 누락될 수 있음
    private static Price Create(decimal value) { ... } // 잘못된 구현
}

// 개선된 방식 (자동 검증) - 아키텍처 테스트로 강제
public class Price : ComparableSimpleValueObject<decimal>
{
    // 아키텍처 테스트가 public static Create 메서드 존재를 검증
    public static Fin<Price> Create(decimal value) { ... } // 올바른 구현
}
```

이 방식의 장점은 설계 규칙 위반을 즉시 감지하고, 모든 값 객체가 일관된 패턴을 따르도록 강제할 수 있다는 것입니다.

### 두 번째 개념: 값 객체 설계 규칙 검증

값 객체 설계 규칙 검증은 IValueObject 인터페이스를 구현하는 모든 클래스가 특정한 구조와 메서드를 갖도록 보장하는 시스템입니다. 이는 마치 인터페이스 구현을 강제하되, 더 세밀한 규칙까지 검증하는 것과 같습니다.

**핵심 아이디어는 "계층적 설계 규칙의 강제화"입니다.** 기본 인터페이스보다 더 구체적인 설계 원칙을 자동으로 검증합니다.

```csharp
// 아키텍처 테스트 규칙 정의
@class
    .RequirePublic()                            // public 클래스
    .RequireSealed()                            // sealed 클래스
    .RequireAllPrivateConstructors()            // 모든 생성자는 private
    .RequireMethod("Create", method => method
        .RequireVisibility(Visibility.Public)    // public 메서드
        .RequireStatic()                         // static 메서드
        .RequireReturnType(typeof(Fin<>)))       // Fin<T> 반환
```

이 방식의 장점은 값 객체의 불변성과 일관된 생성 패턴을 보장하며, 도메인 모델의 무결성을 유지할 수 있다는 것입니다.

### 세 번째 개념: 도메인 에러 규칙 검증

도메인 에러 규칙 검증은 모든 값 객체가 일관된 에러 처리 패턴을 따르도록 보장하는 시스템입니다. 이는 마치 전략 패턴에서 모든 전략이 동일한 인터페이스를 구현하도록 강제하는 것과 같습니다.

**핵심 아이디어는 "에러 처리의 일관성 보장"입니다.** 모든 값 객체가 동일한 방식으로 도메인 에러를 정의하고 처리하도록 강제합니다.

```csharp
// DomainErrors 중첩 클래스 규칙 검증
@class
    .RequireNestedClassIfExists("DomainErrors", domainErrors =>
    {
        domainErrors
            .RequireInternal()                          // internal 클래스
            .RequireSealed()                            // sealed 클래스
            .RequireAllMethods(method => method
                .RequireVisibility(Visibility.Public)   // public 메서드
                .RequireStatic()                        // static 메서드
                .RequireReturnType(typeof(Error)));     // Error 반환
    });
```

이 방식의 장점은 도메인 에러의 일관된 정의와 사용을 보장하며, 에러 처리 로직의 표준화를 달성할 수 있다는 것입니다.

## 실전 지침

### 핵심 구현 포인트
1. **ArchUnitNET 설정**: 아키텍처 로더를 통해 대상 어셈블리를 로드하고 아키텍처 객체 생성
2. **규칙 정의**: IValueObject 구현 클래스에 대한 구체적인 설계 규칙 정의
3. **자동 검증**: 모든 값 객체 클래스에 대해 규칙을 자동으로 적용하고 결과 검증

## 프로젝트 설명

### 프로젝트 구조
```
ArchitectureTest.Tests.Unit/                 # 아키텍처 테스트 프로젝트
├── ArchitectureTestBase.cs                  # 아키텍처 테스트 기본 클래스
├── DomainRuleTests.cs                       # 도메인 규칙 테스트
├── ArchitectureTest.Tests.Unit.csproj       # 프로젝트 파일
└── README.md                                # 메인 문서
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
            ArchitectureTest.AssemblyReference.Assembly,
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
            // 값 객체 클래스 규칙
            @class
                .RequirePublic()
                .RequireSealed()
                .RequireAllPrivateConstructors()
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

            // DomainErrors 중첩 클래스 규칙
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

### 비교 표
| 구분 | 이전 방식 | 현재 방식 |
|------|-----------|-----------|
| **규칙 검증** | 수동 코드 리뷰 | 자동화된 아키텍처 테스트 |
| **일관성 보장** | 개발자 의존적 | 시스템 강제적 |
| **오류 감지** | 런타임 또는 수동 발견 | 컴파일 후 즉시 감지 |
| **유지보수** | 규칙 변경 시 수동 업데이트 | 규칙 변경 시 테스트만 수정 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **자동화된 검증** | **초기 설정 복잡성** |
| **일관된 설계 보장** | **테스트 실행 시간** |
| **규칙 위반 즉시 감지** | **학습 곡선** |
| **유지보수성 향상** | **도구 의존성** |

## FAQ

### Q1: 아키텍처 테스트가 단위 테스트와 다른 점은 무엇인가요?
**A**: 아키텍처 테스트는 코드의 기능적 동작이 아닌 구조적 규칙을 검증하는 테스트입니다. 이는 마치 정적 분석 도구가 코드 품질을 검사하는 것처럼, 설계 원칙의 일관성을 자동으로 보장합니다.

단위 테스트가 "이 메서드가 올바른 결과를 반환하는가?"를 검증한다면, 아키텍처 테스트는 "모든 값 객체가 동일한 설계 패턴을 따르는가?"를 검증합니다. 이는 마치 디자인 패턴의 구현이 일관되게 적용되었는지를 자동으로 확인하는 것과 같습니다.

아키텍처 테스트를 통해 컴파일 타임에 강제할 수 없는 설계 규칙을 런타임에 자동으로 검증하여, 코드의 구조적 일관성을 보장할 수 있습니다.

**실제 예시:**
```csharp
// 단위 테스트: 기능 검증
[Fact]
public void Create_ShouldReturnSuccess_WhenValidValue()
{
    var result = Price.Create(100m);
    result.IsSucc.ShouldBeTrue();
}

// 아키텍처 테스트: 구조 검증
[Fact]
public void ValueObject_ShouldSatisfy_Rules()
{
    // 모든 값 객체가 Create 메서드를 public static으로 구현하는지 검증
    ArchRuleDefinition.Classes()
        .That().ImplementInterface(typeof(IValueObject))
        .Should().HaveMethod("Create", method => method
            .BePublic().And().BeStatic());
}
```

### Q2: 왜 컴파일 타임에 강제할 수 없는 규칙들을 아키텍처 테스트로 검증하나요?
**A**: C#의 제네릭 제약이나 인터페이스만으로는 모든 구현 클래스가 동일한 메서드 시그니처를 갖도록 강제할 수 없기 때문입니다. 이는 마치 템플릿 메서드 패턴에서 추상 메서드의 구현은 강제하지만, 메서드의 접근 제한자나 반환 타입까지는 제어할 수 없는 것과 같습니다.

예를 들어, IValueObject 인터페이스에서 Create 메서드를 추상 메서드로 정의할 수는 있지만, 이 메서드가 public static이어야 한다는 규칙은 컴파일 타임에 강제할 수 없습니다. 이는 마치 인터페이스에서 메서드의 접근 제한자를 지정할 수 없는 것과 같은 제약입니다.

아키텍처 테스트를 통해 이러한 컴파일 타임 제약을 극복하고, 런타임에 설계 규칙을 자동으로 검증할 수 있습니다. 이는 마치 정적 분석 도구가 컴파일러가 잡아내지 못하는 설계 문제를 발견하는 것과 같은 역할을 합니다.

**실제 예시:**
```csharp
// 컴파일 타임 제약: 인터페이스로는 접근 제한자 강제 불가
public interface IValueObject
{
    // public static은 인터페이스에서 정의할 수 없음
    // Fin<T> Create(T value); // static 메서드는 인터페이스에서 불가
}

// 아키텍처 테스트: 런타임에 접근 제한자 검증
@class.RequireMethod("Create", method => method
    .RequireVisibility(Visibility.Public)  // public 강제
    .RequireStatic());                     // static 강제
```

### Q3: DomainErrors 중첩 클래스가 선택적(IfExists)으로 검증되는 이유는 무엇인가요?
**A**: 모든 값 객체가 DomainErrors 중첩 클래스를 가져야 하는 것은 아니기 때문입니다. 이는 마치 전략 패턴에서 모든 클래스가 전략을 구현할 필요는 없는 것처럼, 에러 처리가 필요한 값 객체만 DomainErrors를 구현하면 됩니다.

예를 들어, 단순한 값 객체는 복잡한 검증 로직이 없어 DomainErrors가 필요하지 않을 수 있습니다. 이는 마치 유틸리티 클래스가 모든 인터페이스를 구현할 필요는 없는 것과 같은 원리입니다.

RequireNestedClassIfExists를 사용함으로써 DomainErrors가 있는 값 객체는 올바른 구조를 갖도록 강제하고, 없는 값 객체는 검증을 건너뛰어 유연성을 제공합니다. 이는 마치 옵셔널 패턴에서 값이 있을 때만 검증을 수행하는 것과 같은 접근 방식입니다.

**실제 예시:**
```csharp
// 복잡한 검증이 필요한 값 객체
public sealed class Price : ComparableSimpleValueObject<decimal>
{
    internal static class DomainErrors  // DomainErrors 존재
    {
        public static Error Negative(decimal value) => ...;
    }
}

// 단순한 값 객체
public sealed class Currency : SmartEnum<Currency, string>
{
    // DomainErrors 없음 - 검증 건너뜀
}

// 아키텍처 테스트: 선택적 검증
@class.RequireNestedClassIfExists("DomainErrors", domainErrors =>
{
    // DomainErrors가 있으면 이 규칙들을 적용
    domainErrors.RequireInternal().RequireSealed();
});
```

### Q4: 아키텍처 테스트의 성능 영향은 어떻게 되나요?
**A**: 아키텍처 테스트는 일반적으로 단위 테스트보다 실행 시간이 오래 걸리지만, 전체 테스트 스위트에서 차지하는 비중은 작습니다. 이는 마치 통합 테스트가 단위 테스트보다 느리지만, 전체 테스트 전략에서 중요한 역할을 하는 것과 같습니다.

ArchUnitNET은 리플렉션을 사용하여 클래스 구조를 분석하므로, 대량의 클래스가 있을 때는 실행 시간이 증가할 수 있습니다. 하지만 이는 마치 정적 분석 도구가 코드베이스를 스캔하는 것과 같은 일회성 비용이며, 설계 규칙 위반을 조기에 발견하는 이점이 훨씬 큽니다.

성능 최적화를 위해 아키텍처 테스트는 별도의 테스트 카테고리로 분리하거나, CI/CD 파이프라인에서 병렬로 실행하는 것이 좋습니다. 이는 마치 무거운 테스트를 별도로 분리하여 개발 생산성을 높이는 것과 같은 전략입니다.

**실제 예시:**
```csharp
// 성능 최적화: 테스트 카테고리 분리
[Trait("Category", "Architecture")]
[Fact]
public void ValueObject_ShouldSatisfy_Rules()
{
    // 아키텍처 테스트는 별도 카테고리로 분리
}

// CI/CD에서 선택적 실행
dotnet test --filter "Category=Architecture"  // 아키텍처 테스트만 실행
```

### Q5: 아키텍처 테스트 규칙을 추가하거나 수정하는 방법은 무엇인가요?
**A**: 아키텍처 테스트 규칙은 DomainRuleTests.cs 파일에서 수정할 수 있습니다. 이는 마치 단위 테스트에서 새로운 테스트 케이스를 추가하는 것처럼, 새로운 설계 규칙을 추가하거나 기존 규칙을 수정할 수 있습니다.

규칙 추가 시에는 기존 코드에 미치는 영향을 고려해야 합니다. 이는 마치 인터페이스에 새로운 메서드를 추가할 때 모든 구현 클래스를 수정해야 하는 것과 같은 원리입니다.

규칙 수정 시에는 점진적 접근을 권장합니다. 먼저 새로운 규칙을 추가하고, 기존 코드를 점진적으로 수정한 후, 마지막에 기존 규칙을 제거하는 방식으로 진행합니다. 이는 마치 리팩토링에서 점진적 변경을 통해 안정성을 보장하는 것과 같은 접근 방식입니다.

**실제 예시:**
```csharp
// 새로운 규칙 추가
@class
    .RequireMethod("Create", method => method
        .RequireVisibility(Visibility.Public)
        .RequireStatic())
    .RequireMethod("Validate", method => method  // 새로운 규칙 추가
        .RequireVisibility(Visibility.Public)
        .RequireStatic()
        .RequireReturnType(typeof(Validation<,>)));

// 기존 규칙 수정
@class
    .RequireMethod("Create", method => method
        .RequireVisibility(Visibility.Public)
        .RequireStatic()
        .RequireReturnType(typeof(Fin<>)));  // 반환 타입 변경
```
