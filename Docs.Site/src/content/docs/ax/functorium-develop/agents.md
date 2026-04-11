---
title: "Expert Agents"
description: "6 expert agents of the functorium-develop plugin"
---

## Skills vs Agents

The functorium-develop plugin provides two types of tools: skills and agents. They differ in purpose and usage.

| Aspect | Skill | Agent |
|--------|-------|-------|
| Analogy | Automated workflow | Expert consultation |
| Behavior | Executes predefined Phases in order | Free-form discussion for design decisions |
| Deliverables | Documents + code (structured output) | Design advice, decision rationale |
| When to use | "Implement this" | "How should I design this?" |
| Trigger | Keyword-based automatic matching | Activated when expertise is needed in conversation |

Skills automate repetitive work. Agents provide an expert perspective at moments when design judgment is needed. For complex projects, the effective combination is to first determine the design direction with an agent, then automate implementation with a skill.

## Agent List

| Agent | Area of Expertise | Usage Scenario |
|-------|-------------------|----------------|
| `product-analyst` | PRD writing, requirements analysis, user stories, Aggregate boundaries | Project planning, requirements definition |
| `domain-architect` | Ubiquitous language, Aggregate boundaries, type strategy | Domain model design decisions |
| `application-architect` | CQRS separation, port identification, FinT composition, CtxEnricher 3-Pillar | Use case architecture decisions |
| `adapter-engineer` | Repository, Endpoint, DI, Observable Port, CtxEnricherPipeline | Adapter implementation strategy |
| `observability-engineer` | KPI-to-metric mapping, dashboard, alerts, ctx.* propagation, distributed tracing | Observability strategy development |
| `test-engineer` | Unit/integration/architecture tests, ctx 3-Pillar snapshot, observability verification | Test strategy development |

## product-analyst -- Requirements Analysis Expert

Transforms business requirements into DDD domain models and handles project early-stage planning from PRD writing to architecture design.

**Areas of Expertise:**
- Requirements analysis and PRD (Product Requirements Document) writing
- Ubiquitous Language extraction
- Bounded Context identification
- Aggregate boundary identification and domain event discovery
- User story writing (INVEST principles)
- Priority determination (P0/P1/P2 classification)
- Acceptance criteria writing (Given/When/Then)
- Non-Goals identification and scope management
- Project structure design (solution/project/folder)

**Core Principles:**

| Principle | Description |
|-----------|-------------|
| Business value first | Prioritize by business impact, not technology |
| Explicit Non-Goals | Agree on exclusions to prevent Scope Creep |
| INVEST user stories | Untestable stories must be revised |
| Given/When/Then acceptance criteria | Write both success and rejection scenarios |

### Example 1 (Beginner): Non-Goals Identification

```text
User: What should be excluded from an e-commerce platform?
Agent: Here are Non-Goals candidates:
       - Real-time chat support -- Use a separate SaaS tool
       - Recommendation engine -- Consider after Phase 2
       - Multi-language support -- Outside MVP scope
       Agreeing on "what not to do" is as important as "what to do."
```

### Example 2 (Intermediate): User Story + Acceptance Criteria

```text
User: Write user stories for the product registration feature.
Agent: Writing based on INVEST criteria:
       "As a shop manager, I want to register a new product, in order to sell it to customers."

       Acceptance Criteria:
       Given: A valid product name (100 chars or less), price (positive), and category are ready
       When:  The manager registers a product
       Then:  The product is created in Draft status and a CreatedEvent is published

       Rejection Scenario:
       Given: The product name is empty
       When:  The manager attempts to register a product
       Then:  A "ProductName is required" validation error is returned
```

### Example 3 (Advanced): Aggregate Boundaries + Priority Determination

