using AssetTagBD.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AssetTagBD.Infrastructure.Services;

/// <summary>
/// Development placeholder for an email provider. It writes the reset token to
/// the log instead of sending mail — replace with an SMTP/email API integration
/// before going live.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly IAuthSettings _settings;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IAuthSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        if (!_settings.ExposeResetTokenInResponse)
        {
            // Fail loudly: silently dropping reset emails in production is worse
            // than an error that tells operations to configure SMTP.
            throw new InvalidOperationException(
                "No email sender is configured. Set Email:Smtp:Host to enable password reset.");
        }

        _logger.LogWarning("[DEV ONLY] Password reset requested for {Email}. Token: {Token}", email, token);
        return Task.CompletedTask;
    }
}
