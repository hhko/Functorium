---
title: "첫 번째 아키텍처 테스트"
---
## 개요

이 장에서는 **Functorium ArchitectureRules 프레임워크를** 사용하여 첫 번째 아키텍처 테스트를 작성합니다. 도메인 클래스가 `public`이고 `sealed`인지 검증하는 간단한 테스트를 통해 프레임워크의 기본 사용법을 익힙니다.

## 학습 목표

1. **아키텍처 테스트의 기본 구조** 이해
   - `ArchLoader`로 어셈블리를 로드하여 `Architecture` 객체 생성
   - `ArchRuleDefinition.Classes()`로 검증 대상 클래스 선택
2. **ClassValidator를** 사용한 클래스 규칙 검증
   - `RequirePublic()`: 클래스가 `public`인지 검증
   - `RequireSealed()`: 클래스가 `sealed`인지 검증
3. **여러 규칙의 체이닝**
   - `RequirePublic().RequireSealed()`처럼 여러 규칙을 하나의 검증에서 결합

## 프로젝트 구조

```
01-First-Architecture-Test/
├── FirstArchitectureTest/                    # 메인 프로젝트
│   ├── Domains/
│   │   └── Employee.cs                       # 검증 대상 도메인 클래스
│   ├── Program.cs
│   └── FirstArchitectureTest.csproj
├── FirstArchitectureTest.Tests.Unit/         # 테스트 프로젝트
│   ├── ArchitectureTests.cs                  # 아키텍처 테스트
│   ├── FirstArchitectureTest.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## 검증 대상 코드

### Employee.cs

```csharp
namespace FirstArchitectureTest.Domains;

public sealed class Employee
{
    public string Name { get; }
    public string Department { get; }

    private Employee(string name, string department)
    {
        Name = name;
        Department = department;
    }

    public static Employee Create(string name, string department)
        => new(name, department);
}
```

`Employee` 클래스는 다음 설계 원칙을 따릅니다:
- **`public sealed`**: 외부에 공개하되 상속을 금지하여 불변 계약을 보호합니다.
- **`private` 생성자 + 정적 팩토리 메서드**: 객체 생성을 통제합니다.

## 테스트 코드 설명

### Architecture 로드

```csharp
public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(FirstArchitectureTest.Domains.Employee).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(FirstArchitectureTest.Domains.Employee).Namespace!;
}
```

`ArchLoader`는 **ArchUnitNET**의 핵심 클래스로, 지정한 어셈블리의 타입 정보를 분석하여 `Architecture` 객체를 생성합니다. 이 객체는 모든 아키텍처 검증의 기반이 됩니다.

### 단일 규칙 검증

```csharp
[Fact]
public void DomainClasses_ShouldBe_Public()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Visibility Rule");
}
```

검증 흐름:
1. `ArchRuleDefinition.Classes()` -- 클래스 대상 선택 시작
2. `.That().ResideInNamespace(...)` -- 특정 네임스페이스의 클래스로 필터링
3. `.ValidateAllClasses(...)` -- **ClassValidator를** 사용하여 각 클래스에 규칙 적용
4. `.ThrowIfAnyFailures(...)` -- 위반 사항이 있으면 예외 발생

### 규칙 체이닝

```csharp
[Fact]
public void DomainClasses_ShouldBe_PublicAndSealed()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic()
            .RequireSealed(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Public Sealed Rule");
}
```

**ClassValidator는** 플루언트 API를 제공하여 여러 규칙을 체이닝할 수 있습니다. `RequirePublic().RequireSealed()`는 하나의 검증 안에서 두 가지 조건을 모두 확인합니다.

## 핵심 개념 정리

| 개념 | 설명 |
|------|------|
| **ArchLoader** | 어셈블리를 분석하여 Architecture 객체 생성 |
| **ArchRuleDefinition** | 검증 대상(클래스, 인터페이스 등) 선택의 시작점 |
| **ValidateAllClasses** | 선택된 모든 클래스에 ClassValidator 규칙 적용 |
| **ClassValidator** | 클래스별 규칙을 정의하는 플루언트 API |
| **ThrowIfAnyFailures** | 위반 사항이 있으면 상세 메시지와 함께 예외 발생 |
| **verbose: true** | 검증 대상 클래스 목록을 콘솔에 출력 |
