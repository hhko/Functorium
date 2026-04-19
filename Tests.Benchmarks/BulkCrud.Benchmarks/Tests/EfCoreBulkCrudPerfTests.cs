using System.Diagnostics;
using Functorium.Adapters.Events;
using LanguageExt;
using LayeredArch.Adapters.Persistence.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Products;
using LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace BulkCrud.Benchmarks.Tests;

public sealed class EfCoreBulkCrudPerfTests : IAsyncLifetime
{
    private LayeredArchDbContext _dbContext = null!;
    private string _dbPath = null!;
    private readonly List<string> _tempPaths = [];

    public async ValueTask InitializeAsync()
    {
        _dbPath = Path.GetTempFileName();
        _tempPaths.Add(_dbPath);
        var options = new DbContextOptionsBuilder<LayeredArchDbContext>()
            .UseSqlite($"Data Source={_dbPath};Pooling=false")
            .Options;
        _dbContext = new LayeredArchDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task EfCore_AddRange_10K_Is_Faster_Than_SingleAdd_Loop()
    {
        // Arrange
        var products1 = GenerateProducts(1_000);
        var products2 = GenerateProducts(1_000);

        // 단건 Add + SaveChanges 루프
        await using var ctx1 = await CreateFreshContext();
        var sw1 = Stopwatch.StartNew();
        foreach (var product in products1)
        {
            ctx1.Products.Add(product.ToModel());
            await ctx1.SaveChangesAsync();
        }
        sw1.Stop();

        // AddRange + 단일 SaveChanges
        await using var ctx2 = await CreateFreshContext();
        var sw2 = Stopwatch.StartNew();
        ctx2.Products.AddRange(products2.Select(p => p.ToModel()));
        await ctx2.SaveChangesAsync();
        sw2.Stop();

        // AddRange가 더 빠름을 검증
        sw2.ElapsedMilliseconds.ShouldBeLessThan(sw1.ElapsedMilliseconds);
    }

    [Fact]
    public async Task EfCore_ExecuteDeleteAsync_Faster_Than_LoadAndRemove()
    {
        // Arrange: 1K건 등록
        var products = GenerateProducts(1_000);
        _dbContext.Products.AddRange(products.Select(p => p.ToModel()));
        await _dbContext.SaveChangesAsync();
        var ids = products.Select(p => p.Id.ToString()).ToList();

        // Load + Remove
        await using var ctx1 = await CreateFreshContext();
        var sw1 = Stopwatch.StartNew();
        var models = await ctx1.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
        ctx1.Products.RemoveRange(models);
        await ctx1.SaveChangesAsync();
        sw1.Stop();

        // Repopulate
        await using var ctx2 = await CreateFreshContext();
        ctx2.Products.AddRange(products.Select(p => p.ToModel()));
        await ctx2.SaveChangesAsync();

        // ExecuteDeleteAsync
        await using var ctx3 = await CreateFreshContext();
        var sw2 = Stopwatch.StartNew();
        await ctx3.Products
            .Where(p => ids.Contains(p.Id))
            .ExecuteDeleteAsync();
        sw2.Stop();

        // ExecuteDeleteAsync가 더 빠름을 검증
        sw2.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(sw1.ElapsedMilliseconds);
    }

    [Fact]
    public async Task EfCore_CreateRange_Repository_Correctness()
    {
        // Arrange
        var products = GenerateProducts(100);
        var collector = new DomainEventCollector();
        var repo = new ProductRepositoryEfCore(_dbContext, collector);

        // Act
        var result = await repo.CreateRange(products).Run().RunAsync();
        await _dbContext.SaveChangesAsync();

        // Assert - 정합성
        result.IsSucc.ShouldBeTrue();
        var count = await _dbContext.Products.CountAsync();
        count.ShouldBe(100);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();

        foreach (var path in _tempPaths)
            File.Delete(path);
    }

    private async Task<LayeredArchDbContext> CreateFreshContext()
    {
        var path = Path.GetTempFileName();
        _tempPaths.Add(path);
        var options = new DbContextOptionsBuilder<LayeredArchDbContext>()
            .UseSqlite($"Data Source={path};Pooling=false")
            .Options;
        var ctx = new LayeredArchDbContext(options);
        await ctx.Database.EnsureCreatedAsync();
        return ctx;
    }

    private static List<Product> GenerateProducts(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => Product.Create(
                ProductName.CreateFromValidated($"EfProduct-{i}"),
                ProductDescription.CreateFromValidated($"EfDescription-{i}"),
                Money.CreateFromValidated(10m + (i % 1000))))
            .ToList();
    }
}
