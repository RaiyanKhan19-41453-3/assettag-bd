using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AssetTagBD.Integration.Tests;

[Collection(HttpCollection.Name)]
public sealed class HttpPipelineTests
{
    private readonly HttpTestFixture _fixture;

    public HttpPipelineTests(HttpTestFixture fixture)
    {
        _fixture = fixture;
    }

    private static StringContent Json(object payload) =>
        new(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

    private async Task<HttpResponseMessage> Post(string url, object payload, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = Json(payload) };
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _fixture.Client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> Get(string url, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _fixture.Client.SendAsync(request);
    }

    private async Task<string> Login(string email, string password)
    {
        var response = await Post("/api/auth/login", new { email, password });
        var body = await response.Content.ReadFromJsonAsync<AuthPayload>();
        return body!.Token;
    }

    private sealed record AuthPayload(string Token, string RefreshToken);

    [Fact]
    public async Task Anonymous_AssetList_Returns401()
    {
        var response = await Get("/api/assets");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ThenMe_Returns200()
    {
        var token = await Login("http-admin@test.local", "Admin@123");
        var response = await Get("/api/auth/me", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CannotCreateAsset_ThroughHttp()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"http-emp-{suffix}@test.local";

        var signup = await Post("/api/auth/signup-company", new
        {
            companyName = $"Http Co {suffix}",
            adminFullName = "Http Admin",
            adminEmail = $"http-owner-{suffix}@test.local",
            password = "Admin@123"
        });
        Assert.Equal(HttpStatusCode.OK, signup.StatusCode);
        var adminToken = (await signup.Content.ReadFromJsonAsync<AuthPayload>())!.Token;

        var invite = await Post("/api/auth/register", new
        {
            fullName = "Http Employee",
            email,
            password = "Employee@123",
            role = "Employee"
        }, adminToken);
        Assert.Equal(HttpStatusCode.OK, invite.StatusCode);

        var employeeToken = await Login(email, "Employee@123");
        var create = await Post("/api/assets", new
        {
            name = "Nope",
            category = "Laptop",
            purchaseDate = "2025-01-01"
        }, employeeToken);

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task OldAccessToken_IsRejectedAfterPasswordReset()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"http-reset-{suffix}@test.local";
        await Post("/api/auth/signup-company", new
        {
            companyName = $"Reset Co {suffix}",
            adminFullName = "Reset Admin",
            adminEmail = email,
            password = "Admin@123"
        });

        var oldToken = await Login(email, "Admin@123");
        Assert.Equal(HttpStatusCode.OK, (await Get("/api/auth/me", oldToken)).StatusCode);

        var forgot = await Post("/api/auth/forgot-password", new { email });
        var forgotBody = await forgot.Content.ReadFromJsonAsync<ForgotPayload>();
        Assert.False(string.IsNullOrWhiteSpace(forgotBody!.DevToken));

        var reset = await Post("/api/auth/reset-password", new
        {
            email,
            token = forgotBody.DevToken,
            newPassword = "Changed@123"
        });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // The JWT validation pipeline must reject the now-stale token.
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/auth/me", oldToken)).StatusCode);

        var newToken = await Login(email, "Changed@123");
        Assert.False(string.IsNullOrWhiteSpace(newToken));
        Assert.Equal(HttpStatusCode.OK, (await Get("/api/auth/me", newToken)).StatusCode);
    }

    private sealed record ForgotPayload(string Message, string? DevToken);

    [Fact]
    public async Task BatchTagLookup_UsesSingleRequest()
    {
        var token = await Login("http-admin@test.local", "Admin@123");

        var created = await Post("/api/assets", new
        {
            name = "Batch Http",
            category = "Laptop",
            purchaseDate = "2025-01-01"
        }, token);
        var asset = await created.Content.ReadFromJsonAsync<AssetPayload>();

        var response = await Post("/api/assets/by-tags", new { tags = new[] { asset!.AssetTag, "MISSING" } }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var assets = await response.Content.ReadFromJsonAsync<List<AssetPayload>>();
        Assert.Single(assets!);
    }

    [Fact]
    public async Task CrossTenant_AssetRead_Returns404()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        async Task<string> Signup(string name, string email)
        {
            var response = await Post("/api/auth/signup-company", new
            {
                companyName = name,
                adminFullName = "Admin",
                adminEmail = email,
                password = "Admin@123"
            });
            return (await response.Content.ReadFromJsonAsync<AuthPayload>())!.Token;
        }

        var tokenA = await Signup($"TenantA {suffix}", $"hta-{suffix}@test.local");
        var tokenB = await Signup($"TenantB {suffix}", $"htb-{suffix}@test.local");

        var created = await Post("/api/assets", new
        {
            name = "Secret A",
            category = "Laptop",
            purchaseDate = "2025-01-01"
        }, tokenA);
        var asset = await created.Content.ReadFromJsonAsync<AssetPayload>();

        Assert.Equal(HttpStatusCode.OK, (await Get($"/api/assets/{asset!.Id}", tokenA)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Get($"/api/assets/{asset.Id}", tokenB)).StatusCode);
    }

    private sealed record AssetPayload(Guid Id, string AssetTag);
}
