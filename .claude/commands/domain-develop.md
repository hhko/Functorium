---
title: DOMAIN-DEVELOP
description: 에릭 에반스 DDD 관점에서 Functorium 프레임워크 기반 도메인 레이어 코드, 단위 테스트, 문서를 생성합니다. 도메인 모델링, Value Object, Entity, Aggregate, Union 타입, Specification, Domain Service 구현이 필요할 때 사용합니다. "도메인 구현", "Aggregate 만들어줘", "Value Object 추가", "엔티티 설계" 등의 요청에 반응합니다.
argument-hint: "<도메인 요구사항> 예: 'Product aggregate with ProductName, Money value objects and OrderStatus union type'"
---

# Functorium DDD 도메인 개발 스킬

에릭 에반스의 DDD 전술적 설계 원칙에 따라 Functorium 프레임워크 기반 도메인 코드, 단위 테스트, 문서를 생성한다.

## 입력: `$ARGUMENTS`

사용자의 도메인 요구사항이다. 비어 있으면 대화형으로 요구사항을 수집한다.

---

## 1단계: 요구사항 분석

사용자 입력에서 다음을 식별한다:

| 식별 항목 | 질문 |
|-----------|------|
| **Aggregate Root** | 트랜잭션 경계는 무엇인가? |
| **Child Entity** | Aggregate 내부의 자식 엔티티는? |
| **Value Object (Simple)** | 단일 원시 값을 감싸는 타입은? |
| **Value Object (Composite)** | 여러 VO를 조합하는 타입은? |
| **Union Value Object** | 허용 상태 조합/상태 전이가 필요한 타입은? |
| **Domain Event** | 어떤 상태 변경을 외부에 알려야 하는가? |
| **Domain Error** | 어떤 비즈니스 규칙 위반이 있는가? |
| **Specification** | 어떤 조회/검색 조건이 필요한가? |
| **Domain Service** | 여러 Aggregate에 걸친 순수 로직이 있는가? |
| **Repository** | 영속화 인터페이스가 필요한가? |

불확실한 사항이 있으면 구현 전에 반드시 사용자에게 확인한다.

### 분석 결과 제시 형식

```
## 도메인 모델 분석

### Aggregate: {이름}
- ID: {이름}Id (Ulid 기반)
- Value Objects: ...
- Child Entities: ...
- Domain Events: ...
- Domain Errors: ...
- Specifications: ...
- Domain Services: ...

### 폴더 구조 (예상)
{Aggregate}/
├── {Aggregate}.cs
├── I{Aggregate}Repository.cs
├── ValueObjects/
│   ├── Simples/
│   └── Composites/  (또는 Unions/)
├── Specifications/
└── Services/
```

사용자의 확인을 받은 후 2단계로 진행한다.

---

## 2단계: 코드 생성

다음 순서로 코드를 생성한다. 각 타입은 아래 패턴을 **정확히** 따른다.

### 2.1 프로젝트 파일 (csproj)

기존 프로젝트에 추가하는 경우 이 단계를 건너뛴다. 새 프로젝트가 필요한 경우:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="LanguageExt.Core" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="...(Functorium.csproj 경로)" />
  </ItemGroup>
  <!-- Source Generator 참조 (EntityId 생성용) -->
  <ItemGroup>
    <ProjectReference Include="...(Functorium.SourceGenerators.csproj 경로)"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

### 2.2 Using.cs

```csharp
global using LanguageExt;
global using LanguageExt.Common;
global using Functorium.Domains.Entities;
global using Functorium.Domains.Errors;
global using Functorium.Domains.Events;
global using Functorium.Domains.Repositories;
global using Functorium.Domains.Services;
global using Functorium.Domains.Specifications;
global using Functorium.Domains.ValueObjects;
global using Functorium.Domains.ValueObjects.Unions;
global using Functorium.Domains.ValueObjects.Validations;
global using Functorium.Domains.ValueObjects.Validations.Typed;
// 프로젝트별 네임스페이스 추가
```

### 2.3 Simple Value Object

단일 원시 값을 감싸는 타입. **반드시 이 패턴을 따른다:**

