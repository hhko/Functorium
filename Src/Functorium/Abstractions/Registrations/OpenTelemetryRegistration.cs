using System.Diagnostics;
using System.Reflection;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders;
using Functorium.Adapters.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Serilog;

namespace Functorium.Abstractions.Registrations;

/// <summary>
/// OpenTelemetry 등록을 위한 확장 메서드
/// 기본 Serilog, OpenTelemetry 설정을 Functorium에서 제공하고,
/// 프로젝트는 Builder 패턴으로 확장 포인트만 집중
/// </summary>
public static class OpenTelemetryRegistration
{
    /// <summary>
    /// OpenTelemetry 기본 설정을 등록하고 Builder 반환
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="configuration">IConfiguration</param>
    /// <param name="projectAssembly">
    /// 프로젝트의 AssemblyReference.Assembly를 전달합니다. 해당 어셈블리의 네임스페이스 루트가
    /// 자동으로 Meter와 ActivitySource에 등록됩니다.
    /// </param>
    /// <returns>OpenTelemetryBuilder - 프로젝트별 확장 설정을 위한 Builder</returns>
    /// <example>
    /// <code>
    /// // AssemblyReference를 전달하여 프로젝트 네임스페이스 자동 등록
    /// services
    ///     .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
    ///     .ConfigureSerilog(serilog => { })
    ///     .Build();
    /// </code>
    /// </example>
    public static OpenTelemetryBuilder RegisterOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly projectAssembly)
    {
        // OpenTelemetryOptions 등록 (IOptions<OpenTelemetryOptions> 패턴 사용)
        services.RegisterConfigureOptions<OpenTelemetryOptions, OpenTelemetryOptions.Validator>(OpenTelemetryOptions.SectionName);

        // ActivitySource 등록 (IOptions에서 값 가져오기)
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
            return new ActivitySource(options.ServiceName, options.ServiceVersion);
        });

        // Serilog Logging 추가
        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));

        // OpenTelemetry Logging, Tracing, Metrics 확장 설정을 위한 Builder 클래스 반환
        // Builder는 IServiceProvider를 통해 필요할 때 옵션을 가져옴
        return new OpenTelemetryBuilder(
            services,
            configuration,
            projectAssembly);
    }
}

