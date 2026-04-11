---
title: "functorium-develop"
description: "A Claude Code plugin where AI guides the entire DDD development process from PRD to testing"
---

functorium-develop is a Claude Code plugin that guides the 7-step development workflow for DDD projects based on the Functorium framework.

## Installation

```bash
# Load standalone
claude --plugin-dir ./.claude/plugins/functorium-develop

# Load simultaneously with the release-note plugin
claude --plugin-dir ./.claude/plugins/functorium-develop --plugin-dir ./.claude/plugins/release-note
```

> `--plugin-dir` loads plugins on a per-session basis. They appear in `/skills` in the format `functorium-develop:{skill-name}`.

## 7-Step Workflow

```
project-spec → architecture-design → domain-develop → application-develop → adapter-develop → observability-develop → test-develop
```

| Step | Skill | Role | Key Deliverables |
|------|-------|------|-----------------|
| 1 | [project-spec](./skills/project-spec/) | Requirements specification (PRD) | `00-project-spec.md` |
| 2 | [architecture-design](./skills/architecture-design/) | Project structure and infrastructure design | `01-architecture-design.md` |
| 3 | [domain-develop](./skills/domain-develop/) | Domain model design and implementation | `domain/00~03` + source code |
| 4 | [application-develop](./skills/application-develop/) | CQRS use case implementation | `application/00~03` + source code |
| 5 | [adapter-develop](./skills/adapter-develop/) | Repository, Endpoint, DI implementation | `adapter/00~03` + source code |
| 6 | [observability-develop](./skills/observability-develop/) | Observability strategy (KPI mapping, dashboard, alerts) | Observability strategy document |
| 7 | [test-develop](./skills/test-develop/) | Unit/integration/architecture tests | Test code |

Separately, the [domain-review](./skills/domain-review/) skill reviews existing code from a DDD perspective at any point.

Detailed flows for each step and inter-step connections are covered on the [Workflow](./workflow/) page.

## 8 Skills

| Skill | Layer | Trigger Examples |
|-------|-------|-----------------|
| [project-spec](./skills/project-spec/) | Planning | "Write the PRD", "Define the requirements" |
| [architecture-design](./skills/architecture-design/) | Design | "Design the architecture", "Set up the project structure" |
| [domain-develop](./skills/domain-develop/) | Domain | "Implement the domain", "Create the Aggregate" |
| [application-develop](./skills/application-develop/) | Application | "Implement the use case", "Create a Command" |
| [adapter-develop](./skills/adapter-develop/) | Adapter | "Implement the Repository", "Create an endpoint" |
| [observability-develop](./skills/observability-develop/) | Observability | "Design observability", "Design the dashboard" |
| [test-develop](./skills/test-develop/) | Testing | "Write tests", "Add integration tests" |
| [domain-review](./skills/domain-review/) | Review | "Do a DDD review", "Review the architecture" |

## 6 Expert Agents

Agents are specialists in specific layers, used when in-depth discussion about design decisions is needed. If skills are "automated workflows," agents are "expert consultations."

| Agent | Area of Expertise |
|-------|-------------------|
| product-analyst | PRD writing, requirements analysis, user stories, Aggregate boundary identification |
| domain-architect | Ubiquitous language, Aggregate boundaries, type strategy |
| application-architect | CQRS design, port identification, FinT composition, CtxEnricher 3-Pillar design |
| adapter-engineer | Repository, Endpoint, DI registration, CtxEnricherPipeline integration |
| observability-engineer | KPI-to-metric mapping, dashboard, alerts, ctx.* propagation, distributed tracing |
| test-engineer | Unit/integration/architecture tests, ctx 3-Pillar snapshot tests |

Detailed roles and usage examples for agents are covered on the [Expert Agents](./agents/) page.

## Plugin Structure

```
.claude/plugins/functorium-develop/
├── .claude-plugin/plugin.json      # Manifest (v0.4.0)
├── skills/                         # 8 skills
│   ├── project-spec/
│   ├── architecture-design/
│   ├── domain-develop/
│   ├── application-develop/
│   ├── adapter-develop/
│   ├── observability-develop/
│   ├── test-develop/
│   └── domain-review/
└── agents/                         # 6 expert agents
    ├── product-analyst.md
    ├── domain-architect.md
    ├── application-architect.md
    ├── adapter-engineer.md
    ├── observability-engineer.md
    └── test-engineer.md
```

## Getting Started

If starting a new project, begin with the PRD:

```text
Write the PRD. I want to build an AI model governance platform.
```

If you already have requirements, start with domain development:

```text
Implement the domain. I want to design the Product Aggregate.
```

If you have existing code, start with a review:

```text
Review the current domain code from a DDD perspective.
```
