---
title: "Value Objects: Union Types"
---

This document covers the design and implementation of Union value objects that safely represent domain states using the Discriminated Union pattern. For core value object concepts, see [05a-value-objects](../05a-value-objects). For enumeration and validation patterns, see [05b-value-objects-validation](../05b-value-objects-validation).

## Introduction

"Why is it possible for a contact to have neither an email nor an address?"
"The code allows a verification request on an already-verified email."
"A new contact type was added, but one of the existing branches was not updated, causing a runtime error."

These problems repeatedly arise when representing domain states with enumerations or nullable fields. Union value objects **express only the allowed state combinations as types,** blocking invalid states at compile time.

### What You Will Learn

This document covers the following topics:

1. **Why Discriminated Unions are needed** - Differences from enum and SmartEnum
2. **Base class selection criteria** - Usage scenarios for `UnionValueObject` and `UnionValueObject<TSelf>`
3. **Pure data Union implementation** - Patterns that express only allowed combinations without state transitions
4. **State transition Union implementation** - Safe state transitions using the `TransitionFrom` helper
5. **`[UnionType]` source generator** - Ensuring exhaustiveness through auto-generated Match/Switch
6. **Using Unions in Aggregates** - Guard + transition delegation pattern

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- The Create/Validate separation pattern from [Value Objects](../05a-value-objects)
- C# record types and pattern matching
- Basic concepts of LanguageExt's `Fin<T>`

> Union value objects are the core implementation of the DDD design principle of **"making invalid states unrepresentable."** By defining only the allowed state combinations as types, you can ensure safety at compile time without runtime validation.

## Summary

### Key Commands

```csharp
// Pure data Union definition
[UnionType]
public abstract partial record ContactInfo : UnionValueObject { ... }

// State transition Union definition
[UnionType]
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState> { ... }

// Execute state transition
Fin<Verified> result = emailState.Verify(verifiedAt);

// Process all cases with Match
string display = contactInfo.Match(
    emailOnly: eo => eo.EmailState.ToString(),
    postalOnly: po => po.Address.ToString(),
    emailAndPostal: ep => $"{ep.EmailState}, {ep.Address}");
```

### Key Procedures

**1. Define a Union Value Object:**
1. Choose the base class based on whether pure data or state transitions are needed
2. Declare as `abstract partial record` + `[UnionType]`
3. Define cases as `sealed record` + `private` constructor
4. (For state transitions) Call `TransitionFrom` in transition methods

**2. Use in Aggregate:**
1. Validate guard conditions in the Aggregate method (e.g., deletion status)
2. Extract the required state from the Union using `Match`
3. Delegate state transitions to the Union object

### Key Concepts

| Concept | Description |
|---------|-------------|
| `UnionValueObject` | Base class for pure data Unions |
| `UnionValueObject<TSelf>` | Base class for state transition Unions (CRTP) |
| `[UnionType]` | Source generator that auto-generates Match/Switch/Is/As methods |
| `TransitionFrom` | Type-safe state transition helper |
| `Match<TResult>` | Method that enforces exhaustive handling of all cases |

---

## Why Discriminated Unions Are Needed

When expressing **fixed choices** in the domain, there are several options. The key criterion is whether each choice must have a **different data structure**.

| Property | C# `enum` | `SmartEnum` | `UnionValueObject` |
|----------|-----------|------------|---------------------|
| Different data per value | Impossible | Fixed properties only | Unique fields per case |
| State transition logic | Handled externally | Handled externally | Internal `TransitionFrom` |
| Compile-time exhaustiveness | `switch` warning | Not possible | Enforced by `Match` method |
| Per-case behavior | Not possible | Method override | Pattern matching |
| Usage scenario | Simple flags | Value + properties | Structural state branching |

**Selection Criteria:**
- All values share the **same data structure** -> `enum` or `SmartEnum`
- Each value has **different data** -> `UnionValueObject`

---

## Base Class Selection

From the `IUnionValueObject` -> `UnionValueObject` -> `UnionValueObject<TSelf>` hierarchy, select the base class based on whether state transitions are needed.

```
IUnionValueObject (marker interface)
  +-- UnionValueObject (pure data Union)
       +-- UnionValueObject<TSelf> (state transition Union, CRTP)
```

| Condition | Choice |
|-----------|--------|
| Express only allowed combinations (no state transitions) | `UnionValueObject` |
| State transition logic needed | `UnionValueObject<TSelf>` |

`UnionValueObject<TSelf>` uses CRTP (Curiously Recurring Template Pattern) so that the `TransitionFrom` helper can include accurate type information in `DomainError`.

---

## Pure Data Union Implementation

