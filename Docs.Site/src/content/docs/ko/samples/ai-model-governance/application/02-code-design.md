---
title: "애플리케이션 코드 설계"
description: "AI 모델 거버넌스 플랫폼의 핵심 Command 코드와 이벤트 핸들러 패턴"
---

[타입 설계 의사결정](../01-type-design-decisions/)에서 식별한 Use Case, 포트, 컴포지션 패턴을 C# 코드로 구현합니다.

## 핵심 Command 패턴

### 1. RegisterModelCommand -- VO 합성 + Domain Service + Aggregate 생성

모델 등록은 가장 대표적인 Command 패턴입니다. VO 합성(ApplyT), Domain Service 호출(위험 분류), Aggregate 생성, 저장이 하나의 FinT LINQ 체인으로 구성됩니다.

```csharp
public sealed class RegisterModelCommand
{
    public sealed record Request(
        string Name, string Version, string Purpose) : ICommandRequest<Response>;

    public sealed record Response(string ModelId);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).MustSatisfyValidation(ModelName.Validate);
            RuleFor(x => x.Version).MustSatisfyValidation(ModelVersion.Validate);
            RuleFor(x => x.Purpose).MustSatisfyValidation(ModelPurpose.Validate);
        }
    }

    public sealed class Usecase(
        IAIModelRepository modelRepository,
        RiskClassificationService riskClassificationService)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            FinT<IO, Response> usecase =
                from vos in (
                    ModelName.Create(request.Name),
                    ModelVersion.Create(request.Version),
                    ModelPurpose.Create(request.Purpose)
                ).ApplyT((name, version, purpose) =>
                    (Name: name, Version: version, Purpose: purpose))
                from riskTier in _riskClassificationService
                    .ClassifyByPurpose(vos.Purpose)
                let model = AIModel.Create(
                    vos.Name, vos.Version, vos.Purpose, riskTier)
                from saved in _modelRepository.Create(model)
                select new Response(saved.Id.ToString());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

흐름 분석:
1. `ApplyT`: 3개 VO를 동시 검증, 모든 오류 수집
2. `from riskTier`: Domain Service가 목적 키워드로 위험 등급 분류
3. `let model`: Aggregate 생성 (항상 성공, `Fin`이 아닌 `let`)
4. `from saved`: Repository에 저장 (IO 효과)

### 2. SubmitDeploymentForReviewCommand -- 교차 Aggregate 검증

배포 검토 제출은 `DeploymentEligibilityService`를 통해 3개 Aggregate를 교차 검증합니다.

```csharp
public sealed class Usecase(
    IDeploymentRepository deploymentRepository,
    IAIModelRepository modelRepository,
    IAssessmentRepository assessmentRepository,
    IIncidentRepository incidentRepository,
    DeploymentEligibilityService eligibilityService)
    : ICommandUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(
        Request request, CancellationToken cancellationToken)
    {
        var deploymentId = ModelDeploymentId.Create(request.DeploymentId);

        FinT<IO, Response> usecase =
            from deployment in _deploymentRepository.GetById(deploymentId)
            from model in _modelRepository.GetById(deployment.ModelId)
            from _1 in _eligibilityService.ValidateEligibility(
                model, _assessmentRepository, _incidentRepository)
            from _2 in deployment.SubmitForReview()
            from updated in _deploymentRepository.Update(deployment)
            select new Response();

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

흐름 분석:
1. `from deployment`: 배포 조회
2. `from model`: 배포가 참조하는 모델 조회
3. `from _1`: 적격성 검증 (금지 등급, 컴플라이언스, 인시던트) -- 실패 시 short-circuit
4. `from _2`: 배포 상태 전이 (Draft -> PendingReview) -- 실패 시 short-circuit
5. `from updated`: 저장

### 3. ActivateDeploymentCommand -- guard 패턴

배포 활성화는 `guard`를 사용하여 평가 통과 여부를 확인합니다.

```csharp
FinT<IO, Response> usecase =
    from deployment in _deploymentRepository.GetById(deploymentId)
    from assessment in _assessmentRepository.GetById(assessmentId)
    from _ in guard(assessment.Status == AssessmentStatus.Passed,
        ApplicationError.For<ActivateDeploymentCommand>(
            new BusinessRuleViolated(),
            request.AssessmentId,
            $"Assessment '{request.AssessmentId}' has not passed"))
    from _2 in deployment.Activate()
    from updated in _deploymentRepository.Update(deployment)
    select new Response();
```

`guard`는 조건이 `false`이면 지정된 오류로 즉시 실패합니다. 도메인 Aggregate에 속하지 않는 교차 검증 규칙을 Application Layer에서 표현하는 패턴입니다.

### 4. CreateDeploymentCommand -- 모델 존재 확인 + VO 합성

```csharp
FinT<IO, Response> usecase =
    from vos in (
        EndpointUrl.Create(request.EndpointUrl),
        DeploymentEnvironment.Create(request.Environment),
        DriftThreshold.Create(request.DriftThreshold)
    ).ApplyT((url, env, drift) => (Url: url, Env: env, Drift: drift))
    from model in _modelRepository.GetById(modelId)
    let deployment = ModelDeployment.Create(
        model.Id, vos.Url, vos.Env, vos.Drift)
    from saved in _deploymentRepository.Create(deployment)
    select new Response(saved.Id.ToString());
```

### 5. ReportIncidentCommand -- VO 합성 + 배포 참조

```csharp
FinT<IO, Response> usecase =
    from vos in (
        IncidentSeverity.Create(request.Severity),
        IncidentDescription.Create(request.Description)
    ).ApplyT((severity, description) =>
        (Severity: severity, Description: description))
    from deployment in _deploymentRepository.GetById(deploymentId)
    let incident = ModelIncident.Create(
        deployment.Id, deployment.ModelId,
        vos.Severity, vos.Description)
    from saved in _incidentRepository.Create(incident)
    select new Response(saved.Id.ToString());
```

## 이벤트 핸들러 패턴

### QuarantineDeploymentOnCriticalIncidentHandler

Critical/High 심각도 인시던트 발생 시 배포를 자동 격리합니다. `IncidentSeverity.RequiresQuarantine` 도메인 속성을 활용합니다.

```csharp
public sealed class QuarantineDeploymentOnCriticalIncidentHandler(
    IDeploymentRepository deploymentRepository)
    : IDomainEventHandler<ModelIncident.ReportedEvent>
{
    public async ValueTask Handle(
        ModelIncident.ReportedEvent notification,
        CancellationToken cancellationToken)
    {
        if (!notification.Severity.RequiresQuarantine)
            return;

        var result = await _deploymentRepository
            .GetById(notification.DeploymentId)
            .Run().RunAsync();

        if (result.IsFail) return;

        var deployment = result.Unwrap();
        var quarantineResult = deployment.Quarantine(
            $"Auto-quarantined due to {notification.Severity} incident");

        if (quarantineResult.IsSucc)
            await _deploymentRepository.Update(deployment).Run().RunAsync();
    }
}
```

이벤트 핸들러의 특징:
- 실패해도 전체 트랜잭션을 롤백하지 않음 (eventual consistency)
- `RequiresQuarantine` 도메인 속성으로 조건 판별
- 격리 실패(이미 Decommissioned 등) 시 무시

### InitiateAssessmentOnRiskUpgradeHandler

위험 등급 상향 시 활성 배포에 대해 컴플라이언스 평가를 자동 생성합니다. Specification 합성(`&` 연산자)을 활용합니다.

```csharp
public sealed class InitiateAssessmentOnRiskUpgradeHandler(
    IDeploymentRepository deploymentRepository,
    IAssessmentRepository assessmentRepository)
    : IDomainEventHandler<AIModel.RiskClassifiedEvent>
{
    public async ValueTask Handle(
        AIModel.RiskClassifiedEvent notification,
        CancellationToken cancellationToken)
    {
        if (!notification.NewRiskTier.RequiresComplianceAssessment)
            return;

        var spec = new DeploymentByModelSpec(notification.ModelId)
                 & new DeploymentActiveSpec();
        var deploymentsResult = await _deploymentRepository
            .Find(spec).Run().RunAsync();

        if (deploymentsResult.IsFail) return;

        foreach (var deployment in deploymentsResult.Unwrap())
        {
            var assessment = ComplianceAssessment.Create(
                notification.ModelId, deployment.Id,
                notification.NewRiskTier);
            await _assessmentRepository.Create(assessment).Run().RunAsync();
        }
    }
}
```

핵심 포인트:
- `RequiresComplianceAssessment` 도메인 속성으로 조건 판별
- `DeploymentByModelSpec & DeploymentActiveSpec`: Specification 합성으로 "해당 모델의 활성 배포"를 조회
- 각 활성 배포에 대해 독립적으로 평가 생성

## Command vs Event Handler 실행 모델 비교

| 구분 | Command Handler | Event Handler |
|------|----------------|---------------|
| 트리거 | 외부 요청 | 도메인 이벤트 |
| 반환 타입 | `FinResponse<Response>` | `void` (ValueTask) |
| 실패 처리 | 호출자에게 오류 반환 | 로깅 후 무시 (eventual consistency) |
| 트랜잭션 | 전체 체인이 하나의 트랜잭션 | 독립 트랜잭션 |
| 검증 | FluentValidation + FinT | 도메인 속성으로 조건 분기 |

[구현 결과](./03-implementation-results/)에서 전체 Use Case 목록과 포트 매핑을 확인합니다.
