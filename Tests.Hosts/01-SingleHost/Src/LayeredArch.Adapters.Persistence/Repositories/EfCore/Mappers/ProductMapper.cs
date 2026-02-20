using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.Entities;

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
        UpdatedAt = product.UpdatedAt.ToNullable(),
        ProductTags = product.TagIds.Select(tagId => new ProductTagModel
        {
            ProductId = product.Id.ToString(),
            TagId = tagId.ToString()
        }).ToList()
    };

    public static Product ToDomain(this ProductModel model)
    {
        var tagIds = model.ProductTags.Select(pt => TagId.Create(pt.TagId));

        return Product.CreateFromValidated(
            ProductId.Create(model.Id),
            ProductName.CreateFromValidated(model.Name),
            ProductDescription.CreateFromValidated(model.Description),
            Money.CreateFromValidated(model.Price),
            tagIds,
            model.CreatedAt,
            Optional(model.UpdatedAt));
    }
}
