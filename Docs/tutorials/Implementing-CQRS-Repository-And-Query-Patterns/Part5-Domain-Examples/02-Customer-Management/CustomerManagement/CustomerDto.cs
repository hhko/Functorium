namespace CustomerManagement;

/// <summary>
/// 고객 조회용 DTO.
/// </summary>
public sealed record CustomerDto(
    string Id,
    string Name,
    string Email,
    decimal CreditLimit);
