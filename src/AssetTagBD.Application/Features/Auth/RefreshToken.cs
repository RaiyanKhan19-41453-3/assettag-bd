using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Auth;
using AssetTagBD.Application.DTOs.Users;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponseDto>>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokens,
        IJwtTokenGenerator tokenGenerator)
    {
        _refreshTokens = refreshTokens;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<AuthResponseDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var rotated = await _refreshTokens.RotateAsync(request.RefreshToken, cancellationToken);

        if (rotated is null)
        {
            return Result.Failure<AuthResponseDto>(
                AppErrors.Unauthorized("The refresh token is invalid or has expired."));
        }

        var (token, expiresAt) = _tokenGenerator.GenerateToken(rotated.Value.User);

        return Result.Success(new AuthResponseDto(
            token, rotated.Value.NewToken, expiresAt, UserDto.FromEntity(rotated.Value.User)));
    }
}

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<Result<bool>>;

public sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result<bool>>
{
    private readonly IRefreshTokenService _refreshTokens;

    public RevokeTokenCommandHandler(IRefreshTokenService refreshTokens)
    {
        _refreshTokens = refreshTokens;
    }

    public async Task<Result<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var revoked = await _refreshTokens.RevokeAsync(request.RefreshToken, cancellationToken);

        return revoked
            ? Result.Success(true)
            : Result.Failure<bool>(AppErrors.Unauthorized("The refresh token is invalid or already revoked."));
    }
}
