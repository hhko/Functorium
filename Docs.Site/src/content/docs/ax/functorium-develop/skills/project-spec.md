---
title: "Project Spec"
description: "Writing project requirements specifications (PRD)"
---

> **project-spec** -> architecture-design -> domain-develop -> application-develop -> adapter-develop -> observability-develop -> test-develop

## Prerequisites

None. This skill is the first step of the workflow. It starts from the user's vision and business problem.

## Background

The most common problem when starting a new project is "jumping straight into writing code." Writing code with ambiguous Aggregate boundaries leads to expensive refactoring later. Without defining a Ubiquitous Language, team members end up calling the same concept by different names.

The `project-spec` skill integrates **PM-perspective spec writing** (vision, user stories, priorities, acceptance criteria) with **DDD-perspective domain analysis** (Ubiquitous Language, Aggregate boundaries, business rules). This document becomes the input for `domain-develop`, `application-develop`, and `adapter-develop` skills, ensuring that design intent is consistently carried through to the code.

## Skill Overview

### 4 Phase Workflow

| Phase | Activity | Deliverable |
|-------|----------|-------------|
| 1. Vision Collection | Project basic info, users, KPIs, Non-Goals, timeline | Project overview draft |
| 2. Domain Analysis + User Stories | Ubiquitous Language, business rules, user stories (INVEST), acceptance criteria (Given/When/Then) | Language table + rule catalog + stories |
| 3. Scope Decision + Prioritization | Aggregate boundary identification, P0/P1/P2 priorities, milestones | Aggregate candidates + priority table |
| 4. Document Generation | Organize all content into a structured document | `00-project-spec.md` |

### Trigger Examples

```text
Write the PRD
Define the requirements
Plan the project
Write the spec
Organize business requirements
Start the project
```

## Phase 1: Vision Collection

The skill collects the following information through conversation.

**Project Basic Information:**
- What is the project name?
- Describe it in one sentence?
- What business problem are you solving?

**User Information:**
- Who are the target users (personas)?
- What are each persona's key goals?

**Business Information:**
- What are the key success metrics (KPIs)?
  - Leading indicators (e.g., daily active users, feature adoption rate)
  - Lagging indicators (e.g., revenue, retention rate)
- What are the integration constraints with existing systems?
- What are the technical constraints? (.NET 10, monolith/microservice, etc.)

**Scope Boundaries:**
- **What are the Non-Goals?** -- Features/scope explicitly excluded from this project
- Are there hard deadlines or external dependencies?

The user does not need to provide all information at once. The skill incrementally collects it through questions.

## Phase 2: Domain Analysis + User Stories

Extracts domain model candidates and user stories from the collected vision.

### Ubiquitous Language Extraction

Identifies key elements from business descriptions:
- **Key nouns** -> Entity/VO candidates (e.g., "model", "deployment", "assessment")
- **Key verbs** -> Use case (Command/Query) candidates (e.g., "register", "approve", "quarantine")
- **State changes** -> Domain event candidates (e.g., "activated", "quarantined")

Results are organized into a table like:

| Korean | English | Definition |
|--------|---------|------------|
| AI Model | AIModel | A trained AI/ML model. The core entity managing lifecycle and risk |
| Model Deployment | ModelDeployment | Deployment of a specific AI model to a production environment. Tracks version, environment, and status |
| Risk Tier | RiskTier | EU AI Act-based model risk level (Minimal, Limited, High, Unacceptable) |

### User Story Extraction (INVEST Criteria)

Core stories are written for each persona:

```text
As a [persona], I want to [action], in order to [value].
```

**INVEST Criteria Verification:**
- **I**ndependent: Independent from other stories
- **N**egotiable: Implementation approach is negotiable
- **V**aluable: Provides value to users
- **E**stimable: Size can be estimated
- **S**mall: Completable within one sprint
- **T**estable: Verifiable with acceptance criteria

### Acceptance Criteria per Use Case

For each use case (Command/Query), acceptance criteria are written in Given/When/Then format:

```text
Given: [Precondition]
When:  [User action]
Then:  [Expected result]
```

Both success and rejection scenarios are written.

### Business Rule Classification

Identified rules are classified by type:

| Rule Type | Description | Example |
|-----------|-------------|---------|
| Invariant | A condition that must always be true | "Model name cannot be empty" |
| State Transition | Only allowed transitions are possible | "Only Draft -> PendingReview is allowed" |
| Cross-cutting Rule | References multiple Aggregates | "Automatically quarantine Active deployment on Critical incident" |
| Forbidden State | Must be structurally impossible | "Deployment of Unacceptable risk models" |

## Phase 3: Scope Decision + Prioritization

### Aggregate Boundary Identification

Identifies Aggregate candidates according to Evans' criteria.

**Boundary Decision Criteria:**
1. **Transactional consistency** -- Data changed in the same transaction belongs in the same Aggregate
2. **Invariant scope** -- The scope of data that invariants must guarantee
3. **Independent lifecycle** -- Can be created/deleted independently without other Aggregates
4. **Inter-Aggregate references** -- Reference by ID only (direct object references forbidden)

**Inter-Aggregate Coordination:**
- **Synchronous coordination** -> Domain Service (within the same transaction)
- **Asynchronous coordination** -> Domain Event + Event Handler (eventual consistency)

### P0/P1/P2 Priority Classification

All use cases and user stories are classified by priority:

