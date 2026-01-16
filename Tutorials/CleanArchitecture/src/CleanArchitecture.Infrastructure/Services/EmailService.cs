using CleanArchitecture.Application.Services;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        // In real app: use SendGrid, SMTP, etc.
        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        await Task.CompletedTask;
    }
}
