using Functorium.Domains.Observabilities;

namespace LayeredArch.Application.Ports;

/// <summary>
/// 외부 가격 조회 서비스 Port 인터페이스
/// Infrastructure Adapter에서 구현합니다.
/// </summary>
public interface IExternalPricingService : IPort
{
    /// <summary>
    /// 외부 API에서 상품 가격을 조회합니다.
    /// </summary>
    /// <param name="productCode">상품 코드</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>조회된 가격 (Money)</returns>
    FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken cancellationToken);

    /// <summary>
    /// 외부 API에서 여러 상품의 가격을 일괄 조회합니다.
    /// </summary>
    /// <param name="productCodes">상품 코드 목록</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>상품 코드와 가격의 매핑</returns>
    FinT<IO, Map<string, Money>> GetPricesAsync(Seq<string> productCodes, CancellationToken cancellationToken);
}

/// <summary>
/// 외부 가격 API 응답 DTO
/// </summary>
public sealed record ExternalPriceResponse(
    string ProductCode,
    decimal Price,
    string Currency,
    DateTime ValidUntil);
