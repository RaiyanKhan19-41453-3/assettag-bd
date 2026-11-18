using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Infrastructure.Configuration;
using AssetTagBD.Infrastructure.Identity;
using AssetTagBD.Infrastructure.Persistence;
using AssetTagBD.Infrastructure.Persistence.Repositories;
using AssetTagBD.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTagBD.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = SecretReader.Read(configuration, "ConnectionStrings:DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=assettagbd;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Repositories and the Unit of Work resolve the same scoped DbContext.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IMaintenanceLogRepository, MaintenanceLogRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IExcelAssetReader, ExcelAssetReader>();
        services.AddScoped<IAssetTagGenerator, AssetTagGenerator>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        var authSettings = new AuthSettings(configuration.GetValue<bool>("Auth:ExposeResetTokenInResponse"));
        services.AddSingleton<IAuthSettings>(authSettings);

        var smtpHost = configuration["Email:Smtp:Host"];
        if (!string.IsNullOrWhiteSpace(smtpHost))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            // Development fallback: logs the token only when dev exposure is enabled.
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        services.Configure<JwtSettings>(settings =>
        {
            configuration.GetSection(JwtSettings.SectionName).Bind(settings);

            // The API validates tokens with the same SecretReader-resolved value,
            // so secret files work for both signing and validation in production.
            settings.Secret = SecretReader.Read(configuration, "Jwt:Secret") ?? settings.Secret;
        });

        return services;
    }
}
