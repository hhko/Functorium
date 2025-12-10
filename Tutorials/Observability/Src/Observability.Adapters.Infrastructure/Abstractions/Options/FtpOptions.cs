using FluentValidation;
using Functorium.Adapters.Observabilities.Logging;
using Microsoft.Extensions.Logging;

namespace Observability.Adapters.Infrastructure.Abstractions.Options;

/// <summary>
/// FTP 서버 접속 정보를 관리하는 Options 클래스
/// </summary>
public sealed class FtpOptions : IStartupOptionsLogger
{
    /// <summary>
    /// appsettings.json의 섹션 이름
    /// </summary>
    public const string SectionName = "Ftp";

    /// <summary>
    /// FTP 서버 호스트 주소
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// FTP 서버 포트 (기본값: 21)
    /// </summary>
    public int Port { get; set; } = 21;

    /// <summary>
    /// FTP 사용자명
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// FTP 비밀번호
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Passive 모드 사용 여부 (기본값: true)
    /// </summary>
    public bool UsePassive { get; set; } = true;

    /// <summary>
    /// TLS/SSL 보안 연결 사용 여부 (FTPS) (기본값: false)
    /// </summary>
    public bool UseTls { get; set; } = false;

    /// <summary>
    /// 연결 타임아웃 (초) (기본값: 30)
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// FTP 루트 디렉토리 (기본값: "/")
    /// </summary>
    public string RootDirectory { get; set; } = "/";

    /// <summary>
    /// StartupLogger에 FTP 설정 정보를 출력합니다.
    /// </summary>
    public void LogConfiguration(ILogger logger)
    {
        const int labelWidth = 20;

        // 대주제: FTP Configuration
        logger.LogInformation("FTP Configuration");
        logger.LogInformation("");

        // 세부주제: Connection
        logger.LogInformation("  Connection");
        logger.LogInformation("    {Label}: {Value}",
            "Host".PadRight(labelWidth), Host);
        logger.LogInformation("    {Label}: {Value}",
            "Port".PadRight(labelWidth), Port);
        logger.LogInformation("    {Label}: {Value}",
            "Username".PadRight(labelWidth), Username);
        logger.LogInformation("    {Label}: {Value}",
            "Password".PadRight(labelWidth), MaskPassword(Password));
        logger.LogInformation("");

        // 세부주제: Settings
        logger.LogInformation("  Settings");
        logger.LogInformation("    {Label}: {Value}",
            "Root Directory".PadRight(labelWidth), RootDirectory);
        logger.LogInformation("    {Label}: {Value}",
            "Timeout".PadRight(labelWidth), $"{ConnectionTimeout}s");
        logger.LogInformation("    {Label}: {Value}",
            "Passive Mode".PadRight(labelWidth), UsePassive ? "Enabled" : "Disabled");
        logger.LogInformation("    {Label}: {Value}",
            "TLS/SSL".PadRight(labelWidth), UseTls ? "Enabled" : "Disabled");
    }

    /// <summary>
    /// 비밀번호를 마스킹 처리합니다.
    /// </summary>
    private static string MaskPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "(not configured)";

        return "********";
    }

    /// <summary>
    /// FtpOptions 유효성 검증 클래스
    /// </summary>
    public sealed class Validator : AbstractValidator<FtpOptions>
    {
        public Validator()
        {
            // Host 필수 검증
            RuleFor(x => x.Host)
                .NotEmpty()
                .WithMessage($"{nameof(Host)} is required.");

            // Port 범위 검증 (1-65535)
            RuleFor(x => x.Port)
                .InclusiveBetween(1, 65535)
                .WithMessage($"{nameof(Port)} must be between 1 and 65535.");

            // Username 필수 검증
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage($"{nameof(Username)} is required.");

            // Password 필수 검증
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage($"{nameof(Password)} is required.");

            // ConnectionTimeout 범위 검증 (1-300초)
            RuleFor(x => x.ConnectionTimeout)
                .InclusiveBetween(1, 300)
                .WithMessage($"{nameof(ConnectionTimeout)} must be between 1 and 300 seconds.");

            // RootDirectory 필수 검증
            RuleFor(x => x.RootDirectory)
                .NotEmpty()
                .WithMessage($"{nameof(RootDirectory)} is required.");
        }
    }
}
