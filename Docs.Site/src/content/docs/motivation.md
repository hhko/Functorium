---
title: Design Motivation
description: Structural problems and solutions from the perspectives of domain logic, ubiquitous language, and observability
---

## Problems We Aim to Solve

There are three structural problems that repeatedly arise when developing enterprise .NET applications.
These problems are not apparent in the early stages of a project, but they turn into costs as the service grows and the team scales.

### 1. Domain Logic Is Entangled with Exceptions and Implicit Side Effects

If you are writing service code like the following, this problem applies to you.

```csharp
public OrderResult PlaceOrder(OrderCommand cmd)
{
    var product = _productRepo.GetById(cmd.ProductId)
        ?? throw new NotFoundException("Product not found.");

    if (product.Stock < cmd.Quantity)
        throw new BusinessException("Insufficient stock.");

    var order = new Order(cmd.ProductId, cmd.Quantity, product.Price);
    _orderRepo.Save(order);
    _logger.LogInformation("Order created: {OrderId}", order.Id);

    return new OrderResult(order.Id);
}
```

This code works functionally. It passes unit tests and raises no special concerns during code review.

However, as the service grows, the team expands, and use cases multiply, the following problems emerge.

**Exceptions are used for flow control.** "Product not found" and "Insufficient stock" are not exceptional situations — they are business rule failures. Yet when handled as exceptions, callers must branch with `try-catch`, and the signature alone does not reveal which exceptions may be thrown. Because failure paths are not visible in the type, a new team member reading the code must open the implementation to answer the question: "Can this method fail?"

**Validation logic becomes scattered.** As discount policies, shipping restrictions, and membership tier checks are added, `if-throw` blocks are inserted throughout service methods. The same validations are repeated across multiple use cases, and subtle bugs arise when validation order affects results — for example, an "insufficient stock" error may be returned before checking that the product is ineligible for a discount.

**Composition is impossible.** To combine two validations or choose an alternative path on failure, exception-based code requires nested `try-catch` blocks. This is hard to read and hard to test. Even if you want to run validations A and B together and collect all failures at once, execution halts at the first exception, making this impossible.

**Side effects occur implicitly.** In the code above, `_orderRepo.Save(order)` and `_logger.LogInformation()` are side effects that mutate external state. But the method signature `OrderResult PlaceOrder(OrderCommand cmd)` looks like a pure function. Whether this method writes to a database or calls an external API can only be determined by reading the implementation.

These problems are manageable when the codebase is small. But when use cases grow to dozens, each with 5 to 10 validation rules, exception-based flow control structurally undermines the predictability of the system.

Functorium solves this problem with **`Fin<T>`.** Success and failure are made explicit at the type level, and `from ... in ... select` LINQ composition assembles domain flows without exceptions. Validation rules are declaratively composed with `ValidationRules<T>`, consolidating scattered `if-throw` blocks into a single pipeline. `FinT<IO, T>` tracks side effects at the type level, allowing pure functions and effectful functions to be distinguished by signature alone.

### 2. Development Language and Operations Language Are Separated

Suppose you have code that manages state in an order system.

