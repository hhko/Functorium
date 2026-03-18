using UnitOfWork;

namespace UnitOfWork.Tests.Unit;

public sealed class InMemoryUnitOfWorkTests
{
    [Fact]
    public async Task SaveChanges_ReturnsSuccess()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();

        // Act
        var result = await uow.SaveChanges().Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_SetsIsSavedToTrue()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();
        uow.IsSaved.ShouldBeFalse();

        // Act
        await uow.SaveChanges().Run().RunAsync();

        // Assert
        uow.IsSaved.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_ExecutesPendingActions()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();
        var executed = false;
        uow.AddPendingAction(() => executed = true);

        // Act
        await uow.SaveChanges().Run().RunAsync();

        // Assert
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_ExecutesMultiplePendingActions_InOrder()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();
        var results = new List<int>();
        uow.AddPendingAction(() => results.Add(1));
        uow.AddPendingAction(() => results.Add(2));
        uow.AddPendingAction(() => results.Add(3));

        // Act
        await uow.SaveChanges().Run().RunAsync();

        // Assert
        results.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task SaveChanges_ClearsPendingActions_AfterExecution()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();
        var count = 0;
        uow.AddPendingAction(() => count++);

        // Act
        await uow.SaveChanges().Run().RunAsync();
        await uow.SaveChanges().Run().RunAsync();

        // Assert
        count.ShouldBe(1);
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsTransaction()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();

        // Act
        await using var tx = await uow.BeginTransactionAsync();

        // Assert
        tx.ShouldNotBeNull();
    }

    [Fact]
    public async Task Transaction_CommitAsync_CompletesSuccessfully()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();
        await using var tx = await uow.BeginTransactionAsync();

        // Act & Assert (no exception)
        await tx.CommitAsync();
    }

    [Fact]
    public async Task SaveChanges_CommitsBothAggregates_WhenMultipleActionsRegistered()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();
        var productStore = new Dictionary<ProductId, Product>();
        var orderStore = new Dictionary<OrderId, Order>();

        var product = Product.Create("노트북", 1_500_000m, stock: 10);
        productStore[product.Id] = product;
        var order = Order.Create(product.Id, quantity: 2, unitPrice: product.Price);

        uow.AddPendingAction(() => orderStore[order.Id] = order);
        uow.AddPendingAction(() => product.DeductStock(2));

        // Act
        var result = await uow.SaveChanges().Run().RunAsync();

        // Assert
        result.IsSucc.ShouldBeTrue();
        orderStore.Count.ShouldBe(1);
        product.Stock.ShouldBe(8);
    }

    [Fact]
    public async Task SaveChanges_NeitherApplied_BeforeSaveChangesCall()
    {
        // Arrange
        var uow = new InMemoryUnitOfWork();
        var orderStore = new Dictionary<OrderId, Order>();
        var product = Product.Create("노트북", 1_500_000m, stock: 10);
        var order = Order.Create(product.Id, 2, product.Price);

        uow.AddPendingAction(() => orderStore[order.Id] = order);
        uow.AddPendingAction(() => product.DeductStock(2));

        // Assert — SaveChanges 전에는 둘 다 미반영
        orderStore.Count.ShouldBe(0);
        product.Stock.ShouldBe(10);
    }

    [Fact]
    public void DeductStock_ThrowsException_WhenInsufficientStock()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m, stock: 1);

        // Act & Assert — 재고보다 많은 수량 차감 시 예외 발생
        var ex = Should.Throw<InvalidOperationException>(() => product.DeductStock(100));
        ex.Message.ShouldContain("재고 부족");
    }
}
