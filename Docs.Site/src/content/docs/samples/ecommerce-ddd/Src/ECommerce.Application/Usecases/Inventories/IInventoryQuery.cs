using Functorium.Applications.Queries;
using ECommerce.Domain.AggregateRoots.Inventories;

namespace ECommerce.Application.Usecases.Inventories.Ports;

/// <summary>
/// Inventory 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IInventoryQuery : IQueryPort<Inventory, InventorySummaryDto> { }

public sealed record InventorySummaryDto(
    string InventoryId,
    string ProductId,
    int StockQuantity);
