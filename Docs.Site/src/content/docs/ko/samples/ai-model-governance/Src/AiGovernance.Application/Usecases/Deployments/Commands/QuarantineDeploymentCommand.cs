using AiGovernance.Domain.AggregateRoots.Deployments;
using Functorium.Applications.Linq;

namespace AiGovernance.Application.Usecases.Deployments.Commands;

/// <summary>
/// 배포 격리 Command
/// 배포를 격리 상태로 전환합니다.
/// </summary>
public sealed class QuarantineDeploymentCommand
{
    /// <summary>
    /// Command Request - 격리할 배포 ID와 사유
    /// </summary>
    public sealed record Request(
        string DeploymentId,
        string Reason) : ICommandRequest<Response>;

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

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason must not be empty");
        }
    }

    /// <summary>
    /// Command Handler
    /// 배포 조회 → 격리 → 저장
    /// </summary>
    public sealed class Usecase(
        IDeploymentRepository deploymentRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IDeploymentRepository _deploymentRepository = deploymentRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var deploymentId = ModelDeploymentId.Create(request.DeploymentId);

            FinT<IO, Response> usecase =
                from deployment in _deploymentRepository.GetById(deploymentId)
                from _ in deployment.Quarantine(request.Reason)
                from updated in _deploymentRepository.Update(deployment)
                select new Response();

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
