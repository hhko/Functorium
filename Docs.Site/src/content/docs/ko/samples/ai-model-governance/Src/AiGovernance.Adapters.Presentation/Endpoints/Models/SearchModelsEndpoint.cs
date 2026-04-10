using Functorium.Applications.Queries;
using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Models.Ports;
using AiGovernance.Application.Usecases.Models.Queries;

namespace AiGovernance.Adapters.Presentation.Endpoints.Models;

/// <summary>
/// AI 모델 검색 Endpoint (페이지네이션/정렬 지원)
/// GET /api/models
/// </summary>
public sealed class SearchModelsEndpoint
    : Endpoint<SearchModelsEndpoint.Request, SearchModelsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SearchModelsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/models");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "AI 모델 검색";
            s.Description = "이름, 위험 등급, 페이지네이션, 정렬을 지원하는 AI 모델 검색";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SearchModelsQuery.Request(
            Name: req.Name ?? LanguageExt.Option<string>.None,
            RiskTier: req.RiskTier ?? LanguageExt.Option<string>.None,
            Page: req.Page ?? 1,
            PageSize: req.PageSize ?? PageRequest.DefaultPageSize,
            SortBy: req.SortBy ?? "",
            SortDirection: req.SortDirection ?? "");

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(
            r.Models.ToList(),
            r.TotalCount,
            r.Page,
            r.PageSize,
            r.TotalPages,
            r.HasNextPage,
            r.HasPreviousPage));

        await this.SendFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        [property: QueryParam] string? Name = null,
        [property: QueryParam] string? RiskTier = null,
        [property: QueryParam] int? Page = null,
        [property: QueryParam] int? PageSize = null,
        [property: QueryParam] string? SortBy = null,
        [property: QueryParam] string? SortDirection = null);

    public new sealed record Response(
        List<ModelListDto> Models,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);
}
