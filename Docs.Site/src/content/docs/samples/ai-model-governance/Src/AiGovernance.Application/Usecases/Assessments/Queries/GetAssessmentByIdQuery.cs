using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

namespace AiGovernance.Application.Usecases.Assessments.Queries;

/// <summary>
/// ID로 컴플라이언스 평가 조회 Query
/// Repository에서 Aggregate를 조회하여 DTO로 변환합니다.
/// </summary>
public sealed class GetAssessmentByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 평가 ID
    /// </summary>
    public sealed record Request(string AssessmentId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 평가 정보
    /// </summary>
    public sealed record Response(
        string Id,
        string ModelId,
        string DeploymentId,
        int? Score,
        string Status,
        List<CriterionDto> Criteria);

    /// <summary>
    /// 평가 기준 DTO
    /// </summary>
    public sealed record CriterionDto(
        string Id,
        string Name,
        string Description,
        string? Result,
        string? Notes);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.AssessmentId).MustBeEntityId<Request, ComplianceAssessmentId>();
        }
    }

    /// <summary>
    /// Query Handler - Repository에서 Aggregate 조회 후 DTO로 변환
    /// </summary>
    public sealed class Usecase(IAssessmentRepository assessmentRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly IAssessmentRepository _assessmentRepository = assessmentRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var assessmentId = ComplianceAssessmentId.Create(request.AssessmentId);

            FinT<IO, Response> usecase =
                from assessment in _assessmentRepository.GetById(assessmentId)
                select new Response(
                    assessment.Id.ToString(),
                    assessment.ModelId.ToString(),
                    assessment.DeploymentId.ToString(),
                    assessment.OverallScore.Map(s => (int)s).ToNullable(),
                    assessment.Status,
                    assessment.Criteria.Select(c => new CriterionDto(
                        c.Id.ToString(),
                        c.Name,
                        c.Description,
                        c.Result.Match(Some: r => (string?)r, None: () => null),
                        c.Notes.Match(Some: n => (string?)n, None: () => null))).ToList());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
