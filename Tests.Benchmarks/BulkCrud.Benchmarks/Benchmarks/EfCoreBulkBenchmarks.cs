using BenchmarkDotNet.Attributes;
using BulkCrud.Benchmarks.Helpers;
using Functorium.Adapters.Events;
using LanguageExt;
using LayeredArch.Adapters.Persistence.Repositories.EfCore;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.EntityFrameworkCore;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// EF Core SQLite: 단건 Add+SaveChanges 루프 vs AddRange+SaveChanges
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EfCoreBulkBenchmarks
{
    [Params(1_000, 10_000)]
    public int Count;

    private List<Product> _products = null!;

    [GlobalSetup]
    public void Setup()
    {
        _products = TestDataGenerator.GenerateProducts(Count);
    }

    [Benchmark(Baseline = true, Description = "SingleAdd + SaveChanges Loop")]
    public async Task SingleAdd_Loop()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        foreach (var product in _products)
        {
            dbContext.Products.Add(product.ToModel());
            await dbContext.SaveChangesAsync();
        }
    }

    [Benchmark(Description = "AddRange + Single SaveChanges")]
    public async Task AddRange_SingleSave()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Products.AddRange(_products.Select(p => p.ToModel()));
        await dbContext.SaveChangesAsync();
    }

    [Benchmark(Description = "AddRange via Repository")]
    public async Task AddRange_Repository()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();

        var collector = new DomainEventCollector();
        var repo = new EfCoreProductRepository(dbContext, collector);
        await repo.CreateRange(_products).Run().RunAsync();
        await dbContext.SaveChangesAsync();
    }

    private static LayeredArchDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LayeredArchDbContext>()
            .UseSqlite($"Data Source={Path.GetTempFileName()}")
            .Options;
        return new LayeredArchDbContext(options);
    }
}