A pattern that **expresses only allowed combinations as types** without state transitions.

### Implementation Rules

1. Declare as `abstract partial record` + `[UnionType]`
2. Inherit from `UnionValueObject`
3. Define cases as `sealed record`
4. Block external extension with a `private` constructor

### Example: ContactInfo

Contact information must be one of "email only", "postal only", or "email+postal". "No contact method" is structurally impossible.

```csharp
[UnionType]
public abstract partial record ContactInfo : UnionValueObject
{
    public sealed record EmailOnly(EmailVerificationState EmailState) : ContactInfo;
    public sealed record PostalOnly(PostalAddress Address) : ContactInfo;
    public sealed record EmailAndPostal(EmailVerificationState EmailState, PostalAddress Address) : ContactInfo;

    private ContactInfo() { }
}
```

- The `private` constructor prevents adding new cases externally
- Only one of the three cases can be selected, making "no contact method" impossible
- Since it is a `record`, value-based equality is automatically provided

---

## State Transition Union Implementation

A pattern that **allows only valid transitions** between states. It inherits from `UnionValueObject<TSelf>` to use the `TransitionFrom` helper.

### TransitionFrom Helper

```csharp
protected Fin<TTarget> TransitionFrom<TSource, TTarget>(
    Func<TSource, TTarget> transition,
    string? message = null)
```

| Situation | Result |
|-----------|--------|
| `this` is `TSource` | Applies the transition function -> `Fin.Succ(result)` |
| `this` is not `TSource` | `Fin.Fail(DomainError(InvalidTransition))` |

The `DomainError` includes the `TSelf` type information passed via CRTP and `FromState`/`ToState` information.

**InvalidTransition Error Type:**

```csharp
// Defined in DomainErrorType.Transition.cs
public sealed record InvalidTransition(string? FromState = null, string? ToState = null) : DomainErrorType;
```

Example of the error JSON structure generated on transition failure:

```json
{
  "ErrorCode": "DomainErrors.EmailVerificationState.InvalidTransition",
  "ErrorCurrentValue": "Verified { Email = user@example.com, VerifiedAt = 2026-01-15 }",
  "Message": "Invalid transition from Verified to Verified"
}
```

> **Note**: The `InvalidTransition` error type is documented in the Transition category of [Error System: Domain/Application Errors](../08b-error-system-domain-app).

### Example: EmailVerificationState

Email verification allows only unidirectional transition from `Unverified -> Verified`.

```csharp
[UnionType]
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState>
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState;

    private EmailVerificationState() { }

    /// Unverified -> Verified transition. Returns a failure when already in Verified state.
    public Fin<Verified> Verify(DateTime verifiedAt) =>
        TransitionFrom<Unverified, Verified>(
            u => new Verified(u.Email, verifiedAt));
}
```

- The return type of `Verify` is `Fin<Verified>` -- on success, the result is guaranteed to be in `Verified` state
- Calling `Verify` while already in `Verified` state automatically returns an `InvalidTransition` error
- The transition function `u => new Verified(u.Email, verifiedAt)` preserves the email from `Unverified` while adding the verification timestamp

---

## [UnionType] Source Generator

When the `[UnionType]` attribute is applied to an `abstract partial record`, the source generator automatically generates the following 4 types of members.

### Generated Members

| Generated Member | Signature | Purpose |
|------------------|-----------|---------|
| `Match<TResult>` | `Func<Case, TResult>` parameters (one per case) | Exhaustively handles all cases and returns a value |
| `Switch` | `Action<Case>` parameters (one per case) | Exhaustively handles all cases (no return) |
| `Is{Case}` | `bool` property | Checks whether it is a specific case |
| `As{Case}()` | `Case?` returning method | Safe cast to a specific case |

### Generation Example

For `ContactInfo`, the following code is automatically generated:

```csharp
public abstract partial record ContactInfo
{
    public TResult Match<TResult>(
        Func<EmailOnly, TResult> emailOnly,
        Func<PostalOnly, TResult> postalOnly,
        Func<EmailAndPostal, TResult> emailAndPostal)
    {
        return this switch
        {
            EmailOnly __case => emailOnly(__case),
            PostalOnly __case => postalOnly(__case),
            EmailAndPostal __case => emailAndPostal(__case),
            _ => throw new UnreachableCaseException(this)
        };
    }

    public void Switch(
        Action<EmailOnly> emailOnly,
        Action<PostalOnly> postalOnly,
        Action<EmailAndPostal> emailAndPostal) { ... }

    public bool IsEmailOnly => this is EmailOnly;
    public bool IsPostalOnly => this is PostalOnly;
    public bool IsEmailAndPostal => this is EmailAndPostal;

    public EmailOnly? AsEmailOnly() => this as EmailOnly;
    public PostalOnly? AsPostalOnly() => this as PostalOnly;
    public EmailAndPostal? AsEmailAndPostal() => this as EmailAndPostal;
}
```

