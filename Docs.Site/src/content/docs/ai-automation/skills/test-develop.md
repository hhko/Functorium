---
title: "테스트 개발 스킬"
---

## 배경

Functorium 프로젝트의 테스트 코드는 일관된 규칙을 따릅니다. T1_T2_T3 명명 규칙, AAA 패턴, Shouldly 검증, NSubstitute Mock, `FinTFactory` 헬퍼, ArchUnitNET 아키텍처 규칙 등 — 이 패턴들을 매번 수동으로 작성하면 반복적이고 누락이 발생하기 쉽습니다.

`/test-develop` 스킬은 이 반복을 자동화합니다. 테스트 대상과 시나리오를 전달하면, 프로젝트 테스트 규칙에 맞는 단위 테스트, 통합 테스트, 아키텍처 규칙 테스트를 생성합니다.

## 스킬 개요

### 테스트 유형

| 테스트 유형 | 대상 | 도구 | 설명 |
|------------|------|------|------|
| Value Object 단위 테스트 | `SimpleValueObject`, `ValueObject`, `UnionValueObject` | Shouldly | Create 성공/실패, Normalize, 에러 코드 검증 |
| AggregateRoot 단위 테스트 | `AggregateRoot<TId>` | Shouldly | 커맨드 메서드, 이벤트 발행, 불변식 검증 |
| Usecase 단위 테스트 | `ICommandUsecase`, `IQueryUsecase` | NSubstitute, `FinTFactory` | Mock 기반 성공/실패 시나리오 |
| 통합 테스트 | FastEndpoints | `HostTestFixture<TProgram>` | HTTP 요청/응답, StatusCode 검증 |
| 아키텍처 규칙 테스트 | 레이어 의존성, 네이밍 | ArchUnitNET | sealed class, 레이어 침범, 네이밍 규칙 |

### 핵심 규칙

| 규칙 | 설명 |
|------|------|
| T1_T2_T3 명명 | `Handle_ShouldReturnSuccess_WhenRequestIsValid` |
| AAA 패턴 | `sut` (테스트 대상), `actual` (실행 결과), `expected` (기대값) |
| Shouldly 검증 | `actual.IsSucc.ShouldBeTrue()`, `actual.ThrowIfFail().Name.ShouldBe("value")` |
| NSubstitute Mock | `Substitute.For<T>()`, `.Returns(FinTFactory.Succ(value))` |
| `FinTFactory` | `FinTFactory.Succ(value)` / `FinTFactory.Fail<T>(error)` |

## 사용 방법

### 기본 호출

```text
/test-develop ProductName Value Object 단위 테스트를 작성해줘.
```

### 대화형 모드

인자 없이 `/test-develop`만 호출하면, 스킬이 대화형으로 테스트 대상과 시나리오를 수집합니다.

### 실행 흐름

1. **대상 분석** — 테스트 대상 코드를 읽고 테스트 시나리오를 식별합니다
2. **사용자 확인** — 시나리오 목록을 확인한 후 테스트 생성으로 진행합니다
3. **테스트 생성** — T1_T2_T3 명명 규칙과 AAA 패턴으로 테스트를 생성합니다
4. **테스트 실행** — `dotnet test`를 실행하여 통과를 확인합니다

## 예제 1: 초급 — Value Object 단위 테스트

가장 기본적인 테스트입니다. `SimpleValueObject`의 `Create` 성공/실패, `Validate` 검증, Normalize 동작, 에러 코드 검증을 AAA 패턴으로 작성합니다.

### 프롬프트

```text
/test-develop ProductName Value Object 단위 테스트를 작성해줘.
```

### 기대 결과

| 테스트 | 메서드명 | 설명 |
|--------|---------|------|
| 성공 | `Create_ShouldReturnSuccess_WhenNameIsValid` | 유효한 이름으로 생성 성공 |
| 실패 (null) | `Create_ShouldReturnFail_WhenNameIsNull` | null 입력 시 실패 |
| 실패 (empty) | `Create_ShouldReturnFail_WhenNameIsEmpty` | 빈 문자열 시 실패 |
| 실패 (max) | `Create_ShouldReturnFail_WhenNameExceedsMaxLength` | 최대 길이 초과 시 실패 |
| Normalize | `Create_ShouldTrimWhitespace_WhenNameHasLeadingTrailingSpaces` | 앞뒤 공백 제거 |
| 에러 코드 | `Validate_ShouldReturnExpectedErrorCode_WhenNameIsEmpty` | 에러 코드 검증 |

### 핵심 스니펫

**Value Object 단위 테스트** — T1_T2_T3, AAA 패턴, Shouldly 검증:

```csharp
public class ProductNameTests
{
    [Fact]
    public void Create_ShouldReturnSuccess_WhenNameIsValid()
    {
        // Arrange
        var name = "Valid Product Name";

        // Act
        var actual = ProductName.Create(name);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(name);
    }

    [Fact]
    public void Create_ShouldReturnFail_WhenNameIsEmpty()
    {
        // Arrange
        var name = "";

        // Act
        var actual = ProductName.Create(name);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimWhitespace_WhenNameHasLeadingTrailingSpaces()
    {
        // Arrange
        var name = "  Trimmed Name  ";

        // Act
        var actual = ProductName.Create(name);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe("Trimmed Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnFail_WhenNameIsNullOrWhitespace(string? name)
    {
        // Act
        var actual = ProductName.Create(name);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
```

