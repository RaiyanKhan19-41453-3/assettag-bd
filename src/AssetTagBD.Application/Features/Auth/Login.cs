using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Auth;
using AssetTagBD.Application.DTOs.Users;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponseDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenService _refreshTokens;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IRefreshTokenService refreshTokens)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _refreshTokens = refreshTokens;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailForAuthAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (user is null || user.Tenant is null || !user.Tenant.IsActive ||
            !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResponseDto>(
                AppErrors.Unauthorized("Invalid email or password."));
        }

        var (token, expiresAt) = _tokenGenerator.GenerateToken(user);
        var refreshToken = await _refreshTokens.IssueAsync(user, cancellationToken);

        return Result.Success(new AuthResponseDto(token, refreshToken, expiresAt, UserDto.FromEntity(user)));
    }
}