```csharp
namespace {프로젝트}.{Module}.ValueObjects;

/// <summary>
/// {설명}
/// </summary>
public sealed partial class {Name} : SimpleValueObject<{PrimitiveType}>
{
    // 상수가 있으면 선언
    public const int MaxLength = {N};

    private {Name}({PrimitiveType} value) : base(value) { }

    public static Fin<{Name}> Create({PrimitiveType}? value) =>
        CreateFromValidation(Validate(value), v => new {Name}(v));

    public static Validation<Error, {PrimitiveType}> Validate({PrimitiveType}? value) =>
        ValidationRules<{Name}>
            .NotNull(value)
            .ThenNotEmpty()          // string인 경우
            .ThenMaxLength(MaxLength) // string인 경우
            .ThenMatches({Pattern}()) // 정규식이 필요한 경우
            .ThenNormalize(v => v.Trim()); // 정규화가 필요한 경우

    public static {Name} CreateFromValidated({PrimitiveType} value) => new(value);

    public static implicit operator {PrimitiveType}({Name} vo) => vo.Value;

    // 정규식이 필요한 경우 partial class + [GeneratedRegex] 사용
    [GeneratedRegex(@"패턴")]
    private static partial Regex {Pattern}();
}
```

**핵심 규칙:**
- `Create()` 입력은 nullable (`string?`, `decimal?` 등)
- `Validate()`는 `Validation<Error, TPrimitive>` 반환 (객체가 아닌 원시값)
- `CreateFromValidated()`는 검증 없이 직접 생성 (ORM 복원용)
- `implicit operator`로 원시 타입 변환 제공
- 정규식은 `[GeneratedRegex]` + `partial class` 사용

### 2.4 Composite Value Object

여러 Value Object를 조합하는 타입:

```csharp
namespace {프로젝트}.{Module}.ValueObjects;

public sealed class {Name} : ValueObject
{
    public {VO1Type} {Prop1} { get; }
    public {VO2Type} {Prop2} { get; }
    // 선택적 속성은 nullable 또는 Option<T>

    private {Name}({VO1Type} prop1, {VO2Type} prop2)
    {
        {Prop1} = prop1;
        {Prop2} = prop2;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return {Prop1};
        yield return {Prop2};
        // nullable 속성은 null 검사 후 yield
    }

    public static Validation<Error, ({원시타입들} 튜플)> Validate(
        {원시타입?} prop1, {원시타입?} prop2) =>
        ({VO1}.Validate(prop1), {VO2}.Validate(prop2))
            .Apply((v1, v2) => (v1, v2));

    public static Fin<{Name}> Create({원시타입?} prop1, {원시타입?} prop2) =>
        CreateFromValidation<{Name}, ({원시타입} 튜플)>(
            Validate(prop1, prop2),
            v => new {Name}(
                {VO1}.CreateFromValidated(v.prop1),
                {VO2}.CreateFromValidated(v.prop2)));

    public static {Name} CreateFromValidated({VO1Type} prop1, {VO2Type} prop2) =>
        new(prop1, prop2);

    public override string ToString() => $"...";
}
```

**핵심 규칙:**
- `ValueObject` 상속 (SimpleValueObject가 아님)
- `GetEqualityComponents()` 구현 필수
- `Validate()`에서 하위 VO의 `Validate()`를 `.Apply()`로 병렬 검증
- `Create()` → `CreateFromValidation<TVO, TTuple>()` 패턴 사용

### 2.5 Union Value Object

허용 상태 조합 또는 상태 전이:

**순수 데이터 Union (상태 전이 없음):**

```csharp
[UnionType]
public abstract partial record {Name} : UnionValueObject
{
    public sealed record {Case1}({VO1} Prop1) : {Name};
    public sealed record {Case2}({VO2} Prop2) : {Name};
    public sealed record {Case3}({VO1} Prop1, {VO2} Prop2) : {Name};

    private {Name}() { }
}
```

**상태 전이 Union:**

```csharp
[UnionType]
public abstract partial record {Name} : UnionValueObject<{Name}>
{
    public sealed record {State1}({Props}) : {Name};
    public sealed record {State2}({Props}) : {Name};

    private {Name}() { }

    /// <summary>
    /// {State1} → {State2} 전이
    /// </summary>
    public Fin<{State2}> {TransitionVerb}({추가파라미터}) =>
        TransitionFrom<{State1}, {State2}>(
            s => new {State2}(s.{기존속성}, {추가값}));
}
```

