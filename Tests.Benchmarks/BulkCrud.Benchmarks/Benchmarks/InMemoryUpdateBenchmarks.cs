using BenchmarkDotNet.Attributes;
using BulkCrud.Benchmarks.Helpers;
using LanguageExt;
using LayeredArch.Domain.AggregateRoots.Products;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// InMemory Update: 단건 루프 vs UpdateRange
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class InMemoryUpdateBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int Count;

    private List<Product> _products = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        TestDataGenerator.ClearInMemoryProducts();
        _products = TestDataGenerator.GenerateProducts(Count);
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.CreateRange(_products).Run().RunAsync();
    }

    [Benchmark(Baseline = true, Description = "SingleUpdate Loop")]
    public async Task SingleUpdate_Loop()
    {
        var repo = TestDataGenerator.CreateInMemoryRepo();
        foreach (var product in _products)
            await repo.Update(product).Run().RunAsync();
    }

    [Benchmark(Description = "UpdateRange Bulk")]
    public async Task UpdateRange_Bulk()
    {
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.UpdateRange(_products).Run().RunAsync();
    }
}
