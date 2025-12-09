using System.Diagnostics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders;
using Functorium.Adapters.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace Functorium.Abstractions.Registrations;

/// <summary>
/// OpenTelemetry 등록을 위한 확장 메서드
/// 기본 Serilog, OpenTelemetry 설정을 Framework에서 제공하고,
/// 프로젝트는 Builder 패턴으로 확장 포인트만 집중
/// </summary>
public static class OpenTelemetryRegistration
{
    /// <summary>
    /// OpenTelemetry 기본 설정을 등록하고 Builder 반환
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="configuration">IConfiguration</param>
    /// <returns>OpenTelemetryBuilder - 프로젝트별 확장 설정을 위한 Builder</returns>
    public static OpenTelemetryBuilder RegisterObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // OpenTelemetryOptions 읽기
        services.RegisterConfigureOptions<OpenTelemetryOptions, OpenTelemetryOptions.Validator>(OpenTelemetryOptions.SectionName);
        OpenTelemetryOptions openTelemetryOptions = services.GetOptions<OpenTelemetryOptions>();

        // IOpenTelemetryOptions 등록 (DI)
        services.AddSingleton<IOpenTelemetryOptions>(openTelemetryOptions);

        // ActivitySource 등록
        services.AddSingleton(_ =>
            new ActivitySource(
                openTelemetryOptions.ServiceName,
                openTelemetryOptions.ServiceVersion));

        // Serilog Logging 추가
        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));

        return new OpenTelemetryBuilder(
            services,
            configuration,
            openTelemetryOptions);

        //// Destructure 설정값 추출
        //(int maxDepth, int maxStringLength, int maxCollectionCount) destructureSettings = SerilogDestructureHelper.ExtractDestructureSettings(configuration);

        // OpenTelemetryBuilder 반환 (프로젝트는 이 Builder로 확장 설정)
        //return new OpenTelemetryBuilder(
        //    services,
        //    configuration,
        //    openTelemetryOptions,
        //    destructureSettings);
    }
}

