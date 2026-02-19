using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// InMemory 기반 Product 단건 조회 읽기 전용 어댑터.
/// InMemoryProductRepository의 정적 저장소에서 데이터를 가져온 후 DTO로 프로젝션합니다.
/// </summary>
[GeneratePipeline]
public class InMemoryProductDetailQueryAdapter : IProductDetailQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, ProductDetailDto> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (InMemoryProductRepository.Products.TryGetValue(id, out var product))
            {
                return Fin.Succ(new ProductDetailDto(
                    product.Id.ToString(),
                    product.Name,
                    product.Description,
                    product.Price,
                    product.CreatedAt,
                    product.UpdatedAt.ToNullable()));
            }

            return AdapterError.For<InMemoryProductDetailQueryAdapter>(
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }
}
