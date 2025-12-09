using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Loggers;

/// <summary>
/// 애플리케이션 시작 시 설정 정보를 로그로 출력하는 IHostedService
/// 애플리케이션 생명주기와 통합되어 의존성 주입을 활용합니다.
/// </summary>
public class StartupLogger : IHostedService
{
    private const int LineWidth = 80;
    private const int LabelWidth = 22;
    private const string Separator = "================================================================================";
    private const string SectionSeparator = "--------------------------------------------------------------------------------";

    private readonly ILogger<StartupLogger> _logger;
    private readonly IHostEnvironment _environment;
    private readonly Action<ILogger>? _additionalLogger;
    private readonly IEnumerable<IStartupOptionsLoggable> _loggableOptions;

    public StartupLogger(
        ILogger<StartupLogger> logger,
        IHostEnvironment environment,
        IEnumerable<IStartupOptionsLoggable> loggableOptions,
        Action<ILogger>? additionalLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _loggableOptions = loggableOptions ?? throw new ArgumentNullException(nameof(loggableOptions));
        _additionalLogger = additionalLogger;
    }

    /// <summary>
    /// 애플리케이션 시작 시 설정 정보를 로그로 출력합니다.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogSeparator();
        _logger.LogInformation("  Application Configuration Report");
        LogSeparator();
        _logger.LogInformation("");

        // Environment & Runtime Information
        LogEnvironmentInformation();

        // Host & Network Information
        LogHostInformation();

        // 프로젝트별 추가 로깅 (사용자 정의)
        if (_additionalLogger != null)
        {
            LogSectionSeparator();
            _additionalLogger.Invoke(_logger);
            _logger.LogInformation("");
        }

        // DI에 등록된 모든 IStartupLoggable Options 자동 출력
        LogStartupLoggableOptions();

        LogSeparator();

        return Task.CompletedTask;
    }

    private void LogEnvironmentInformation()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        DateTime startTime = DateTime.Now;

        _logger.LogInformation("Environment & Runtime Information");
        LogField("Environment", _environment.EnvironmentName);
        LogField(".NET Version", RuntimeInformation.FrameworkDescription);
        LogField("Process ID", currentProcess.Id.ToString());
        LogField("Start Time", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
        LogField("Working Directory", Environment.CurrentDirectory);
        _logger.LogInformation("");
    }

    private void LogHostInformation()
    {
        string hostName = Dns.GetHostName();
        string osDescription = RuntimeInformation.OSDescription;
        string osArchitecture = RuntimeInformation.OSArchitecture.ToString();
        string processArchitecture = RuntimeInformation.ProcessArchitecture.ToString();

        _logger.LogInformation("Host & System Information");
        LogField("Host Name", hostName);
        LogField("Machine Name", Environment.MachineName);
        LogField("OS", osDescription);
        LogField("OS Architecture", osArchitecture);
        LogField("Process Architecture", processArchitecture);
        LogField("Processor Cores", Environment.ProcessorCount.ToString());
        _logger.LogInformation("");
    }

    /// <summary>
    /// DI에 등록된 모든 IStartupLoggable을 자동으로 출력합니다.
    /// OptionsUtilities.AddConfigureOptions를 통해 등록된 Options는 자동으로 IStartupLoggable로 등록됩니다.
    /// </summary>
    private void LogStartupLoggableOptions()
    {
        var options = _loggableOptions.ToList();

        if (options.Count == 0)
            return;

        // Summary 정보 출력
        LogSectionSeparator();
        _logger.LogInformation("Options Configuration Summary");
        LogField("Total Options", options.Count.ToString());
        _logger.LogInformation("");

        foreach (var option in options)
        {
            var typeName = option.GetType().Name;
            _logger.LogInformation("  - {OptionType}", typeName);
        }

        _logger.LogInformation("");

        // 각 Options의 상세 정보 출력
        foreach (var option in options)
        {
            LogSectionSeparator();
            option.LogConfiguration(_logger);
            _logger.LogInformation("");
        }
    }

    private void LogSeparator()
    {
        _logger.LogInformation(Separator);
    }

    private void LogSectionSeparator()
    {
        _logger.LogInformation(SectionSeparator);
    }

    private void LogField(string label, string value)
    {
        string paddedLabel = label.PadRight(LabelWidth);
        _logger.LogInformation("  {Label}: {Value}", paddedLabel, value);
    }

    /// <summary>
    /// 애플리케이션 종료 시 호출됩니다. (현재는 아무 작업도 수행하지 않음)
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
