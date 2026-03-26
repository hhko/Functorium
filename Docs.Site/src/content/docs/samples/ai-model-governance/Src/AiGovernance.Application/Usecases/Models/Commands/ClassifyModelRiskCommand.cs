using AiGovernance.Domain.AggregateRoots.Models;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;
using Functorium.Applications.Linq;

namespace AiGovernance.Application.Usecases.Models.Commands;

/// <summary>
/// AI 모델 위험 등급 재분류 Command
/// 모델을 조회하여 위험 등급을 재분류합니다.
/// </summary>
public sealed class ClassifyModelRiskCommand
{
    /// <summary>
    /// Command Request - 분류 대상 모델 ID와 새 위험 등급
    /// </summary>
    public sealed record Request(
        string ModelId,
        string RiskTier) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 빈 응답
    /// </summary>
    public sealed record Response;

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ModelId).MustBeEntityId<Request, AIModelId>();
            RuleFor(x => x.RiskTier).MustSatisfyValidationOf<Request, string, RiskTier>(RiskTier.Validate);
        }
    }

    /// <summary>
    /// Command Handler
    /// 모델 조회 → 위험 등급 재분류 → 업데이트
    /// </summary>
    public sealed class Usecase(
        IAIModelRepository modelRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IAIModelRepository _modelRepository = modelRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var modelId = AIModelId.Create(request.ModelId);
            var riskTier = RiskTier.Create(request.RiskTier).Unwrap();

            FinT<IO, Response> usecase =
                from model in _modelRepository.GetById(modelId)
                from classified in model.ClassifyRisk(riskTier)
                from updated in _modelRepository.Update(classified)
                select new Response();

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
