using Functorium.SourceGenerators.Generators.DomainEventCtxEnricherGenerator;
using Functorium.Testing.Actions.SourceGenerators;

using Microsoft.CodeAnalysis;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

// # DomainEventCtxEnricherGenerator 소스 생성기 테스트
//
// IDomainEventHandler<T> 구현 클래스를 감지하여 T(이벤트 타입)에 대한
// IDomainEventCtxEnricher 구현체가 올바르게 생성되는지 검증합니다.
//
// ## 테스트 시나리오
//
// ### 1. 최상위 이벤트 스칼라 속성 PushProperty 테스트
// ### 2. 중첩 이벤트 스칼라 속성 PushProperty 테스트
// ### 3. 컬렉션 Count 테스트
// ### 4. [CtxIgnore] 속성 제외 테스트
// ### 5. 값 객체 .ToString() 변환 테스트
// ### 6. partial void OnEnrich 확장 포인트 테스트
// ### 7. 클래스 이름 (중첩) 테스트
// ### 8. 클래스 이름 (최상위) 테스트
// ### 9. IDomainEventCtxEnricher<T> 인터페이스 구현 테스트
// ### 10. GeneratedCompositeDisposable 테스트
// ### 11. PushEventCtx 헬퍼 메서드 테스트
// ### 12. IDomainEvent 기본 속성 제외 테스트
// ### 13. [CtxIgnore] 클래스 레벨 옵트아웃 테스트
// ### 14. FUNCTORIUM004 접근 불가능 타입 진단 경고 테스트
// ### 15. FUNCTORIUM002 ctx 필드 충돌 테스트
// ### 16. [CtxRoot] 인터페이스 root context 테스트
// ### 17. [CtxRoot] 속성 직접 적용 테스트
// ### 18. PushRootCtx 조건부 생성 테스트
// ### 19. 스냅샷 — 최상위 이벤트
// ### 20. 스냅샷 — 중첩 이벤트
// ### 21. 스냅샷 — Root 속성 포함
// ### 22. 네임스페이스 정확성 테스트
// ### 23. 같은 이벤트에 여러 Handler — Enricher 1개만 생성
// ### 24. Handler 없는 이벤트 — Enricher 미생성
// ### 25. Handler의 네임스페이스 사용
//

[Trait(nameof(UnitTest), UnitTest.Functorium_SourceGenerator)]
public sealed class DomainEventCtxEnricherGeneratorTests
{
    private readonly DomainEventCtxEnricherGenerator _sut;

    public DomainEventCtxEnricherGeneratorTests()
    {
        _sut = new DomainEventCtxEnricherGenerator();
    }

    private const string TopLevelEventInput = """
        using System.Threading;
        using System.Threading.Tasks;
        using Functorium.Applications.Events;
        using Functorium.Domains.Events;

        namespace TestNamespace;

        public sealed record OrderPlacedEvent(
            string CustomerId,
            string OrderId,
            int LineCount,
            decimal TotalAmount) : DomainEvent;

        public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
        {
            public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                => ValueTask.CompletedTask;
        }
        """;

    private const string NestedEventInput = """
        using System.Threading;
        using System.Threading.Tasks;
        using Functorium.Applications.Events;
        using Functorium.Domains.Events;

        namespace TestNamespace;

        public static class Order
        {
            public sealed record CreatedEvent(
                string CustomerId,
                string OrderId,
                int LineCount) : DomainEvent;
        }

        public sealed class OrderCreatedEventHandler : IDomainEventHandler<Order.CreatedEvent>
        {
            public ValueTask Handle(Order.CreatedEvent notification, CancellationToken ct)
                => ValueTask.CompletedTask;
        }
        """;

    #region 1. 최상위 이벤트 스칼라 속성 PushProperty 테스트