| Priority | Criteria | MoSCoW Mapping |
|----------|----------|----------------|
| **P0** | Cannot ship without it | Must Have |
| **P1** | Weakens competitiveness without it | Should Have |
| **P2** | Differentiates if present | Could Have |

### Scope Creep Prevention Checklist

When a feature addition is requested, check these 5 items:
1. Does this feature directly solve the core problem?
2. Does this feature provide value without P0?
3. Can users accept it being deferred to post-launch?
4. Is the value sufficient relative to implementation cost?
5. Does it not fall under Non-Goals?

### Timeline + Milestones

- Identify hard deadlines
- External dependencies (other teams, third-party APIs, infrastructure)
- Scope per milestone (Phase 1: P0, Phase 2: P0+P1, ...)

## Phase 4: Document Generation

All collected information is structured into `{context}/00-project-spec.md`.

### Output Document Structure

```markdown
# {Project Name} -- Project Requirements Specification

## 1. Project Overview
### Background / Goals / Target Users / Success Metrics (Leading+Lagging) / Technical Constraints

## 2. Non-Goals (What We Won't Do)

## 3. Ubiquitous Language
| Korean | English | Definition |

## 4. User Stories (INVEST)
### Per-persona stories + priorities

## 5. Aggregate Candidates
| Aggregate | Core Responsibility | State Transitions | Key Events |

## 6. Business Rules
### Per-Aggregate rules + cross-cutting rules

## 7. Use Cases + Acceptance Criteria
### Commands / Queries / Event Handlers + Given/When/Then

## 8. Forbidden States
| Forbidden State | Prevention Strategy | Functorium Pattern |

## 9. Priority Summary (P0/P1/P2)

## 10. Timeline / Milestones

## 11. Open Questions (engineering/product/design/legal)

## 12. Next Steps
```

### Next Step Guidance

After document generation, the skill guides the next steps:

> The project spec is complete.
>
> **Next Steps:**
> 1. Use the `architecture-design` skill to design the project structure and infrastructure
> 2. Use the `domain-develop` skill to design and implement each Aggregate in detail
> 3. Use the `application-develop` skill to implement use cases

## Example: AI Model Governance PRD

Here is a key summary of a real project's PRD.

### Non-Goals

- Model training pipeline management -- Separate MLOps platform domain
- A/B testing platform -- Consider after Phase 2
- Real-time model performance dashboard -- Replaced with external monitoring tool integration

### 4 Aggregates

| Aggregate | Core Responsibility | State Transitions | Key Events |
|-----------|--------------------|--------------------|------------|
| AIModel | Model lifecycle management | - | RegisteredEvent, RiskClassifiedEvent |
| ModelDeployment | Deployment environment management | Draft -> PendingReview -> Active -> Quarantined -> Decommissioned | ActivatedEvent, QuarantinedEvent |
| ComplianceAssessment | Regulatory compliance assessment | Initiated -> InProgress -> Passed/Failed | PassedEvent, FailedEvent |
| ModelIncident | Model incident/issue tracking | Reported -> Investigating -> Resolved/Escalated | ReportedEvent, ResolvedEvent |

### User Story Example

| ID | Story | Priority |
|----|-------|----------|
| US-001 | As an AI governance manager, I want to register a new AI model, in order to systematically manage risk. | P0 |
| US-002 | As an AI governance manager, I want to register a model in a deployment environment, in order to track operational status. | P0 |
| US-003 | As a compliance officer, I want to conduct a compliance assessment before deployment, in order to comply with the EU AI Act. | P0 |

### Acceptance Criteria Example

**Model Registration (RegisterModel):**

Success Scenario:
```text
Given: A valid model name, version (SemVer), and purpose are ready
When:  The manager registers a model
Then:  The model is created with Minimal risk level and a RegisteredEvent is published
```

Rejection Scenario:
```text
Given: The model name is empty
When:  The manager attempts to register a model
Then:  A "ModelName is required" validation error is returned
```

### Priorities

| Priority | Use Cases |
|----------|-----------|
| **P0** | RegisterModel, CreateDeployment, ReportIncident, InitiateAssessment |
| **P1** | SubmitForReview, ActivateDeployment, QuarantineDeployment |
| **P2** | Drift detection automation, parallel compliance checks |

### Open Questions

| ID | Question | Category | Blocking |
|----|----------|----------|----------|
| Q-001 | When will the external ML monitoring API spec be finalized? | engineering | Non-blocking |
| Q-002 | Is there a possibility of compliance criteria changes based on the EU AI Act enforcement timeline? | legal | Blocking |

## Core Principles

- **Start with business language, don't end with technical language** -- Ubiquitous Language is the source of code naming
- **Explicitly state Non-Goals to prevent scope creep** -- Agreeing on "what not to do" is as important as "what to do"
- **User stories are verified against INVEST criteria** -- Untestable stories must be revised
- **Acceptance criteria include both success and rejection scenarios** -- Rejection scenarios are the source of domain rules
- **Start from P0** -- Cannot ship without P0, P1/P2 are lower priority
- **Aggregate boundaries are based on transactional consistency** -- Business rules, not data models, determine boundaries
- **Forbidden states are structurally eliminated through the type system** -- Compile-time guarantees take priority over runtime validation
- **Open Questions are tracked with per-category tagging** -- Blocking/non-blocking distinction determines whether progress can continue

## References

- [Workflow](../workflow/) -- 7-step overall flow
- [Architecture Design Skill](./architecture-design/) -- Next step: Project structure design
- [Domain Develop Skill](./domain-develop/) -- Aggregate detailed design and implementation
