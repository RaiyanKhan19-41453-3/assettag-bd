using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace AssetTagBD.Integration.Tests;

/// <summary>Boots the real API pipeline (auth middleware included) against PostgreSQL.</summary>
public sealed class HttpTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("http_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());
            builder.UseSetting("Jwt:Secret", "http-integration-secret-at-least-32-characters");
            builder.UseSetting("Seed:AdminEmail", "http-admin@test.local");
            builder.UseSetting("Seed:AdminPassword", "Admin@123");
            builder.UseSetting("Auth:ExposeResetTokenInResponse", "true");
            builder.UseEnvironment("Development");
        });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HttpCollection : ICollectionFixture<HttpTestFixture>
{
    public const string Name = "Http collection";
}
