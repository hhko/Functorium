using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

namespace LayeredArch.Domain.AggregateRoots.Customers;

/// <summary>
/// 고객 도메인 모델 (Aggregate Root)
/// CustomerId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// 공유 VO(Money)를 CreditLimit에 사용하여 공유 시나리오를 증명합니다.
/// </summary>
[GenerateEntityId]
public sealed class Customer : AggregateRoot<CustomerId>, IAuditable
{
    #region Domain Events

    /// <summary>
    /// 고객 생성 이벤트
    /// </summary>
    public sealed record CreatedEvent(
        CustomerId CustomerId,
        CustomerName Name,
        Email Email) : DomainEvent;

    /// <summary>
    /// 신용한도 변경 이벤트
    /// </summary>
    public sealed record CreditLimitUpdatedEvent(
        CustomerId CustomerId,
        Money OldCreditLimit,
        Money NewCreditLimit) : DomainEvent;

    /// <summary>
    /// 이메일 변경 이벤트
    /// </summary>
    public sealed record EmailChangedEvent(
        CustomerId CustomerId,
        Email OldEmail,
        Email NewEmail) : DomainEvent;

    #endregion

    // Value Object 속성
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public Money CreditLimit { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private Customer(
        CustomerId id,
        CustomerName name,
        Email email,
        Money creditLimit)
        : base(id)
    {
        Name = name;
        Email = email;
        CreditLimit = creditLimit;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 직접 받음
    /// Application Layer에서 Email 중복 검사 후 호출
    /// </summary>
    public static Customer Create(
        CustomerName name,
        Email email,
        Money creditLimit)
    {
        var customer = new Customer(CustomerId.New(), name, email, creditLimit);
        customer.AddDomainEvent(new CreatedEvent(customer.Id, name, email));
        return customer;
    }

    /// <summary>
    /// 신용한도를 변경합니다.
    /// </summary>
    public Customer UpdateCreditLimit(Money newLimit)
    {
        var oldLimit = CreditLimit;
        CreditLimit = newLimit;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CreditLimitUpdatedEvent(Id, oldLimit, newLimit));
        return this;
    }

    /// <summary>
    /// 이메일을 변경합니다.
    /// Application Layer에서 이메일 고유성 확인 후 호출해야 합니다.
    /// </summary>
    public Customer ChangeEmail(Email newEmail)
    {
        var oldEmail = Email;
        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new EmailChangedEvent(Id, oldEmail, newEmail));
        return this;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Customer CreateFromValidated(
        CustomerId id,
        CustomerName name,
        Email email,
        Money creditLimit,
        DateTime createdAt,
        Option<DateTime> updatedAt)
    {
        return new Customer(id, name, email, creditLimit)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
