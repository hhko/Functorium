using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using AiGovernance.Application.Usecases.Models.Ports;
using AiGovernance.Domain.AggregateRoots.Models;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models.Specifications;

namespace AiGovernance.Application.Usecases.Models.Queries;

/// <summary>
/// AI 모델 검색 Query - Specification 패턴 조합 + 페이지네이션/정렬
/// </summary>
public sealed class SearchModelsQuery
{
    private static readonly string[] AllowedSortFields = ["Name", "Version", "RiskTier"];

    /// <summary>
    /// Query Request - 선택적 검색 필터 + 페이지네이션/정렬
    /// </summary>
    public sealed record Request(
        Option<string> Name = default,
        Option<string> RiskTier = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 페이지네이션된 검색 결과
    /// </summary>
    public sealed record Response(
        IReadOnlyList<ModelListDto> Models,
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
            RuleFor(x => x.Name)
                .MustSatisfyValidation(ModelName.Validate);

            RuleFor(x => x.RiskTier)
                .MustSatisfyValidationOf<Request, string, RiskTier>(RiskTier.Validate);

            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, SortDirection>();
        }
    }

    /// <summary>
    /// Query Handler - Read Adapter를 통한 페이지네이션 검색
    /// </summary>
    public sealed class Usecase(IAIModelQuery modelQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IAIModelQuery _modelQuery = modelQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));

            FinT<IO, Response> usecase =
                from result in _modelQuery.Search(spec, pageRequest, sortExpression)
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

        private static Specification<AIModel> BuildSpecification(Request request)
        {
            var spec = Specification<AIModel>.All;

            request.Name.Iter(name =>
                spec &= new ModelNameSpec(
                    ModelName.Create(name).Unwrap()));

            request.RiskTier.Iter(tier =>
                spec &= new ModelRiskTierSpec(
                    RiskTier.Create(tier).Unwrap()));

            return spec;
        }
    }
}