**핵심 규칙:**
- `[UnionType]` 어트리뷰트 필수 → Match/Switch 자동 생성
- 상태 전이 없으면 `UnionValueObject`, 있으면 `UnionValueObject<TSelf>` (CRTP)
- `private` 생성자로 외부 상속 차단
- 상태 전이는 `TransitionFrom<TFrom, TTo>()` 사용

### 2.6 Entity / Aggregate Root

```csharp
using static LanguageExt.Prelude;

namespace {프로젝트}.{Module};

/// <summary>
/// {설명}
/// </summary>
[GenerateEntityId]
public sealed class {Name} : AggregateRoot<{Name}Id>
{
    #region Error Types

    public sealed record {ErrorName1} : DomainErrorType.Custom;
    public sealed record {ErrorName2} : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    public sealed record CreatedEvent({Name}Id {Name}Id, {관련VO들}) : DomainEvent;
    public sealed record {Action}Event({Name}Id {Name}Id, {관련VO들}) : DomainEvent;

    #endregion

    // Value Object 속성
    public {VOType} {Prop} { get; private set; }

    // 자식 엔티티 컬렉션 (있는 경우)
    private readonly List<{ChildEntity}> _{children} = [];
    public IReadOnlyList<{ChildEntity}> {Children} => _{children}.AsReadOnly();

    // Audit 속성 (필요한 경우)
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // private 생성자
    private {Name}({Name}Id id, {VO params}, DateTime createdAt) : base(id)
    {
        // 속성 할당
    }

    #region Create — VO input (도메인 내부용)

    public static {Name} Create({VO params}, DateTime createdAt)
    {
        var entity = new {Name}({Name}Id.New(), {params}, createdAt);
        entity.AddDomainEvent(new CreatedEvent(entity.Id, {params}));
        return entity;
    }

    #endregion

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음, 이벤트 없음)
    /// </summary>
    public static {Name} CreateFromValidated(
        {Name}Id id, {all params including audit}) { ... }

    /// <summary>
    /// 커맨드 메서드: 불변식 보호, Fin<T> 반환
    /// </summary>
    public Fin<Unit> {CommandVerb}({params})
    {
        // 가드 조건 (삭제 상태 등)
        if ({가드조건})
            return DomainError.For<{Name}>(
                new {ErrorType}(),
                Id.ToString(),
                "{에러 메시지}");

        // 상태 변경
        {Prop} = newValue;
        UpdatedAt = now;
        AddDomainEvent(new {Action}Event(Id, {params}));
        return unit;
    }
}
```

**핵심 규칙:**
- `[GenerateEntityId]` → `{Name}Id` 자동 생성 (Ulid 기반)
- `AggregateRoot<TId>` 상속 (자식 Entity는 `Entity<TId>`)
- Domain Event는 **중첩 sealed record** (`{Aggregate}.{PastTense}Event`)
- Domain Error는 **중첩 sealed record** (`DomainErrorType.Custom` 상속)
- `Create()`: 검증된 VO를 받아 생성 + 이벤트 발행
- `CreateFromValidated()`: ORM 복원용 (검증 없음, 이벤트 없음)
- 커맨드 메서드는 `Fin<Unit>` 반환, 실패 시 `DomainError.For<T>()` 사용
- `using static LanguageExt.Prelude;` → `unit` 사용

### 2.7 Child Entity

```csharp
[GenerateEntityId]
public sealed class {Name} : Entity<{Name}Id>
{
    public {VOType} {Prop} { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private {Name}({Name}Id id, {params}) : base(id) { ... }

    public static {Name} Create({params}) =>
        new({Name}Id.New(), {params});

    public static {Name} CreateFromValidated({Name}Id id, {params}) =>
        new(id, {params});
}
```

**핵심 규칙:**
- `Entity<TId>` 상속 (AggregateRoot가 아님)
- 이벤트 발행은 **부모 Aggregate를 통해서만** 수행
- 생성/삭제도 부모 Aggregate 메서드를 통해 관리

