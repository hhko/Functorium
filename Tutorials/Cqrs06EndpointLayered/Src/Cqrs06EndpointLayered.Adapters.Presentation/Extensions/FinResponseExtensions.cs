using FastEndpoints;
using Functorium.Applications.Cqrs;
using LanguageExt.Common;

namespace Cqrs06EndpointLayered.Adapters.Presentation.Extensions;

/// <summary>
/// FinResponse를 FastEndpoints HTTP Response로 변환하는 확장 메서드
/// </summary>
public static class FinResponseExtensions
{
    /// <summary>
    /// FinResponse를 HTTP 응답으로 변환합니다.
    /// 성공: 200 OK + Response body
    /// 실패: 400 Bad Request + Error details
    /// </summary>
    public static async Task SendFinResponseAsync<TResponse>(
        this IEndpoint ep,
        FinResponse<TResponse> result,
        CancellationToken ct = default)
    {
        await result.Match(
            Succ: async response => await ep.HttpContext.Response.SendAsync(response, 200, cancellation: ct),
            Fail: async error => await ep.HttpContext.Response.SendAsync(
                new ErrorResponse(error),
                400,
                cancellation: ct));
    }

    /// <summary>
    /// FinResponse를 HTTP 응답으로 변환합니다 (201 Created).
    /// 성공: 201 Created + Response body
    /// 실패: 400 Bad Request + Error details
    /// </summary>
    public static async Task SendCreatedFinResponseAsync<TResponse>(
        this IEndpoint ep,
        FinResponse<TResponse> result,
        CancellationToken ct = default)
    {
        await result.Match(
            Succ: async response => await ep.HttpContext.Response.SendAsync(response, 201, cancellation: ct),
            Fail: async error => await ep.HttpContext.Response.SendAsync(
                new ErrorResponse(error),
                400,
                cancellation: ct));
    }

    /// <summary>
    /// Not Found (404) 처리가 필요한 경우
    /// </summary>
    public static async Task SendFinResponseWithNotFoundAsync<TResponse>(
        this IEndpoint ep,
        FinResponse<TResponse> result,
        CancellationToken ct = default)
    {
        await result.Match(
            Succ: async response => await ep.HttpContext.Response.SendAsync(response, 200, cancellation: ct),
            Fail: async error =>
            {
                // ErrorCode에 "찾을 수 없습니다" 포함 여부로 404 결정
                int statusCode = error.Message.Contains("찾을 수 없습니다") ||
                                 error.Message.Contains("NotFound")
                    ? 404
                    : 400;
                await ep.HttpContext.Response.SendAsync(new ErrorResponse(error), statusCode, cancellation: ct);
            });
    }
}

/// <summary>
/// Error 응답 DTO
/// </summary>
public sealed record ErrorResponse
{
    public int StatusCode { get; init; }
    public string Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; }

    public ErrorResponse(Error error)
    {
        StatusCode = 400;
        Message = error.Message;

        // ManyErrors 처리
        if (error is ManyErrors manyErrors)
        {
            Errors = manyErrors.Errors.Select(e => e.Message).ToList();
        }
        else
        {
            Errors = new[] { error.Message };
        }
    }
}
