namespace HexagonalMapping.Strategy2.OneWayMapping.Model;

/// <summary>
/// 공통 인터페이스: Domain 엔티티와 Adapter 모델 모두 이 인터페이스를 구현합니다.
/// One-Way Mapping 전략의 핵심 구성요소입니다.
///
/// 제한사항: 이 인터페이스는 데이터 접근자만 노출해야 하며,
/// 비즈니스 로직 메서드는 포함하면 안 됩니다.
/// </summary>
public interface IProductModel
{
    Guid Id { get; }
    string Name { get; }
    decimal Price { get; }
    string Currency { get; }
}