### 2.8 Specification

```csharp
using System.Linq.Expressions;

namespace {프로젝트}.{Module}.Specifications;

public sealed class {Aggregate}{Concept}Spec : ExpressionSpecification<{Aggregate}>
{
    public {VOType} {Param} { get; }

    public {Aggregate}{Concept}Spec({VOType} param) => {Param} = param;

    public override Expression<Func<{Aggregate}, bool>> ToExpression()
    {
        // VO를 primitive로 변환 후 클로저 캡처
        {PrimitiveType} value = {Param};
        return entity => entity.{Property} == value;
    }
}
```

### 2.9 Domain Service

```csharp
using static LanguageExt.Prelude;

namespace {프로젝트}.{Module}.Services;

public sealed class {Name}Service : IDomainService
{
    #region Error Types

    public sealed record {ErrorName} : DomainErrorType.Custom;

    #endregion

    public Fin<Unit> {MethodName}({params})
    {
        // 순수 로직 (I/O 없음, 상태 없음)
        if ({비즈니스규칙위반})
            return DomainError.For<{Name}Service>(
                new {ErrorName}(),
                {식별값},
                "{에러 메시지}");

        return unit;
    }
}
```

### 2.10 Repository Interface

```csharp
namespace {프로젝트}.{Module};

public interface I{Aggregate}Repository : IRepository<{Aggregate}, {Aggregate}Id>
{
    // Specification 기반 메서드 (필요 시)
    FinT<IO, bool> Exists(Specification<{Aggregate}> spec);
}
```

---

## 3단계: 단위 테스트 생성

**반드시 `Docs.Site/src/content/docs/guides/testing/15a-unit-testing.md`의 규칙을 준수한다.**

### 테스트 프로젝트 csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="LanguageExt.Core" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\{도메인프로젝트}.csproj" />
  </ItemGroup>