```text
User: Identify the Aggregates and priorities for an AI governance platform.
Agent: 4 Aggregates identified:
       1. AIModel -- Model lifecycle (registration, classification, status management)
       2. ModelDeployment -- Deployment environment management (Draft->Active->Quarantined)
       3. ComplianceAssessment -- Regulatory compliance assessment (EU AI Act)
       4. ModelIncident -- Incident/issue tracking

       Priorities:
       P0: RegisterModel, CreateDeployment, ReportIncident
       P1: SubmitForReview, ActivateDeployment, QuarantineDeployment
       P2: Drift detection automation, parallel compliance checks

       Cross-cutting rule: Critical incident -> Automatic quarantine of Active deployment
       (Implemented with Domain Event + EventHandler)
```

## domain-architect -- Domain Design Expert

Designs domain models based on Eric Evans' DDD principles.

**Areas of Expertise:**
- Ubiquitous Language definition
- Bounded Context boundary design
- Invariant classification and Functorium type mapping
- Aggregate Root boundary decisions
- Value Object vs Entity judgment
- Domain Service bulk operation design

**Functorium Type Mapping Principles:**

| Invariant Type | Functorium Type |
|----------------|-----------------|
| Single-value invariant | `SimpleValueObject<T>` |
| Comparison/operation invariant | `ComparableSimpleValueObject<T>` |
| Enumeration state | SmartEnum (`SimpleValueObject<string>` + `HashMap`) |
| State transition | `UnionValueObject<TSelf>` + `TransitionFrom` |
| Lifecycle management | `AggregateRoot<TId>` + `[GenerateEntityId]` |
| Conditional query | `ExpressionSpecification<T>` |
| Cross-Aggregate rule | static Domain Service (`Fin<T>` return) |

### Example 1 (Beginner): Value Object Type Decision

```text
User: How should I model the product price?
Agent: Price is a single numeric value requiring positive validation, so ComparableSimpleValueObject<decimal> is appropriate.
       It also provides Zero constant and Add/Subtract operations. Negative deduction returns a Fin<Money> failure.
```

### Example 2 (Intermediate): Aggregate Boundary Decision

```text
User: Can I combine Product and Inventory into a single Aggregate?
Agent: Separation is recommended. Reasons:
       1. Different change frequencies -- Product info changes rarely, inventory changes with every order
       2. Concurrency conflicts -- In the same Aggregate, product edits and inventory deductions would conflict
       3. Transaction boundary -- In DDD, one transaction should modify only one Aggregate
       Use ProductId as a cross-reference and connect via domain events.
```

### Example 3 (Advanced): Bounded Context Design

```text
User: How should I divide the Bounded Contexts for an e-commerce system?
Agent: Separate based on Ubiquitous Language:
       - Product Catalog (Product, Tag, Category)
       - Order Management (Order, OrderLine, ShippingAddress)
       - Customer Management (Customer, CreditLimit)
       - Inventory Management (Inventory, StockQuantity)

       Inter-context communication via domain events:
       Order.CreatedEvent -> Inventory deduction
       Order.CancelledEvent -> Inventory restoration

       Each context has independent Aggregates,
       and cross-references use only IDs (ProductId, CustomerId).
```

## application-architect -- Use Case Design Expert

Designs use cases based on the CQRS pattern.

**Areas of Expertise:**
- Command vs Query classification
- Port identification (IRepository, IQueryPort)
- FinT<IO, T> LINQ composition patterns
- FluentValidation integration
- Domain event handler design
- CtxEnricher 3-Pillar design (simultaneous Logging + Tracing + MetricsTag propagation)
- IDomainEventCollector.TrackEvent bulk event tracking

**Core Patterns:**

| Pattern | Purpose |
|---------|---------|
| `ICommandRequest<Response>` + `ICommandUsecase<Request, Response>` | State-changing use case |
| `IQueryRequest<Response>` + `IQueryUsecase<Request, Response>` | Read use case |
| `IDomainEventHandler<T.Event>` | Event reaction |
| `from...in...select` LINQ composition | Functional chaining |
| `guard()` condition check | Declarative condition short-circuit |
| `ApplyT` pattern | Parallel VO validation + FinT lifting |
| `[CtxRoot]` / `[CtxTarget]` / `[CtxIgnore]` | Observability field targeting |

