using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.DTOs.Auth;
using AssetTagBD.Application.Features.Auth;
using AssetTagBD.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class AuthHardeningTests : IntegrationTestBase
{
    public AuthHardeningTests(TestFixture fixture) : base(fixture)
    {
    }

    private async Task<(Guid TenantId, Guid AdminId, string Email)> ArrangeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"auth-{suffix}@test.local";
        var (tenantId, adminId) = await SeedTenantAsync($"Auth Co {suffix}", $"auth-{suffix}", email);
        ActAs(adminId, tenantId, UserRole.SuperAdmin, email);
        return (tenantId, adminId, email);
    }

    [Fact]
    public async Task Refresh_RotatesTokenAndOldTokenCannotBeReused()
    {
        var (_, _, email) = await ArrangeAsync();
        var login = await Send(new LoginCommand(email, "Admin@123"));
        var original = login.Value!.RefreshToken;

        Fixture.CurrentUser.Clear();
        var rotated = await Send(new RefreshTokenCommand(original));

        Assert.True(rotated.IsSuccess);
        Assert.NotEqual(original, rotated.Value!.RefreshToken);

        var replay = await Send(new RefreshTokenCommand(original));
        Assert.True(replay.IsFailure);
        Assert.Equal("Unauthorized", replay.Error.Code);

        var secondRotation = await Send(new RefreshTokenCommand(rotated.Value.RefreshToken));
        Assert.True(secondRotation.IsSuccess);
    }

    [Fact]
    public async Task ConcurrentRefresh_OnlyOneRequestSucceeds()
    {
        var (_, _, email) = await ArrangeAsync();
        var login = await Send(new LoginCommand(email, "Admin@123"));
        var original = login.Value!.RefreshToken;

        Fixture.CurrentUser.Clear();
        var first = Send(new RefreshTokenCommand(original));
        var second = Send(new RefreshTokenCommand(original));
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.IsSuccess);
        Assert.Single(results, result => result.IsFailure);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_DoesNotDiscloseExistence()
    {
        Fixture.CurrentUser.Clear();

        var result = await Send(new ForgotPasswordCommand($"missing-{Guid.NewGuid():N}@test.local"));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.DevToken);
        Assert.Contains("if an account exists", result.Value.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ForgotPassword_NewRequestInvalidatesPreviousToken()
    {
        await ArrangeAsync();
        Fixture.CurrentUser.Clear();
        var email = $"reissue-{Guid.NewGuid():N}@test.local";
        await SeedTenantAsync($"Reissue {Guid.NewGuid():N}", $"reissue-{Guid.NewGuid():N}", email);

        var first = await Send(new ForgotPasswordCommand(email));
        var second = await Send(new ForgotPasswordCommand(email));

        var replay = await Send(new ResetPasswordCommand(email, first.Value!.DevToken!, "First@123"));
        Assert.True(replay.IsFailure);

        var valid = await Send(new ResetPasswordCommand(email, second.Value!.DevToken!, "Second@123"));
        Assert.True(valid.IsSuccess);
    }

    [Fact]
    public async Task ForgotPassword_IsRateLimitedPerEmail()
    {
        Fixture.CurrentUser.Clear();
        var email = $"throttle-{Guid.NewGuid():N}@test.local";
        await SeedTenantAsync($"Throttle {Guid.NewGuid():N}", $"throttle-{Guid.NewGuid():N}", email);

        var responses = new List<ForgotPasswordResponse>();
        for (var i = 0; i < 6; i++)
        {
            responses.Add((await Send(new ForgotPasswordCommand(email))).Value!);
        }

        Assert.Equal(5, responses.Count(response => response.DevToken is not null));
        Assert.All(responses, response => Assert.Contains("if an account exists", response.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PasswordReset_InvalidatesOtherOutstandingLinks()
    {
        await ArrangeAsync();
        Fixture.CurrentUser.Clear();
        var email = $"multi-{Guid.NewGuid():N}@test.local";
        await SeedTenantAsync($"Multi {Guid.NewGuid():N}", $"multi-{Guid.NewGuid():N}", email);

        var first = await Send(new ForgotPasswordCommand(email));
        // The second request invalidates the first one at issue time.
        var second = await Send(new ForgotPasswordCommand(email));

        Assert.False(string.IsNullOrWhiteSpace(first.Value!.DevToken));
        Assert.False(string.IsNullOrWhiteSpace(second.Value!.DevToken));

        // Issuing the second link invalidated the first.
        var stale = await Send(new ResetPasswordCommand(email, first.Value.DevToken!, "Stale@123"));
        Assert.True(stale.IsFailure);

        var reset = await Send(new ResetPasswordCommand(email, second.Value!.DevToken!, "Changed@123"));
        Assert.True(reset.IsSuccess);

        Fixture.CurrentUser.Clear();
        var reuse = await Send(new ResetPasswordCommand(email, second.Value.DevToken!, "Again@123"));
        Assert.True(reuse.IsFailure);
    }

    [Fact]
    public async Task ForgotPassword_EmailFailure_RollsBackTokenChanges()
    {
        await ArrangeAsync();
        Fixture.CurrentUser.Clear();
        var email = $"rollback-{Guid.NewGuid():N}@test.local";
        await SeedTenantAsync($"Rollback {Guid.NewGuid():N}", $"rollback-{Guid.NewGuid():N}", email);

        var first = await Send(new ForgotPasswordCommand(email));
        var firstToken = first.Value!.DevToken!;

        // A second request whose email delivery fails must not consume the
        // previous still-valid link.
        var provider = Fixture.CreateServices(new ThrowingEmailSender());
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new ForgotPasswordCommand(email)));

        var reset = await Send(new ResetPasswordCommand(email, firstToken, "Recovered@123"));
        Assert.True(reset.IsSuccess);
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("SMTP unavailable.");
    }

    [Fact]
    public async Task PasswordReset_ChangesPasswordAndRevokesExistingSessions()
    {
        var (_, _, email) = await ArrangeAsync();
        var login = await Send(new LoginCommand(email, "Admin@123"));
        var refreshToken = login.Value!.RefreshToken;

        Fixture.CurrentUser.Clear();
        var forgot = await Send(new ForgotPasswordCommand(email));
        Assert.True(forgot.IsSuccess);
        var devToken = forgot.Value!.DevToken;
        Assert.False(string.IsNullOrWhiteSpace(devToken));

        var reset = await Send(new ResetPasswordCommand(email, devToken!, "NewAdmin@123"));
        Assert.True(reset.IsSuccess);

        var oldPassword = await Send(new LoginCommand(email, "Admin@123"));
        Assert.True(oldPassword.IsFailure);

        var newPassword = await Send(new LoginCommand(email, "NewAdmin@123"));
        Assert.True(newPassword.IsSuccess);

        var staleRefresh = await Send(new RefreshTokenCommand(refreshToken));
        Assert.True(staleRefresh.IsFailure);
        Assert.Equal("Unauthorized", staleRefresh.Error.Code);

        var reusedResetToken = await Send(new ResetPasswordCommand(email, devToken!, "Another@123"));
        Assert.True(reusedResetToken.IsFailure);
    }
}
