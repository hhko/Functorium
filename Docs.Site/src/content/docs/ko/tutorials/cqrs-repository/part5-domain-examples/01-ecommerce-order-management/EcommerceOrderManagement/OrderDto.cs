namespace EcommerceOrderManagement;

/// <summary>
/// 주문 조회용 DTO.
/// Query 측에서 사용하며 도메인 모델과 분리됩니다.
/// </summary>
public sealed record OrderDto(
    string Id,
    string CustomerName,
    decimal TotalAmount,
    string Status,
    int LineCount);