### Example 1 (Beginner): Command vs Query Judgment

```text
User: Is product creation a Command? Query?
Agent: It's a Command. Since it changes state (creates), use ICommandRequest<Response>.
       Product retrieval is a Query -- use IQueryRequest<Response>.
```

### Example 2 (Intermediate): Composite Validation Design

```text
User: How should I handle name duplication and input validation during product creation?
Agent: Separate into 2 stages:
       1. Input validation: FluentValidation (Validator class)
          - MustSatisfyValidation(ProductName.Validate) -> Format validation
       2. Business rule: Inside the Usecase
          - guard(!exists, ApplicationError) -> Duplication validation

       FluentValidation executes before the Usecase in the pipeline,
       so format errors immediately return 400 without DB queries.
```

### Example 3 (Advanced): Event-Driven Workflow Design

```text
User: Design the flow for Order creation -> Inventory deduction -> Payment processing -> Shipping reservation.
Agent: Designing with the Eventual Consistency pattern:

       1. CreateOrderCommand (synchronous)
          -> Order.Create() -> Order.CreatedEvent
          -> Repository.Create() + SaveChanges

       2. OnOrderCreated EventHandler (asynchronous)
          -> Inventory.Deduct(quantity)
          -> On failure -> Publish Order.CancelledEvent

       3. OnOrderConfirmed EventHandler (asynchronous)
          -> PaymentService.Charge()
          -> On failure -> Order.CancelledEvent + Inventory.Restock

       Each step is an independent transaction. Rollback via compensating events on failure.
```

## adapter-engineer -- Adapter Implementation Expert

Specializes in Repository, Query Adapter, and Endpoint implementation.

**Areas of Expertise:**
- EfCoreRepositoryBase / InMemoryRepositoryBase implementation
- DapperQueryBase implementation (SQL + SpecTranslator)
- FastEndpoints Endpoint implementation
- [GenerateObservablePort] + DI registration
- EF Core Configuration, Migration
- CtxEnricherPipeline pipeline integration
- CtxEnricherContext.SetPushFactory initialization (Serilog + Activity + MetricsTagContext)
- Adapter internal operational logging with ObservableSignal

**Core Rules:**

| Rule | Description |
|------|-------------|
| `virtual` methods | All adapter methods must be virtual (required for Source Generator pipeline) |
| `[GenerateObservablePort]` | Observability wrapper auto-generation is mandatory |
| `IO.lift()` / `IO.liftAsync()` | Synchronous/asynchronous IO wrapping |
| `RequestCategory` | Observability classification: `"repository"`, `"query"`, `"external_api"`, `"unit_of_work"` |

### Example 1 (Beginner): Repository Implementation Strategy Question

```text
User: Should I implement InMemory or EF Core first?
Agent: Implement InMemory first.
       1. You can focus on domain logic testing
       2. Fast feedback loop without a DB
       3. Add EF Core when persistence is needed
       Switch via Provider branching in DI registration.
```

### Example 2 (Intermediate): EF Core Mapper Design

```text
User: How should I map the Product Aggregate to an EF Core Model?
Agent: You need 3 files:
       1. ProductModel (IHasStringId) -- DB table mapping
       2. ProductConfiguration (IEntityTypeConfiguration) -- Column/index settings
       3. ProductMapper -- ToDomain()/ToModel() conversion

       VOs are stored as primitives:
       ProductName -> string Name
       Money -> decimal Price
       ProductId -> string Id (Ulid.ToString())
```

### Example 3 (Advanced): Dapper + Specification Pattern Implementation

