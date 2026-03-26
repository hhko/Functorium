using Functorium.Testing.Assertions.Errors;
using LayeredArch.Adapters.Persistence.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Inventories;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LayeredArch.Tests.Unit.Persistence;

public class EfCoreUnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreUnitOfWorkTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LayeredArchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new LayeredArchDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveChanges_ReturnsConcurrencyConflictError_WhenRowVersionIsStale()
    {
        // Arrange: Raw SQL로 Inventory 삽입 (DB가 RowVersion을 직접 생성)
        var inventoryId = InventoryId.New().ToString();
        var productId = ProductId.New().ToString();
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO Inventories (Id, ProductId, StockQuantity, RowVersion, CreatedAt) VALUES ({0}, {1}, {2}, randomblob(8), {3})",
            inventoryId, productId, 100, DateTime.UtcNow);

        // EF Core 트랙킹을 위해 엔티티 로드
        var model = await _dbContext.Inventories.FindAsync(inventoryId);
        model.ShouldNotBeNull();

        // DB의 RowVersion을 직접 변경 (다른 트랜잭션이 먼저 수정한 것을 시뮬레이션)
        await _dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE Inventories SET RowVersion = randomblob(8) WHERE Id = {0}", inventoryId);

        // Act: 트랙킹된 엔티티를 수정하여 UPDATE를 유발
        model.StockQuantity = 50;

        var sut = new UnitOfWorkEfCore(_dbContext);
        var actual = await sut.SaveChanges().Run().RunAsync();

        // Assert: ConcurrencyConflict 에러로 변환되어야 함
        actual.ShouldBeAdapterExceptionalError<UnitOfWorkEfCore, LanguageExt.Unit>(
            new UnitOfWorkEfCore.ConcurrencyConflict());
    }

    [Fact]
    public async Task SaveChanges_ReturnsDatabaseUpdateFailedError_WhenConstraintViolation()
    {
        // Arrange: Raw SQL로 첫 번째 Inventory 삽입
        var sharedProductId = ProductId.New().ToString();
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO Inventories (Id, ProductId, StockQuantity, RowVersion, CreatedAt) VALUES ({0}, {1}, {2}, randomblob(8), {3})",
            InventoryId.New().ToString(), sharedProductId, 100, DateTime.UtcNow);

        // Act: 같은 ProductId로 두 번째 Inventory 추가 (unique constraint 위반)
        _dbContext.Inventories.Add(new InventoryModel
        {
            Id = InventoryId.New().ToString(),
            ProductId = sharedProductId,
            StockQuantity = 50,
            RowVersion = [1, 2, 3, 4, 5, 6, 7, 8],
            CreatedAt = DateTime.UtcNow
        });

        var sut = new UnitOfWorkEfCore(_dbContext);
        var actual = await sut.SaveChanges().Run().RunAsync();

        // Assert: DatabaseUpdateFailed 에러로 변환되어야 함
        actual.ShouldBeAdapterExceptionalError<UnitOfWorkEfCore, LanguageExt.Unit>(
            new UnitOfWorkEfCore.DatabaseUpdateFailed());
    }

    [Fact]
    public async Task SaveChanges_ReturnsSuccess_WhenNoConflict()
    {
        // Arrange: Raw SQL로 Inventory 삽입
        var inventoryId = InventoryId.New().ToString();
        await _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO Inventories (Id, ProductId, StockQuantity, RowVersion, CreatedAt) VALUES ({0}, {1}, {2}, randomblob(8), {3})",
            inventoryId, ProductId.New().ToString(), 100, DateTime.UtcNow);

        var model = await _dbContext.Inventories.FindAsync(inventoryId);
        model.ShouldNotBeNull();

        // Act: 값 수정 후 SaveChanges
        model.StockQuantity = 80;

        var sut = new UnitOfWorkEfCore(_dbContext);
        var actual = await sut.SaveChanges().Run().RunAsync();

        // Assert: 성공 및 변경사항 영속화 확인
        // Note: SQLite는 SQL Server rowversion처럼 UPDATE 시 자동 갱신을 지원하지 않아
        //   RowVersion 변경 검증 대신 ReloadAsync()로 영속화를 확인한다.
        //   동시성 충돌 감지는 SaveChanges_ReturnsConcurrencyConflictError_WhenRowVersionIsStale에서 증명한다.
        actual.IsSucc.ShouldBeTrue();
        await _dbContext.Entry(model).ReloadAsync();
        model.StockQuantity.ShouldBe(80);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
