using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.SharedModels.Entities;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;

internal static class TagMapper
{
    public static TagModel ToModel(this Tag tag) => new()
    {
        Id = tag.Id.ToString(),
        Name = tag.Name,
        CreatedAt = tag.CreatedAt,
        UpdatedAt = tag.UpdatedAt.ToNullable()
    };

    public static Tag ToDomain(this TagModel model) =>
        Tag.CreateFromValidated(
            TagId.Create(model.Id),
            TagName.CreateFromValidated(model.Name),
            model.CreatedAt,
            Optional(model.UpdatedAt));
}
