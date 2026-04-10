using AiGovernance.Domain.AggregateRoots.Incidents;

namespace AiGovernance.Application.Usecases.Incidents.Queries;

/// <summary>
/// ID로 인시던트 조회 Query
/// Repository에서 Aggregate를 조회하여 DTO로 변환합니다.
/// </summary>
public sealed class GetIncidentByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 인시던트 ID
    /// </summary>
    public sealed record Request(string IncidentId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 인시던트 정보
    /// </summary>
    public sealed record Response(
        string Id,
        string DeploymentId,
        string ModelId,
        string Severity,
        string Status,
        string Description,
        string? ResolutionNote);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).MustBeEntityId<Request, ModelIncidentId>();
        }
    }

    /// <summary>
    /// Query Handler - Repository에서 Aggregate 조회 후 DTO로 변환
    /// </summary>
    public sealed class Usecase(IIncidentRepository incidentRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly IIncidentRepository _incidentRepository = incidentRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var incidentId = ModelIncidentId.Create(request.IncidentId);

            FinT<IO, Response> usecase =
                from incident in _incidentRepository.GetById(incidentId)
                select new Response(
                    incident.Id.ToString(),
                    incident.DeploymentId.ToString(),
                    incident.ModelId.ToString(),
                    incident.Severity,
                    incident.Status,
                    incident.Description,
                    incident.ResolutionNote.Match(Some: n => (string?)n, None: () => null));

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