```text
User: How do I implement a product search API with Dapper?
Agent: This is a CQRS Read Side pattern:
       1. IProductQuery : IQueryPort<Product, ProductDto>
       2. DapperProductQuery : DapperQueryBase<Product, ProductDto>
          - SelectQuery: "SELECT Id, Name, Price FROM Products"
       3. ProductSpecTranslator -- Specification -> WHERE clause conversion
          - ProductNameContainsSpec -> "Name LIKE @Name"
          - PriceRangeSpec -> "Price BETWEEN @Min AND @Max"
       4. Pagination: offset-based (Page/PageSize) or cursor-based
```

## observability-engineer -- Observability Strategy Expert

Specializes in designing the Functorium framework's OpenTelemetry 3-Pillar observability.

**Areas of Expertise:**
- OpenTelemetry Logging / Metrics / Tracing 3-Pillar design
- CtxEnricher 3-Pillar propagation strategy (CtxPillar targeting, cardinality management)
- Observable Port + Source Generator observability automation
- ObservableSignal (adapter internal developer logging)
- Business KPI -> technical metric mapping
- Dashboard design (L1 scorecard, L2 drilldown)
- Alert patterns (P0/P1/P2 classification, AlertManager rules)
- Distributed tracing analysis (span chain bottleneck identification)

**Core Rules:**

| Rule | Description |
|------|-------------|
| Meter Name | `{service.namespace}.{layer}[.{category}]` |
| Instrument Name | `{layer}.{category}[.{cqrs}].{type}` (dot-separated, lowercase, plural) |
| `response.elapsed` | Recorded as Histogram instrument (MetricsTag forbidden) |
| Error classification | `expected` (business), `exceptional` (system), `aggregate` (composite) |
| MetricsTag | Only bounded values allowed (bool, low-cardinality enum) |

### Example 1 (Beginner): ctx.* Propagation Strategy Question

```text
User: Can I put customer_id in MetricsTag?
Agent: Forbidden. customer_id is high-cardinality (millions) and would create unbounded series.
       - customer_id -> CtxPillar.Default (Logging + Tracing only)
       - customer_tier -> CtxPillar.All (MetricsTag allowed, 3-5 kinds)
       Cardinality rule: Only use fields with limited unique values for MetricsTag.
```

### Example 2 (Intermediate): Dashboard Design

```text
User: Design an L1 dashboard for the e-commerce API.
Agent: 6 health indicator scorecard:
       1. Request count: rate(usecase_request_total[5m]) -- Throughput trend
       2. Success rate: 1 - (error_total / request_total) -- 99.9% or higher
       3. P95 latency: histogram_quantile(0.95, duration_bucket) -- < 200ms
       4. Error rate: rate(error_total[5m]) / rate(request_total[5m]) -- < 0.1%
       5. Exceptional errors: rate(error_total{error_type="exceptional"}[5m]) -- Converge to 0
       6. DomainEvent throughput: rate(event_publish_total[5m]) -- Trend check

       L2 drilldown decomposes into request.layer x request.category.name x request.handler.name
       3 dimensions to identify bottlenecks.
```

### Example 3 (Advanced): Distributed Tracing Diagnosis + Alert Design

```text
User: The order creation API is intermittently slow. How do I diagnose it?
Agent: 5-step analysis procedure:
       1. Signal detection -- Check P95 > 200ms on L1 dashboard
       2. Trace query -- request.handler.name="CreateOrderCommand" AND duration > 200ms
       3. Span analysis -- Check which child span is consuming time
          - Repository.Create: Normal (< 10ms)
          - CreditCheckService: Delayed (> 500ms) <-- Bottleneck
       4. Hypothesis -- External credit check API delay? Cache miss?
       5. Alert design:
          P0: CreditCheck API timeout 3 consecutive times -> On-call page
          P1: P95 > 1s sustained for 5 min -> Slack alert
          P2: New error.code appearance -> Daily dashboard review
```

## test-engineer -- Test Strategy Expert

Develops effective test strategies based on the test pyramid.