### Requirements

- Must be declared as `abstract partial record`
- Must have the `[UnionType]` attribute applied
- Cases must be defined as `sealed record` and directly inherit the Union type

### UnreachableCaseException

Used in the default branch (`_ =>`) of `Match`/`Switch`. Since all cases are closed as `sealed record`, this should never be reached in normal circumstances, but it is included to resolve the compiler's exhaustiveness warning.

```csharp
public sealed class UnreachableCaseException(object value)
    : InvalidOperationException($"Unreachable case: {value.GetType().FullName}");
```

---

## Using Unions in Aggregates

### Guard + Transition Delegation Pattern

The Aggregate does not perform the transition itself, but **validates guard conditions and delegates to the Union object.**

```csharp
// Error type definitions
public sealed record AlreadyDeleted : DomainErrorType.Custom;
public sealed record NoEmailToVerify : DomainErrorType.Custom;

// Contact Aggregate's VerifyEmail method
public Fin<Unit> VerifyEmail(DateTime verifiedAt)
{
    // 1. Guard: check deletion status
    if (DeletedAt.IsSome)
        return DomainError.For<Contact>(
            new AlreadyDeleted(), Id.ToString(),
            "Cannot verify email of a deleted contact");

    // 2. Extract email state via Match
    var emailState = ContactInfo.Match<EmailVerificationState?>(
        emailOnly: eo => eo.EmailState,
        postalOnly: _ => null,
        emailAndPostal: ep => ep.EmailState);

    // 3. Guard: check email existence
    if (emailState is null)
        return DomainError.For<Contact>(
            new NoEmailToVerify(), Id.ToString(),
            "Contact does not have an email");

    // 4. Delegate state transition to EmailVerificationState
    return emailState.Verify(verifiedAt).Map(verified =>
    {
        ContactInfo = ContactInfo.Match(
            emailOnly: _ => (ContactInfo)new ContactInfo.EmailOnly(verified),
            postalOnly: _ => throw new InvalidOperationException(),
            emailAndPostal: ep => new ContactInfo.EmailAndPostal(verified, ep.Address));
        UpdatedAt = verifiedAt;
        AddDomainEvent(new EmailVerifiedEvent(Id, verified.Email, verifiedAt));
        return unit;
    });
}
```

**Pattern Summary:**

| Step | Role | Owner |
|------|------|-------|
| Guard | Precondition validation | Aggregate |
| State extraction | Get current state via `Match` | Aggregate |
| Transition execution | Change state via `TransitionFrom` | Union object |
| Apply result | Save new state + publish event | Aggregate |

### Projection Property Pattern

When Union internal values need to be used in queries, define a projection property on the Aggregate.

```csharp
public sealed class Contact : AggregateRoot<ContactId>
{
    // Automatically syncs EmailValue when ContactInfo is set
    private ContactInfo _contactInfo = null!;
    public ContactInfo ContactInfo
    {
        get => _contactInfo;
        private set
        {
            _contactInfo = value;
            EmailValue = ExtractEmail(value);
        }
    }

    // Email projection property (for Specification support)
    public string? EmailValue { get; private set; }

    private static string? ExtractEmail(ContactInfo contactInfo) => contactInfo.Match(
        emailOnly: eo => GetEmailString(eo.EmailState),
        postalOnly: _ => (string?)null,
        emailAndPostal: ep => GetEmailString(ep.EmailState));
}
```

This pattern allows the `EmailValue` property to be queried directly in `ExpressionSpecification`.

---

## Comparing ValueObject and UnionValueObject

| Item | `sealed class : ValueObject` | `abstract partial record : UnionValueObject` |
|------|------------------------------|----------------------------------------------|
| Purpose | Composite VO (PersonalName, PostalAddress) | Discriminated Union (ContactInfo, EmailVerificationState) |
| Equality | Explicit `GetEqualityComponents()` implementation | Compiler-generated (record) |
| Immutability | private constructor + `{ get; }` | record positional parameters |
| VO hierarchy | Participates in `ValueObject` hierarchy | Participates in `IUnionValueObject` hierarchy |
| ORM compatibility | Automatic proxy type handling | No proxy support |
| Hash code | Cached hash code | Compiler-generated (record) |
| Source Generator | -- | Auto-generates Match/Switch via `[UnionType]` |

---

## Troubleshooting

### Compile Error When Adding a New Case to Match

