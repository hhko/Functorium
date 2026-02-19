using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Domain.SharedModels.Entities;
using LayeredArch.Domain.SharedModels.Entities.ValueObjects;

namespace LayeredArch.Tests.Unit.Persistence.Mappers;

public class TagMapperTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        // Arrange
        var tag = Tag.Create(TagName.Create("test-tag").ThrowIfFail());
        var productId = "01JMTEST000000000000000000";

        // Act
        var model = tag.ToModel(productId);
        var actual = model.ToDomain();

        // Assert
        actual.Id.ToString().ShouldBe(tag.Id.ToString());
        ((string)actual.Name).ShouldBe(tag.Name);
        model.ProductId.ShouldBe(productId);
    }
}
