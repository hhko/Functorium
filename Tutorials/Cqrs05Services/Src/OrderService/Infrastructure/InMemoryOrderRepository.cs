using System.Collections.Concurrent;
using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using OrderService.Domain;
using static LanguageExt.Prelude;

namespace OrderService.Infrastructure;

/// <summary>
/// 메모리 기반 주문 리포지토리 구현
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 구현
/// GeneratePipeline 애트리뷰트로 파이프라인 버전 자동 생성
/// </summary>
[GeneratePipeline]
public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ILogger<InMemoryOrderRepository> _logger;
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "Repository";

    /// <summary>
    /// 테스트용 생성자
    /// </summary>
    public InMemoryOrderRepository(ILogger<InMemoryOrderRepository> logger)
    {
        _logger = logger;
    }

    public virtual FinT<IO, Order> Create(Order order)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            _orders[order.Id] = order;
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Order> GetById(Guid id)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            if (_orders.TryGetValue(id, out Order? order))
            {
                return Fin.Succ(order);
            }

            return Fin.Fail<Order>(Error.New($"주문 ID '{id}'을(를) 찾을 수 없습니다"));
        });
    }
}

