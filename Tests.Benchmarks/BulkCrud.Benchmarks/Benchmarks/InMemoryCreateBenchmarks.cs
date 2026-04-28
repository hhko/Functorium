using BenchmarkDotNet.Attributes;
using BulkCrud.Benchmarks.Helpers;
using LanguageExt;
using LayeredArch.Domain.AggregateRoots.Products;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// InMemory Create: 단건 루프 vs CreateRange
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class InMemoryCreateBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int Count;

    private List<Product> _products = null!;

    [GlobalSetup]
    public void Setup()
    {
        _products = TestDataGenerator.GenerateProducts(Count);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        TestDataGenerator.ClearInMemoryProducts();
    }

    [Benchmark(Baseline = true, Description = "SingleCreate Loop")]
    public async Task SingleCreate_Loop()
    {
        var repo = TestDataGenerator.CreateInMemoryRepo();
        foreach (var product in _products)
            await repo.Create(product).Run().RunAsync();
    }

    [Benchmark(Description = "CreateRange Bulk")]
    public async Task CreateRange_Bulk()
    {
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.CreateRange(_products).Run().RunAsync();
    }
}
