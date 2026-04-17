---
title: "Domain Type Design Decisions"
description: "Invariant classification and Functorium pattern mapping for the AI Model Governance domain"
---

## Overview

This document analyzes the natural language requirements defined in the [Business Requirements](../00-business-requirements/) from a DDD perspective. The first step is to identify independent consistency boundaries (Aggregates) from the business areas, and the second step is to classify the rules within each boundary as invariants.

## Identifying Aggregates from Business Areas

Before encoding business rules as types, we must first identify independent consistency boundaries from the business areas. Four Aggregates are derived from four business topics.

### Business Topic -> Aggregate Mapping

| Business Topic | Aggregate | Derivation Rationale |
|---------------|-----------|---------------------|
| AI Model Management | AIModel | Unique model lifecycle, independent risk tier management |
| Deployment Lifecycle | ModelDeployment | 6-state transition rules, independent per-deployment management |
| Compliance Assessment | ComplianceAssessment | Owns assessment criteria, score calculation consistency boundary |
| Incident Management | ModelIncident | Unique incident state transitions, independent lifecycle |
| Cross-Domain Rules | -- | Cross-Aggregate validation -> Domain Service |

### Why Separate Deployment from Model?

Model information changes (name, purpose, version) and deployment state transitions have different frequencies and concurrency requirements. One model can have multiple deployments, each following independent state transition rules. Model archiving should not block deployment transactions.

### Why Is Assessment an Independent Aggregate?

A compliance assessment owns multiple assessment criteria (AssessmentCriterion) and automatically calculates the score after all criteria are evaluated. This calculation logic must be independent of the deployment. Changes to deployment status should not affect an in-progress assessment.

### Summary of Aggregate Separation Rationale

| Aggregate | Separation Reason | Core Invariants |
|-----------|-------------------|----------------|
| **AIModel** | Unique model lifecycle, archive/restore | Archive guard, model name validity |
| **ModelDeployment** | 6-state transition, independent per-deployment management | State transitions, endpoint URL validity |
| **ComplianceAssessment** | Owns assessment criteria, score calculation consistency | All criteria evaluated before completion, automatic score calculation |
| **ModelIncident** | Unique incident state transitions, auto-quarantine trigger | State transitions, severity-based quarantine |

## Domain Terminology Mapping

Business terms are mapped to DDD tactical patterns.

| English | DDD Pattern | Role |
|---------|------------|------|
| AIModel | Aggregate Root | Governance target, owns risk tier |
| ModelDeployment | Aggregate Root | Deployment status management, references Model ID |
| ComplianceAssessment | Aggregate Root | Owns assessment criteria, score calculation |
| AssessmentCriterion | Entity (child) | Individual criterion within assessment, subordinate to Assessment |
| ModelIncident | Aggregate Root | Incident status management |
| ModelName | Value Object | String, 100 characters or fewer |
| ModelVersion | Value Object | SemVer format, regex validation |
| ModelPurpose | Value Object | String, 500 characters or fewer |
| RiskTier | Value Object (Smart Enum) | 4 levels, domain properties embedded |
| DeploymentStatus | Value Object (Smart Enum) | 6 states, transition rules embedded |
| DeploymentEnvironment | Value Object (Smart Enum) | Staging, Production |
| EndpointUrl | Value Object | URL format validation |
| DriftThreshold | Value Object | 0.0~1.0 range, comparable |
| AssessmentScore | Value Object | 0~100 range, passing threshold embedded |
| AssessmentStatus | Value Object (Smart Enum) | 5 states, transition rules embedded |
| CriterionResult | Value Object (Smart Enum) | Pass, Fail, NotApplicable |
| IncidentSeverity | Value Object (Smart Enum) | 4 levels, quarantine condition embedded |
| IncidentStatus | Value Object (Smart Enum) | 4 states, transition rules embedded |
| IncidentDescription | Value Object | String, 2000 characters or fewer |
| ResolutionNote | Value Object | String, 2000 characters or fewer |
| RiskClassificationService | Domain Service | Keyword-based risk tier classification |
| DeploymentEligibilityService | Domain Service | Cross-Aggregate eligibility verification |

## Invariant Classification System

Six invariant types were identified in this domain.

| Type | Scope | Key Question |
|------|-------|-------------|
| Single Value | Individual field | Is this value always valid? |
| Structural | Field combination | Are derived values consistent in parent-child relationships? |
| State Transition | Change over time | Do only allowed state changes occur? |
| Lifecycle | Aggregate lifecycle | Are actions blocked on deleted objects? |
| Ownership | Child entity boundary | Do children stay within the parent boundary? |
| Cross-Aggregate | Across multiple Aggregates | Where are rules guaranteed that cannot be validated by a single Aggregate? |

## Design Decisions by Invariant

### 1. Single Value Invariants

Constraints that individual fields must always hold valid values.

**Business Rules:**
- "Model name must be 100 characters or fewer and must not be empty"
- "Model version must be in SemVer format"
- "Model purpose must be 500 characters or fewer and must not be empty"
- "Endpoint URL must be a valid HTTP/HTTPS URL"
- "Drift threshold must be in the range 0.0~1.0"
- "Assessment score must be in the range 0~100"
- "Incident description must be 2000 characters or fewer and must not be empty"

**Design Decision:** Validate at creation and guarantee immutability thereafter.

