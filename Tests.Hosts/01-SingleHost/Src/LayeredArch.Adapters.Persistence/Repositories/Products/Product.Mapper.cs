using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Tags;

namespace LayeredArch.Adapters.Persistence.Repositories.Products;

internal static class ProductMapper
{
    public static ProductModel ToModel(this Product product)
    {
        var productId = product.Id.ToString();
        return new()
        {
            Id = productId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt.ToNullable(),
            DeletedAt = product.DeletedAt.ToNullable(),
            DeletedBy = product.DeletedBy.Match(Some: v => (string?)v, None: () => null),
            ProductTags = product.TagIds.Select(tagId => new ProductTagModel
            {
                ProductId = productId,
                TagId = tagId.ToString()
            }).ToList()
        };
    }

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
            Optional(model.UpdatedAt),
            Optional(model.DeletedAt),
            Optional(model.DeletedBy));
    }
}
