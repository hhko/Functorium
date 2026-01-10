using Functorium.Adapters.Observabilities.Loggers;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Configurations;

/// <summary>
/// SloConfiguration을 로깅하는 IStartupOptionsLogger 구현체입니다.
/// </summary>
public sealed class SloConfigurationLogger : IStartupOptionsLogger
{
    private readonly SloConfiguration _configuration;

    public SloConfigurationLogger(SloConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// StartupLogger에 SLO 설정 정보를 출력합니다.
    /// </summary>
    public void LogConfiguration(ILogger logger)
    {
        const int labelWidth = 25;

        logger.LogInformation("SLO Configuration");
        logger.LogInformation("");

        // Global Defaults
        logger.LogInformation("  Global Defaults");
        LogSloTargets(logger, _configuration.GlobalDefaults, labelWidth);
        logger.LogInformation("");

        // CQRS Defaults
        logger.LogInformation("  CQRS Defaults");
        logger.LogInformation("    Command:");
        LogSloTargets(logger, _configuration.MergeWithGlobalDefaults(_configuration.CqrsDefaults.Command), labelWidth, "      ");
        logger.LogInformation("    Query:");
        LogSloTargets(logger, _configuration.MergeWithGlobalDefaults(_configuration.CqrsDefaults.Query), labelWidth, "      ");
        logger.LogInformation("");

        // Handler Overrides
        if (_configuration.HandlerOverrides.Count > 0)
        {
            logger.LogInformation("  Handler Overrides ({Count})", _configuration.HandlerOverrides.Count);
            foreach (var (handler, targets) in _configuration.HandlerOverrides)
            {
                logger.LogInformation("    {Handler}:", handler);
                LogSloTargets(logger, targets, labelWidth, "      ");
            }
            logger.LogInformation("");
        }

        // Histogram Buckets
        logger.LogInformation("  Histogram Buckets");
        logger.LogInformation("    {Label}: [{Values}]",
            "Boundaries (seconds)".PadRight(labelWidth),
            string.Join(", ", _configuration.HistogramBuckets));
    }

    private static void LogSloTargets(ILogger logger, SloTargets targets, int labelWidth, string indent = "    ")
    {
        if (targets.AvailabilityPercent.HasValue)
            logger.LogInformation("{Indent}{Label}: {Value}%", indent, "Availability".PadRight(labelWidth), targets.AvailabilityPercent.Value);

        if (targets.LatencyP95Milliseconds.HasValue)
            logger.LogInformation("{Indent}{Label}: {Value}ms", indent, "Latency P95".PadRight(labelWidth), targets.LatencyP95Milliseconds.Value);

        if (targets.LatencyP99Milliseconds.HasValue)
            logger.LogInformation("{Indent}{Label}: {Value}ms", indent, "Latency P99".PadRight(labelWidth), targets.LatencyP99Milliseconds.Value);

        if (targets.ErrorBudgetWindowDays.HasValue)
            logger.LogInformation("{Indent}{Label}: {Value} days", indent, "Error Budget Window".PadRight(labelWidth), targets.ErrorBudgetWindowDays.Value);
    }
}
