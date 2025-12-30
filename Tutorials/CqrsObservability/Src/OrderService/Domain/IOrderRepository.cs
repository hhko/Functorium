using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;

namespace OrderService.Domain;

/// <summary>
/// 주문 리포지토리 인터페이스
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 상속
/// </summary>
public interface IOrderRepository : IAdapter
{
    /// <summary>
    /// 주문 생성
    /// </summary>
    FinT<IO, Order> Create(Order order);

    /// <summary>
    /// ID로 주문 조회.
    /// 주문이 없으면 실패(Error)를 반환합니다.
    /// </summary>
    FinT<IO, Order> GetById(Guid id);
}

