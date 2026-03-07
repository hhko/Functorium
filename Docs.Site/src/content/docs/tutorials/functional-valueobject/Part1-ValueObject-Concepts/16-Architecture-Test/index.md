---
title: "아키텍처 테스트"
---

## 개요

값 객체를 여러 개 만들다 보면, Create 메서드가 private이거나 sealed를 빠뜨리는 등 설계 규칙이 슬금슬금 무너지기 시작합니다. C#의 제네릭 제약이나 인터페이스만으로는 이런 규칙을 컴파일 타임에 강제할 수 없습니다. 이 장에서는 ArchUnitNET을 활용하여 값 객체의 구조적 규칙을 런타임 테스트로 자동 검증하는 방법을 다룹니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다.

1. ArchUnitNET을 활용한 아키텍처 규칙 검증 시스템을 구축할 수 있습니다
2. 컴파일 타임에 강제할 수 없는 값 객체 설계 규칙을 런타임 테스트로 보장할 수 있습니다
3. IValueObject 구현 클래스들의 일관된 설계 패턴을 자동으로 검증할 수 있습니다

## 왜 필요한가?

C#의 제네릭 제약이나 인터페이스만으로는 모든 값 객체가 동일한 메서드 시그니처를 갖도록 강제할 수 없습니다. 예를 들어, IValueObject 인터페이스에서 Create 메서드를 정의할 수는 있지만 public static이어야 한다는 규칙까지 강제하기는 어렵습니다. 개발자가 실수로 Create 메서드를 private으로 만들거나, sealed를 누락하거나, DomainErrors 클래스 구조를 다르게 작성할 수 있고, 이런 문제는 코드 리뷰로 잡기에는 번거롭고 누락되기 쉽습니다.

아키텍처 테스트를 도입하면 컴파일 타임에 강제할 수 없는 설계 규칙을 CI 파이프라인에서 자동으로 검증할 수 있습니다.

## 핵심 개념

### 아키텍처 테스트

아키텍처 테스트는 코드의 기능적 동작이 아닌 구조적 규칙을 검증합니다. 단위 테스트가 "이 메서드가 올바른 결과를 반환하는가?"를 검증한다면, 아키텍처 테스트는 "모든 값 객체가 동일한 설계 패턴을 따르는가?"를 검증합니다.

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

### 값 객체 설계 규칙 검증

IValueObject 인터페이스를 구현하는 모든 클래스가 특정한 구조와 메서드를 갖도록 보장합니다. 접근 제한자, sealed 여부, 생성자 가시성, 메서드 반환 타입 등 인터페이스로 강제할 수 없는 세밀한 규칙까지 검증합니다.

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

### 도메인 에러 규칙 검증

DomainErrors 중첩 클래스가 있는 값 객체에 대해, 해당 클래스가 올바른 구조를 갖추고 있는지 검증합니다. 모든 값 객체에 DomainErrors가 필수는 아니므로 `RequireNestedClassIfExists`로 선택적으로 검증합니다.

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

## 실전 지침

### 핵심 구현 포인트

ArchUnitNET의 아키텍처 로더로 대상 어셈블리를 로드하고, IValueObject 구현 클래스에 대한 설계 규칙을 정의한 뒤, 모든 값 객체 클래스에 규칙을 자동으로 적용합니다. 새로운 값 객체를 추가하더라도 기존 테스트가 자동으로 커버합니다.

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

IValueObject를 구현하는 모든 비추상 클래스에 대해 설계 규칙을 일괄 적용합니다.

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
                    .RequireVisibility(Visibility.Public)
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

수동 코드 리뷰 방식과 아키텍처 테스트 방식의 차이를 비교합니다.

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
| **자동화된 검증** | 초기 설정 복잡성 |
| **일관된 설계 보장** | ArchUnitNET 의존성 |
| **규칙 위반 즉시 감지** | 리플렉션 기반으로 실행 시간 증가 |
| **새 값 객체 자동 커버** | - |

## FAQ

### Q1: 아키텍처 테스트가 단위 테스트와 다른 점은 무엇인가요?

단위 테스트가 "이 메서드가 올바른 결과를 반환하는가?"를 검증한다면, 아키텍처 테스트는 "모든 값 객체가 동일한 설계 패턴을 따르는가?"를 검증합니다.

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

### Q2: DomainErrors 중첩 클래스가 선택적(IfExists)으로 검증되는 이유는?

모든 값 객체가 DomainErrors를 가져야 하는 것은 아닙니다. 단순한 값 객체는 복잡한 검증 로직이 없어 DomainErrors가 불필요할 수 있습니다. `RequireNestedClassIfExists`는 DomainErrors가 있는 값 객체에만 올바른 구조를 강제하고, 없는 값 객체는 검증을 건너뜁니다.

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

### Q3: 아키텍처 테스트 규칙을 추가하는 방법은?

DomainRuleTests.cs에서 `RequireMethod`나 `RequireImplements` 등의 호출을 추가합니다. 새 규칙 추가 시 기존 값 객체들이 규칙을 위반하면 테스트가 실패하므로, 점진적으로 코드를 수정한 뒤 규칙을 활성화하는 것이 안전합니다.

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

---

Part 1에서 값 객체의 기초부터 아키텍처 테스트까지 다루었습니다. Part 2에서는 여러 값 객체를 동시에 검증하는 Bind/Apply 패턴을 학습합니다.