## 예제 2: 중급 — Usecase 단위 테스트 (Mock)

예제 1에 Usecase 테스트를 추가합니다. NSubstitute로 Repository를 Mock하고, `FinTFactory.Succ`/`FinTFactory.Fail`로 성공/실패 시나리오를 구성합니다. 중복 검사, 검증 실패 등 다양한 시나리오를 다룹니다.

### 프롬프트

```text
/test-develop CreateProductCommand Usecase 단위 테스트를 작성해줘.
```

### 기대 결과

| 테스트 | 메서드명 | 설명 |
|--------|---------|------|
| 성공 | `Handle_ShouldReturnSuccess_WhenRequestIsValid` | 유효 요청 → 성공 응답 |
| 실패 (검증) | `Handle_ShouldReturnFailure_WhenNameIsEmpty` | VO 검증 실패 |
| 실패 (중복) | `Handle_ShouldReturnFailure_WhenDuplicateName` | 이름 중복 → AlreadyExists |

### 핵심 스니펫

**Usecase 단위 테스트** — NSubstitute Mock, `FinTFactory`, AAA 패턴:

```csharp
public class CreateProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly CreateProductCommand.Usecase _sut;

    public CreateProductCommandTests()
    {
        _sut = new CreateProductCommand.Usecase(_productRepository, _inventoryRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        _productRepository.Exists(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(false));
        _productRepository.Create(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
        _inventoryRepository.Create(Arg.Any<Inventory>())
            .Returns(call => FinTFactory.Succ(call.Arg<Inventory>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Test Product");
        actual.ThrowIfFail().Price.ShouldBe(100m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateProductCommand.Request("", "Description", 100m, 10);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDuplicateName()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Existing Product", "Description", 100m, 10);

        _productRepository.Exists(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(true));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
```

## 예제 3: 고급 — 통합 테스트 + 아키텍처 규칙

예제 2에 통합 테스트와 아키텍처 규칙 테스트를 추가합니다. `HostTestFixture<Program>`으로 실제 HTTP 서버를 구동하여 엔드포인트를 테스트하고, ArchUnitNET으로 레이어 의존성과 sealed class 규칙을 검증합니다.

### 프롬프트

```text
/test-develop 상품 API 통합 테스트와 도메인 아키텍처 규칙 테스트를 작성해줘.
```

### 기대 결과

| 테스트 유형 | 클래스 | 설명 |
|------------|--------|------|
| 통합 테스트 | `CreateProductEndpointTests` | POST /api/products, 201/400 검증 |
| 아키텍처 규칙 | `DomainArchitectureRuleTests` | sealed class, AggregateRoot 상속 |
| 아키텍처 규칙 | `LayerDependencyArchitectureRuleTests` | 레이어 의존성 위반 검사 |

### 핵심 스니펫

**통합 테스트** — `HostTestFixture<Program>`, HttpClient, StatusCode 검증:

```csharp
public class LayeredArchFixture : HostTestFixture<Program> { }

public abstract class IntegrationTestBase : IClassFixture<LayeredArchFixture>
{
    protected HttpClient Client { get; }
    protected IntegrationTestBase(LayeredArchFixture fixture) => Client = fixture.Client;
}

public class CreateProductEndpointTests : IntegrationTestBase
{
    public CreateProductEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateProduct_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange
        var request = new
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content
            .ReadFromJsonAsync<CreateProductEndpoint.Response>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(request.Name);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn400BadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new { Name = "", Description = "Desc", Price = 100.00m, StockQuantity = 10 };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
```

**아키텍처 규칙 테스트** — ArchUnitNET, 레이어 의존성 검증:

```csharp
public sealed class LayerDependencyArchitectureRuleTests
{
    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }
}
```

**도메인 아키텍처 규칙** — AggregateRoot 상속 검증:

```csharp
public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override ArchUnitNET.Domain.Architecture Architecture => ArchitectureTestBase.Architecture;
    protected override string DomainNamespace => ArchitectureTestBase.DomainNamespace;

    [Fact]
    public void AggregateRoot_ShouldInherit_AggregateRootBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(AggregateRoot<>)),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Inheritance Rule");
    }
}
```

## 참고 자료

### 프레임워크 가이드

- [단위 테스트 가이드](../guides/testing/15a-unit-testing/)
- [통합 테스트 가이드](../guides/testing/15b-integration-testing/)
- [테스트 라이브러리](../guides/testing/16-testing-library/)
- [에러 시스템: Adapter & 테스트](../guides/domain/08c-error-system-adapter-testing/)

### 관련 스킬

- [도메인 개발 스킬](./domain-develop/) — Aggregate, Value Object, Event 등 도메인 빌딩블록 생성
- [Application 레이어 개발 스킬](./application-develop/) — Command/Query/EventHandler 유스케이스 생성
- [Adapter 레이어 개발 스킬](./adapter-develop/) — Repository, Query Adapter, Endpoint, DI 등록 생성
