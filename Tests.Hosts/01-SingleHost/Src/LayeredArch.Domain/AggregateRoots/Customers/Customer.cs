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

    #endregion

    // Value Object 속성
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public Money CreditLimit { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // ORM용 기본 생성자
#pragma warning disable CS8618
    private Customer() { }
#pragma warning restore CS8618

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
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Customer CreateFromValidated(
        CustomerId id,
        CustomerName name,
        Email email,
        Money creditLimit,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        return new Customer(id, name, email, creditLimit)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
