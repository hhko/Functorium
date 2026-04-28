namespace EfCoreRepository;

/// <summary>
/// Product ↔ ProductModel 간의 매핑을 담당합니다.
/// EfCoreRepositoryBase의 ToDomain/ToModel 메서드에서 사용됩니다.
/// </summary>
public static class ProductMapper
{
    /// <summary>
    /// 퍼시스턴스 모델 → 도메인 모델 변환.
    /// DB에서 읽은 ProductModel을 Product Aggregate로 복원합니다.
    /// </summary>
    public static Product ToDomain(ProductModel model)
    {
        return new Product(
            ProductId.Create(model.Id),
            model.Name,
            model.Price,
            model.IsActive);
    }

    /// <summary>
    /// 도메인 모델 → 퍼시스턴스 모델 변환.
    /// Product Aggregate를 DB에 저장할 ProductModel로 변환합니다.
    /// </summary>
    public static ProductModel ToModel(Product aggregate)
    {
        return new ProductModel
        {
            Id = aggregate.Id.ToString(),
            Name = aggregate.Name,
            Price = aggregate.Price,
            IsActive = aggregate.IsActive,
        };
    }
}
