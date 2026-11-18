using System.IdentityModel.Tokens.Jwt;
using AssetTagBD.Application.Features.Auth;
using AssetTagBD.Application.Features.Users;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class AuthTests : IntegrationTestBase
{
    public AuthTests(TestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SignupCompany_CreatesTenantAndReturnsTokenWithTenantClaim()
    {
        Fixture.CurrentUser.Clear();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"owner-{suffix}@test.local";

        var result = await Send(new CreateTenantCommand(
            $"Signup Co {suffix}", "Owner User", email, "Admin@123"));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.SuperAdmin, result.Value!.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.Token);
        var tenantClaim = token.Claims.FirstOrDefault(c => c.Type == "tenant_id");
        Assert.NotNull(tenantClaim);
        Assert.True(Guid.TryParse(tenantClaim!.Value, out _));

        // The new admin can immediately log in.
        Fixture.CurrentUser.Clear();
        var login = await Send(new LoginCommand(email, "Admin@123"));
        Assert.True(login.IsSuccess);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        Fixture.CurrentUser.Clear();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"login-{suffix}@test.local";
        await SeedTenantAsync($"Login Co {suffix}", $"login-{suffix}", email);

        var result = await Send(new LoginCommand(email, "WrongPassword1"));

        Assert.True(result.IsFailure);
        Assert.Equal("Unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task Register_CreatesUserOnlyInCurrentTenant()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var emailA = $"admin-register-a-{suffix}@test.local";
        var emailB = $"admin-register-b-{suffix}@test.local";
        var (tenantA, adminA) = await SeedTenantAsync($"Register A {suffix}", $"register-a-{suffix}", emailA);
        var (tenantB, _) = await SeedTenantAsync($"Register B {suffix}", $"register-b-{suffix}", emailB);

        ActAs(adminA, tenantA, UserRole.SuperAdmin, emailA);
        var invitedEmail = $"invited-{suffix}@test.local";
        var invite = await Send(new RegisterCommand(
            "Invited Employee", invitedEmail, "Employee@123", UserRole.Employee, "HR"));
        Assert.True(invite.IsSuccess);

        var usersA = await Send(new GetUsersQuery(null));
        Assert.Contains(usersA.Value!, user => user.Email == invitedEmail);

        ActAs(adminA, tenantB, UserRole.SuperAdmin, emailB);
        var usersB = await Send(new GetUsersQuery(null));
        Assert.DoesNotContain(usersB.Value!, user => user.Email == invitedEmail);
    }
}
