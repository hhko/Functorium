# 단위 테스트 패턴 레퍼런스

## 공통 설정

### Global Usings (Using.cs)

```csharp
global using Xunit;
global using Shouldly;
global using LanguageExt;
global using LanguageExt.Common;
global using Functorium.Abstractions.Errors;
global using VerifyXunit;

global using static LanguageExt.Prelude;
global using static VerifyXunit.Verifier;
```

### Trait 상수 (선택)

```csharp
[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
```

## AAA 패턴 (Arrange-Act-Assert)

| 단계 | 변수명 | 설명 |
|------|--------|------|
| Arrange | `sut`, `request` | 테스트 준비 |
| Act | `actual` | 실행 결과 |
| Assert | - | Shouldly로 검증 |

## Value Object 테스트

### Create 성공/실패

```csharp
public class ProductNameTests
{
    [Theory]
    [InlineData("Laptop")]
    [InlineData("a")]
    public void Create_ShouldSucceed_WhenValueIsValid(string value)
    {
        // Act
        var actual = ProductName.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = ProductName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', ProductName.MaxLength + 1);

        // Act
        var actual = ProductName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
```

### Normalize 검증

```csharp
[Fact]
public void Create_ShouldTrimValue()
{
    // Act
    var actual = ProductName.Create("  Laptop  ").ThrowIfFail();

    // Assert
    ((string)actual).ShouldBe("Laptop");
}
```

### 에러 코드 검증

```csharp
[Fact]
public void For_WithDomainErrorType_CreatesErrorWithCorrectErrorCode_WhenEmpty()
{
    // Arrange
    var currentValue = "";
    var message = "Value cannot be empty";

    // Act
    var actual = DomainError.For<TestValueObject>(new DomainErrorType.Empty(), currentValue, message);

    // Assert
    actual.ShouldBeOfType<ErrorCodeExpected>();
    actual.Message.ShouldBe(message);
    actual.IsExpected.ShouldBeTrue();

    var errorCode = (ErrorCodeExpected)actual;
    errorCode.ErrorCode.ShouldBe("DomainErrors.TestValueObject.Empty");
}
```

## AggregateRoot 테스트

### 상태 변경 + DomainEvent 발행 검증

```csharp
public class ProductTests
{
    private static Product CreateSampleProduct(
        string name = "Test Product",
        string description = "Test Description",
        decimal price = 100m)
    {
        return Product.Create(
            ProductName.Create(name).ThrowIfFail(),
            ProductDescription.Create(description).ThrowIfFail(),
            Money.Create(price).ThrowIfFail());
    }

    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Act
        var sut = CreateSampleProduct();

        // Assert
        sut.Id.ShouldNotBe(default);
        sut.DomainEvents.ShouldContain(e => e is Product.CreatedEvent);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Act
        var sut = CreateSampleProduct(name: "Laptop", description: "Good laptop", price: 1500m);

        // Assert
        ((string)sut.Name).ShouldBe("Laptop");
        ((string)sut.Description).ShouldBe("Good laptop");
        ((decimal)sut.Price).ShouldBe(1500m);
    }

    [Fact]
    public void Update_ShouldPublishUpdatedEvent()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.ClearDomainEvents();

        var newName = ProductName.Create("Updated Name").ThrowIfFail();
        var newDescription = ProductDescription.Create("Updated Desc").ThrowIfFail();
        var newPrice = Money.Create(200m).ThrowIfFail();

        // Act
        var actual = sut.Update(newName, newDescription, newPrice);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.DomainEvents.ShouldContain(e => e is Product.UpdatedEvent);
        ((string)sut.Name).ShouldBe("Updated Name");
    }
}
```

### 이벤트 상세 검증

```csharp
[Fact]
public void AssignTag_ShouldAddTagIdAndPublishEvent()
{
    // Arrange
    var sut = CreateSampleProduct();
    sut.ClearDomainEvents();
    var tagId = TagId.New();

    // Act
    sut.AssignTag(tagId);

    // Assert
    sut.TagIds.ShouldContain(tagId);
    var assignedEvent = sut.DomainEvents.OfType<Product.TagAssignedEvent>().ShouldHaveSingleItem();
    assignedEvent.ProductId.ShouldBe(sut.Id);
    assignedEvent.TagId.ShouldBe(tagId);
}
```

## Command Usecase 테스트

### NSubstitute + FinTFactory 패턴

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

## 핵심 Mock 패턴

### FinTFactory 반환값

```csharp
// 성공 반환
_repository.GetById(Arg.Any<ProductId>())
    .Returns(FinTFactory.Succ(someProduct));

// 인자를 그대로 성공으로 반환
_repository.Create(Arg.Any<Product>())
    .Returns(call => FinTFactory.Succ(call.Arg<Product>()));

// 실패 반환
_repository.GetById(Arg.Any<ProductId>())
    .Returns(FinTFactory.Fail<Product>(Error.New("not found")));

// bool 성공 반환
_repository.Exists(Arg.Any<Specification<Product>>())
    .Returns(FinTFactory.Succ(false));
```

## T1_T2_T3 명명 규칙

| 구성요소 | 설명 | 예시 |
|---------|------|------|
| **T1** | 테스트 대상 메서드명 | `Create`, `Handle`, `Validate` |
| **T2** | 예상 결과 | `ShouldSucceed`, `ShouldFail`, `ShouldReturnSuccess`, `ShouldPublishEvent` |
| **T3** | 테스트 시나리오 | `WhenValueIsValid`, `WhenNameIsEmpty`, `WhenDuplicateName` |

### 예시

```
Create_ShouldSucceed_WhenValueIsValid
Create_ShouldFail_WhenValueIsEmptyOrNull
Handle_ShouldReturnSuccess_WhenRequestIsValid
Handle_ShouldReturnFailure_WhenDuplicateName
Update_ShouldPublishUpdatedEvent
```

## Theory + InlineData 패턴

```csharp
[Theory]
[InlineData("Laptop")]
[InlineData("a")]
[InlineData("Very Long Product Name")]
public void Create_ShouldSucceed_WhenValueIsValid(string value)
{
    var actual = ProductName.Create(value);
    actual.IsSucc.ShouldBeTrue();
}

[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("   ")]
public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
{
    var actual = ProductName.Create(value);
    actual.IsFail.ShouldBeTrue();
}
```

## 테스트 실행 (MTP 모드)

```bash
# 전체 테스트
dotnet test --solution Functorium.slnx

# 특정 프로젝트
dotnet test --project Tests/Functorium.Tests.Unit

# 메서드 필터
dotnet test -- --filter-method "Handle_ShouldReturnSuccess"

# 클래스 필터
dotnet test -- --filter-class "MyNamespace.ProductNameTests"
```
