using System.IdentityModel.Tokens.Jwt;
using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Features.Assets;
using AssetTagBD.Application.Features.Assignments;
using AssetTagBD.Application.Features.Auth;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using AssetTagBD.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class SecurityRegressionTests : IntegrationTestBase
{
    public SecurityRegressionTests(TestFixture fixture) : base(fixture)
    {
    }

    private async Task<(Guid TenantId, Guid AdminId, string Email)> ArrangeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"security-{suffix}@test.local";
        var (tenantId, adminId) = await SeedTenantAsync($"Security {suffix}", $"security-{suffix}", email);
        ActAs(adminId, tenantId, UserRole.SuperAdmin, email);
        return (tenantId, adminId, email);
    }

    [Fact]
    public async Task PasswordReset_IncrementsTokenVersionInIssuedJwt()
    {
        var (_, _, email) = await ArrangeAsync();
        var login = await Send(new LoginCommand(email, "Admin@123"));
        var versionBefore = TokenVersion(login.Value!.Token);

        Fixture.CurrentUser.Clear();
        var forgot = await Send(new ForgotPasswordCommand(email));
        await Send(new ResetPasswordCommand(email, forgot.Value!.DevToken!, "Changed@123"));

        var nextLogin = await Send(new LoginCommand(email, "Changed@123"));
        Assert.NotEqual(versionBefore, TokenVersion(nextLogin.Value!.Token));
    }

    private static string TokenVersion(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token)
            .Claims.First(claim => claim.Type == "token_version").Value;

    [Fact]
    public async Task GetById_DoesNotReturnTrackedForeignTenantUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (tenantA, _) = await SeedTenantAsync($"Filter A {suffix}", $"filter-a-{suffix}", $"fa-{suffix}@test.local");
        var (tenantB, _) = await SeedTenantAsync($"Filter B {suffix}", $"filter-b-{suffix}", $"fb-{suffix}@test.local");
        var foreignUserId = await SeedUserAsync(tenantB, UserRole.Employee, $"foreign-{suffix}@test.local");

        using var scope = Fixture.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Auth-style lookup bypasses the filter and tracks the foreign entity.
        var tracked = await users.GetByIdForAuthAsync(foreignUserId);
        Assert.NotNull(tracked);

        // A regular lookup in the same context must still respect the filter.
        // Current tenant is null here, so no tenant-owned row should be returned.
        var filtered = await users.GetByIdAsync(foreignUserId);
        Assert.Null(filtered);
    }

    [Fact]
    public async Task DeactivatedTenant_CannotLogIn()
    {
        var (tenantId, _, email) = await ArrangeAsync();
        using (var scope = Fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = await db.Tenants.FindAsync(tenantId);
            tenant!.IsActive = false;
            await db.SaveChangesAsync();
        }

        Fixture.CurrentUser.Clear();
        var result = await Send(new LoginCommand(email, "Admin@123"));

        Assert.True(result.IsFailure);
        Assert.Equal("Unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsset_RetiredWithActiveAssignment_IsRejected()
    {
        var (tenantId, _, _) = await ArrangeAsync();
        var employee = await SeedUserAsync(tenantId, UserRole.Employee, $"ret-{Guid.NewGuid():N}@test.local");
        var asset = await Send(new CreateAssetCommand(null, "Busy", AssetCategory.Laptop, null, DateTime.UtcNow, null));
        await Send(new AssignAssetCommand(asset.Value!.Id, employee, null));

        var result = await Send(new UpdateAssetCommand(
            asset.Value.Id, "Busy", AssetCategory.Laptop, null, DateTime.UtcNow, null, AssetStatus.Retired));

        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }
}
