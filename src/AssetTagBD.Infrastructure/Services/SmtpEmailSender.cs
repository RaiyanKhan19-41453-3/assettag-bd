using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace AssetTagBD.Infrastructure.Services;

/// <summary>Production email sender. Configure Email:Smtp:* and Email:From.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Email:Smtp:Host"]
            ?? throw new InvalidOperationException("Email:Smtp:Host is not configured.");
        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var parsed) ? parsed : 587;
        var from = _configuration["Email:From"] ?? "noreply@assettagbd.local";
        var fromName = _configuration["Email:FromName"] ?? "AssetTag BD";
        var user = _configuration["Email:Smtp:User"];
        var password = SecretReader.Read(_configuration, "Email:Smtp:Password");
        var useSsl = _configuration.GetValue<bool>("Email:Smtp:UseSsl");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, from));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Reset your AssetTag BD password";
        message.Body = new TextPart("plain")
        {
            Text = $"Use this token to reset your password:\n\n{token}\n\n" +
                   "It expires in one hour. If you did not request this, ignore this email."
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, useSsl, cancellationToken);

        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
        {
            await client.AuthenticateAsync(user, password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
