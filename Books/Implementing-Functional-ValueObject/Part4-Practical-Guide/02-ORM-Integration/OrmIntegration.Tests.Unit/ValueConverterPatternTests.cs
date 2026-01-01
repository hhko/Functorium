using Microsoft.EntityFrameworkCore;

namespace OrmIntegration.Tests.Unit;

/// <summary>
/// Value Converter 패턴 테스트
///
/// 테스트 목적:
/// 1. HasConversion을 통한 값 객체 저장 검증
/// 2. 값 객체가 단일 컬럼으로 저장되는지 확인
/// 3. 저장/로드 시 값 변환이 올바른지 확인
/// </summary>
[Trait("Part4-ORM-Integration", "ValueConverterPatternTests")]
public class ValueConverterPatternTests
{
    private static DbContextOptions<AppDbContext> CreateOptions(string dbName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
    }

    [Fact]
    public async Task Product_SavesAndLoads_WithProductCodeValueConverter()
    {
        // Arrange
        var options = CreateOptions(nameof(Product_SavesAndLoads_WithProductCodeValueConverter));
        var productId = Guid.NewGuid();
        var code = ProductCode.CreateFromValidated("EL-001234");

        // Act - Save
        await using (var context = new AppDbContext(options))
        {
            var product = new Product
            {
                Id = productId,
                Code = code,
                Price = Money.CreateFromValidated(50000, "KRW")
            };
            context.Products.Add(product);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert - Load
        await using (var context = new AppDbContext(options))
        {
            var loaded = await context.Products.FirstAsync(p => p.Id == productId, TestContext.Current.CancellationToken);
            ((string)loaded.Code).ShouldBe("EL-001234");
        }
    }

    [Fact]
    public async Task Product_SavesAndLoads_WithMoneyOwnsOne()
    {
        // Arrange
        var options = CreateOptions(nameof(Product_SavesAndLoads_WithMoneyOwnsOne));
        var productId = Guid.NewGuid();

        // Act - Save
        await using (var context = new AppDbContext(options))
        {
            var product = new Product
            {
                Id = productId,
                Code = ProductCode.CreateFromValidated("BK-000001"),
                Price = Money.CreateFromValidated(25000, "KRW")
            };
            context.Products.Add(product);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert - Load
        await using (var context = new AppDbContext(options))
        {
            var loaded = await context.Products.FirstAsync(p => p.Id == productId, TestContext.Current.CancellationToken);
            loaded.Price.Amount.ShouldBe(25000);
            loaded.Price.Currency.ShouldBe("KRW");
        }
    }
}
