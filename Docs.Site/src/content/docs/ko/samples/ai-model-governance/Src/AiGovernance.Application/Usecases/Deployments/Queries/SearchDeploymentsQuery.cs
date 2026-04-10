using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Deployments.Specifications;
using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Application.Usecases.Deployments.Queries;

/// <summary>
/// 배포 검색 Query - Specification 패턴 조합 + 페이지네이션/정렬
/// </summary>
public sealed class SearchDeploymentsQuery
{
    private static readonly string[] AllowedSortFields = ["EndpointUrl", "Status", "Environment"];

    /// <summary>
    /// Query Request - 선택적 검색 필터 + 페이지네이션/정렬
    /// </summary>
    public sealed record Request(
        Option<string> ModelId = default,
        Option<string> Status = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 페이지네이션된 검색 결과
    /// </summary>
    public sealed record Response(
        IReadOnlyList<DeploymentListDto> Deployments,
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
            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, SortDirection>();
        }
    }

    /// <summary>
    /// Query Handler - Read Adapter를 통한 페이지네이션 검색
    /// </summary>
    public sealed class Usecase(IDeploymentQuery deploymentQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IDeploymentQuery _deploymentQuery = deploymentQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));

            FinT<IO, Response> usecase =
                from result in _deploymentQuery.Search(spec, pageRequest, sortExpression)
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

        private static Specification<ModelDeployment> BuildSpecification(Request request)
        {
            var spec = Specification<ModelDeployment>.All;

            request.ModelId.Iter(modelId =>
                spec &= new DeploymentByModelSpec(AIModelId.Create(modelId)));

            request.Status.Iter(status =>
            {
                if (status == DeploymentStatus.Active)
                    spec &= new DeploymentActiveSpec();
            });

            return spec;
        }
    }
}
