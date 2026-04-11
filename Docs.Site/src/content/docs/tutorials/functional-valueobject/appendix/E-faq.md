---
title: "FAQ"
---
## General Questions

### Q: What is the difference between a value object and an entity?

**A:** A value object is identified by its values, while an entity is identified by a unique ID.

| Property | Value Object | Entity |
|----------|-------------|--------|
| Identification | Identified by value | Identified by unique ID |
| Equality | Equal if all properties are the same | Equal if ID is the same |
| Immutability | Always immutable | Mutable |
| Lifecycle | None | Has one |

```csharp
// Value object: equal if values are the same
var email1 = Email.Create("user@example.com");
var email2 = Email.Create("user@example.com");
// email1 == email2 (true)

// Entity: equal if ID is the same
var user1 = new User(id: 1, name: "Alice");
var user2 = new User(id: 1, name: "Bob");
// user1 == user2 (true, even though names differ)
```

---

### Q: When should I use Fin<T> vs Validation<Error, T>?

**A:** Choose based on whether there are dependencies between validations.

| Type | Execution Mode | Error Handling | When to Use |
|------|---------------|----------------|-------------|
| `Fin<T>` | Sequential (Bind) | Stops at first error | Dependent validations |
| `Validation<Error, T>` | Parallel (Apply) | Collects all errors | Independent validations |

```csharp
// Fin<T>: Sequential validation - if A fails, B is not executed
ValidateA().Bind(_ => ValidateB()).Bind(_ => ValidateC());

// Validation: Parallel validation - all validations run, errors collected
(ValidateA(), ValidateB(), ValidateC()).Apply((a, b, c) => new Result(a, b, c));
```

---

### Q: Can I put business logic in a value object?

**A:** Yes, logic related to that value should be included in the value object.

```csharp
public sealed class Money : ComparableValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    // ✅ Appropriate: monetary operations
    public Money Add(Money other) =>
        Currency == other.Currency
            ? new Money(Amount + other.Amount, Currency)
            : throw new InvalidOperationException("Different currencies");

    // ✅ Appropriate: formatting
    public string ToFormattedString() => $"{Amount:N2} {Currency}";

    // ❌ Inappropriate: depends on external system
    public async Task<decimal> GetExchangeRate() { /* API call */ }
}
```

---

### Q: Why use a private constructor and a Create factory method?

**A:** To guarantee an **always-valid state**. A private constructor prevents object creation that bypasses validation, and the Create factory method returns an instance only when validation passes.

```csharp
// ❌ Public constructor: can create invalid objects
public class Email
{
    public Email(string value) { Value = value; }
}
var invalid = new Email("not-an-email"); // Invalid!

// ✅ Private constructor + Create: created only after validation
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            ValidationRules<Email>.NotNull(value)
                .ThenNotEmpty()
                .ThenMaxLength(255),
            v => new Email(v));
}
var result = Email.Create("not-an-email"); // Returns Fail
```

---

## Implementation Questions

### Q: How do I persist value objects in EF Core?

**A:** There are three approaches.

**1. OwnsOne (Recommended)**
```csharp
modelBuilder.Entity<User>()
    .OwnsOne(u => u.Email, email =>
    {
        email.Property(e => e.Value).HasColumnName("Email");
    });
```

**2. Value Converter**
```csharp
modelBuilder.Entity<User>()
    .Property(u => u.Email)
    .HasConversion(
        e => (string)e,
        s => Email.CreateFromValidated(s));
```

**3. OwnsMany (Collections)**
```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems);
```

---

### Q: How do I handle value objects in JSON serialization?

**A:** Implement a JsonConverter.

```csharp
public class EmailJsonConverter : JsonConverter<Email>
{
    public override Email Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return Email.Create(value!)
            .IfFail(e => throw new JsonException(e.Message));
    }

    public override void Write(Utf8JsonWriter writer, Email email, JsonSerializerOptions options)
    {
        writer.WriteStringValue((string)email);
    }
}
```

---

### Q: Is it okay to throw exceptions when creating a value object?

**A:** Return `Fin<T>` or `Validation` instead of throwing exceptions. Exceptions are only allowed internally for pre-validated values.

```csharp
// ❌ Using exceptions
public static Email Create(string value)
{
    if (!IsValid(value))
        throw new ArgumentException("Invalid email");
    return new Email(value);
}

// ✅ Using result types
public static Fin<Email> Create(string value)
{
    if (!IsValid(value))
        return Error.New("Invalid email");
    return new Email(value);
}

// ⚠️ Exceptions only allowed for validated values (internal use)
public static Email CreateFromValidated(string value) => new(value);
```

