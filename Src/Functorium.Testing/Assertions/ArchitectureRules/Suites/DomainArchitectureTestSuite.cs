using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using Functorium.Domains.Observabilities;
using Functorium.Domains.Services;
using Functorium.Domains.Specifications;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Testing.Assertions.ArchitectureRules.Suites;

public abstract class DomainArchitectureTestSuite
{
    protected abstract Architecture Architecture { get; }
    protected abstract string DomainNamespace { get; }

    protected virtual IReadOnlyList<Type> ValueObjectExcludeFromFactoryMethods => [];
    protected virtual string[] DomainServiceAllowedFieldTypes => [];

    // --- Entity (7) ---

    /// <summary>
    /// AggregateRoot는 public sealed 클래스여야 합니다.
    /// sealed로 강제하여 상속을 통한 불변식 우회를 방지하고,
    /// public으로 노출하여 Application 레이어에서 접근 가능하게 합니다.
    /// </summary>
    [Fact]
    public void AggregateRoot_ShouldBe_PublicSealedClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed()
                .RequireNotStatic(),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Visibility Rule");
    }

    /// <summary>
    /// AggregateRoot는 Create/CreateFromValidated 정적 팩토리 메서드를 가져야 합니다.
    /// 생성자 직접 호출을 차단하고 유효성 검증을 거친 생성만 허용합니다.
    /// </summary>
    [Fact]
    public void AggregateRoot_ShouldHave_CreateAndCreateFromValidated()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod(IEntity.CreateMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod(IEntity.CreateFromValidatedMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass()),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Factory Method Rule");
    }

    /// <summary>
    /// AggregateRoot는 [GenerateEntityId] 어트리뷰트를 가져야 합니다.
    /// Source Generator가 강타입 ID를 자동 생성하도록 보장합니다.
    /// </summary>
    [Fact]
    public void AggregateRoot_ShouldHave_GenerateEntityIdAttribute()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAttribute("GenerateEntityId"),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot GenerateEntityId Attribute Rule");
    }

    /// <summary>
    /// AggregateRoot의 모든 생성자는 private이어야 합니다.
    /// 팩토리 메서드를 통한 생성만 허용하여 불변식을 보장합니다.
    /// </summary>
    [Fact]
    public void AggregateRoot_ShouldHave_AllPrivateConstructors()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllPrivateConstructors(),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Private Constructors Rule");
    }

    /// <summary>
    /// AggregateRoot가 아닌 Entity도 public sealed 클래스여야 합니다.
    /// sealed로 상속을 통한 불변식 우회를 방지하고,
    /// public으로 Application 레이어에서 접근 가능하게 합니다.
    /// </summary>
    [Fact]
    public void Entity_ShouldBe_PublicSealedClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .And().AreNotAbstract()
            .And().AreNotAssignableTo(typeof(AggregateRoot<>))
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed()
                .RequireNotStatic(),
                verbose: true)
            .ThrowIfAnyFailures("Entity Visibility Rule");
    }

    /// <summary>
    /// Entity는 Create/CreateFromValidated 정적 팩토리 메서드를 가져야 합니다.
    /// 생성자 직접 호출을 차단하고 유효성 검증을 거친 생성만 허용합니다.
    /// </summary>
    [Fact]
    public void Entity_ShouldHave_CreateAndCreateFromValidated()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .And().AreNotAbstract()
            .And().AreNotAssignableTo(typeof(AggregateRoot<>))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod(IEntity.CreateMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod(IEntity.CreateFromValidatedMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass()),
                verbose: true)
            .ThrowIfAnyFailures("Entity Factory Method Rule");
    }

    /// <summary>
    /// Entity의 모든 생성자는 private이어야 합니다.
    /// 팩토리 메서드를 통한 생성만 허용하여 불변식을 보장합니다.
    /// </summary>
    [Fact]
    public void Entity_ShouldHave_AllPrivateConstructors()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .And().AreNotAbstract()
            .And().AreNotAssignableTo(typeof(AggregateRoot<>))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllPrivateConstructors(),
                verbose: true)
            .ThrowIfAnyFailures("Entity Private Constructors Rule");
    }

    // --- ValueObject (4) ---

    /// <summary>
    /// ValueObject는 public sealed 클래스이고 모든 생성자가 private이어야 합니다.
    /// 동등성 의미론을 보호하고 팩토리 메서드를 통한 생성만 허용합니다.
    /// </summary>
    [Fact]
    public void ValueObject_ShouldBe_PublicSealedWithPrivateConstructors()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed()
                .RequireAllPrivateConstructors(),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Visibility Rule");
    }

    /// <summary>
    /// ValueObject는 불변이어야 합니다.
    /// 값의 동등성이 생성 후 변경되지 않도록 보장합니다.
    /// </summary>
    [Fact]
    public void ValueObject_ShouldBe_Immutable()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImmutable(),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Immutability Rule");
    }

    /// <summary>
    /// ValueObject는 Create 정적 팩토리 메서드가 Fin&lt;T&gt;를 반환해야 합니다.
    /// 실패 가능한 생성을 타입으로 표현합니다.
    /// </summary>
    [Fact]
    public void ValueObject_ShouldHave_CreateFactoryMethod()
    {
        var query = ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract();

        foreach (var excludeType in ValueObjectExcludeFromFactoryMethods)
            query = query.And().AreNotAssignableTo(excludeType);

        query
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod(IValueObject.CreateMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Fin<>))),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Create Method Rule");
    }

    /// <summary>
    /// ValueObject는 Validate 정적 메서드가 Validation&lt;Error, T&gt;를 반환해야 합니다.
    /// 여러 유효성 오류를 누적 수집할 수 있게 합니다.
    /// </summary>
    [Fact]
    public void ValueObject_ShouldHave_ValidateMethod()
    {
        var query = ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract();

        foreach (var excludeType in ValueObjectExcludeFromFactoryMethods)
            query = query.And().AreNotAssignableTo(excludeType);

        query
            .ValidateAllClasses(Architecture, @class => @class
                .RequireMethod(IValueObject.ValidateMethodName, m => m
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Validation<,>))),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Validate Method Rule");
    }

    // --- DomainEvent (2) ---

    /// <summary>
    /// DomainEvent는 sealed record여야 합니다.
    /// 값 의미론과 불변성을 record로 보장하고,
    /// sealed로 이벤트 계약 변경을 방지합니다.
    /// </summary>
    [Fact]
    public void DomainEvent_ShouldBe_SealedRecord()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(DomainEvent))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireSealed()
                .RequireRecord(),
                verbose: true)
            .ThrowIfAnyFailures("DomainEvent Sealed Record Rule");
    }

    /// <summary>
    /// DomainEvent 클래스명은 "Event" 접미사를 가져야 합니다.
    /// 유비쿼터스 언어에서 이벤트임을 명확히 식별합니다.
    /// </summary>
    [Fact]
    public void DomainEvent_ShouldHave_EventSuffix()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(DomainEvent))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNameEndsWith("Event"),
                verbose: true)
            .ThrowIfAnyFailures("DomainEvent Naming Rule");
    }

    // --- Specification (3) ---

    /// <summary>
    /// Specification은 public sealed 클래스여야 합니다.
    /// 비즈니스 규칙의 캡슐화를 보장합니다.
    /// </summary>
    [Fact]
    public void Specification_ShouldBe_PublicSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespaceMatching($@"{DomainNamespace}\..*")
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Specification Visibility Rule");
    }

    /// <summary>
    /// Specification은 Specification&lt;T&gt; 베이스 클래스를 상속해야 합니다.
    /// And/Or/Not 합성 연산 지원을 보장합니다.
    /// </summary>
    [Fact]
    public void Specification_ShouldInherit_SpecificationBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespaceMatching($@"{DomainNamespace}\..*")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(Specification<>)),
                verbose: true)
            .ThrowIfAnyFailures("Specification Inheritance Rule");
    }

    /// <summary>
    /// Specification은 도메인 레이어에만 위치해야 합니다.
    /// 비즈니스 규칙이 도메인 외부로 유출되지 않도록 강제합니다.
    /// </summary>
    [Fact]
    public void Specification_ShouldResideIn_DomainLayer()
    {
        ArchRuleDefinition.Classes()
            .That()
            .AreAssignableTo(typeof(Specification<>))
            .And().AreNotAbstract()
            .And().ResideInNamespaceMatching($@"{DomainNamespace}\..*")
            .Should().ResideInNamespaceMatching($@"{DomainNamespace}\..*")
            .Check(Architecture);
    }

    // --- DomainService (5) ---

    /// <summary>
    /// DomainService는 public sealed 클래스여야 합니다.
    /// </summary>
    [Fact]
    public void DomainService_ShouldBe_PublicSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Visibility Rule");
    }

    /// <summary>
    /// DomainService는 인스턴스 필드를 갖지 않아야 합니다(허용 타입 제외).
    /// 상태 없는 순수 도메인 로직만 포함하도록 강제합니다.
    /// </summary>
    [Fact]
    public void DomainService_ShouldBe_Stateless()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoInstanceFields(DomainServiceAllowedFieldTypes),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Stateless Rule");
    }

    /// <summary>
    /// DomainService는 IObservablePort에 의존하면 안 됩니다.
    /// 관측 관심사를 도메인 로직에서 분리합니다.
    /// </summary>
    [Fact]
    public void DomainService_ShouldNotDependOn_IObservablePort()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoDependencyOn("IObservablePort"),
                verbose: true)
            .ThrowIfAnyFailures("DomainService No IObservablePort Dependency Rule");
    }

    /// <summary>
    /// DomainService의 public 인스턴스 메서드는 Fin&lt;T&gt;를 반환해야 합니다.
    /// 실패 가능성을 타입으로 명시합니다.
    /// </summary>
    [Fact]
    public void DomainService_PublicMethods_ShouldReturn_Fin()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireAllMethods(
                    m => m.Visibility == Visibility.Public
                         && m.IsStatic != true
                         && m.MethodForm == MethodForm.Normal,
                    method => method.RequireReturnTypeContaining("Fin")),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Public Methods Return Fin Rule");
    }

    /// <summary>
    /// DomainService는 record가 아니어야 합니다.
    /// 값 의미론이 아닌 행위 중심 객체임을 명확히 구분합니다.
    /// </summary>
    [Fact]
    public void DomainService_ShouldNotBe_Record()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ImplementInterface(typeof(IDomainService))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNotRecord(),
                verbose: true)
            .ThrowIfAnyFailures("DomainService Not Record Rule");
    }
}
