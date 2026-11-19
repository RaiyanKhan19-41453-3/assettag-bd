using AssetTagBD.Application;
using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Infrastructure;
using AssetTagBD.Infrastructure.Identity;
using AssetTagBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AssetTagBD.Integration.Tests;

/// <summary>
/// One PostgreSQL container for the whole test collection. Each test uses its
/// own DI scope and unique data, so tests stay fast without sharing state.
/// </summary>
public sealed class TestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("assettagbd_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public TestCurrentUser CurrentUser { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Services = CreateServices();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Database.MigrateAsync();
    }

    /// <summary>Build a provider against the same database, optionally swapping a service.</summary>
    public IServiceProvider CreateServices(IEmailSender? emailSender = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructureForTests(_postgres.GetConnectionString());
        services.AddSingleton<ICurrentUserService>(CurrentUser);

        if (emailSender is not null)
        {
            services.AddScoped<IEmailSender>(_ => emailSender);
        }

        return services.BuildServiceProvider();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}

internal static class TestServiceRegistration
{
    public static IServiceCollection AddInfrastructureForTests(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITenantRepository, Infrastructure.Persistence.Repositories.TenantRepository>();
        services.AddScoped<IAssetRepository, Infrastructure.Persistence.Repositories.AssetRepository>();
        services.AddScoped<IUserRepository, Infrastructure.Persistence.Repositories.UserRepository>();
        services.AddScoped<IAssignmentRepository, Infrastructure.Persistence.Repositories.AssignmentRepository>();
        services.AddScoped<IMaintenanceLogRepository, Infrastructure.Persistence.Repositories.MaintenanceLogRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IQrCodeService, Infrastructure.Services.QrCodeService>();
        services.AddScoped<IExcelAssetReader, Infrastructure.Services.ExcelAssetReader>();
        services.AddScoped<IAssetTagGenerator, Infrastructure.Services.AssetTagGenerator>();
        services.AddScoped<IRefreshTokenRepository, Infrastructure.Persistence.Repositories.RefreshTokenRepository>();
        services.AddScoped<IPasswordResetRepository, Infrastructure.Persistence.Repositories.PasswordResetRepository>();
        services.AddScoped<IRefreshTokenService, Infrastructure.Services.RefreshTokenService>();
        services.AddScoped<IAuditWriter, Infrastructure.Services.AuditWriter>();
        services.AddScoped<IAuditRepository, Infrastructure.Persistence.Repositories.AuditRepository>();
        services.AddScoped<IEmailSender, Infrastructure.Services.LoggingEmailSender>();
        services.AddSingleton<IAuthSettings>(new AuthSettings(ExposeResetTokenInResponse: true));
        services.Configure<JwtSettings>(settings =>
        {
            settings.Secret = "integration-test-secret-at-least-32-characters-long";
            settings.Issuer = "AssetTagBD";
            settings.Audience = "AssetTagBD";
            settings.ExpiryMinutes = 60;
        });
        return services;
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseCollection : ICollectionFixture<TestFixture>
{
    public const string Name = "Database collection";
}
