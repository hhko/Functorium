using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Incidents;
using AiGovernance.Domain.AggregateRoots.Models;
using AiGovernance.Domain.SharedModels.Services;
using Functorium.Applications.Linq;

namespace AiGovernance.Application.Usecases.Deployments.Commands;

/// <summary>
/// 배포 검토 제출 Command
/// 배포 적격성 검증 후 검토를 위해 제출합니다.
/// </summary>
public sealed class SubmitDeploymentForReviewCommand
{
    /// <summary>
    /// Command Request - 제출할 배포 ID
    /// </summary>
    public sealed record Request(
        string DeploymentId) : ICommandRequest<Response>;

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
            RuleFor(x => x.DeploymentId).MustBeEntityId<Request, ModelDeploymentId>();
        }
    }

    /// <summary>
    /// Command Handler
    /// 배포 조회 → 모델 조회 → 적격성 검증 → 검토 제출 → 저장
    /// </summary>
    public sealed class Usecase(
        IDeploymentRepository deploymentRepository,
        IAIModelRepository modelRepository,
        IAssessmentRepository assessmentRepository,
        IIncidentRepository incidentRepository,
        DeploymentEligibilityService eligibilityService)
        : ICommandUsecase<Request, Response>
    {
        private readonly IDeploymentRepository _deploymentRepository = deploymentRepository;
        private readonly IAIModelRepository _modelRepository = modelRepository;
        private readonly IAssessmentRepository _assessmentRepository = assessmentRepository;
        private readonly IIncidentRepository _incidentRepository = incidentRepository;
        private readonly DeploymentEligibilityService _eligibilityService = eligibilityService;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var deploymentId = ModelDeploymentId.Create(request.DeploymentId);

            FinT<IO, Response> usecase =
                from deployment in _deploymentRepository.GetById(deploymentId)
                from model in _modelRepository.GetById(deployment.ModelId)
                from _1 in _eligibilityService.ValidateEligibility(model, _assessmentRepository, _incidentRepository)
                from _2 in deployment.SubmitForReview()
                from updated in _deploymentRepository.Update(deployment)
                select new Response();

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
