using System.Collections.Concurrent;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using InMemoryQueryAdapter;
using LanguageExt;
using SortDirection = Functorium.Applications.Queries.SortDirection;

namespace InMemoryQueryAdapter.Tests.Unit;

public sealed class InMemoryOrderSummaryQueryTests
{
    private static (InMemoryOrderSummaryQuery Query, Product Keyboard, Product Mouse, Product Monitor)
        CreateQueryWithSampleData()
    {
        var productStore = new ConcurrentDictionary<ProductId, Product>();
        var keyboard = new Product(ProductId.New(), "Keyboard", 89_000m, 50, "Electronics");
        var mouse = new Product(ProductId.New(), "Mouse", 35_000m, 100, "Electronics");
        var monitor = new Product(ProductId.New(), "Monitor", 350_000m, 20, "Electronics");
        productStore[keyboard.Id] = keyboard;
        productStore[mouse.Id] = mouse;
        productStore[monitor.Id] = monitor;

        var query = new InMemoryOrderSummaryQuery(productStore);
        query.AddOrder(Order.Create(keyboard.Id, 2, keyboard.Price));   // 178,000
        query.AddOrder(Order.Create(mouse.Id, 5, mouse.Price));         // 175,000
        query.AddOrder(Order.Create(keyboard.Id, 1, keyboard.Price));   //  89,000
        query.AddOrder(Order.Create(monitor.Id, 1, monitor.Price));     // 350,000

        return (query, keyboard, mouse, monitor);
    }

    [Fact]
    public async Task Search_T1_AllSpec_T2_ShouldReturnJoinedResults_T3()
    {
        // Arrange
        var (query, _, _, _) = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            Specification<Order>.All,
            new PageRequest(1, 10),
            SortExpression.By("ProductName"))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(4);
        paged.Items.ShouldAllBe(dto => !string.IsNullOrEmpty(dto.ProductName));
    }

    [Fact]
    public async Task Search_T1_AllSpec_T2_ShouldProjectProductFields_T3()
    {
        // Arrange
        var (query, keyboard, _, _) = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            Specification<Order>.All,
            new PageRequest(1, 10),
            SortExpression.By("ProductName"))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        var keyboardOrder = paged.Items.First(dto => dto.ProductName == "Keyboard");
        keyboardOrder.UnitPrice.ShouldBe(89_000m);
        keyboardOrder.Category.ShouldBe("Electronics");
    }

    [Fact]
    public async Task Search_T1_OrderHasProductSpec_T2_ShouldFilterByProduct_T3()
    {
        // Arrange
        var (query, keyboard, _, _) = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            new OrderHasProductSpec(keyboard.Id),
            new PageRequest(1, 10),
            SortExpression.By("Quantity"))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(2);
        paged.Items.ShouldAllBe(dto => dto.ProductName == "Keyboard");
    }

    [Fact]
    public async Task Search_T1_SortByTotalAmountDesc_T2_ShouldReturnSorted_T3()
    {
        // Arrange
        var (query, _, _, _) = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            Specification<Order>.All,
            new PageRequest(1, 10),
            SortExpression.By("TotalAmount", SortDirection.Descending))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        paged.Items[0].TotalAmount.ShouldBe(350_000m);   // Monitor x1
        paged.Items[^1].TotalAmount.ShouldBe(89_000m);   // Keyboard x1
    }

    [Fact]
    public async Task Stream_T1_AllSpec_T2_ShouldStreamJoinedResults_T3()
    {
        // Arrange
        var (query, _, _, _) = CreateQueryWithSampleData();
        var items = new List<OrderSummaryDto>();

        // Act
        await foreach (var item in query.Stream(
            Specification<Order>.All, SortExpression.By("ProductName")))
        {
            items.Add(item);
        }

        // Assert
        items.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Search_T1_NoMatchingProduct_T2_ShouldReturnEmpty_T3()
    {
        // Arrange - 존재하지 않는 Product를 참조하는 고아 Order
        var productStore = new ConcurrentDictionary<ProductId, Product>();
        var query = new InMemoryOrderSummaryQuery(productStore);
        query.AddOrder(Order.Create(ProductId.New(), 1, 10_000m));

        // Act
        var result = await query.Search(
            Specification<Order>.All,
            new PageRequest(1, 10),
            SortExpression.By("ProductName"))
            .Run().RunAsync();

        // Assert - inner join이므로 매칭 안 되면 0건
        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(0);
    }
}
