using LanguageExt;

namespace QueryUsecase;

public interface IProductQuery
{
    FinT<IO, List<ProductDto>> SearchByName(string keyword);
    FinT<IO, ProductDto> GetById(ProductId id);
}
