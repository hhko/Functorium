using System.Diagnostics;
using BulkCrud.Benchmarks.Helpers;
using LanguageExt;
using LayeredArch.Domain.AggregateRoots.Products;
using Shouldly;
using Xunit;

namespace BulkCrud.Benchmarks.Tests;

public sealed class InMemoryBulkCrudPerfTests : IDisposable
{
    public InMemoryBulkCrudPerfTests()
    {
        TestDataGenerator.ClearInMemoryProducts();
    }

    [Fact]
    public async Task CreateRange_100K_Products_Completes_Under_500ms()
    {
        // Arrange
        var products = TestDataGenerator.GenerateProducts(100_000);
        var repo = TestDataGenerator.CreateInMemoryRepo();

        // Act
        var sw = Stopwatch.StartNew();
        var result = await repo.CreateRange(products).Run().RunAsync();
        sw.Stop();

        // Assert - 정합성
        result.IsSucc.ShouldBeTrue();

        // Assert - 성능
        sw.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    [Fact]
    public async Task GetByIds_100K_Products_Completes_Under_500ms()
    {
        // Arrange: 10만건 사전 등록
        var products = TestDataGenerator.GenerateProducts(100_000);
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.CreateRange(products).Run().RunAsync();
        var ids = products.Select(p => p.Id).ToList();

        // Act
        var sw = Stopwatch.StartNew();
        var result = await repo.GetByIds(ids).Run().RunAsync();
        sw.Stop();

        // Assert - 정합성
        result.IsSucc.ShouldBeTrue();

        // Assert - 성능
        sw.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    [Fact]
    public async Task UpdateRange_100K_Products_Completes_Under_500ms()
    {
        // Arrange
        var products = TestDataGenerator.GenerateProducts(100_000);
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.CreateRange(products).Run().RunAsync();

        // Act
        var sw = Stopwatch.StartNew();
        var result = await repo.UpdateRange(products).Run().RunAsync();
        sw.Stop();

        // Assert - 정합성
        result.IsSucc.ShouldBeTrue();

        // Assert - 성능
        sw.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    [Fact]
    public async Task DeleteRange_100K_Products_Completes_Under_500ms()
    {
        // Arrange
        var products = TestDataGenerator.GenerateProducts(100_000);
        var repo = TestDataGenerator.CreateInMemoryRepo();
        await repo.CreateRange(products).Run().RunAsync();
        var ids = products.Select(p => p.Id).ToList();

        // Act
        var sw = Stopwatch.StartNew();
        var result = await repo.DeleteRange(ids).Run().RunAsync();
        sw.Stop();

        // Assert - 정합성
        result.IsSucc.ShouldBeTrue();

        // Assert - 성능
        sw.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    [Fact]
    public async Task SingleItem_Create_Loop_Is_Slower_Than_CreateRange()
    {
        // Arrange
        var products1 = TestDataGenerator.GenerateProducts(10_000);
        var products2 = TestDataGenerator.GenerateProducts(10_000);

        // 단건 루프
        TestDataGenerator.ClearInMemoryProducts();
        var repo1 = TestDataGenerator.CreateInMemoryRepo();
        var sw1 = Stopwatch.StartNew();
        foreach (var p in products1)
            await repo1.Create(p).Run().RunAsync();
        sw1.Stop();

        // 벌크
        TestDataGenerator.ClearInMemoryProducts();
        var repo2 = TestDataGenerator.CreateInMemoryRepo();
        var sw2 = Stopwatch.StartNew();
        await repo2.CreateRange(products2).Run().RunAsync();
        sw2.Stop();

        // 벌크가 더 빠름을 검증
        sw2.ElapsedMilliseconds.ShouldBeLessThan(sw1.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        TestDataGenerator.ClearInMemoryProducts();
    }
}
