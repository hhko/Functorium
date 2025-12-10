using Functorium.Abstractions.Registrations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Observability.Adapters.Infrastructure.Abstractions.Registrations;

/// <summary>
/// 프로젝트별 OpenTelemetry 확장 등록
/// Functorium에서 제공하는 Builder 패턴으로 Enricher, Processor 등 프로젝트 특화 확장 포인트만 집중
/// </summary>
internal static class OpenTelemetryRegistration
{
    internal static IServiceCollection RegisterOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        // OpenTelemetry 인프라 설정 (Serilog, Metrics, Traces)
        services
            .RegisterObservability(configuration)
            .ConfigureSerilog(serilog =>
            {
                // 프로젝트별 Serilog 확장 설정
            })
            .ConfigureMetrics(metrics =>
            {
                // 프로젝트별 Meter 추가
                metrics.AddMeter("Observability.*");
            })
            .ConfigureTraces(traces =>
            {
                // 프로젝트별 ActivitySource 추가
                traces.AddSource("Observability.*");
            })
            .ConfigureStartupLogger(logger =>
            {
                const int labelWidth = 22;

                // 프로젝트별 커스텀 설정 정보 출력
                logger.LogInformation("Application Configuration");
                logger.LogInformation("  {Label}: {Value}", "Database".PadRight(labelWidth), configuration.GetConnectionString("DefaultConnection") ?? "(not configured)");
                logger.LogInformation("  {Label}: {Value}", "API Base URL".PadRight(labelWidth), configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000");
                logger.LogInformation("  {Label}: {Value}", "Cache Enabled".PadRight(labelWidth), configuration.GetValue<bool>("Features:EnableCache"));
                logger.LogInformation("  {Label}: {Value}", "Max Connections".PadRight(labelWidth), configuration.GetValue<int>("ConnectionPool:MaxSize", 100));
            })
            .Build();

        return services;
    }
}

