using AiGovernance.Application.Usecases.Models.Ports;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Application.Usecases.Models.Queries;

/// <summary>
/// ID로 AI 모델 조회 Query
/// </summary>
public sealed class GetModelByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 모델 ID
    /// </summary>
    public sealed record Request(string ModelId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 모델 정보
    /// </summary>
    public sealed record Response(
        string Id,
        string Name,
        string Version,
        string Purpose,
        string RiskTier,
        DateTimeOffset CreatedAt);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ModelId).MustBeEntityId<Request, AIModelId>();
        }
    }

    /// <summary>
    /// Query Handler - IModelDetailQuery 포트를 통한 조회
    /// </summary>
    public sealed class Usecase(IModelDetailQuery modelDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IModelDetailQuery _modelDetailQuery = modelDetailQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var modelId = AIModelId.Create(request.ModelId);

            FinT<IO, Response> usecase =
                from result in _modelDetailQuery.GetById(modelId)
                select new Response(
                    result.Id,
                    result.Name,
                    result.Version,
                    result.Purpose,
                    result.RiskTier,
                    result.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
