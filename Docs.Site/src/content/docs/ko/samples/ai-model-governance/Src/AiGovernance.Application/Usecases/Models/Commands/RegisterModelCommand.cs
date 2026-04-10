using AiGovernance.Domain.AggregateRoots.Models;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;
using AiGovernance.Domain.SharedModels.Services;
using Functorium.Applications.Linq;

namespace AiGovernance.Application.Usecases.Models.Commands;

/// <summary>
/// AI 모델 등록 Command
/// Value Object 생성 + 위험 등급 분류 + Aggregate 생성 후 저장합니다.
/// </summary>
public sealed class RegisterModelCommand
{
    /// <summary>
    /// Command Request - 모델 등록에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string Name,
        string Version,
        string Purpose) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 모델 ID
    /// </summary>
    public sealed record Response(string ModelId);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).MustSatisfyValidation(ModelName.Validate);
            RuleFor(x => x.Version).MustSatisfyValidation(ModelVersion.Validate);
            RuleFor(x => x.Purpose).MustSatisfyValidation(ModelPurpose.Validate);
        }
    }

    /// <summary>
    /// Command Handler
    /// VO 합성 → 위험 등급 분류 → AI 모델 생성 → 저장
    /// </summary>
    public sealed class Usecase(
        IAIModelRepository modelRepository,
        RiskClassificationService riskClassificationService)
        : ICommandUsecase<Request, Response>
    {
        private readonly IAIModelRepository _modelRepository = modelRepository;
        private readonly RiskClassificationService _riskClassificationService = riskClassificationService;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // ApplyT: VO 합성 + 에러 수집 → FinT<IO, R> LINQ from 첫 구문
            FinT<IO, Response> usecase =
                from vos in (
                    ModelName.Create(request.Name),
                    ModelVersion.Create(request.Version),
                    ModelPurpose.Create(request.Purpose)
                ).ApplyT((name, version, purpose) => (Name: name, Version: version, Purpose: purpose))
                from riskTier in _riskClassificationService.ClassifyByPurpose(vos.Purpose)
                let model = AIModel.Create(vos.Name, vos.Version, vos.Purpose, riskTier)
                from saved in _modelRepository.Create(model)
                select new Response(saved.Id.ToString());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
