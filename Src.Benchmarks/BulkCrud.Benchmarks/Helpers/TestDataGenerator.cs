using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using LayeredArch.Adapters.Persistence.Repositories.InMemory;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace BulkCrud.Benchmarks.Helpers;

internal static class TestDataGenerator
{
    public static List<Product> GenerateProducts(int count)
    {
        var products = new List<Product>(count);
        for (int i = 0; i < count; i++)
        {
            products.Add(Product.Create(
                ProductName.CreateFromValidated($"Product-{i}"),
                ProductDescription.CreateFromValidated($"Description-{i}"),
                Money.CreateFromValidated(10m + (i % 1000))));
        }
        return products;
    }

    public static IDomainEventCollector CreateCollector() => new DomainEventCollector();

    public static InMemoryProductRepository CreateInMemoryRepo(IDomainEventCollector? collector = null)
    {
        collector ??= CreateCollector();
        return new InMemoryProductRepository(collector);
    }

    public static void ClearInMemoryProducts()
    {
        InMemoryProductRepository.Products.Clear();
    }
}