**Cause:** This is expected behavior. `Match<TResult>` requires a `Func` parameter for every case, so adding a new case causes argument count mismatch compile errors at existing `Match` call sites.

**Resolution:** Add a handler for the new case at all `Match`/`Switch` call sites. This is the core benefit of exhaustiveness guarantees.

### InvalidTransition Error from TransitionFrom

**Cause:** The current state does not match the transition source type. For example, calling `Verify` again while already in `Verified` state.

**Resolution:** Either check the current state before calling the transition in the Aggregate, or handle the `InvalidTransition` error appropriately in upper layers.

```csharp
// The error includes FromState and ToState information
// "Invalid transition from Verified to Verified"
```

### Source Generator Not Working When partial Keyword Is Missing

**Cause:** The `[UnionType]` source generator only recognizes records with the `partial` keyword. Without `partial`, the generator cannot add code.

**Resolution:** Declare as `abstract partial record`.

```csharp
// Correct declaration
[UnionType]
public abstract partial record ContactInfo : UnionValueObject { ... }

// Missing partial -- Match/Switch will not be generated
[UnionType]
public abstract record ContactInfo : UnionValueObject { ... }
```

### record Cannot Inherit from class

**Cause:** In C#, a record cannot inherit from another class. This is why the design uses `IUnionValueObject` (interface) instead of `ValueObject` (class).

**Resolution:** Union types should inherit from `UnionValueObject` (abstract record). `ValueObject` (class) cannot be used.

---

## FAQ

### Q1. What is the selection criteria between SmartEnum and UnionValueObject?

**Use `SmartEnum` when all values share the same data structure,** and **`UnionValueObject` when each value has different data.**

```csharp
// SmartEnum: all currencies have the same structure (Name, Value, Symbol, KoreanName)
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency KRW = new("KRW", "KRW", "₩", "Korean Won");
    public static readonly Currency USD = new("USD", "USD", "$", "US Dollar");
}

// UnionValueObject: different data structures per case
public abstract partial record ContactInfo : UnionValueObject
{
    public sealed record EmailOnly(EmailVerificationState EmailState) : ContactInfo;
    public sealed record PostalOnly(PostalAddress Address) : ContactInfo;
}
```

| Scenario | Choice |
|----------|--------|
| Fixed list + same properties | `SmartEnum` |
| Unique data per case | `UnionValueObject` |
| State transition logic needed | `UnionValueObject<TSelf>` |

### Q2. Do Unions have a Validate/Create pattern?

Union value objects **do not use the Validate/Create pattern.** Each Union case receives already-validated VOs as parameters, so the Union's own validation amounts to "which case is it" -- a business logic decision. This is handled in the Aggregate or Application Layer.

```csharp
// Union cases receive already-validated VOs
var contactInfo = new ContactInfo.EmailOnly(
    new EmailVerificationState.Unverified(email));  // email is an already-validated EmailAddress VO
```

### Q3. Can C# switch be used instead of Match?

**Possible but not recommended.** C# `switch` does not require a default branch (`_`), so adding a new case may not be caught at compile time. `Match` enforces handlers for all cases.

```csharp
// Match: compile error when new case is added (safe)
contactInfo.Match(
    emailOnly: eo => ...,
    postalOnly: po => ...,
    emailAndPostal: ep => ...);

// C# switch: new case falls through to _ branch (risky)
var result = contactInfo switch
{
    ContactInfo.EmailOnly eo => ...,
    ContactInfo.PostalOnly po => ...,
    _ => ...  // New cases may silently fall through here
};
```

### Q4. Can behavior methods be defined on Union cases?

**Possible, but state transition methods should be defined on the Union root.** `TransitionFrom` is defined on `UnionValueObject<TSelf>`, so it must be called from the root record. Per-case utility methods can be defined on individual cases.

```csharp
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState>
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState
    {
        // Per-case utility is fine
        public bool IsExpired(DateTime now) => (now - VerifiedAt).TotalDays > 365;
    }

    // State transition methods are defined on the root
    public Fin<Verified> Verify(DateTime verifiedAt) =>
        TransitionFrom<Unverified, Verified>(u => new Verified(u.Email, verifiedAt));
}
```

---

## References

- [Value Objects](../05a-value-objects) - Core value object concepts and base class selection
- [Value Objects: Enumerations, Validation, and Practical Patterns](../05b-value-objects-validation) - SmartEnum, Application Layer validation merging
- [Error System: Basics and Naming](../08a-error-system) - DomainError, DomainErrorType
- [Error System: Domain/Application Errors](../08b-error-system-domain-app) - InvalidTransition error type
- [Unit Testing Guide](../testing/15a-unit-testing)
