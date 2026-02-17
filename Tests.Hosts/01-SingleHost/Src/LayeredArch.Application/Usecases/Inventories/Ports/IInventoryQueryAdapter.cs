using Functorium.Applications.Queries;
using LayeredArch.Application.Usecases.Inventories.Dtos;
using LayeredArch.Domain.AggregateRoots.Inventories;

namespace LayeredArch.Application.Usecases.Inventories.Ports;

/// <summary>
/// Inventory 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IInventoryQueryAdapter : IQueryAdapter<Inventory, InventorySummaryDto> { }
