using AiGovernance.Adapters.Persistence.Assessments.Repositories;
using AiGovernance.Adapters.Persistence.Deployments.Queries;
using AiGovernance.Adapters.Persistence.Deployments.Repositories;
using AiGovernance.Adapters.Persistence.Incidents.Queries;
using AiGovernance.Adapters.Persistence.Incidents.Repositories;
using AiGovernance.Adapters.Persistence.Models.Queries;
using AiGovernance.Adapters.Persistence.Models.Repositories;
using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Application.Usecases.Incidents.Ports;
using AiGovernance.Application.Usecases.Models.Ports;
using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Incidents;
using AiGovernance.Domain.AggregateRoots.Models;
using Functorium.Abstractions.Registrations;
using Functorium.Adapters.Abstractions.Options;
using Functorium.Applications.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiGovernance.Adapters.Persistence.Registrations;

public static class AdapterPersistenceRegistration
{
    public static IServiceCollection RegisterAdapterPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options 등록
        services.RegisterConfigureOptions<PersistenceOptions, PersistenceOptions.Validator>(
            PersistenceOptions.SectionName);

        var options = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>() ?? new PersistenceOptions();

        switch (options.Provider)
        {
            case "Sqlite":
                services.AddDbContext<GovernanceDbContext>(opt =>
                    opt.UseSqlite(options.ConnectionString));
                RegisterSqliteRepositories(services);
                break;

            case "InMemory":
            default:
                RegisterInMemoryRepositories(services);
                break;
        }

        return services;
    }

    public static IApplicationBuilder UseAdapterPersistence(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<PersistenceOptions>>().Value;

        if (options.Provider == "Sqlite")
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GovernanceDbContext>();
            dbContext.Database.EnsureCreated();
        }

        return app;
    }

    private static void RegisterInMemoryRepositories(IServiceCollection services)
    {
        // Repository 등록 (Source Generator가 생성한 Observable 버전 사용)
        services.RegisterScopedObservablePort<IAIModelRepository, AIModelRepositoryInMemoryObservable>();
        services.RegisterScopedObservablePort<IDeploymentRepository, DeploymentRepositoryInMemoryObservable>();
        services.RegisterScopedObservablePort<IAssessmentRepository, AssessmentRepositoryInMemoryObservable>();
        services.RegisterScopedObservablePort<IIncidentRepository, IncidentRepositoryInMemoryObservable>();

        // UnitOfWork 등록
        services.RegisterScopedObservablePort<IUnitOfWork, UnitOfWorkInMemoryObservable>();

        // Read Adapter 등록
        services.RegisterScopedObservablePort<IAIModelQuery, AIModelQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IModelDetailQuery, AIModelDetailQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IDeploymentQuery, DeploymentQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IDeploymentDetailQuery, DeploymentDetailQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IIncidentQuery, IncidentQueryInMemoryObservable>();
    }

    private static void RegisterSqliteRepositories(IServiceCollection services)
    {
        // Repository 등록 (Source Generator가 생성한 Observable 버전 사용)
        services.RegisterScopedObservablePort<IAIModelRepository, AIModelRepositoryEfCoreObservable>();
        services.RegisterScopedObservablePort<IDeploymentRepository, DeploymentRepositoryEfCoreObservable>();
        services.RegisterScopedObservablePort<IAssessmentRepository, AssessmentRepositoryEfCoreObservable>();
        services.RegisterScopedObservablePort<IIncidentRepository, IncidentRepositoryEfCoreObservable>();

        // UnitOfWork 등록
        services.RegisterScopedObservablePort<IUnitOfWork, UnitOfWorkEfCoreObservable>();

        // Read Adapter 등록 (InMemory 쿼리 재사용 — 향후 Dapper 쿼리로 교체 가능)
        services.RegisterScopedObservablePort<IAIModelQuery, AIModelQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IModelDetailQuery, AIModelDetailQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IDeploymentQuery, DeploymentQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IDeploymentDetailQuery, DeploymentDetailQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IIncidentQuery, IncidentQueryInMemoryObservable>();
    }
}
