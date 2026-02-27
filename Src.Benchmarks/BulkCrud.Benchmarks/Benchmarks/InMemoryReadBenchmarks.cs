using BenchmarkDotNet.Attributes;
using BulkCrud.Benchmarks.Helpers;
using LanguageExt;
using LayeredArch.Domain.AggregateRoots.Products;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// InMemory Read: 페이징(100) vs 페이징(10K) vs GetByIds
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class InMemoryReadBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int Count;

    private List<ProductId> _ids = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        TestDataGenerator.ClearInMemoryProducts();
        var products = TestDataGenerator.GenerateProducts(Count);
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.CreateRange(products).Run().RunAsync();
        _ids = products.Select(p => p.Id).ToList();
    }

    [Benchmark(Baseline = true, Description = "GetById Loop")]
    public async Task GetById_Loop()
    {
        var repo = TestDataGenerator.CreateInMemoryRepo();
        foreach (var id in _ids)
            await repo.GetById(id).Run().RunAsync();
    }

    [Benchmark(Description = "GetByIds Bulk")]
    public async Task GetByIds_Bulk()
    {
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.GetByIds(_ids).Run().RunAsync();
    }
}
