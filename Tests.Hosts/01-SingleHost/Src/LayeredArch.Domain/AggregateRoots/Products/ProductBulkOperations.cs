using static LanguageExt.Prelude;

namespace LayeredArch.Domain.AggregateRoots.Products;

/// <summary>
/// 상품 벌크 연산 Domain Service.
/// 여러 Aggregate를 조율하는 벌크 연산과 해당 도메인 이벤트를 소유합니다.
/// </summary>
public static class ProductBulkOperations
{
    #region Domain Events

    /// <summary>
    /// 상품 벌크 삭제 이벤트 — Domain Service가 발행
    /// </summary>
    public sealed record BulkDeletedEvent(Seq<ProductId> DeletedIds, string DeletedBy) : DomainEvent;

    /// <summary>
    /// 상품 벌크 생성 이벤트 — Domain Service가 발행
    /// </summary>
    public sealed record BulkCreatedEvent(Seq<ProductId> CreatedIds) : DomainEvent;

    #endregion

    /// <summary>
    /// 여러 상품을 벌크 삭제합니다.
    /// 각 Aggregate의 상태를 변경하고, 개별 이벤트는 정리합니다.
    /// 벌크 이벤트는 호출자(Use Case)가 EventCollector에 추적합니다.
    /// </summary>
    public static (Seq<Product> Deleted, BulkDeletedEvent Event) BulkDelete(
        IReadOnlyList<Product> products, string deletedBy)
    {
        foreach (var product in products)
        {
            product.Delete(deletedBy);
            product.ClearDomainEvents();
        }

        var ids = toSeq(products.Select(p => p.Id));
        return (toSeq(products), new BulkDeletedEvent(ids, deletedBy));
    }

    /// <summary>
    /// 여러 상품을 벌크 생성합니다.
    /// 각 Aggregate의 상태를 변경하고, 개별 이벤트는 정리합니다.
    /// 벌크 이벤트는 호출자(Use Case)가 EventCollector에 추적합니다.
    /// </summary>
    public static (Seq<Product> Created, BulkCreatedEvent Event) BulkCreate(
        IReadOnlyList<Product> products)
    {
        var ids = toSeq(products.Select(p => p.Id));
        foreach (var product in products)
            product.ClearDomainEvents();

        return (toSeq(products), new BulkCreatedEvent(ids));
    }
}
