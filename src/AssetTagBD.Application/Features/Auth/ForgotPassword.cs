using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Auth;
using AssetTagBD.Domain.Entities;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Auth;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<ForgotPasswordResponse>>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private const int TokenLifetimeHours = 1;
    private const int MaxRequestsPerHour = 5;

    // In-process throttle. For multi-instance deployments replace with a
    // distributed store (Redis) or an API gateway rate limit.
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> Requests = new();

    private const string GenericMessage =
        "If an account exists for that email, a password reset link has been sent.";

    private readonly IUserRepository _users;
    private readonly IPasswordResetRepository _resetTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly IAuthSettings _settings;

    public ForgotPasswordCommandHandler(
        IUserRepository users,
        IPasswordResetRepository resetTokens,
        IUnitOfWork unitOfWork,
        IEmailSender emailSender,
        IAuthSettings settings)
    {
        _users = users;
        _resetTokens = resetTokens;
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _settings = settings;
    }

    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _users.GetByEmailForAuthAsync(email, cancellationToken);

        if (user is null)
        {
            // Same response whether the account exists or not. Counting only
            // real accounts also prevents throttling one address from locking
            // out unrelated requests.
            return Result.Success(new ForgotPasswordResponse(GenericMessage, null));
        }

        if (IsRateLimited(email))
        {
            return Result.Success(new ForgotPasswordResponse(GenericMessage, null));
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Everything or nothing: a failed email must not leave a live token,
        // nor invalidate the user's previous still-valid link.
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        // Only the newest link should remain usable.
        await _resetTokens.RevokeUnusedForUserAsync(user.Id, DateTime.UtcNow, cancellationToken);

        await _resetTokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(TokenLifetimeHours),
            TenantId = user.TenantId
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _emailSender.SendPasswordResetAsync(user.Email, rawToken, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var devToken = _settings.ExposeResetTokenInResponse ? rawToken : null;
        return Result.Success(new ForgotPasswordResponse(GenericMessage, devToken));
    }

    private static bool IsRateLimited(string email)
    {
        var now = DateTime.UtcNow;

        // Bound memory in long-running processes: prune stale windows once the
        // dictionary grows large instead of accumulating every address forever.
        if (Requests.Count > 1000)
        {
            foreach (var key in Requests.Keys)
            {
                if (Requests.TryGetValue(key, out var old) && now - old.WindowStart > TimeSpan.FromHours(2))
                {
                    Requests.TryRemove(key, out _);
                }
            }
        }

        var entry = Requests.AddOrUpdate(
            email,
            _ => (1, now),
            (_, previous) => now - previous.WindowStart > TimeSpan.FromHours(1)
                ? (1, now)
                : (previous.Count + 1, previous.WindowStart));

        return entry.Count > MaxRequestsPerHour;
    }

    internal static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
