using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using AiGovernance.Application.Usecases.Incidents.Ports;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Incidents;
using AiGovernance.Domain.AggregateRoots.Incidents.Specifications;
using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

namespace AiGovernance.Application.Usecases.Incidents.Queries;

/// <summary>
/// 인시던트 검색 Query - Specification 패턴 조합 + 페이지네이션/정렬
/// </summary>
public sealed class SearchIncidentsQuery
{
    private static readonly string[] AllowedSortFields = ["Severity", "Status", "ReportedAt"];

    /// <summary>
    /// Query Request - 선택적 검색 필터 + 페이지네이션/정렬
    /// </summary>
    public sealed record Request(
        Option<string> DeploymentId = default,
        Option<string> Severity = default,
        Option<bool> OpenOnly = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 페이지네이션된 검색 결과
    /// </summary>
    public sealed record Response(
        IReadOnlyList<IncidentListDto> Incidents,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Severity)
                .MustSatisfyValidationOf<Request, string, IncidentSeverity>(IncidentSeverity.Validate);

            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, SortDirection>();
        }
    }

    /// <summary>
    /// Query Handler - Read Adapter를 통한 페이지네이션 검색
    /// </summary>
    public sealed class Usecase(IIncidentQuery incidentQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IIncidentQuery _incidentQuery = incidentQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));

            FinT<IO, Response> usecase =
                from result in _incidentQuery.Search(spec, pageRequest, sortExpression)
                select new Response(
                    result.Items,
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    result.TotalPages,
                    result.HasNextPage,
                    result.HasPreviousPage);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Specification<ModelIncident> BuildSpecification(Request request)
        {
            var spec = Specification<ModelIncident>.All;

            request.DeploymentId.Iter(deploymentId =>
                spec &= new IncidentByDeploymentSpec(ModelDeploymentId.Create(deploymentId)));

            request.Severity.Iter(severity =>
                spec &= new IncidentBySeveritySpec(
                    IncidentSeverity.Create(severity).Unwrap()));

            request.OpenOnly.Iter(openOnly =>
            {
                if (openOnly)
                    spec &= new IncidentOpenSpec();
            });

            return spec;
        }
    }
}
