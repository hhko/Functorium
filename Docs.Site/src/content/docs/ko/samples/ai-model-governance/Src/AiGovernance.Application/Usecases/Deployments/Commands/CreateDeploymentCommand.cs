using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;
using Functorium.Applications.Linq;

namespace AiGovernance.Application.Usecases.Deployments.Commands;

/// <summary>
/// 배포 생성 Command
/// 모델 존재 확인 → Value Object 생성 → 배포 Aggregate 생성 → 저장
/// </summary>
public sealed class CreateDeploymentCommand
{
    /// <summary>
    /// Command Request - 배포 생성에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string ModelId,
        string EndpointUrl,
        string Environment,
        decimal DriftThreshold) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 배포 ID
    /// </summary>
    public sealed record Response(string DeploymentId);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ModelId).MustBeEntityId<Request, AIModelId>();
            RuleFor(x => x.EndpointUrl).MustSatisfyValidation(EndpointUrl.Validate);
            RuleFor(x => x.Environment).MustSatisfyValidationOf<Request, string, DeploymentEnvironment>(DeploymentEnvironment.Validate);
            RuleFor(x => x.DriftThreshold).MustSatisfyValidation(DriftThreshold.Validate);
        }
    }

    /// <summary>
    /// Command Handler
    /// 모델 존재 확인 → VO 합성 → 배포 생성 → 저장
    /// </summary>
    public sealed class Usecase(
        IDeploymentRepository deploymentRepository,
        IAIModelRepository modelRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IDeploymentRepository _deploymentRepository = deploymentRepository;
        private readonly IAIModelRepository _modelRepository = modelRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var modelId = AIModelId.Create(request.ModelId);

            // ApplyT: VO 합성 + 에러 수집 → FinT<IO, R> LINQ from 첫 구문
            FinT<IO, Response> usecase =
                from vos in (
                    EndpointUrl.Create(request.EndpointUrl),
                    DeploymentEnvironment.Create(request.Environment),
                    DriftThreshold.Create(request.DriftThreshold)
                ).ApplyT((url, env, drift) => (Url: url, Env: env, Drift: drift))
                from model in _modelRepository.GetById(modelId)
                let deployment = ModelDeployment.Create(model.Id, vos.Url, vos.Env, vos.Drift)
                from saved in _deploymentRepository.Create(deployment)
                select new Response(saved.Id.ToString());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
