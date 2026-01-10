using Functorium.Adapters.Observabilities.Builders.Configurators;
using Functorium.Adapters.Observabilities.Pipelines;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

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
        var configurator = CreateConfigurator();

        // Act
        configurator.UseAll();
        configurator.Apply(services);

        // Assert
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors.Count.ShouldBe(5); // Metrics, Tracing, Logging, Validation, Exception
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
    public void PipelineOrder_MetricsTracingLoggingValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurator = CreateConfigurator();

        // Act
        configurator.UseAll();
        configurator.Apply(services);

        // Assert - 등록 순서 확인
        var pipelineBehaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        pipelineBehaviors[0].ImplementationType.ShouldBe(typeof(UsecaseMetricsPipeline<,>));
        pipelineBehaviors[1].ImplementationType.ShouldBe(typeof(UsecaseTracingPipeline<,>));
        pipelineBehaviors[2].ImplementationType.ShouldBe(typeof(UsecaseLoggingPipeline<,>));
        pipelineBehaviors[3].ImplementationType.ShouldBe(typeof(UsecaseValidationPipeline<,>));
        pipelineBehaviors[4].ImplementationType.ShouldBe(typeof(UsecaseExceptionPipeline<,>));
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

    #endregion
}