```csharp
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

The development team uses `OrderStatus.Confirmed`. But the operations team's incident response manual refers to it as "order confirmed" or "confirmed status," the CS team's customer service script says "payment completed," and the database stores it as the numeric code `1`. A single domain concept has four different representations.

Even in this state, the system operates correctly. Each team uses the correct term in its own context, and during everyday work, the problem remains hidden.

However, when an incident occurs, this discrepancy becomes a cost.

**Log searches fail.** When the operations team searches for "order confirmed" in Seq, no results appear — because the code logs the English enum value `Confirmed`. "What was that log keyword again?" gets posted on Slack, and response is delayed until a developer checks the code and replies. During a late-night incident, this delay directly impacts MTTR (Mean Time To Recovery).

**Error code systems are maintained in duplicate.** The API returns `INSUFFICIENT_STOCK`, while the operations wiki documents "Error code E-1042: Insufficient stock." When new errors are added, the API code is updated but the wiki is not, and the gap between the two documents widens over time. After six months, no one trusts the operations wiki, but there is no alternative, so the document is left abandoned.

**Interpretation of domain concepts diverges.** Whether "order cancellation" means only the `Cancelled` state or includes `Refunded` is interpreted differently by the development team and the operations team. Such interpretation gaps are not caught in code review — they surface only in incident postmortems. The "cancellation count" metric shows different numbers for each team, and more time is spent determining which is correct.

The root cause of this problem is that code, documentation, and operational metrics use different language systems. Agreeing to unify the language is easy, but actually maintaining it is hard — because documentation does not automatically follow when code changes.

Functorium solves this by **embedding Ubiquitous Language directly in code.** Bounded Contexts are clearly defined, and domain concepts are consistently reflected in code, documentation, and operational metrics. Error codes are automatically generated from types, eliminating the need for separate document synchronization.

### 3. Observability Is Added as an Afterthought

A week after release, a report comes in that response times spike sharply during certain time windows. You want to identify which use case is causing the bottleneck, but most APIs have no logs recording response times. Distributed Tracing is not configured either, so there is no way to determine which services a request passes through or where the delay occurs.

In the development schedule, Observability was always a lower priority. The decision to "finish features first, add monitoring later" was repeated, and "later" arrives only after an incident.

The typical response pattern that follows looks like this.

```csharp
public async Task<OrderResult> PlaceOrder(OrderCommand cmd)
{
    var sw = Stopwatch.StartNew();
    _logger.LogInformation("PlaceOrder started: {@Command}", cmd);

    // ... existing business logic ...

    sw.Stop();
    _logger.LogInformation("PlaceOrder completed: {Elapsed}ms", sw.ElapsedMilliseconds);
    return result;
}
```

A Stopwatch and logging are inserted as an emergency patch and redeployed. The immediate issue can now be diagnosed, but as this pattern repeats, structural problems accumulate in the code.

**Logging code overtakes business logic.** Start/end logs, elapsed time measurements, and input serialization code are inserted repeatedly in each use case. It is not uncommon for 10 lines of business logic to be accompanied by 15 lines of instrumentation code. Answering "What is the core logic of this method?" during code review becomes increasingly difficult.

**Instrumentation coverage is uneven.** Logging added as an emergency patch applies only to the API where the problem occurred. If an incident recurs in a different API, the same work is repeated. An asymmetric state solidifies where some APIs have detailed logs and others have none. If the root cause lies in an API with no logs, the double work of adding logs and redeploying is required before diagnosis can even begin.

**Afterthought instrumentation structurally permits omissions.** Even when the team agrees to "add logging to every use case," the compiler does not catch missing logging in newly written use cases. Instrumentation quality that depends on code review degrades in proportion to reviewer fatigue. As a result, observability becomes an uncertain property that "may or may not exist."

Functorium solves this problem by **embedding Observability from the design stage.** OpenTelemetry-based Logging, Metrics, and Tracing are automatically applied through the Mediator Pipeline. Simply registering a use case in the pipeline causes instrumentation data to be collected, so there is no need to insert logging code into individual methods. Even when new use cases are added, there are no gaps in instrumentation coverage, and observability becomes a default property guaranteed by the framework rather than a property that "may or may not exist."

:::caution[A Structural Problem]
These three problems are not simply process issues — they are problems of design philosophy and structure. Features may be complete, yet operational stability is not assured; or the process of achieving stability causes costs and time to increase repeatedly. Strengthening code reviews or adding documentation processes does not solve them. The structure in which problems arise must itself be changed.
:::

## Direction for Breaking Through

The three problems above are not independent of one another. When domain logic is expressed through exceptions, it becomes difficult to reflect consistent domain language in logs; when Observability is added after the fact, domain flows and instrumentation data end up disconnected. Functorium breaks through all three problems by unifying them into a single architecture.

:::tip[Core Principle]
By combining domain-centric Ubiquitous Language and Observability on top of a functional architecture, a consistent structure is established from development through operations.
:::

### Maintaining Domain Logic Purity Through Functional Architecture

Failures are expressed through types instead of exceptions.

`Fin<T>` represents success (`Succ`) and failure (`Fail`) in a single type. The method signature alone reveals that "this operation can fail," and callers handle failures through pattern matching or LINQ composition. Exceptions are reserved for truly exceptional situations (network failures, out of memory), while business rule failures are conveyed as values.

`FinT<IO, T>` adds side effect tracking on top of this. Because IO operations (database queries, external API calls) are declared in the type, pure functions and effectful functions are distinguished at the signature level. The `IO` monad provides advanced capabilities such as Timeout, Retry (exponential backoff), Fork (parallel execution), and Bracket (resource lifecycle management) out of the box, enabling type-safe fault tolerance for external service calls. This keeps domain logic pure while managing side effects at the pipeline boundary.

### Unifying with Ubiquitous Language

Code, logs, and metrics share the same language.

Bounded Contexts are clearly defined, and domain concepts are consistently reflected in code types, log messages, and operational metrics. Error codes are derived from types, so separate error code documentation is unnecessary. Code is documentation, and logs are the operational language.

This approach is more than a naming convention. It creates a structure where domain types determine log structures and metric labels, so that when code changes, the operational language automatically follows.

### Embedding Observability from the Design Stage

All use cases are instrumented without writing any instrumentation code.

OpenTelemetry-based Logging, Metrics, and Tracing are automatically applied through the Mediator Pipeline. When a use case is registered, instrumentation data is collected alongside it, and domain flows and instrumentation data are designed within the same structure. Even when new use cases are added, there are no gaps in instrumentation coverage, and developers do not need to write instrumentation code manually.

The pipeline automatically records the start and end of each use case, success and failure outcomes, and execution time — making observability a property guaranteed by the architecture, not by the developer's diligence.

---

The concrete design principles for breaking through these three problems
are covered in [Design Philosophy](/Functorium/design-philosophy/).
