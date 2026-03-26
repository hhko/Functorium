using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Deployments;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;

namespace AiGovernance.Application.Usecases.Deployments.Commands;

/// <summary>
/// 배포 활성화 Command
/// 컴플라이언스 평가 통과 확인 후 배포를 활성화합니다.
/// </summary>
public sealed class ActivateDeploymentCommand
{
    /// <summary>
    /// Command Request - 활성화할 배포 ID와 평가 ID
    /// </summary>
    public sealed record Request(
        string DeploymentId,
        string AssessmentId) : ICommandRequest<Response>;

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
            RuleFor(x => x.AssessmentId).MustBeEntityId<Request, ComplianceAssessmentId>();
        }
    }

    /// <summary>
    /// Command Handler
    /// 배포 조회 → 평가 조회 → 평가 통과 확인 → 활성화 → 저장
    /// </summary>
    public sealed class Usecase(
        IDeploymentRepository deploymentRepository,
        IAssessmentRepository assessmentRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IDeploymentRepository _deploymentRepository = deploymentRepository;
        private readonly IAssessmentRepository _assessmentRepository = assessmentRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var deploymentId = ModelDeploymentId.Create(request.DeploymentId);
            var assessmentId = ComplianceAssessmentId.Create(request.AssessmentId);

            FinT<IO, Response> usecase =
                from deployment in _deploymentRepository.GetById(deploymentId)
                from assessment in _assessmentRepository.GetById(assessmentId)
                from _ in guard(assessment.Status == AssessmentStatus.Passed,
                    ApplicationError.For<ActivateDeploymentCommand>(
                        new BusinessRuleViolated(),
                        request.AssessmentId,
                        $"Assessment '{request.AssessmentId}' has not passed (current status: '{assessment.Status}')"))
                from _2 in deployment.Activate()
                from updated in _deploymentRepository.Update(deployment)
                select new Response();

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
