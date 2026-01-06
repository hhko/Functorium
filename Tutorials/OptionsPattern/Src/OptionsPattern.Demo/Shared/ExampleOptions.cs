using FluentValidation;

namespace OptionsPattern.Demo.Shared;

/// <summary>
/// 예제용 Options 클래스들
/// </summary>

/// <summary>
/// 데이터베이스 연결 설정
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
    public int ConnectionTimeout { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public bool EnablePooling { get; set; } = true;
    public int MaxPoolSize { get; set; } = 100;

    public sealed class Validator : AbstractValidator<DatabaseOptions>
    {
        public Validator()
        {
            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .WithMessage($"{nameof(ConnectionString)} is required.");

            RuleFor(x => x.ConnectionTimeout)
                .InclusiveBetween(1, 300)
                .WithMessage($"{nameof(ConnectionTimeout)} must be between 1 and 300 seconds.");

            RuleFor(x => x.RetryCount)
                .InclusiveBetween(0, 10)
                .WithMessage($"{nameof(RetryCount)} must be between 0 and 10.");

            RuleFor(x => x.MaxPoolSize)
                .GreaterThan(0)
                .WithMessage($"{nameof(MaxPoolSize)} must be greater than 0.");
        }
    }
}

/// <summary>
/// API 클라이언트 설정
/// </summary>
public sealed class ApiClientOptions
{
    public const string SectionName = "ApiClient";

    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public bool EnableLogging { get; set; } = false;

    public sealed class Validator : AbstractValidator<ApiClientOptions>
    {
        public Validator()
        {
            RuleFor(x => x.BaseUrl)
                .NotEmpty()
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage($"{nameof(BaseUrl)} must be a valid absolute URL.");

            RuleFor(x => x.TimeoutSeconds)
                .InclusiveBetween(1, 300)
                .WithMessage($"{nameof(TimeoutSeconds)} must be between 1 and 300 seconds.");

            RuleFor(x => x.MaxRetries)
                .InclusiveBetween(0, 10)
                .WithMessage($"{nameof(MaxRetries)} must be between 0 and 10.");
        }
    }
}

/// <summary>
/// 캐시 설정
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int DefaultExpirationMinutes { get; set; } = 60;
    public int MaxSize { get; set; } = 1000;
    public string CacheType { get; set; } = "Memory";
    public string? RedisConnectionString { get; set; }

    public sealed class Validator : AbstractValidator<CacheOptions>
    {
        public Validator()
        {
            RuleFor(x => x.DefaultExpirationMinutes)
                .GreaterThan(0)
                .WithMessage($"{nameof(DefaultExpirationMinutes)} must be greater than 0.");

            RuleFor(x => x.MaxSize)
                .GreaterThan(0)
                .WithMessage($"{nameof(MaxSize)} must be greater than 0.");

            RuleFor(x => x.CacheType)
                .Must(type => type == "Memory" || type == "Redis")
                .WithMessage($"{nameof(CacheType)} must be either 'Memory' or 'Redis'.");

            RuleFor(x => x.RedisConnectionString)
                .NotEmpty()
                .When(x => x.CacheType == "Redis")
                .WithMessage($"{nameof(RedisConnectionString)} is required when CacheType is Redis.");
        }
    }
}

/// <summary>
/// 간단한 설정 예제 (검증 없음)
/// </summary>
public sealed class SimpleOptions
{
    public const string SectionName = "Simple";

    public string Name { get; set; } = string.Empty;
    public int Value { get; set; } = 0;
    public bool Enabled { get; set; } = false;
}
