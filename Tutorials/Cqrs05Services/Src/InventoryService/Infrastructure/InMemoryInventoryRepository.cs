using System.Collections.Concurrent;
using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using InventoryService.Domain;
using static LanguageExt.Prelude;

namespace InventoryService.Infrastructure;

/// <summary>
/// 메모리 기반 재고 리포지토리 구현
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 구현
/// GeneratePipeline 애트리뷰트로 파이프라인 버전 자동 생성
/// </summary>
[GeneratePipeline]
public class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly ILogger<InMemoryInventoryRepository> _logger;
    private readonly ConcurrentDictionary<Guid, InventoryItem> _inventoryItems = new();

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "Repository";

    /// <summary>
    /// 테스트용 생성자
    /// </summary>
    public InMemoryInventoryRepository(ILogger<InMemoryInventoryRepository> logger)
    {
        _logger = logger;
    }

    public virtual FinT<IO, InventoryItem> Create(InventoryItem item)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            _inventoryItems[item.Id] = item;
            return Fin.Succ(item);
        });
    }

    public virtual FinT<IO, InventoryItem> GetByProductId(Guid productId)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            var item = _inventoryItems.Values.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                return Fin.Succ(item);
            }

            return Fin.Fail<InventoryItem>(Error.New($"상품 ID '{productId}'에 대한 재고를 찾을 수 없습니다"));
        });
    }

    public virtual FinT<IO, InventoryItem> ReserveQuantity(Guid productId, int quantity)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            var item = _inventoryItems.Values.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
            {
                return Fin.Fail<InventoryItem>(Error.New($"상품 ID '{productId}'에 대한 재고를 찾을 수 없습니다"));
            }

            var reserveResult = item.ReserveQuantity(quantity);
            return reserveResult.Match(
                Succ: updatedItem =>
                {
                    _inventoryItems[updatedItem.Id] = updatedItem;
                    return Fin.Succ(updatedItem);
                },
                Fail: error => Fin.Fail<InventoryItem>(error));
        });
    }
}

