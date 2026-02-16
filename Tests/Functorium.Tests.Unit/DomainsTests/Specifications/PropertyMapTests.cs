using Functorium.Domains.Specifications.Expressions;

namespace Functorium.Tests.Unit.DomainsTests.Specifications;

public class PropertyMapTests
{
    // 테스트용 Entity / Model (내부 클래스)
    private class TestEntity
    {
        public decimal Price { get; init; }
        public string Name { get; init; } = "";
        public int Stock { get; init; }
    }

    private class TestModel
    {
        public decimal Price { get; init; }
        public string Name { get; init; } = "";
        public int StockQuantity { get; init; }
    }

    private class ConvertTestEntity
    {
        public object Price { get; init; } = 0m;
    }

    private class ToStringTestEntity
    {
        public object Id { get; init; } = "";
    }

    private class StringIdModel
    {
        public string Id { get; init; } = "";
    }

    private static PropertyMap<TestEntity, TestModel> CreateSut() =>
        new PropertyMap<TestEntity, TestModel>()
            .Map(e => e.Price, m => m.Price)
            .Map(e => e.Name, m => m.Name)
            .Map(e => e.Stock, m => m.StockQuantity);

    [Fact]
    public void Translate_RewritesMemberAccess_WhenDirectPropertyAccess()
    {
        // Arrange
        var sut = CreateSut();
        var minPrice = 100m;

        // Act
        var actual = sut.Translate(e => e.Price >= minPrice);

        // Assert
        var compiled = actual.Compile();
        compiled(new TestModel { Price = 150m }).ShouldBeTrue();
        compiled(new TestModel { Price = 50m }).ShouldBeFalse();
    }

    [Fact]
    public void Translate_RewritesMemberAccess_WhenDifferentPropertyNames()
    {
        // Arrange
        var sut = CreateSut();
        var threshold = 5;

        // Act
        var actual = sut.Translate(e => e.Stock < threshold);

        // Assert
        var compiled = actual.Compile();
        compiled(new TestModel { StockQuantity = 3 }).ShouldBeTrue();
        compiled(new TestModel { StockQuantity = 10 }).ShouldBeFalse();
    }

    [Fact]
    public void Translate_RewritesStringComparison_WhenStringProperty()
    {
        // Arrange
        var sut = CreateSut();
        var name = "Test";

        // Act
        var actual = sut.Translate(e => e.Name == name);

        // Assert
        var compiled = actual.Compile();
        compiled(new TestModel { Name = "Test" }).ShouldBeTrue();
        compiled(new TestModel { Name = "Other" }).ShouldBeFalse();
    }

    [Fact]
    public void Translate_RewritesComplexExpression_WhenMultipleConditions()
    {
        // Arrange
        var sut = CreateSut();
        var minPrice = 100m;
        var maxPrice = 200m;

        // Act
        var actual = sut.Translate(e => e.Price >= minPrice && e.Price <= maxPrice);

        // Assert
        var compiled = actual.Compile();
        compiled(new TestModel { Price = 150m }).ShouldBeTrue();
        compiled(new TestModel { Price = 50m }).ShouldBeFalse();
        compiled(new TestModel { Price = 250m }).ShouldBeFalse();
    }

    [Fact]
    public void Translate_HandlesConvertExpression_WhenExplicitCastUsed()
    {
        // Arrange: Value Object → primitive 변환 시뮬레이션
        var sut = new PropertyMap<ConvertTestEntity, TestModel>()
            .Map(e => (decimal)e.Price, m => m.Price);

        var min = 100m;

        // Act
        var actual = sut.Translate(e => (decimal)e.Price >= min);

        // Assert
        var compiled = actual.Compile();
        compiled(new TestModel { Price = 150m }).ShouldBeTrue();
        compiled(new TestModel { Price = 50m }).ShouldBeFalse();
    }

    [Fact]
    public void Translate_HandlesToStringCall_WhenToStringMapped()
    {
        // Arrange: EntityId.ToString() → Model.Id 매핑
        var sut = new PropertyMap<ToStringTestEntity, StringIdModel>()
            .Map(e => e.Id.ToString(), m => m.Id);

        var targetId = "ABC123";

        // Act
        var actual = sut.Translate(e => e.Id.ToString() == targetId);

        // Assert
        var compiled = actual.Compile();
        compiled(new StringIdModel { Id = "ABC123" }).ShouldBeTrue();
        compiled(new StringIdModel { Id = "OTHER" }).ShouldBeFalse();
    }
}
