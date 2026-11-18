using AssetTagBD.Application.Common.Interfaces;

namespace AssetTagBD.Integration.Tests;

/// <summary>Mutable identity used to simulate requests from different tenants.</summary>
public sealed class TestCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public Guid? TenantId { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void ActAs(Guid userId, Guid tenantId, string role, string email)
    {
        UserId = userId;
        TenantId = tenantId;
        Role = role;
        Email = email;
    }

    public void Clear()
    {
        UserId = null;
        TenantId = null;
        Role = null;
        Email = null;
    }
}
