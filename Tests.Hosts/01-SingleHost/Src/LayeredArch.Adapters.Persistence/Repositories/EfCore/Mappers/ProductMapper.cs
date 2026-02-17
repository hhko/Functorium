using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;

internal static class ProductMapper
{
    public static ProductModel ToModel(this Product product) => new()
    {
        Id = product.Id.ToString(),
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
        Tags = product.Tags.Select(t => t.ToModel(product.Id.ToString())).ToList()
    };

    public static Product ToDomain(this ProductModel model)
    {
        var product = Product.CreateFromValidated(
            ProductId.Create(model.Id),
            ProductName.CreateFromValidated(model.Name),
            ProductDescription.CreateFromValidated(model.Description),
            Money.CreateFromValidated(model.Price),
            model.CreatedAt,
            model.UpdatedAt);

        foreach (var tag in model.Tags)
            product.AddTag(tag.ToDomain());

        // AddTag이 발행한 이벤트는 복원 과정의 부산물이므로 제거
        product.ClearDomainEvents();

        return product;
    }
}