| Naive Field | Functorium Type | Validation Rules |
|------------|----------------|-----------------|
| `string Name` | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(100) |
| `string Version` | `SimpleValueObject<string>` | NotNull, NotEmpty, SemVer regex |
| `string Purpose` | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(500) |
| `string Url` | `SimpleValueObject<string>` | NotNull, NotEmpty, URI format validation |
| `decimal DriftThreshold` | `ComparableSimpleValueObject<decimal>` | Between(0.0, 1.0) |
| `int Score` | `ComparableSimpleValueObject<int>` | Between(0, 100) |
| `string Description` | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(2000) |

### 2. Smart Enum Invariants

Constraints that enumerated values must only hold allowed values.

**Business Rules:**
- "Risk tier allows only Minimal, Limited, High, Unacceptable"
- "High or Unacceptable tiers require compliance assessments"
- "Unacceptable tier prohibits deployment"
- "Critical or High severity requires automatic deployment quarantine"

**Design Decision:** Embed domain properties in value objects using the Smart Enum pattern.

| Smart Enum | Value List | Embedded Domain Properties |
|-----------|-----------|--------------------------|
| RiskTier | Minimal, Limited, High, Unacceptable | `RequiresComplianceAssessment`, `IsProhibited` |
| IncidentSeverity | Critical, High, Medium, Low | `RequiresQuarantine` |
| CriterionResult | Pass, Fail, NotApplicable | -- |
| DeploymentEnvironment | Staging, Production | -- |

### 3. State Transition Invariants

Constraints that states transition only according to defined rules. The Smart Enum + `HashMap` transition map pattern is applied.

| Smart Enum | Transition Rules | Terminal States |
|-----------|-----------------|----------------|
| DeploymentStatus | Draft->PendingReview, PendingReview->Active/Rejected, Active->Quarantined/Decommissioned, Quarantined->Active/Decommissioned | Decommissioned, Rejected |
| IncidentStatus | Reported->Investigating/Escalated, Investigating->Resolved/Escalated | Resolved, Escalated |
| AssessmentStatus | Initiated->InProgress, InProgress->Passed/Failed/RequiresRemediation | Passed, Failed, RequiresRemediation |

### 4. Lifecycle Invariants

**Business Rule:** "Archived models cannot be modified"

**Design Decision:** `ISoftDeletableWithUser` + `DeletedAt.IsSome` guard pattern. `ClassifyRisk`, `UpdateVersion`, `Update` methods return `AlreadyDeleted` error when `DeletedAt.IsSome`.

### 5. Ownership Invariants

**Business Rule:** "Assessment criteria are subordinate to the compliance assessment"

**Design Decision:** `ComplianceAssessment` manages child entities via a private `List<AssessmentCriterion>`, exposing only `IReadOnlyList` externally. Criterion creation, evaluation, and completion are all possible only through the parent Aggregate.

### 6. Cross-Aggregate Invariants

**Business Rules:**
- "Deployment eligibility: prohibited tier check, compliance assessment check, unresolved incident check"
- "Risk tier classification: automatic classification based on purpose keywords"

**Design Decision:** Implemented as Domain Services.

| Domain Service | Cross-Domain Targets | Verification Content |
|---------------|---------------------|---------------------|
| RiskClassificationService | AIModel (purpose) | Keyword -> risk tier mapping |
| DeploymentEligibilityService | AIModel, ComplianceAssessment, ModelIncident | Prohibited tier, assessment passed, incident absence |

### Decision Not to Use UnionValueObject

All Smart Enums in this domain are single-dimensional (a single string value). `RiskTier`, `DeploymentStatus`, etc. do not have different field structures per value, so the `SimpleValueObject<string>` + `HashMap` pattern was chosen over UnionValueObject (discriminated union). Domain properties (`RequiresComplianceAssessment`, `IsProhibited`, etc.) are sufficiently expressed as Smart Enum instance methods/properties.

## Design Decision Summary Table

| Design Decision | Functorium Type | Application Example | Guaranteed Effect |
|---|---|---|---|
| Single value validation + immutability + normalization | `SimpleValueObject<T>` + `Validate` chain | ModelName, ModelVersion, EndpointUrl | Validation at creation, Trim normalization, empty string blocking |
| Comparable single value + range | `ComparableSimpleValueObject<T>` | DriftThreshold, AssessmentScore | Range validation, size comparison, domain property (IsPassing) |
| Smart Enum + domain properties | `SimpleValueObject<string>` + `HashMap` | RiskTier, IncidentSeverity | Only valid values allowed, domain properties embedded |
| Smart Enum + state transition rules | `SimpleValueObject<string>` + transition map | DeploymentStatus, IncidentStatus, AssessmentStatus | Only allowed transitions possible, terminal state guaranteed |
| Aggregate Root dual factory | `AggregateRoot<TId>` + `Create`/`CreateFromValidated` | AIModel, ModelDeployment, ComplianceAssessment, ModelIncident | Separation of domain creation and ORM restoration |
| Child entity + collection management | `Entity<TId>` + private `List` + `IReadOnlyList` | AssessmentCriterion | Collection cannot be directly modified externally |
| Cross-Aggregate business rules | `IDomainService` | DeploymentEligibilityService, RiskClassificationService | Cross-Aggregate verification |
| Queryable domain specifications | `ExpressionSpecification<T>` | ModelNameSpec, DeploymentActiveSpec, etc. (12 types) | Expression Tree-based querying |
| Domain events + domain errors | Nested `sealed record` | AIModel.RegisteredEvent, ModelDeployment.InvalidStatusTransition | Events/errors cohesive within Aggregate |
| Soft Delete + guard | `ISoftDeletableWithUser` + `DeletedAt.IsSome` | AIModel.ClassifyRisk(), Update() | Blocking changes on archived Aggregates |

In the next step, these design decisions are mapped to C# code to proceed with [Code Design](../02-code-design/).