**Areas of Expertise:**
- Unit tests: NSubstitute, Shouldly, FinTFactory
- Integration tests: HostTestFixture, HttpClient
- Architecture rules: ArchUnitNET
- Snapshot tests: Verify.Xunit, LogTestContext
- CtxEnricher 3-Pillar snapshot tests (MetricsTagContext, Activity.Current, LogContext)

**Tools:**

| Tool | Purpose |
|------|---------|
| xUnit v3 (MTP) | Test framework |
| Shouldly | Assertions |
| NSubstitute | Mocking |
| FinTFactory | `FinT<IO, T>` mock return values |
| HostTestFixture<Program> | Integration test fixture |
| ArchUnitNET | Architecture rule verification |
| MetricsTagContext | AsyncLocal-based metric tag verification |

### Example 1 (Beginner): Test Naming Question

```text
User: How should I name test methods?
Agent: Use the T1_T2_T3 pattern:
       T1: Target method (Create, Handle, Delete)
       T2: Expected result (ShouldSucceed, ShouldFail, ShouldRaise)
       T3: Condition (WhenNameIsValid, WhenPriceIsNegative)

       Example: Create_ShouldSucceed_WhenNameIsValid
```

### Example 2 (Intermediate): Mock Strategy

```text
User: How do I mock the Repository in Usecase tests?
Agent: Use NSubstitute + FinTFactory:

       _repo.GetById(Arg.Any<ProductId>())
           .Returns(FinTFactory.Succ(product));

       _repo.Create(Arg.Any<Product>())
           .Returns(FinTFactory.Fail<Product>(error));

       FinTFactory conveniently creates FinT<IO, T> return values.
```

### Example 3 (Advanced): Test Pyramid Strategy

```text
User: What tests should I write when adding a new Aggregate?
Agent: Organize into 3 tiers:

       1. Unit tests (70%) -- Fast and many
          - Value Object: Create success/failure/normalization (3-5 per VO)
          - AggregateRoot: State change + event + guard conditions (3-5 per method)
          - Usecase: Success/failure/edge cases (3-5 per scenario)

       2. Integration tests (20%) -- Core flows
          - Endpoint: Success 201, Failure 400, NotFound 404
          - E2E: Create -> Read -> Update -> Delete

       3. Architecture rules (10%) -- Structural guarantees
          - sealed class, private constructors
          - [GenerateEntityId] attribute
          - Layer dependency direction
```

## Agent Usage Guide

### Inter-Agent Collaboration

For complex designs, use multiple agents sequentially:

1. **domain-architect** -> Determine Aggregate boundaries first
2. **application-architect** -> Use case patterns and port identification
3. **adapter-engineer** -> Implementation technology selection and strategy decisions
4. **test-engineer** -> Test pyramid and coverage strategy

### Invoking Agents

When you ask questions in conversation that fall within an agent's area of expertise, the relevant expertise is activated. You can also explicitly specify an agent.

### Combining with Skills

| Situation | Approach |
|-----------|----------|
| Design judgment is clear | Go directly to implementation with a skill |
| Aggregate boundaries are ambiguous | Consult domain-architect -> Implement with domain-develop skill |
| CQRS separation is complex | Consult application-architect -> Implement with application-develop skill |
| Need to compare persistence strategies | Consult adapter-engineer -> Implement with adapter-develop skill |
| Need to determine test scope | Consult test-engineer -> Implement with test-develop skill |

## References

- [Workflow](../workflow/) -- 7-step development workflow
- [Domain Develop Skill](../skills/domain-develop/) -- Skill reflecting domain-architect's expertise
- [Application Develop Skill](../skills/application-develop/) -- Skill reflecting application-architect's expertise
- [Adapter Develop Skill](../skills/adapter-develop/) -- Skill reflecting adapter-engineer's expertise
- [Test Develop Skill](../skills/test-develop/) -- Skill reflecting test-engineer's expertise
