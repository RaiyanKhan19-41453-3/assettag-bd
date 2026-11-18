using System.Text.RegularExpressions;
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
/// Public company sign-up. Creates a new tenant together with its first
/// SuperAdmin in a single transaction — the foundation of tenant isolation.
/// </summary>
public sealed record CreateTenantCommand(
    string CompanyName,
    string AdminFullName,
    string AdminEmail,
    string Password) : IRequest<Result<AuthResponseDto>>;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdminFullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<AuthResponseDto>>
{
    private readonly ITenantRepository _tenants;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenService _refreshTokens;

    public CreateTenantCommandHandler(
        ITenantRepository tenants,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork,
        IRefreshTokenService refreshTokens)
    {
        _tenants = tenants;
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _unitOfWork = unitOfWork;
        _refreshTokens = refreshTokens;
    }

    public async Task<Result<AuthResponseDto>> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var companyName = request.CompanyName.Trim();
        var email = request.AdminEmail.Trim().ToLowerInvariant();

        if (await _tenants.NameExistsAsync(companyName, cancellationToken))
        {
            return Result.Failure<AuthResponseDto>(AppErrors.Conflict("An organisation with this name already exists."));
        }

        if (await _users.EmailExistsGloballyAsync(email, cancellationToken))
        {
            return Result.Failure<AuthResponseDto>(AppErrors.Conflict("An account with this email already exists."));
        }

        var tenant = new Tenant
        {
            Name = companyName,
            Slug = await GenerateUniqueSlugAsync(companyName, cancellationToken)
        };

        var admin = new User
        {
            FullName = request.AdminFullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.SuperAdmin,
            TenantId = tenant.Id,
            Department = "Administration",
            Tenant = tenant
        };

        await _tenants.AddAsync(tenant, cancellationToken);
        await _users.AddAsync(admin, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = _tokenGenerator.GenerateToken(admin);
        var refreshToken = await _refreshTokens.IssueAsync(admin, cancellationToken);

        return Result.Success(new AuthResponseDto(token, refreshToken, expiresAt, UserDto.FromEntity(admin)));
    }

    private async Task<string> GenerateUniqueSlugAsync(string companyName, CancellationToken cancellationToken)
    {
        var baseSlug = Regex.Replace(companyName.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "company";
        if (baseSlug.Length > 90) baseSlug = baseSlug[..90];

        if (!await _tenants.SlugExistsAsync(baseSlug, cancellationToken)) return baseSlug;

        for (var i = 2; i < 100; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!await _tenants.SlugExistsAsync(candidate, cancellationToken)) return candidate;
        }

        return $"{baseSlug}-{Guid.NewGuid():N}";
    }
}
