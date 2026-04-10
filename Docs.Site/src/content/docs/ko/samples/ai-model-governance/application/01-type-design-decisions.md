---
title: "애플리케이션 타입 설계 의사결정"
description: "AI 모델 거버넌스 플랫폼의 포트 설계, ApplyT 패턴, FinT<IO> 컴포지션"
---

## 개요

[애플리케이션 비즈니스 요구사항](../00-business-requirements/)에서 정의한 워크플로우 규칙을 분석하여, Use Case(Command/Query)와 포트(Port)를 식별합니다. 각 Use Case가 어떤 포트를 사용하고, 어떤 순서로 조율하는지 결정합니다.

## Command 식별

| Command | 입력 | 핵심 흐름 | 결과 |
|---------|------|----------|------|
| RegisterModelCommand | Name, Version, Purpose | VO 합성 -> 위험 분류 -> 모델 생성 -> 저장 | ModelId |
| ClassifyModelRiskCommand | ModelId, RiskTier | 모델 조회 -> 재분류 -> 업데이트 | -- |
| CreateDeploymentCommand | ModelId, Url, Env, Drift | VO 합성 -> 모델 확인 -> 배포 생성 -> 저장 | DeploymentId |
| SubmitDeploymentForReviewCommand | DeploymentId | 배포 조회 -> 모델 조회 -> 적격성 검증 -> 제출 -> 저장 | -- |
| ActivateDeploymentCommand | DeploymentId, AssessmentId | 배포 조회 -> 평가 조회 -> 통과 확인 -> 활성화 -> 저장 | -- |
| QuarantineDeploymentCommand | DeploymentId, Reason | 배포 조회 -> 격리 -> 저장 | -- |
| InitiateAssessmentCommand | ModelId, DeploymentId | 모델 조회 -> 배포 조회 -> 평가 생성 -> 저장 | AssessmentId |
| ReportIncidentCommand | DeploymentId, Severity, Desc | VO 합성 -> 배포 조회 -> 인시던트 생성 -> 저장 | IncidentId |

## Query 식별

| Query | 입력 | 결과 | 포트 |
|-------|------|------|------|
| GetModelByIdQuery | ModelId | 모델 상세 (배포/평가/인시던트 포함) | IModelDetailQuery |
| SearchModelsQuery | RiskTier?, Page, Size | 모델 목록 | IAIModelQuery |
| GetDeploymentByIdQuery | DeploymentId | 배포 상세 | IDeploymentDetailQuery |
| SearchDeploymentsQuery | Status?, Env?, Page, Size | 배포 목록 | IDeploymentQuery |
| GetAssessmentByIdQuery | AssessmentId | 평가 상세 (기준 포함) | IAssessmentRepository |
| GetIncidentByIdQuery | IncidentId | 인시던트 상세 | IIncidentRepository |
| SearchIncidentsQuery | Severity?, Status?, Page, Size | 인시던트 목록 | IIncidentQuery |

## Event Handler 식별

| Event Handler | 트리거 이벤트 | 동작 |
|--------------|-------------|------|
| QuarantineDeploymentOnCriticalIncidentHandler | ModelIncident.ReportedEvent | Critical/High 심각도 시 배포 자동 격리 |
| InitiateAssessmentOnRiskUpgradeHandler | AIModel.RiskClassifiedEvent | High/Unacceptable 상향 시 활성 배포에 평가 생성 |

## 포트 설계

포트는 Application Layer가 외부 세계와 소통하는 인터페이스입니다. Command 포트(Repository)는 상태 변경과 조회를 위한 쓰기 전용 인터페이스이고, Query 포트(Read Adapter)는 읽기 전용 인터페이스입니다. 이 분리는 CQRS 원칙에 따라 쓰기와 읽기의 최적화 경로를 독립적으로 관리할 수 있게 합니다.

외부 서비스 포트는 LanguageExt IO 고급 기능(Timeout, Retry, Fork, Bracket)을 반환 타입 `FinT<IO, T>`으로 표현하여, Application Layer의 FinT LINQ 체인에 자연스럽게 합성됩니다.

### Command 포트 (Repository)

| 포트 | 기본 CRUD | 커스텀 메서드 |
|------|-----------|-------------|
| IAIModelRepository | IRepository 기본 | Exists(spec), GetByIdIncludingDeleted(id) |
| IDeploymentRepository | IRepository 기본 | Exists(spec), Find(spec) |
| IAssessmentRepository | IRepository 기본 | Exists(spec), Find(spec) |
| IIncidentRepository | IRepository 기본 | Exists(spec), Find(spec) |

