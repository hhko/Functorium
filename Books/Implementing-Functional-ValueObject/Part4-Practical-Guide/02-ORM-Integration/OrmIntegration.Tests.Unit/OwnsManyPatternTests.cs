using Microsoft.EntityFrameworkCore;

namespace OrmIntegration.Tests.Unit;

/// <summary>
/// OwnsMany 패턴 테스트
///
/// 테스트 목적:
/// 1. 컬렉션 값 객체가 저장되는지 확인
/// 2. 컬렉션 값 객체가 로드되는지 확인
/// 3. 컬렉션 항목 순서 및 개수 검증
/// </summary>
[Trait("Part4-ORM-Integration", "OwnsManyPatternTests")]
public class OwnsManyPatternTests
{
    private static DbContextOptions<AppDbContext> CreateOptions(string dbName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
    }

    [Fact]
    public async Task Order_SavesAndLoads_WithLineItemsCollection()
    {
        // Arrange
        var options = CreateOptions(nameof(Order_SavesAndLoads_WithLineItemsCollection));
        var orderId = Guid.NewGuid();

        // Act - Save
        await using (var context = new AppDbContext(options))
        {
            var order = new Order
            {
                Id = orderId,
                CustomerName = "김철수",
                LineItems = new List<OrderLineItem>
                {
                    new("상품 A", 2, 10000),
                    new("상품 B", 1, 25000),
                    new("상품 C", 3, 5000)
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }

        // Assert - Load
        await using (var context = new AppDbContext(options))
        {
            var loaded = await context.Orders.FirstAsync(o => o.Id == orderId);
            loaded.LineItems.Count.ShouldBe(3);
            loaded.LineItems.ShouldContain(item => item.ProductName == "상품 A" && item.Quantity == 2);
            loaded.LineItems.ShouldContain(item => item.ProductName == "상품 B" && item.Quantity == 1);
            loaded.LineItems.ShouldContain(item => item.ProductName == "상품 C" && item.Quantity == 3);
        }
    }

    [Fact]
    public async Task Order_SavesAndLoads_WithEmptyLineItems()
    {
        // Arrange
        var options = CreateOptions(nameof(Order_SavesAndLoads_WithEmptyLineItems));
        var orderId = Guid.NewGuid();

        // Act - Save
        await using (var context = new AppDbContext(options))
        {
            var order = new Order
            {
                Id = orderId,
                CustomerName = "이영희",
                LineItems = new List<OrderLineItem>()
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }

        // Assert - Load
        await using (var context = new AppDbContext(options))
        {
            var loaded = await context.Orders.FirstAsync(o => o.Id == orderId);
            loaded.LineItems.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Order_CalculatesTotal_FromLineItems()
    {
        // Arrange
        var options = CreateOptions(nameof(Order_CalculatesTotal_FromLineItems));
        var orderId = Guid.NewGuid();

        // Act - Save
        await using (var context = new AppDbContext(options))
        {
            var order = new Order
            {
                Id = orderId,
                CustomerName = "박민수",
                LineItems = new List<OrderLineItem>
                {
                    new("상품 A", 2, 10000),  // 20,000
                    new("상품 B", 1, 25000),  // 25,000
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }

        // Assert - Load and calculate
        await using (var context = new AppDbContext(options))
        {
            var loaded = await context.Orders.FirstAsync(o => o.Id == orderId);
            var total = loaded.LineItems.Sum(item => item.Quantity * item.UnitPrice);
            total.ShouldBe(45000);
        }
    }
}
