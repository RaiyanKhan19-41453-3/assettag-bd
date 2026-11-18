using AssetTagBD.Infrastructure;
using AssetTagBD.Infrastructure.Configuration;
using AssetTagBD.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class ConfigurationTests
{
    [Fact]
    public void SecretReader_ReadsFileWhenConfigured()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "secret-from-file-1234567890");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "secret-from-appsettings-1234567890",
                    ["Jwt:Secret_FILE"] = file
                })
                .Build();

            Assert.Equal("secret-from-file-1234567890", SecretReader.Read(configuration, "Jwt:Secret"));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void AddInfrastructure_BindsJwtSigningSecretFromFile()
    {
        // Regression: the API validated with the file secret but signed with the
        // appsettings default, which broke every authenticated request in prod.
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "file-signing-secret-longer-than-32-characters");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "appsettings-secret-longer-than-32-characters",
                    ["Jwt:Secret_FILE"] = file
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);
            using var provider = services.BuildServiceProvider();

            var secret = provider.GetRequiredService<IOptions<JwtSettings>>().Value.Secret;
            Assert.Equal("file-signing-secret-longer-than-32-characters", secret);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