### Query 포트 (Read Adapter)

| 포트 | 역할 |
|------|------|
| IAIModelQuery | 모델 목록 검색 (필터, 페이지네이션) |
| IModelDetailQuery | 모델 상세 조회 (배포/평가/인시던트 집계) |
| IDeploymentQuery | 배포 목록 검색 |
| IDeploymentDetailQuery | 배포 상세 조회 |
| IIncidentQuery | 인시던트 목록 검색 |

### 외부 서비스 포트

| 포트 | 역할 | IO 패턴 |
|------|------|---------|
| IModelHealthCheckService | 모델 헬스 체크 | Timeout + Catch |
| IModelMonitoringService | 모델 드리프트 모니터링 | Retry + Schedule |
| IParallelComplianceCheckService | 병렬 컴플라이언스 체크 | Fork + awaitAll |
| IModelRegistryService | 외부 레지스트리 조회 | Bracket |

## VO 합성 패턴: ApplyT

여러 Value Object를 동시에 검증하여 **모든 오류를 한번에 수집합니다.** 첫 번째 오류에서 멈추지 않고 모든 검증을 실행합니다.

```csharp
// RegisterModelCommand.Usecase.Handle()
from vos in (
    ModelName.Create(request.Name),
    ModelVersion.Create(request.Version),
    ModelPurpose.Create(request.Purpose)
).ApplyT((name, version, purpose) => (Name: name, Version: version, Purpose: purpose))
```

`ApplyT`는 각 `Fin<T>`의 검증 결과를 applicative하게 합성합니다. 3개 중 2개가 실패하면 2개의 오류가 모두 수집됩니다.

## FinT<IO, T> LINQ 컴포지션

Command Handler의 핵심 패턴은 `FinT<IO, T>` LINQ 합성입니다. IO 효과(데이터베이스 조회, 저장)와 실패 가능성(도메인 오류)을 하나의 체인으로 표현합니다.

```csharp
// SubmitDeploymentForReviewCommand.Usecase.Handle()
FinT<IO, Response> usecase =
    from deployment in _deploymentRepository.GetById(deploymentId)
    from model in _modelRepository.GetById(deployment.ModelId)
    from _1 in _eligibilityService.ValidateEligibility(
        model, _assessmentRepository, _incidentRepository)
    from _2 in deployment.SubmitForReview()
    from updated in _deploymentRepository.Update(deployment)
    select new Response();
```

각 `from` 절은 이전 단계가 성공했을 때만 실행됩니다. `ValidateEligibility`가 `ProhibitedModel` 오류를 반환하면 `SubmitForReview`는 실행되지 않습니다.

## Nested Class 캡슐화

모든 Use Case는 Nested Class 패턴으로 Request, Response, Validator, Usecase를 하나의 외부 클래스 안에 캡슐화합니다.

```csharp
public sealed class RegisterModelCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(string ModelId);

    public sealed class Validator : AbstractValidator<Request> { ... }

    public sealed class Usecase(...) : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(...) { ... }
    }
}
```

이 패턴의 장점:
- Use Case의 모든 구성 요소가 한 파일에 응집
- Request/Response 타입이 Use Case 네임스페이스 수준에서 명확
- Validator가 Request와 동일 스코프에 위치하여 규칙 추적 용이

## FluentValidation + MustSatisfyValidation

Validator에서 도메인 VO의 `Validate` 메서드를 직접 재사용합니다.

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name).MustSatisfyValidation(ModelName.Validate);
        RuleFor(x => x.Version).MustSatisfyValidation(ModelVersion.Validate);
        RuleFor(x => x.Purpose).MustSatisfyValidation(ModelPurpose.Validate);
    }
}
```

`MustSatisfyValidation`은 VO의 `Validate` 메서드를 FluentValidation 규칙으로 변환합니다. 도메인 검증 규칙이 한 곳에서 정의되고 Application Layer에서 재사용됩니다.

Smart Enum 타입의 검증은 `MustSatisfyValidationOf`를 사용합니다:

```csharp
RuleFor(x => x.RiskTier)
    .MustSatisfyValidationOf<Request, string, RiskTier>(RiskTier.Validate);
```

Entity ID 검증은 `MustBeEntityId`를 사용합니다:

```csharp
RuleFor(x => x.ModelId).MustBeEntityId<Request, AIModelId>();
```

다음 단계에서는 이 타입 설계를 C# 코드로 매핑하여 [코드 설계](./02-code-design/)를 진행합니다.
