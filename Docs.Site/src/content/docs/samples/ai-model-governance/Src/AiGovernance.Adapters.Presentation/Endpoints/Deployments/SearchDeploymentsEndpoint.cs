using Functorium.Applications.Queries;
using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Application.Usecases.Deployments.Queries;

namespace AiGovernance.Adapters.Presentation.Endpoints.Deployments;

/// <summary>
/// 배포 검색 Endpoint (페이지네이션/정렬 지원)
/// GET /api/deployments
/// </summary>
public sealed class SearchDeploymentsEndpoint
    : Endpoint<SearchDeploymentsEndpoint.Request, SearchDeploymentsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SearchDeploymentsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/deployments");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "배포 검색";
            s.Description = "모델 ID, 상태, 페이지네이션, 정렬을 지원하는 배포 검색";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SearchDeploymentsQuery.Request(
            ModelId: req.ModelId ?? LanguageExt.Option<string>.None,
            Status: req.Status ?? LanguageExt.Option<string>.None,
            Page: req.Page ?? 1,
            PageSize: req.PageSize ?? PageRequest.DefaultPageSize,
            SortBy: req.SortBy ?? "",
            SortDirection: req.SortDirection ?? "");

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(
            r.Deployments.ToList(),
            r.TotalCount,
            r.Page,
            r.PageSize,
            r.TotalPages,
            r.HasNextPage,
            r.HasPreviousPage));

        await this.SendFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        [property: QueryParam] string? ModelId = null,
        [property: QueryParam] string? Status = null,
        [property: QueryParam] int? Page = null,
        [property: QueryParam] int? PageSize = null,
        [property: QueryParam] string? SortBy = null,
        [property: QueryParam] string? SortDirection = null);

    public new sealed record Response(
        List<DeploymentListDto> Deployments,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);
}
