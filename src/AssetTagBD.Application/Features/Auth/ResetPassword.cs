using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Auth;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword)
    : IRequest<Result<bool>>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IPasswordResetRepository _resetTokens;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IPasswordResetRepository resetTokens,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IRefreshTokenService refreshTokens,
        IUnitOfWork unitOfWork)
    {
        _resetTokens = resetTokens;
        _users = users;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var token = await _resetTokens.GetByHashAsync(
            ForgotPasswordCommandHandler.Hash(request.Token), cancellationToken);

        if (token is null)
        {
            return Result.Failure<bool>(AppErrors.BadRequest("This password reset link is invalid or has expired."));
        }

        var user = await _users.GetByIdForAuthAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<bool>(AppErrors.BadRequest("This password reset link is invalid."));
        }

        // The token is the real secret, but binding it to the submitted email
        // prevents a valid token from being replayed against a different address.
        if (!string.Equals(user.Email, request.Email.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            return Result.Failure<bool>(AppErrors.BadRequest("This password reset link is invalid."));
        }

        // Conditional UPDATE is the real single-use guard - two concurrent
        // requests cannot both consume the same token.
        var consumed = await _resetTokens.TryConsumeAsync(
            ForgotPasswordCommandHandler.Hash(request.Token), DateTime.UtcNow, cancellationToken);
        if (!consumed)
        {
            return Result.Failure<bool>(AppErrors.BadRequest("This password reset link is invalid or has expired."));
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.TokenVersion++; // invalidates every access token issued before this reset

        // Logging out every session is the safe default after a password change.
        await _refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
