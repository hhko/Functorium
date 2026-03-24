using Functorium.Adapters.Observabilities.Builders.Configurators;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Events;
using Functorium.Applications.Persistence;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Configurators;

/// <summary>
/// PipelineConfigurator 테스트
/// Fluent API를 통한 파이프라인 설정 테스트
/// </summary>
public sealed class PipelineConfiguratorTests
{
    [Fact]
    public void UseAll_RegistersAllPipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterTransactionDependencies(services);
        var configurator = CreateConfigurator();

        // Act
        configurator.UseAll();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(7); // CtxEnricher, Metrics, Tracing, Logging, Validation, Exception, Transaction
    }

    [Fact]
    public void UseMetrics_RegistersOnlyMetricsPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseMetrics();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(1);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(UsecaseMetricsPipeline<,>));
    }

    [Fact]
    public void UseTracing_RegistersOnlyTracingPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseTracing();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(1);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(UsecaseTracingPipeline<,>));
    }

    [Fact]
    public void UseLogging_RegistersOnlyLoggingPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseLogging();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(1);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(UsecaseLoggingPipeline<,>));
    }

    [Fact]
    public void UseValidation_RegistersOnlyValidationPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseValidation();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(1);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(UsecaseValidationPipeline<,>));
    }

    [Fact]
    public void UseException_RegistersOnlyExceptionPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseException();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(1);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(UsecaseExceptionPipeline<,>));
    }

    [Fact]
    public void FluentChaining_RegistersMultiplePipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator
            .UseMetrics()
            .UseTracing()
            .UseLogging();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(3);
    }

    [Fact]
    public void WithLifetime_Singleton_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator
            .UseMetrics()
            .WithLifetime(ServiceLifetime.Singleton);
        configurator.Apply(services);

        // Assert
        var pipelineBehavior = services.First(s => s.ServiceType == typeof(IPipelineBehavior<,>));
        pipelineBehavior.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void WithLifetime_Scoped_RegistersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator
            .UseMetrics()
            .WithLifetime(ServiceLifetime.Scoped);
        configurator.Apply(services);

        // Assert
        var pipelineBehavior = services.First(s => s.ServiceType == typeof(IPipelineBehavior<,>));
        pipelineBehavior.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void WithLifetime_Transient_RegistersAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator
            .UseMetrics()
            .WithLifetime(ServiceLifetime.Transient);
        configurator.Apply(services);

        // Assert
        var pipelineBehavior = services.First(s => s.ServiceType == typeof(IPipelineBehavior<,>));
        pipelineBehavior.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void DefaultLifetime_IsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseMetrics();
        configurator.Apply(services);

        // Assert
        var pipelineBehavior = services.First(s => s.ServiceType == typeof(IPipelineBehavior<,>));
        pipelineBehavior.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddCustomPipeline_RegistersCustomPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.AddCustomPipeline<CustomTestPipeline<TestMessage, TestResponse>>();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(1);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(CustomTestPipeline<TestMessage, TestResponse>));
    }

    [Fact]
    public void PipelineOrder_MetricsTracingLoggingValidationExceptionTransaction()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterTransactionDependencies(services);
        var configurator = CreateConfigurator();

        // Act
        configurator.UseAll();
        configurator.Apply(services);

        // Assert - 등록 순서 확인: CtxEnricher → Metrics → Tracing → Logging → Validation → Exception → Transaction
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(CtxEnricherPipeline<,>));
        pipelineBehaviors[1].ImplementationType.ShouldBe(typeof(UsecaseMetricsPipeline<,>));
        pipelineBehaviors[2].ImplementationType.ShouldBe(typeof(UsecaseTracingPipeline<,>));
        pipelineBehaviors[3].ImplementationType.ShouldBe(typeof(UsecaseLoggingPipeline<,>));
        pipelineBehaviors[4].ImplementationType.ShouldBe(typeof(UsecaseValidationPipeline<,>));
        pipelineBehaviors[5].ImplementationType.ShouldBe(typeof(UsecaseExceptionPipeline<,>));
        pipelineBehaviors[6].ImplementationType.ShouldBe(typeof(UsecaseTransactionPipeline<,>));
    }

    [Fact]
    public void CustomPipeline_RegisteredAfterBuiltInPipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator
            .UseException()
            .AddCustomPipeline<CustomTestPipeline<TestMessage, TestResponse>>();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(2);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(UsecaseExceptionPipeline<,>));
        pipelineBehaviors[1].ImplementationType.ShouldBe(typeof(CustomTestPipeline<TestMessage, TestResponse>));
    }

    [Fact]
    public void NoPipelinesConfigured_NoPipelinesRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(0);
    }

    [Fact]
    public void UseTransaction_RegistersTransactionPipeline_WhenDependenciesExist()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterTransactionDependencies(services);
        var configurator = CreateConfigurator();

        // Act
        configurator.UseTransaction();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(1);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(UsecaseTransactionPipeline<,>));
    }

    [Fact]
    public void UseAll_IncludesTransaction_WhenDependenciesExist()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterTransactionDependencies(services);
        var configurator = CreateConfigurator();

        // Act
        configurator.UseAll();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.ShouldContain(s => s.ImplementationType == typeof(UsecaseTransactionPipeline<,>));
    }

    [Fact]
    public void UseAll_SkipsTransaction_WhenDependenciesMissing()
    {
        // Arrange — Transaction 의존성(IUnitOfWork 등) 미등록
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseAll();
        configurator.Apply(services);

        // Assert — Transaction 제외한 6개만 등록
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(6);
        pipelineBehaviors.ShouldNotContain(s => s.ImplementationType == typeof(UsecaseTransactionPipeline<,>));
    }

    [Fact]
    public void UseTransaction_SkipsRegistration_WhenDependenciesMissing()
    {
        // Arrange — Transaction 의존성 미등록
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseTransaction();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(0);
    }

    [Fact]
    public void MultipleCustomPipelines_RegisteredInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator
            .AddCustomPipeline<CustomTestPipeline<TestMessage, TestResponse>>()
            .AddCustomPipeline<SecondCustomPipeline<TestMessage, TestResponse>>();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(2);
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(CustomTestPipeline<TestMessage, TestResponse>));
        pipelineBehaviors[1].ImplementationType.ShouldBe(typeof(SecondCustomPipeline<TestMessage, TestResponse>));
    }

    [Fact]
    public void GetRegisteredPipelineNames_ReturnsAllNames_WhenUseAll()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterTransactionDependencies(services);
        var configurator = CreateConfigurator();

        // Act
        configurator.UseAll();
        configurator.Apply(services);
        var actual = GetRegisteredPipelineNames(configurator);

        // Assert
        actual.ShouldBe(new[] { "CtxEnricher", "Metrics", "Tracing", "Logging", "Validation", "Exception", "Transaction", "Handler" });
    }

    [Fact]
    public void GetRegisteredPipelineNames_ExcludesTransaction_WhenDepsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseAll();
        configurator.Apply(services);
        var actual = GetRegisteredPipelineNames(configurator);

        // Assert
        actual.ShouldBe(new[] { "CtxEnricher", "Metrics", "Tracing", "Logging", "Validation", "Exception", "Handler" });
        actual.ShouldNotContain("Transaction");
    }

    [Fact]
    public void GetRegisteredPipelineNames_IncludesCustomPipelineName()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator
            .UseException()
            .AddCustomPipeline<CustomTestPipeline<TestMessage, TestResponse>>();
        configurator.Apply(services);
        var actual = GetRegisteredPipelineNames(configurator);

        // Assert
        actual.ShouldBe(new[] { "Exception", "CustomTest", "Handler" });
    }

    [Fact]
    public void GetRegisteredPipelineNames_ReturnsOnlyHandler_WhenNoPipelinesConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.Apply(services);
        var actual = GetRegisteredPipelineNames(configurator);

        // Assert
        actual.ShouldBe(new[] { "Handler" });
    }

    /// <summary>
    /// PipelineConfigurator를 생성합니다.
    /// 생성자가 internal이므로 리플렉션을 사용합니다.
    /// </summary>
    private static PipelineConfigurator CreateConfigurator()
    {
        return (PipelineConfigurator)Activator.CreateInstance(
            typeof(PipelineConfigurator),
            nonPublic: true)!;
    }

    /// <summary>
    /// GetRegisteredPipelineNames를 호출합니다.
    /// internal 메서드이므로 리플렉션을 사용합니다.
    /// </summary>
    private static IReadOnlyList<string> GetRegisteredPipelineNames(PipelineConfigurator configurator)
    {
        return (IReadOnlyList<string>)typeof(PipelineConfigurator)
            .GetMethod("GetRegisteredPipelineNames", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(configurator, null)!;
    }

    /// <summary>
    /// Transaction 파이프라인에 필요한 의존성을 ServiceCollection에 등록합니다.
    /// </summary>
    private static void RegisterTransactionDependencies(IServiceCollection services)
    {
        services.AddScoped(_ => Substitute.For<IUnitOfWork>());
        services.AddScoped(_ => Substitute.For<IDomainEventPublisher>());
        services.AddScoped(_ => Substitute.For<IDomainEventCollector>());
    }

    [Fact]
    public void AddCustomPipelinesFromAssembly_RegistersCustomPipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.AddCustomPipelinesFromAssembly(typeof(PipelineConfiguratorTests).Assembly);
        configurator.Apply(services);

        // Assert — 현재 어셈블리에 ICustomUsecasePipeline 구현체가 있으면 등록됨
        var customServices = services
            .Where(s => s.ServiceType.IsGenericType
                && s.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)
                && s.ImplementationType != null
                && typeof(ICustomUsecasePipeline).IsAssignableFrom(s.ImplementationType))
            .ToList();
        customServices.Count.ShouldBe(1); // MarkerCustomPipeline
    }

    [Fact]
    public void AddCustomPipelinesFromAssembly_IgnoresBuiltInPipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act — Functorium.Adapters 어셈블리 스캔
        configurator.AddCustomPipelinesFromAssembly(typeof(UsecaseMetricsPipeline<,>).Assembly);
        configurator.Apply(services);

        // Assert — 내장 파이프라인은 ICustomUsecasePipeline 미구현이므로 IPipelineBehavior로 등록되지 않음
        var pipelineBehaviors = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)
                && s.ImplementationType != null
                && (s.ImplementationType == typeof(UsecaseMetricsPipeline<,>)
                    || s.ImplementationType == typeof(UsecaseTracingPipeline<,>)
                    || s.ImplementationType == typeof(UsecaseLoggingPipeline<,>)
                    || s.ImplementationType == typeof(UsecaseValidationPipeline<,>)
                    || s.ImplementationType == typeof(UsecaseExceptionPipeline<,>)
                    || s.ImplementationType == typeof(UsecaseTransactionPipeline<,>)))
            .ToList();
        pipelineBehaviors.Count.ShouldBe(0);
    }

    #region Test Fixtures

    public sealed record class TestMessage(string Name) : IMessage;
    public sealed record class TestResponse(Guid Id);

    public sealed class CustomTestPipeline<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
        where TMessage : IMessage
    {
        public ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
        {
            return next(message, cancellationToken);
        }
    }

    public sealed class SecondCustomPipeline<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
        where TMessage : IMessage
    {
        public ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
        {
            return next(message, cancellationToken);
        }
    }

    /// <summary>
    /// ICustomUsecasePipeline 마커를 구현한 테스트용 커스텀 파이프라인.
    /// AddCustomPipelinesFromAssembly 테스트에서 Scrutor 자동 검색 대상으로 사용됩니다.
    /// </summary>
    public sealed class MarkerCustomPipeline : ICustomUsecasePipeline, IPipelineBehavior<TestMessage, TestResponse>
    {
        public ValueTask<TestResponse> Handle(TestMessage message, MessageHandlerDelegate<TestMessage, TestResponse> next, CancellationToken cancellationToken)
        {
            return next(message, cancellationToken);
        }
    }

    #endregion
}