    /// <summary>
    /// 시나리오: 최상위 이벤트의 스칼라 속성이 ctx.{event_snake}.{field} 로 PushProperty되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_TopLevelEventScalarProperty()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("Push(\"ctx.order_placed_event.customer_id\", domainEvent.CustomerId)");
        actual.ShouldContain("Push(\"ctx.order_placed_event.order_id\", domainEvent.OrderId)");
        actual.ShouldContain("Push(\"ctx.order_placed_event.line_count\", domainEvent.LineCount)");
        actual.ShouldContain("Push(\"ctx.order_placed_event.total_amount\", domainEvent.TotalAmount)");
    }

    #endregion

    #region 2. 중첩 이벤트 스칼라 속성 PushProperty 테스트

    /// <summary>
    /// 시나리오: 중첩 이벤트의 스칼라 속성이 ctx.{containing}.{event_snake}.{field} 로 PushProperty되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_NestedEventScalarProperty()
    {
        // Act
        string? actual = _sut.Generate(NestedEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("Push(\"ctx.order.created_event.customer_id\", domainEvent.CustomerId)");
        actual.ShouldContain("Push(\"ctx.order.created_event.order_id\", domainEvent.OrderId)");
        actual.ShouldContain("Push(\"ctx.order.created_event.line_count\", domainEvent.LineCount)");
    }

    #endregion

    #region 3. 컬렉션 Count 테스트

    /// <summary>
    /// 시나리오: 컬렉션 속성에 대해 _count 접미사와 .Count 표현식이 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_CollectionCountProperty()
    {
        // Arrange
        string input = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public sealed record OrderPlacedEvent(
                string OrderId,
                List<string> OrderLines) : DomainEvent;

            public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
            {
                public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("Push(\"ctx.order_placed_event.order_lines_count\", domainEvent.OrderLines?.Count ?? 0)");
    }

    #endregion

    #region 4. [CtxIgnore] 속성 제외 테스트

    /// <summary>
    /// 시나리오: [CtxIgnore] 속성이 붙은 프로퍼티는 생성에서 제외되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldExclude_IgnoredProperties()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Applications.Observabilities;
            using Functorium.Applications.Usecases;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public sealed record OrderPlacedEvent(
                string OrderId,
                [CtxIgnore] string InternalToken,
                int Amount) : DomainEvent;

            public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
            {
                public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("ctx.order_placed_event.order_id");
        actual.ShouldContain("ctx.order_placed_event.amount");
        actual.ShouldNotContain("internal_token");
        actual.ShouldNotContain("InternalToken");
    }

    #endregion

    #region 5. 값 객체 .ToString() 변환 테스트

    /// <summary>
    /// 시나리오: IValueObject 구현 값 객체 속성에 .ToString() 호출이 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_ToStringForValueObjects()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Domains.Events;
            using Functorium.Domains.ValueObjects;

            namespace TestNamespace;

            public sealed record CustomerName : IValueObject
            {
                public string Value { get; }
                public CustomerName(string value) => Value = value;
                public override string ToString() => Value;
            }

            public sealed record CustomerCreatedEvent(
                CustomerName CustomerName,
                string Email) : DomainEvent;

            public sealed class CustomerCreatedEventHandler : IDomainEventHandler<CustomerCreatedEvent>
            {
                public ValueTask Handle(CustomerCreatedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("domainEvent.CustomerName.ToString()");
        actual.ShouldContain("domainEvent.Email)");
        actual.ShouldNotContain("domainEvent.Email.ToString()");
    }

    /// <summary>
    /// 시나리오: IEntityId&lt;T&gt; 구현 EntityId 속성에 .ToString() 호출이 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_ToStringForEntityId()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Domains.Entities;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public readonly partial record struct CustomerId(Ulid Value) : IEntityId<CustomerId>
            {
                public static CustomerId New() => new(Ulid.NewUlid());
                public static CustomerId Create(Ulid id) => new(id);
                public static CustomerId Create(string id) => new(Ulid.Parse(id));
                public static CustomerId Parse(string s, IFormatProvider? provider) => Create(s);
                public static bool TryParse(string? s, IFormatProvider? provider, out CustomerId result)
                {
                    if (Ulid.TryParse(s, out var ulid)) { result = new(ulid); return true; }
                    result = default; return false;
                }
                public int CompareTo(CustomerId other) => Value.CompareTo(other.Value);
            }

            public sealed record CustomerCreatedEvent(
                CustomerId CustomerId,
                string CustomerName) : DomainEvent;

            public sealed class CustomerCreatedEventHandler : IDomainEventHandler<CustomerCreatedEvent>
            {
                public ValueTask Handle(CustomerCreatedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("domainEvent.CustomerId.ToString()");
        actual.ShouldContain("domainEvent.CustomerName)");
        actual.ShouldNotContain("domainEvent.CustomerName.ToString()");
    }

    /// <summary>
    /// 시나리오: IValueObject/IEntityId를 구현하지 않는 복합 타입은 건너뛰는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldSkip_NonValueObjectComplexType()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public sealed record Address(string Street, string City);

            public sealed record CustomerCreatedEvent(
                Address ShippingAddress,
                string CustomerName) : DomainEvent;

            public sealed class CustomerCreatedEventHandler : IDomainEventHandler<CustomerCreatedEvent>
            {
                public ValueTask Handle(CustomerCreatedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("customer_name");
        actual.ShouldNotContain("shipping_address");
        actual.ShouldNotContain("ShippingAddress");
    }

    #endregion

    #region 6. partial void OnEnrich 확장 포인트 테스트

    /// <summary>
    /// 시나리오: partial void OnEnrich 메서드가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_PartialVoidExtensionPoint()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("partial void OnEnrich(");
    }

    #endregion

    #region 7. 클래스 이름 (중첩) 테스트

    /// <summary>
    /// 시나리오: 중첩 이벤트의 Enricher 클래스 이름이 {ContainingTypes}{EventTypeName}CtxEnricher 형식인지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_NestedClassName()
    {
        // Act
        string? actual = _sut.Generate(NestedEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public partial class OrderCreatedEventCtxEnricher");
    }

    #endregion

    #region 8. 클래스 이름 (최상위) 테스트

    /// <summary>
    /// 시나리오: 최상위 이벤트의 Enricher 클래스 이름이 {EventTypeName}CtxEnricher 형식인지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_TopLevelClassName()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public partial class OrderPlacedEventCtxEnricher");
    }

    #endregion

    #region 9. IDomainEventCtxEnricher<T> 인터페이스 구현 테스트

    /// <summary>
    /// 시나리오: 생성된 클래스가 IDomainEventCtxEnricher&lt;TEvent&gt;를 구현하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldImplement_IDomainEventCtxEnricherInterface()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("IDomainEventCtxEnricher<");
        actual.ShouldContain("global::TestNamespace.OrderPlacedEvent");
    }

    #endregion

    #region 10. GeneratedCompositeDisposable 테스트

    /// <summary>
    /// 시나리오: GeneratedCompositeDisposable 내부 클래스가 LIFO 역순 Dispose로 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_CompositeDisposable()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("GeneratedCompositeDisposable");
        actual.ShouldContain("public void Dispose()");
        actual.ShouldContain("for (int i = disposables.Count - 1; i >= 0; i--)");
    }

    #endregion

    #region 11. PushEventCtx 헬퍼 메서드 테스트

    /// <summary>
    /// 시나리오: PushEventCtx 헬퍼 메서드가 올바른 ctx prefix로 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_PushEventCtxHelper_TopLevel()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("private static void PushEventCtx(");
        actual.ShouldContain("\"ctx.order_placed_event.\" + fieldName, value, pillars)");
    }

    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_PushEventCtxHelper_Nested()
    {
        // Act
        string? actual = _sut.Generate(NestedEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("private static void PushEventCtx(");
        actual.ShouldContain("\"ctx.order.created_event.\" + fieldName, value, pillars)");
    }

    #endregion

    #region 12. IDomainEvent 기본 속성 제외 테스트

    /// <summary>
    /// 시나리오: IDomainEvent의 기본 속성(OccurredAt, EventId, CorrelationId, CausationId)이 생성에서 제외되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldExclude_DomainEventBaseProperties()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldNotContain("occurred_at");
        actual.ShouldNotContain("OccurredAt");
        actual.ShouldNotContain("event_id");
        actual.ShouldNotContain("EventId");
        actual.ShouldNotContain("correlation_id");
        actual.ShouldNotContain("CorrelationId");
        actual.ShouldNotContain("causation_id");
        actual.ShouldNotContain("CausationId");
    }

    #endregion

    #region 13. [CtxIgnore] 클래스 레벨 옵트아웃 테스트

    /// <summary>
    /// 시나리오: [CtxIgnore]가 이벤트 record에 적용되면 생성하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldNotGenerate_WhenClassLevelIgnoreApplied()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Applications.Observabilities;
            using Functorium.Applications.Usecases;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            [CtxIgnore]
            public sealed record InternalEvent(string Secret) : DomainEvent;

            public sealed class InternalEventHandler : IDomainEventHandler<InternalEvent>
            {
                public ValueTask Handle(InternalEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldBeNull();
    }

    #endregion

    #region 14. FUNCTORIUM004 접근 불가능 타입 진단 경고 테스트

    /// <summary>
    /// 시나리오: private 이벤트 record가 IDomainEvent를 구현하면
    /// FUNCTORIUM004 경고가 발생하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldReportDiagnostic_WhenEventTypeIsInaccessible()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public sealed class OuterClass
            {
                private sealed record InternalEvent(string Value) : DomainEvent;

                public sealed class InternalEventHandler : IDomainEventHandler<InternalEvent>
                {
                    public ValueTask Handle(InternalEvent notification, CancellationToken ct)
                        => ValueTask.CompletedTask;
                }
            }
            """;

        // Act
        var (_, diagnostics) = _sut.GenerateWithDiagnostics(input);

        // Assert
        diagnostics.ShouldContain(d => d.Id == "FUNCTORIUM004");
    }

    /// <summary>
    /// 시나리오: [CtxIgnore]가 적용된 private 이벤트에 대해서는
    /// FUNCTORIUM004 경고가 발생하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldNotReportDiagnostic_WhenInaccessibleButIgnored()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Applications.Observabilities;
            using Functorium.Applications.Usecases;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public sealed class OuterClass
            {
                [CtxIgnore]
                private sealed record InternalEvent(string Value) : DomainEvent;

                public sealed class InternalEventHandler : IDomainEventHandler<InternalEvent>
                {
                    public ValueTask Handle(InternalEvent notification, CancellationToken ct)
                        => ValueTask.CompletedTask;
                }
            }
            """;

        // Act
        var (_, diagnostics) = _sut.GenerateWithDiagnostics(input);

        // Assert
        diagnostics.ShouldNotContain(d => d.Id == "FUNCTORIUM004");
    }

    #endregion

    #region 15. FUNCTORIUM002 ctx 필드 충돌 테스트

    /// <summary>
    /// 시나리오: 같은 root 필드명에 서로 다른 OpenSearch 매핑 타입 그룹이 할당되면
    /// FUNCTORIUM002 Warning이 발생하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldReportDiagnostic_WhenRootFieldTypeConflicts()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Applications.Observabilities;
            using Functorium.Applications.Usecases;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            [CtxRoot]
            public interface IOrderEvent { string OrderId { get; } }

            [CtxRoot]
            public interface INumericOrderEvent { int OrderId { get; } }

            public sealed record OrderPlacedEvent(string OrderId)
                : DomainEvent, IOrderEvent;

            public sealed record OrderCancelledEvent(int OrderId)
                : DomainEvent, INumericOrderEvent;

            public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
            {
                public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }

            public sealed class OrderCancelledEventHandler : IDomainEventHandler<OrderCancelledEvent>
            {
                public ValueTask Handle(OrderCancelledEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        var (_, diagnostics) = _sut.GenerateWithDiagnostics(input);

        // Assert
        diagnostics.ShouldContain(d => d.Id == "FUNCTORIUM002");
    }

    #endregion

    #region 16. [CtxRoot] 인터페이스 root context 테스트

    private const string RootInterfaceInput = """
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Functorium.Applications.Events;
        using Functorium.Applications.Observabilities;
        using Functorium.Domains.Events;

        namespace TestNamespace;

        [CtxRoot]
        public interface IOrderEvent { string OrderId { get; } }

        public sealed record OrderPlacedEvent(
            string OrderId,
            string CustomerId,
            List<string> OrderLines) : DomainEvent, IOrderEvent;

        public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
        {
            public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                => ValueTask.CompletedTask;
        }
        """;

    /// <summary>
    /// 시나리오: [CtxRoot] 인터페이스의 속성이 ctx.{field} 루트 레벨로 승격되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_RootCtxField_WhenInterfaceHasRootAttribute()
    {
        // Act
        string? actual = _sut.Generate(RootInterfaceInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("Push(\"ctx.order_id\", domainEvent.OrderId)");
    }

    /// <summary>
    /// 시나리오: [CtxRoot] 인터페이스 속성이 root로 승격되어도
    /// root가 아닌 속성은 기존 ctx.{event}.{field} 형식을 유지하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldKeep_NormalCtxField_WhenNotRoot()
    {
        // Act
        string? actual = _sut.Generate(RootInterfaceInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("Push(\"ctx.order_placed_event.customer_id\"");
    }

    #endregion

    #region 16a. 인터페이스 스코프 — 비-root 인터페이스 + 상속 체인

    private const string InterfaceScopedEventInput = """
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using Functorium.Applications.Events;
        using Functorium.Applications.Observabilities;
        using Functorium.Domains.Events;

        namespace TestNamespace;

        public interface IAuditable { string OperatorId { get; } }
        public interface IRegional { string RegionCode { get; } }
        public interface IPartnerContext : IRegional { string PartnerId { get; } }

        [CtxRoot]
        public interface IOrderEvent { string OrderId { get; } }

        public sealed record OrderPlacedEvent(
            string OrderId,
            string OperatorId,
            string RegionCode,
            string PartnerId,
            List<string> OrderLines) : DomainEvent, IOrderEvent, IAuditable, IPartnerContext;

        public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
        {
            public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                => ValueTask.CompletedTask;
        }
        """;

    /// <summary>
    /// 시나리오: [CtxRoot] 인터페이스의 속성이 ctx.{field} 루트로 승격되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_InterfaceScope_ShouldPromote_RootInterfaceProperty()
    {
        string? actual = _sut.Generate(InterfaceScopedEventInput);

        actual.ShouldNotBeNull();
        actual.ShouldContain("\"ctx.order_id\"");
    }

    /// <summary>
    /// 시나리오: 비-root 인터페이스 IAuditable의 속성이 ctx.{interface}.{field} 형식인지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_InterfaceScope_ShouldUse_InterfacePrefix_ForAuditable()
    {
        string? actual = _sut.Generate(InterfaceScopedEventInput);

        actual.ShouldNotBeNull();
        actual.ShouldContain("\"ctx.auditable.operator_id\"");
    }

    /// <summary>
    /// 시나리오: IPartnerContext : IRegional 상속 체인에서 RegionCode가
    /// IRegional(선언 인터페이스)의 스코프로 출력되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_InterfaceScope_ShouldUse_DeclaringInterface_ForInheritedProperty()
    {
        string? actual = _sut.Generate(InterfaceScopedEventInput);

        actual.ShouldNotBeNull();
        actual.ShouldContain("\"ctx.regional.region_code\"");
    }

    /// <summary>
    /// 시나리오: IPartnerContext에 직접 선언된 PartnerId가 ctx.partner_context.partner_id 형식인지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_InterfaceScope_ShouldUse_InterfacePrefix_ForPartnerContext()
    {
        string? actual = _sut.Generate(InterfaceScopedEventInput);

        actual.ShouldNotBeNull();
        actual.ShouldContain("\"ctx.partner_context.partner_id\"");
    }

    /// <summary>
    /// 시나리오: 인터페이스 없는 컬렉션 프로퍼티가 기존 ctx.{event}.{field}_count 형식을 유지하는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_InterfaceScope_ShouldKeep_DirectCollectionInEventScope()
    {
        string? actual = _sut.Generate(InterfaceScopedEventInput);

        actual.ShouldNotBeNull();
        actual.ShouldContain("\"ctx.order_placed_event.order_lines_count\"");
    }

    /// <summary>
    /// 시나리오: 인터페이스 스코프가 포함된 생성 코드의 전체 형태를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public Task DomainEventCtxEnricherGenerator_InterfaceScope_ShouldGenerate_ExpectedOutput()
    {
        string? actual = _sut.Generate(InterfaceScopedEventInput);

        return Verify(actual).UseDirectory("Snapshots/DomainEventCtxEnricherGenerator");
    }

    #endregion

    #region 17. [CtxRoot] 속성 직접 적용 테스트

    /// <summary>
    /// 시나리오: record 생성자 파라미터에 [CtxRoot]를 직접 적용하면
    /// 해당 속성만 ctx.{field} 루트 레벨로 승격되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_RootCtxField_WhenPropertyHasRootAttribute()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Applications.Observabilities;
            using Functorium.Applications.Usecases;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public sealed record OrderPlacedEvent(
                [CtxRoot] string OrderId,
                string CustomerId) : DomainEvent;

            public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
            {
                public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("Push(\"ctx.order_id\", domainEvent.OrderId)");
        actual.ShouldContain("Push(\"ctx.order_placed_event.customer_id\", domainEvent.CustomerId)");
    }

    #endregion

    #region 18. PushRootCtx 조건부 생성 테스트

    /// <summary>
    /// 시나리오: root 속성이 있을 때 PushRootCtx 헬퍼 메서드가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_PushRootCtxHelper_WhenRootPropertyExists()
    {
        // Act
        string? actual = _sut.Generate(RootInterfaceInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("private static void PushRootCtx(");
        actual.ShouldContain("\"ctx.\" + fieldName, value, pillars)");
    }

    /// <summary>
    /// 시나리오: root 속성이 없을 때 PushRootCtx 헬퍼 메서드가 생성되지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldNotGenerate_PushRootCtxHelper_WhenNoRootProperty()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldNotContain("PushRootCtx");
    }

    #endregion

    #region 19. 스냅샷 — 최상위 이벤트

    /// <summary>
    /// 시나리오: 최상위 이벤트의 생성 코드 전체 형태를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public Task DomainEventCtxEnricherGenerator_ShouldGenerate_TopLevelEvent_ExpectedOutput()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        return Verify(actual).UseDirectory("Snapshots/DomainEventCtxEnricherGenerator");
    }

    #endregion

    #region 20. 스냅샷 — 중첩 이벤트

    /// <summary>
    /// 시나리오: 중첩 이벤트의 생성 코드 전체 형태를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public Task DomainEventCtxEnricherGenerator_ShouldGenerate_NestedEvent_ExpectedOutput()
    {
        // Act
        string? actual = _sut.Generate(NestedEventInput);

        // Assert
        return Verify(actual).UseDirectory("Snapshots/DomainEventCtxEnricherGenerator");
    }

    #endregion

    #region 21. 스냅샷 — Root 속성 포함

    /// <summary>
    /// 시나리오: root context가 포함된 생성 코드의 전체 형태를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public Task DomainEventCtxEnricherGenerator_ShouldGenerate_RootContext_ExpectedOutput()
    {
        // Act
        string? actual = _sut.Generate(RootInterfaceInput);

        // Assert
        return Verify(actual).UseDirectory("Snapshots/DomainEventCtxEnricherGenerator");
    }

    #endregion

    #region 22. 네임스페이스 정확성 테스트

    /// <summary>
    /// 시나리오: 올바른 네임스페이스가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_CorrectNamespace()
    {
        // Act
        string? actual = _sut.Generate(TopLevelEventInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("namespace TestNamespace;");
    }

    #endregion

    #region 23. 같은 이벤트에 여러 Handler — Enricher 1개만 생성

    /// <summary>
    /// 시나리오: 같은 이벤트에 대해 여러 Handler가 존재해도 Enricher는 1개만 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldGenerate_SingleEnricher_WhenMultipleHandlersForSameEvent()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public sealed record OrderPlacedEvent(
                string OrderId,
                string CustomerId) : DomainEvent;

            public sealed class OrderNotifier : IDomainEventHandler<OrderPlacedEvent>
            {
                public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }

            public sealed class InventoryDeductor : IDomainEventHandler<OrderPlacedEvent>
            {
                public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct)
                    => ValueTask.CompletedTask;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();

        // Enricher 클래스는 정확히 1번만 선언되어야 함
        int classCount = actual.Split("public partial class OrderPlacedEventCtxEnricher").Length - 1;
        classCount.ShouldBe(1);
    }

    #endregion

    #region 24. Handler 없는 이벤트 — Enricher 미생성

    /// <summary>
    /// 시나리오: Handler가 없는 이벤트에 대해서는 Enricher가 생성되지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldNotGenerate_WhenNoHandler()
    {
        // Arrange
        string input = """
            using Functorium.Domains.Events;

            namespace TestNamespace;

            public sealed record OrderPlacedEvent(
                string OrderId,
                string CustomerId) : DomainEvent;
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldBeNull();
    }

    #endregion

    #region 25. Handler의 네임스페이스 사용

    /// <summary>
    /// 시나리오: 생성된 Enricher의 네임스페이스가 이벤트가 아닌 Handler의 네임스페이스인지 확인합니다.
    /// </summary>
    [Fact]
    public void DomainEventCtxEnricherGenerator_ShouldUse_HandlerNamespace()
    {
        // Arrange
        string input = """
            using System.Threading;
            using System.Threading.Tasks;
            using Functorium.Applications.Events;
            using Functorium.Domains.Events;

            namespace DomainNamespace
            {
                public sealed record OrderPlacedEvent(
                    string OrderId) : DomainEvent;
            }

            namespace AppNamespace
            {
                public sealed class OrderPlacedEventHandler
                    : IDomainEventHandler<DomainNamespace.OrderPlacedEvent>
                {
                    public ValueTask Handle(DomainNamespace.OrderPlacedEvent notification, CancellationToken ct)
                        => ValueTask.CompletedTask;
                }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("namespace AppNamespace;");
        actual.ShouldNotContain("namespace DomainNamespace;");
    }

    #endregion
}