---

### Q: Can a value object contain an ID?

**A:** No, if it has an ID then it is an entity.

```csharp
// ❌ ID in a value object
public sealed class Email : SimpleValueObject<string>
{
    public Guid Id { get; } // This makes it an entity!
}

// ✅ Value objects contain only values
public sealed class Email : SimpleValueObject<string>
{
    // No ID, identified by value only
}
```

---

## Performance Questions

### Q: Are there performance issues with creating many value objects?

**A:** In most cases, no. Value objects are small objects and the .NET GC handles them efficiently. For high-performance scenarios, consider stack allocation with `record struct`.

```csharp
// Heap allocation (class-based)
public sealed class Email : SimpleValueObject<string> { }

// Stack allocation possible (struct-based) - for high performance
public readonly record struct EmailStruct(string Value);
```

---

### Q: Is it a problem if GetHashCode() is called frequently?

**A:** Since the object is immutable, you can cache the hash code in a field.

```csharp
public abstract class ValueObject
{
    private int? _cachedHashCode;

    public override int GetHashCode()
    {
        return _cachedHashCode ??= ComputeHashCode();
    }

    private int ComputeHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(17, (hash, obj) =>
                HashCode.Combine(hash, obj?.GetHashCode() ?? 0));
    }
}
```

---

## Testing Questions

### Q: What should I verify in value object tests?

**A:** Verify creation validation (valid/invalid input), value equality (same values are equal, different values are not equal, hash code consistency), immutability (original unchanged after operations), and where applicable, comparison/sort order.

```csharp
[Fact]
public void Create_WithValidEmail_ShouldSucceed()
{
    var result = Email.Create("user@example.com");
    result.IsSucc.ShouldBeTrue();
}

[Fact]
public void Equals_WithSameValue_ShouldBeTrue()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");
    email1.ShouldBe(email2);
}
```

---

### Q: Can I enforce value object rules with architecture tests?

**A:** Yes, using ArchUnitNET.

```csharp
[Fact]
public void ValueObjects_ShouldBeSealed()
{
    var rule = Classes()
        .That().AreAssignableTo(typeof(ValueObject))
        .Should().BeSealed();

    rule.Check(Architecture);
}

[Fact]
public void ValueObjects_ShouldNotHavePublicConstructors()
{
    var rule = Classes()
        .That().AreAssignableTo(typeof(ValueObject))
        .Should().NotHavePublicConstructors();

    rule.Check(Architecture);
}
```

---

### Q: When should I use ValidationRules<T> vs raw Validation<Error, T>?

**A:** Use `ValidationRules<T>` for single-field sequential validation, and raw `Validation` for composite-field parallel validation.

| Approach | Characteristics | When to Use |
|----------|----------------|-------------|
| `ValidationRules<T>` | Auto-includes type name, chaining | Single-field sequential validation |
| raw `Validation<Error, T>` | Flexible composition, Apply/Bind | Composite fields, custom logic |

```csharp
// ValidationRules<T>: Single-field sequential validation (concise)
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(
        ValidationRules<Email>.NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(255),
        v => new Email(v));

// raw Validation: Apply composition in composite value objects
public static Fin<Money> Create(decimal amount, string currency) =>
    CreateFromValidation(
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => (a, c)),
        t => new Money(t.a, t.c));
```

---

## Design Questions

### Q: Won't it get complex if there are too many value objects?

**A:** Apply them only to values that have validation rules, are reused in multiple places, or carry business meaning. Splitting simple strings into individual types is excessive.

```csharp
// ❌ Excessive: separate types for simple strings
public sealed class FirstName : SimpleValueObject<string> { }
public sealed class LastName : SimpleValueObject<string> { }
public sealed class MiddleName : SimpleValueObject<string> { }

// ✅ Appropriate: group as a composite value object
public sealed class FullName : ValueObject
{
    public string First { get; }
    public string Last { get; }
    public string? Middle { get; }
}
```

---

### Q: How do I handle dependencies between value objects?

**A:** Use composition by including already-validated value objects as properties.

```csharp
public sealed class Order : ValueObject
{
    public OrderId Id { get; }           // Another value object
    public Money TotalAmount { get; }    // Another value object
    public ShippingAddress Address { get; } // Another value object

    public static Validation<Error, Order> Create(
        OrderId id,
        Money totalAmount,
        ShippingAddress address)
    {
        // Each value object is already valid
        return new Order(id, totalAmount, address);
    }
}
```

---

## Wrapping Up

If you have more questions:
- Submit questions on GitHub Issues
- Use the tags `value-objects`, `languageext` on Stack Overflow
- Participate in community discussions
