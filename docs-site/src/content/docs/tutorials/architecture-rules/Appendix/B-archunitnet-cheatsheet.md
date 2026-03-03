---
title: "ArchUnitNET 치트시트"
---

## 기본 구조

```csharp
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
```

## Architecture 로딩

```csharp
static readonly Architecture Architecture =
    new ArchLoader()
        .LoadAssemblies(
            typeof(SomeClass).Assembly,
            typeof(AnotherClass).Assembly)
        .Build();
```

## 타입 선택

| 코드 | 설명 |
|------|------|
| `Classes()` | 모든 클래스 선택 |
| `Interfaces()` | 모든 인터페이스 선택 |
| `Types()` | 모든 타입 선택 |

## 필터 체인 (That)

| 필터 | 설명 |
|------|------|
| `.That().ResideInNamespace(ns)` | 네임스페이스에 위치 |
| `.That().DoNotResideInNamespace(ns)` | 네임스페이스에 위치하지 않음 |
| `.That().HaveNameStartingWith(prefix)` | 이름이 접두사로 시작 |
| `.That().HaveNameEndingWith(suffix)` | 이름이 접미사로 끝남 |
| `.That().HaveNameContaining(fragment)` | 이름에 문자열 포함 |
| `.That().ArePublic()` | public 타입 |
| `.That().AreInternal()` | internal 타입 |
| `.That().AreSealed()` | sealed 타입 |
| `.That().AreAbstract()` | abstract 타입 |
| `.That().AreNotAbstract()` | abstract가 아닌 타입 |
| `.That().ImplementInterface(typeof(I))` | 인터페이스 구현 |
| `.That().AreAssignableTo(typeof(T))` | 타입에 할당 가능 |
| `.That().HaveAnyAttributes(typeof(A))` | 어트리뷰트 보유 |

## 필터 결합

```csharp
// And: 조건 추가
.That()
.ResideInNamespace(ns)
.And().ArePublic()
.And().AreNotAbstract()

// Or: 조건 분기
.That()
.HaveNameEndingWith("Service")
.Or().HaveNameEndingWith("Repository")
```

## 규칙 체인 (Should)

### 가시성/수정자

| 코드 | 설명 |
|------|------|
| `.Should().BePublic()` | public이어야 함 |
| `.Should().BeInternal()` | internal이어야 함 |
| `.Should().BeSealed()` | sealed여야 함 |
| `.Should().BeAbstract()` | abstract여야 함 |

### 네이밍

| 코드 | 설명 |
|------|------|
| `.Should().HaveNameStartingWith(prefix)` | 이름 접두사 |
| `.Should().HaveNameEndingWith(suffix)` | 이름 접미사 |
| `.Should().HaveNameContaining(fragment)` | 이름 포함 |

### 의존성

| 코드 | 설명 |
|------|------|
| `.Should().NotDependOnAnyTypesThat().ResideInNamespace(ns)` | 네임스페이스 의존 금지 |
| `.Should().OnlyDependOnTypesThat().ResideInNamespace(ns)` | 허용 네임스페이스만 |
| `.Should().NotHaveDependencyOtherThan(ns)` | 지정 네임스페이스 외 의존 금지 |

### 상속/구현

| 코드 | 설명 |
|------|------|
| `.Should().ImplementInterface(typeof(I))` | 인터페이스 구현 필수 |
| `.Should().BeAssignableTo(typeof(T))` | 타입 할당 가능 필수 |

## 규칙 실행

```csharp
// ArchUnitNET 기본 방식
rule.Check(Architecture);

// Functorium 확장 방식
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(ns)
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed(),
        verbose: true)
    .ThrowIfAnyFailures("Rule Name");
```

## 레이어 의존성 패턴

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

// 도메인 → 애플리케이션 의존 금지
[Fact]
public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
{
    Types().That().ResideInNamespace(DomainNamespace)
        .Should().NotDependOnAnyTypesThat()
        .ResideInNamespace(ApplicationNamespace)
        .Check(Architecture);
}

// 도메인 → 인프라 의존 금지
[Fact]
public void DomainLayer_ShouldNotDependOn_InfrastructureLayer()
{
    Types().That().ResideInNamespace(DomainNamespace)
        .Should().NotDependOnAnyTypesThat()
        .ResideInNamespace(InfrastructureNamespace)
        .Check(Architecture);
}
```

## 자주 사용하는 조합

### 도메인 엔티티 필터링

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreAssignableTo(typeof(Entity<>))
    .And().AreNotAbstract()
```

### 인터페이스 필터링

```csharp
ArchRuleDefinition.Interfaces()
    .That()
    .ResideInNamespace(PortNamespace)
    .And().HaveNameStartingWith("I")
```

### 네임스페이스 제외

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().DoNotResideInNamespace(DomainNamespace + ".Ports")
```
