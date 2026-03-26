using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Domain.AggregateRoots.Deployments;

namespace AiGovernance.Application.Usecases.Deployments.Queries;

/// <summary>
/// ID로 배포 조회 Query
/// </summary>
public sealed class GetDeploymentByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 배포 ID
    /// </summary>
    public sealed record Request(string DeploymentId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 배포 정보
    /// </summary>
    public sealed record Response(
        string Id,
        string ModelId,
        string EndpointUrl,
        string Status,
        string Environment,
        decimal DriftThreshold);

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
    /// Query Handler - IDeploymentDetailQuery 포트를 통한 조회
    /// </summary>
    public sealed class Usecase(IDeploymentDetailQuery deploymentDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IDeploymentDetailQuery _deploymentDetailQuery = deploymentDetailQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var deploymentId = ModelDeploymentId.Create(request.DeploymentId);

            FinT<IO, Response> usecase =
                from result in _deploymentDetailQuery.GetById(deploymentId)
                select new Response(
                    result.Id,
                    result.ModelId,
                    result.EndpointUrl,
                    result.Status,
                    result.Environment,
                    result.DriftThreshold);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
