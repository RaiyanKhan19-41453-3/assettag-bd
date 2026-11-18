using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Auth;
using AssetTagBD.Application.DTOs.Users;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Auth;

/// <summary>
/// Invites a user into the caller's tenant. Only a tenant SuperAdmin may call
/// this; public company creation uses <see cref="CreateTenantCommand"/>.
/// </summary>
public sealed record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    string? Department) : IRequest<Result<AuthResponseDto>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Department).MaximumLength(150);
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IRefreshTokenService _refreshTokens;

    public RegisterCommandHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IRefreshTokenService refreshTokens)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _refreshTokens = refreshTokens;
    }

    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Result.Failure<AuthResponseDto>(AppErrors.Unauthorized("An active tenant is required."));
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsGloballyAsync(email, cancellationToken))
        {
            return Result.Failure<AuthResponseDto>(AppErrors.Conflict("An account with this email already exists."));
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim(),
            TenantId = _currentUser.TenantId.Value
        };

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = _tokenGenerator.GenerateToken(user);
        var refreshToken = await _refreshTokens.IssueAsync(user, cancellationToken);

        return Result.Success(new AuthResponseDto(token, refreshToken, expiresAt, UserDto.FromEntity(user)));
    }
}
