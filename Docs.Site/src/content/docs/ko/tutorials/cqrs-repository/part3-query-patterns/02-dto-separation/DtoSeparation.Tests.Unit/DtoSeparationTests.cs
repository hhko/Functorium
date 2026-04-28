using DtoSeparation;

namespace DtoSeparation.Tests.Unit;

public sealed class DtoSeparationTests
{
    [Fact]
    public void CreateProductRequest_T1_CommandInputDto_T2_ShouldContainOnlyWriteFields_T3()
    {
        // Arrange & Act
        var request = new CreateProductRequest(
            "Keyboard", "Mechanical keyboard", 89_000m, 50, "Electronics");

        // Assert - 서버가 생성하는 Id, CreatedAt은 포함하지 않음
        request.Name.ShouldBe("Keyboard");
        request.Description.ShouldBe("Mechanical keyboard");
        request.Price.ShouldBe(89_000m);
        request.Stock.ShouldBe(50);
        request.Category.ShouldBe("Electronics");
    }

    [Fact]
    public void CreateProductResponse_T1_CommandOutputDto_T2_ShouldContainMinimalConfirmation_T3()
    {
        // Arrange
        var id = ProductId.New().ToString();
        var createdAt = DateTime.UtcNow;

        // Act
        var response = new CreateProductResponse(id, "Keyboard", createdAt);

        // Assert - 생성 확인에 필요한 최소 필드만 포함
        response.Id.ShouldBe(id);
        response.Name.ShouldBe("Keyboard");
        response.CreatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void ProductListDto_T1_QueryListDto_T2_ShouldExcludeHeavyFields_T3()
    {
        // Arrange & Act
        var dto = new ProductListDto(
            ProductId.New().ToString(), "Keyboard", 89_000m, "Electronics");

        // Assert - 목록에 필요한 최소 필드만 포함 (Description, Stock 제외)
        dto.Name.ShouldBe("Keyboard");
        dto.Price.ShouldBe(89_000m);
        dto.Category.ShouldBe("Electronics");
    }

    [Fact]
    public void ProductDetailDto_T1_QueryDetailDto_T2_ShouldContainAllFields_T3()
    {
        // Arrange
        var id = ProductId.New().ToString();
        var createdAt = DateTime.UtcNow;

        // Act
        var dto = new ProductDetailDto(
            id, "Keyboard", "Mechanical keyboard", 89_000m, 50, "Electronics", createdAt, null);

        // Assert - 상세 조회에 필요한 모든 필드 포함
        dto.Id.ShouldBe(id);
        dto.Name.ShouldBe("Keyboard");
        dto.Description.ShouldBe("Mechanical keyboard");
        dto.Price.ShouldBe(89_000m);
        dto.Stock.ShouldBe(50);
        dto.Category.ShouldBe("Electronics");
        dto.CreatedAt.ShouldBe(createdAt);
        dto.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void ProductListDto_vs_ProductDetailDto_T1_FieldCount_T2_ListShouldHaveFewerFields_T3()
    {
        // Arrange & Act
        var listFields = typeof(ProductListDto).GetProperties().Length;
        var detailFields = typeof(ProductDetailDto).GetProperties().Length;

        // Assert - 목록 DTO는 상세 DTO보다 필드가 적어야 함
        listFields.ShouldBeLessThan(detailFields);
    }

    [Fact]
    public void CommandDto_vs_QueryDto_T1_DifferentPurpose_T2_ShouldHaveDifferentShapes_T3()
    {
        // Arrange
        var id = ProductId.New().ToString();

        // Act - 같은 Product이지만 용도에 따라 다른 DTO
        var commandInput = new CreateProductRequest("Keyboard", "Desc", 89_000m, 50, "Electronics");
        var commandOutput = new CreateProductResponse(id, "Keyboard", DateTime.UtcNow);
        var queryList = new ProductListDto(id, "Keyboard", 89_000m, "Electronics");
        var queryDetail = new ProductDetailDto(id, "Keyboard", "Desc", 89_000m, 50, "Electronics", DateTime.UtcNow, null);

        // Assert - 각 DTO는 서로 다른 타입
        commandInput.ShouldNotBeNull();
        commandOutput.ShouldNotBeNull();
        queryList.ShouldNotBeNull();
        queryDetail.ShouldNotBeNull();

        // Command 입력에는 Id가 없고, Command 출력에는 Id가 있음
        typeof(CreateProductRequest).GetProperty("Id").ShouldBeNull();
        typeof(CreateProductResponse).GetProperty("Id").ShouldNotBeNull();
    }
}
