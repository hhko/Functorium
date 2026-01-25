using FastEndpoints;
using Functorium.Applications.Cqrs;
using LanguageExt.Common;

namespace TwoWayMappingLayered.Adapters.Presentation.Extensions;

/// <summary>
/// FastEndpoints와 FinResponse 통합을 위한 확장 메서드
/// </summary>
public static class FinResponseExtensions
{
    /// <summary>
    /// FinResponse를 HTTP 응답으로 변환하여 전송 (200 OK)
    /// </summary>
    public static async Task SendFinResponseAsync<T>(
        this IEndpoint ep,
        FinResponse<T> response,
        CancellationToken ct = default)
    {
        await response.Match(
            Succ: async value => await ep.HttpContext.Response.SendAsync(value, 200, cancellation: ct),
            Fail: async error => await SendErrorResponseAsync(ep, error, ct));
    }

    /// <summary>
    /// FinResponse를 HTTP 응답으로 변환하여 전송 (201 Created)
    /// </summary>
    public static async Task SendCreatedFinResponseAsync<T>(
        this IEndpoint ep,
        FinResponse<T> response,
        CancellationToken ct = default)
    {
        await response.Match(
            Succ: async value => await ep.HttpContext.Response.SendAsync(value, 201, cancellation: ct),
            Fail: async error => await SendErrorResponseAsync(ep, error, ct));
    }

    private static async Task SendErrorResponseAsync(
        IEndpoint ep,
        Error error,
        CancellationToken ct)
    {
        int statusCode = error.Code switch
        {
            var code when code.ToString().Contains("NotFound") => 404,
            var code when code.ToString().Contains("AlreadyExists") => 409,
            var code when code.ToString().Contains("Empty") => 400,
            var code when code.ToString().Contains("Invalid") => 400,
            _ => 400
        };

        var problemDetails = new
        {
            Status = statusCode,
            Title = GetErrorTitle(statusCode),
            Detail = error.Message,
            ErrorCode = error.Code
        };

        await ep.HttpContext.Response.SendAsync(problemDetails, statusCode, cancellation: ct);
    }

    private static string GetErrorTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        404 => "Not Found",
        409 => "Conflict",
        _ => "Error"
    };
}
