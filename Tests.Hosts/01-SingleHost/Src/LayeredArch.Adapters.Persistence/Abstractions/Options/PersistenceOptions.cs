using FluentValidation;
using Functorium.Adapters.Observabilities.Loggers;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Adapters.Persistence.Abstractions.Options;

/// <summary>
/// Persistence 어댑터 옵션
/// Provider에 따라 InMemory(ConcurrentDictionary), Sqlite 선택
/// </summary>
public sealed class PersistenceOptions : IStartupOptionsLogger
{
    public const string SectionName = "Persistence";

    /// <summary>
    /// 영속성 Provider: "InMemory" | "Sqlite"
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// SQLite 연결 문자열
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=layeredarch.db";

    public static readonly string[] SupportedProviders = ["InMemory", "Sqlite"];

    public void LogConfiguration(ILogger logger)
    {
        const int labelWidth = 20;

        logger.LogInformation("Persistence Configuration");
        logger.LogInformation("  {Label}: {Value}", "Provider".PadRight(labelWidth), Provider);

        if (Provider == "Sqlite")
        {
            logger.LogInformation("  {Label}: {Value}", "ConnectionString".PadRight(labelWidth), ConnectionString);
        }
    }

    public sealed class Validator : AbstractValidator<PersistenceOptions>
    {
        public Validator()
        {
            RuleFor(x => x.Provider)
                .NotEmpty()
                .WithMessage($"{nameof(Provider)} is required.")
                .Must(p => SupportedProviders.Contains(p))
                .WithMessage($"{nameof(Provider)} must be one of: {string.Join(", ", SupportedProviders)}");

            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .When(x => x.Provider == "Sqlite")
                .WithMessage($"{nameof(ConnectionString)} is required when Provider is 'Sqlite'.");
        }
    }
}
