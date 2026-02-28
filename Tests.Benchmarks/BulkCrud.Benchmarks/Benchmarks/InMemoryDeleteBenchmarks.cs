using BenchmarkDotNet.Attributes;
using BulkCrud.Benchmarks.Helpers;
using LanguageExt;
using LayeredArch.Domain.AggregateRoots.Products;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// InMemory Delete: 단건 루프 vs DeleteRange
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class InMemoryDeleteBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int Count;

    private List<Product> _products = null!;
    private List<ProductId> _ids = null!;

    [GlobalSetup]
    public void Setup()
    {
        _products = TestDataGenerator.GenerateProducts(Count);
        _ids = _products.Select(p => p.Id).ToList();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        TestDataGenerator.ClearInMemoryProducts();
        var repo = TestDataGenerator.CreateInMemoryRepo();
        repo.CreateRange(_products).Run().RunAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "SingleDelete Loop")]
    public async Task SingleDelete_Loop()
    {
        var repo = TestDataGenerator.CreateInMemoryRepo();
        foreach (var id in _ids)
            await repo.Delete(id).Run().RunAsync();
    }

    [Benchmark(Description = "DeleteRange Bulk")]
    public async Task DeleteRange_Bulk()
    {
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.DeleteRange(_ids).Run().RunAsync();
    }
}
