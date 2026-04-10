using Functorium.Applications.Queries;
using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Incidents.Ports;
using AiGovernance.Application.Usecases.Incidents.Queries;

namespace AiGovernance.Adapters.Presentation.Endpoints.Incidents;

/// <summary>
/// 인시던트 검색 Endpoint (페이지네이션/정렬 지원)
/// GET /api/incidents
/// </summary>
public sealed class SearchIncidentsEndpoint
    : Endpoint<SearchIncidentsEndpoint.Request, SearchIncidentsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SearchIncidentsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/incidents");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "인시던트 검색";
            s.Description = "배포 ID, 심각도, 페이지네이션, 정렬을 지원하는 인시던트 검색";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SearchIncidentsQuery.Request(
            DeploymentId: req.DeploymentId ?? LanguageExt.Option<string>.None,
            Severity: req.Severity ?? LanguageExt.Option<string>.None,
            OpenOnly: req.OpenOnly ?? LanguageExt.Option<bool>.None,
            Page: req.Page ?? 1,
            PageSize: req.PageSize ?? PageRequest.DefaultPageSize,
            SortBy: req.SortBy ?? "",
            SortDirection: req.SortDirection ?? "");

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(
            r.Incidents.ToList(),
            r.TotalCount,
            r.Page,
            r.PageSize,
            r.TotalPages,
            r.HasNextPage,
            r.HasPreviousPage));

        await this.SendFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        [property: QueryParam] string? DeploymentId = null,
        [property: QueryParam] string? Severity = null,
        [property: QueryParam] bool? OpenOnly = null,
        [property: QueryParam] int? Page = null,
        [property: QueryParam] int? PageSize = null,
        [property: QueryParam] string? SortBy = null,
        [property: QueryParam] string? SortDirection = null);

    public new sealed record Response(
        List<IncidentListDto> Incidents,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);
}
