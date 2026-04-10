using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Incidents;
using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;
using Functorium.Applications.Linq;

namespace AiGovernance.Application.Usecases.Incidents.Commands;

/// <summary>
/// 인시던트 보고 Command
/// 배포에 대한 인시던트를 생성합니다.
/// </summary>
public sealed class ReportIncidentCommand
{
    /// <summary>
    /// Command Request - 인시던트 보고에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string DeploymentId,
        string Severity,
        string Description) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 인시던트 ID
    /// </summary>
    public sealed record Response(string IncidentId);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DeploymentId).MustBeEntityId<Request, ModelDeploymentId>();
            RuleFor(x => x.Severity).MustSatisfyValidationOf<Request, string, IncidentSeverity>(IncidentSeverity.Validate);
            RuleFor(x => x.Description).MustSatisfyValidation(IncidentDescription.Validate);
        }
    }

    /// <summary>
    /// Command Handler
    /// 배포 조회 → VO 합성 → 인시던트 생성 → 저장
    /// </summary>
    public sealed class Usecase(
        IDeploymentRepository deploymentRepository,
        IIncidentRepository incidentRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IDeploymentRepository _deploymentRepository = deploymentRepository;
        private readonly IIncidentRepository _incidentRepository = incidentRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var deploymentId = ModelDeploymentId.Create(request.DeploymentId);

            // ApplyT: VO 합성 + 에러 수집
            FinT<IO, Response> usecase =
                from vos in (
                    IncidentSeverity.Create(request.Severity),
                    IncidentDescription.Create(request.Description)
                ).ApplyT((severity, description) => (Severity: severity, Description: description))
                from deployment in _deploymentRepository.GetById(deploymentId)
                let incident = ModelIncident.Create(deployment.Id, deployment.ModelId, vos.Severity, vos.Description)
                from saved in _incidentRepository.Create(incident)
                select new Response(saved.Id.ToString());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
