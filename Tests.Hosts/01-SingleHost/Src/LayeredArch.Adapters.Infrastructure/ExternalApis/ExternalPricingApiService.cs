using System.Net;
using System.Net.Http.Json;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Application.Ports;
using LanguageExt;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Adapters.Infrastructure.ExternalApis;

/// <summary>
/// 외부 가격 조회 API 서비스 구현
/// 예외를 명시적 실패(Fin.Fail)로 처리하는 패턴을 보여줍니다.
/// </summary>
[GenerateObservablePort]
public class ExternalPricingApiService : IExternalPricingService
{
    #region Error Types

    public sealed record OperationCancelled : AdapterErrorType.Custom;
    public sealed record UnexpectedException : AdapterErrorType.Custom;
    public sealed record PriceConversionFailed : AdapterErrorType.Custom;
    public sealed record RateLimited : AdapterErrorType.Custom;
    public sealed record HttpError : AdapterErrorType.Custom;

    #endregion

    private readonly HttpClient _httpClient;

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "ExternalApi";

    public ExternalPricingApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 외부 API에서 상품 가격을 조회합니다.
    /// 예외 처리 패턴:
    /// 1. HTTP 오류 응답 → AdapterError.For (Custom 또는 표준 타입)
    /// 2. 응답 데이터 null → AdapterError.For (Null)
    /// 3. 예외 발생 → AdapterError.FromException
    /// </summary>
    public virtual FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/pricing/{productCode}",
                    cancellationToken);

                // HTTP 오류 응답 처리
                if (!response.IsSuccessStatusCode)
                {
                    return HandleHttpError<Money>(response, productCode);
                }

                // 응답 역직렬화
                var priceResponse = await response.Content
                    .ReadFromJsonAsync<ExternalPriceResponse>(cancellationToken: cancellationToken);

                // null 응답 처리
                if (priceResponse is null)
                {
                    return AdapterError.For<ExternalPricingApiService>(
                        new Null(),
                        productCode,
                        $"외부 API 응답이 null입니다. ProductCode: {productCode}");
                }

                // Money VO 생성 (검증 실패 시 Fin.Fail)
                return Money.Create(priceResponse.Price);
            }
            catch (HttpRequestException ex)
            {
                // HTTP 요청 예외 → 연결 실패
                return AdapterError.FromException<ExternalPricingApiService>(
                    new ConnectionFailed("ExternalPricingApi"),
                    ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // 사용자 취소
                return AdapterError.For<ExternalPricingApiService>(
                    new OperationCancelled(),
                    productCode,
                    "요청이 취소되었습니다");
            }
            catch (TaskCanceledException ex)
            {
                // 타임아웃
                return AdapterError.FromException<ExternalPricingApiService>(
                    new AdapterErrorType.Timeout(TimeSpan.FromSeconds(30)),
                    ex);
            }
            catch (Exception ex)
            {
                // 기타 예외 → 일반 예외 래핑
                return AdapterError.FromException<ExternalPricingApiService>(
                    new UnexpectedException(),
                    ex);
            }
        });
    }

    /// <summary>
    /// 외부 API에서 여러 상품의 가격을 일괄 조회합니다.
    /// </summary>
    public virtual FinT<IO, Map<string, Money>> GetPricesAsync(
        Seq<string> productCodes,
        CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "/api/pricing/batch",
                    productCodes.ToArray(),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return HandleHttpError<Map<string, Money>>(
                        response,
                        string.Join(",", productCodes));
                }

                var priceResponses = await response.Content
                    .ReadFromJsonAsync<ExternalPriceResponse[]>(cancellationToken: cancellationToken);

                if (priceResponses is null)
                {
                    return AdapterError.For<ExternalPricingApiService>(
                        new Null(),
                        string.Join(",", productCodes),
                        "외부 API 응답이 null입니다");
                }

                // 각 응답을 Money로 변환 (실패 시 전체 실패)
                var results = priceResponses
                    .Select(r => (Code: r.ProductCode, Price: Money.Create(r.Price)))
                    .ToArray();

                // 변환 실패한 항목 확인
                var failed = results.Where(r => r.Price.IsFail).ToArray();
                if (failed.Length > 0)
                {
                    var failedCodes = string.Join(", ", failed.Select(f => f.Code));
                    return AdapterError.For<ExternalPricingApiService>(
                        new PriceConversionFailed(),
                        failedCodes,
                        $"가격 변환에 실패한 상품이 있습니다: {failedCodes}");
                }

                // 성공한 결과를 Map으로 변환
                var priceMap = toMap(results.Select(r =>
                    (r.Code, (Money)r.Price)));

                return Fin.Succ(priceMap);
            }
            catch (HttpRequestException ex)
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new ConnectionFailed("ExternalPricingApi"),
                    ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                return AdapterError.For<ExternalPricingApiService>(
                    new OperationCancelled(),
                    string.Join(",", productCodes),
                    "요청이 취소되었습니다");
            }
            catch (TaskCanceledException ex)
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new AdapterErrorType.Timeout(TimeSpan.FromSeconds(30)),
                    ex);
            }
            catch (Exception ex)
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new UnexpectedException(),
                    ex);
            }
        });
    }

    /// <summary>
    /// HTTP 오류 응답을 AdapterError로 변환합니다.
    /// </summary>
    protected virtual Fin<T> HandleHttpError<T>(HttpResponseMessage response, string context) =>
        response.StatusCode switch
        {
            HttpStatusCode.NotFound => AdapterError.For<ExternalPricingApiService>(
                new NotFound(),
                context,
                $"외부 API에서 리소스를 찾을 수 없습니다. Context: {context}"),

            HttpStatusCode.Unauthorized => AdapterError.For<ExternalPricingApiService>(
                new Unauthorized(),
                context,
                "외부 API 인증에 실패했습니다"),

            HttpStatusCode.Forbidden => AdapterError.For<ExternalPricingApiService>(
                new Forbidden(),
                context,
                "외부 API 접근이 금지되었습니다"),

            HttpStatusCode.TooManyRequests => AdapterError.For<ExternalPricingApiService>(
                new RateLimited(),
                context,
                "외부 API 요청 제한에 도달했습니다"),

            HttpStatusCode.ServiceUnavailable => AdapterError.For<ExternalPricingApiService>(
                new ExternalServiceUnavailable("ExternalPricingApi"),
                context,
                "외부 가격 서비스를 사용할 수 없습니다"),

            _ => AdapterError.For<ExternalPricingApiService, HttpStatusCode>(
                new HttpError(),
                response.StatusCode,
                $"외부 API 호출 실패. Status: {response.StatusCode}")
        };
}