</Project>
```

### 테스트 Using.cs

```csharp
global using LanguageExt;
global using LanguageExt.Common;
global using Shouldly;
global using Xunit;
global using Functorium.Domains.ValueObjects.Validations;
// 도메인 네임스페이스
```

### 명명 규칙 (T1_T2_T3)

| 구성요소 | 설명 | 예시 |
|---------|------|------|
| T1 | 테스트 대상 메서드 | `Create`, `Validate`, `VerifyEmail` |
| T2 | 예상 결과 | `ReturnsSuccess`, `ReturnsFail`, `PublishesEvent` |
| T3 | 시나리오 | `WhenValid`, `WhenNull`, `WhenAlreadyDeleted` |

### AAA 패턴 + 표준 변수명

```csharp
[Fact]
public void T1_T2_T3()
{
    // Arrange
    var sut = ...;  // System Under Test

    // Act
    var actual = sut.Method(...);

    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

### Assertion 라이브러리: Shouldly

```csharp
actual.IsSucc.ShouldBeTrue();
actual.IsFail.ShouldBeTrue();
actual.ShouldBeOfType<{Type}>();
actual.ShouldBe(expected);
actual.ShouldNotBe(unexpected);
actual.ShouldBeNull();
list.Count.ShouldBe(N);
```

### Value Object 테스트 필수 케이스

각 Simple VO에 대해 최소한 다음을 테스트한다:

```csharp
[Trait("Category", "{프로젝트}")]
public class {Name}Tests
{
    // 성공
    [Fact] Create_ReturnsSuccess_WhenValid
    // null 실패
    [Fact] Create_ReturnsFail_WhenNull
    // 빈값 실패 (string인 경우)
    [Fact] Create_ReturnsFail_WhenEmpty
    // 최대길이 초과 (MaxLength가 있는 경우)
    [Fact] Create_ReturnsFail_WhenTooLong
    // 경계값 성공 (MaxLength 정확히)
    [Fact] Create_ReturnsSuccess_WithExactlyMaxLength
    // 정규화 (Trim, 소문자 등)
    [Fact] Create_TrimsWhitespace  // 또는 NormalizesToLowerCase
    // CreateFromValidated
    [Fact] CreateFromValidated_CreatesDirectly
    // 암시적 변환
    [Fact] ImplicitOperator_ReturnsValue
    // 포맷 검증 실패 (정규식이 있는 경우)
    [Fact] Create_ReturnsFail_WhenInvalidFormat
}
```

### Composite Value Object 테스트 필수 케이스

```csharp
public class {Name}Tests
{
    // Validate 성공
    [Fact] Validate_ReturnsSuccess_WhenValid
    // Validate 에러 누적
    [Fact] Validate_CollectsAllErrors_WhenMultipleFieldsInvalid
    // Create 성공
    [Fact] Create_ReturnsSuccess_WhenValid
    // Create 필드별 실패
    [Fact] Create_ReturnsFail_When{Field}Invalid
    // CreateFromValidated
    [Fact] CreateFromValidated_CreatesDirectly
    // 동등성
    [Fact] Equals_ReturnsTrue_WhenSameValues
    [Fact] Equals_ReturnsFalse_WhenDifferent{Field}
    [Fact] GetHashCode_ReturnsSame_WhenEqual
}
```

### Union Value Object 테스트 필수 케이스

```csharp
public class {Name}Tests
{
    // 각 케이스 생성
    [Fact] {Case1}_Creates...
    [Fact] {Case2}_Creates...
    // 패턴 매칭
    [Fact] PatternMatch_CoversAllCases
    // Match 메서드
    [Fact] Match_CoversAllCases
    // Switch 메서드
    [Fact] Switch_CoversAllCases
    // 상태 전이 (있는 경우)
    [Fact] {Transition}_ReturnsSuccess_When{ValidState}
    [Fact] {Transition}_ReturnsFail_When{InvalidState}
}
```

### Aggregate Root 테스트 필수 케이스

```csharp
public class {Name}Tests
{
    private static readonly DateTime Now = new(2024, 1, 1);

    // 헬퍼 메서드
    private static {VOType} Create{VO}() =>
        {VOType}.Create({유효값}).ThrowIfFail();

    #region Create
    [Fact] Create_ReturnsEntity_With{특성}
    [Fact] Create_PublishesCreatedEvent
    [Fact] Create_SetsCreatedAt
    #endregion

    #region CreateFromValidated
    [Fact] CreateFromValidated_DoesNotPublishEvents
    [Fact] CreateFromValidated_Restores{Properties}
    #endregion

    #region {CommandMethod}
    [Fact] {Command}_ReturnsSuccess_When{ValidCondition}
    [Fact] {Command}_Publishes{Event}
    [Fact] {Command}_ReturnsFail_When{InvalidCondition}
    [Fact] {Command}_SetsUpdatedAt
    #endregion

    #region Entity ID
    [Fact] Create_AssignsUniqueId
    #endregion

    #region FinApply (교차 검증)
    [Fact] FinApply_ReturnsSuccess_WhenAllValid
    [Fact] FinApply_AccumulatesErrors_WhenMultipleInvalid
    #endregion
}
```

### Specification 테스트 필수 케이스

```csharp
public class {Name}Tests
{
    [Fact] IsSatisfiedBy_ReturnsTrue_When{조건일치}
    [Fact] IsSatisfiedBy_ReturnsFalse_When{조건불일치}
}
```

### Domain Service 테스트 필수 케이스

```csharp
public class {Name}Tests
{
    private readonly {ServiceType} _sut = new();

    [Fact] {Method}_ReturnsSuccess_When{ValidCondition}
    [Fact] {Method}_ReturnsFail_When{InvalidCondition}
}
```

---

## 4단계: 빌드 및 테스트 검증

코드 생성 후 반드시 다음을 실행한다:

```bash
# 빌드
dotnet build {솔루션또는프로젝트}

# 테스트
dotnet test --solution {솔루션파일} # .slnx인 경우
# 또는
dotnet test --project {테스트프로젝트}
```

빌드 오류나 테스트 실패가 있으면 즉시 수정한다.

---

## 5단계: 문서 생성 (선택)

사용자가 문서를 요청한 경우, `Docs.Site/src/content/docs/` 하위에 마크다운 문서를 생성한다.

### 문서 구조

```markdown
---
title: "{문서 제목}"
---

## 들어가며

{도메인 개념 설명과 해결하는 문제}

## 도메인 모델

### {Aggregate 이름}

{Aggregate의 역할, 불변식, 생명주기 설명}

### Value Objects

| Value Object | 설명 | 기반 클래스 |
|-------------|------|------------|
| {이름} | {설명} | SimpleValueObject<{T}> |

### Domain Events

| 이벤트 | 발생 시점 |
|--------|----------|
| {이름}Event | {설명} |

## 테스트 전략

{각 빌딩블록별 테스트 범위 설명}
```

### 마크다운 규칙

- `**텍스트(...)**` 뒤에 한글 조사가 바로 오면 볼드가 깨진다. 한글 조사를 볼드 안에 포함시킨다:
  - Bad: `**공변성(Covariance)**은`
  - Good: `**공변성(Covariance)은**`
- 모든 `.md` 파일에 YAML frontmatter 포함

---

## 참고 자료 (기존 문서)

코드 생성 시 다음 문서의 패턴을 참조한다:

| 문서 | 경로 |
|------|------|
| DDD 전술적 설계 개요 | `Docs.Site/src/content/docs/guides/domain/04-ddd-tactical-overview.md` |
| 값 객체 | `Docs.Site/src/content/docs/guides/domain/05a-value-objects.md` |
| 값 객체 검증 | `Docs.Site/src/content/docs/guides/domain/05b-value-objects-validation.md` |
| Union 값 객체 | `Docs.Site/src/content/docs/guides/domain/05c-union-value-objects.md` |
| Aggregate 설계 | `Docs.Site/src/content/docs/guides/domain/06a-aggregate-design.md` |
| Entity/Aggregate 핵심 | `Docs.Site/src/content/docs/guides/domain/06b-entity-aggregate-core.md` |
| Entity/Aggregate 고급 | `Docs.Site/src/content/docs/guides/domain/06c-entity-aggregate-advanced.md` |
| 도메인 이벤트 | `Docs.Site/src/content/docs/guides/domain/07-domain-events.md` |
| 에러 시스템 | `Docs.Site/src/content/docs/guides/domain/08a-error-system.md` |
| 에러: Domain/App | `Docs.Site/src/content/docs/guides/domain/08b-error-system-domain-app.md` |
| 도메인 서비스 | `Docs.Site/src/content/docs/guides/domain/09-domain-services.md` |
| Specification | `Docs.Site/src/content/docs/guides/domain/10-specifications.md` |
| 단위 테스트 | `Docs.Site/src/content/docs/guides/testing/15a-unit-testing.md` |

### 실전 예제 프로젝트

| 용도 | 경로 |
|------|------|
| 도메인 코드 예제 | `Docs.Site/src/content/docs/samples/designing-with-types/DesigningWithTypes/` |
| 단위 테스트 예제 | `Docs.Site/src/content/docs/samples/designing-with-types/DesigningWithTypes.Tests.Unit/` |
| 설계 문서 예제 | `Docs.Site/src/content/docs/samples/designing-with-types/*.md` |

---

## 핵심 API 패턴 요약

| 패턴 | 사용법 |
|------|--------|
| `Fin<T>` 성공 확인 | `result.IsSucc`, `result.IsFail` (IsSuccess가 아님) |
| `Fin<T>` 값 추출 | `result.ThrowIfFail()` |
| 성공 반환 | `unit` (`using static LanguageExt.Prelude;` 필요) |
| 실패 반환 | `DomainError.For<{Type}>(new {ErrorRecord}(), id, message)` |
| 팩토리 메서드 | `Fin.Succ(value)`, `Fin.Fail<T>(error)` (Fin 클래스의 static) |
| EntityId 생성 | `{Type}Id.New()` (Ulid 기반, Guid 아님) |
| 검증 병렬 합성 | `(Fin1, Fin2).Apply((v1, v2) => ...)` |
| `ThenMatches()` | `Regex` 파라미터 필요, `[GeneratedRegex]` 패턴 사용 |
| Union Match | 자동 생성된 `Match()` / `Switch()` 메서드 사용 |
| 상태 전이 | `TransitionFrom<TFrom, TTo>(mapper)` |
