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
}
