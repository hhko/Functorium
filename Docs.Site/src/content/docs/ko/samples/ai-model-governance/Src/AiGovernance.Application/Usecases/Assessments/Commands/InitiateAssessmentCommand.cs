using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Models;
using Functorium.Applications.Linq;

namespace AiGovernance.Application.Usecases.Assessments.Commands;

/// <summary>
/// 컴플라이언스 평가 개시 Command
/// 모델과 배포를 조회하여 위험 등급 기반 평가를 생성합니다.
/// </summary>
public sealed class InitiateAssessmentCommand
{
    /// <summary>
    /// Command Request - 평가 대상 모델 ID와 배포 ID
    /// </summary>
    public sealed record Request(
        string ModelId,
        string DeploymentId) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 평가 ID
    /// </summary>
    public sealed record Response(string AssessmentId);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ModelId).MustBeEntityId<Request, AIModelId>();
            RuleFor(x => x.DeploymentId).MustBeEntityId<Request, ModelDeploymentId>();
        }
    }

    /// <summary>
    /// Command Handler
    /// 모델 조회 → 배포 조회 → 위험 등급 기반 평가 생성 → 저장
    /// </summary>
    public sealed class Usecase(
        IAIModelRepository modelRepository,
        IDeploymentRepository deploymentRepository,
        IAssessmentRepository assessmentRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IAIModelRepository _modelRepository = modelRepository;
        private readonly IDeploymentRepository _deploymentRepository = deploymentRepository;
        private readonly IAssessmentRepository _assessmentRepository = assessmentRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var modelId = AIModelId.Create(request.ModelId);
            var deploymentId = ModelDeploymentId.Create(request.DeploymentId);

            FinT<IO, Response> usecase =
                from model in _modelRepository.GetById(modelId)
                from deployment in _deploymentRepository.GetById(deploymentId)
                let assessment = ComplianceAssessment.Create(model.Id, deployment.Id, model.RiskTier)
                from saved in _assessmentRepository.Create(assessment)
                select new Response(saved.Id.ToString());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
