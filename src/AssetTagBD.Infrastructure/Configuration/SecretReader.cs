using Microsoft.Extensions.Configuration;

namespace AssetTagBD.Infrastructure.Configuration;

/// <summary>
/// Reads a setting from a mounted secret file when <c>KEY_FILE</c> is provided,
/// otherwise from normal configuration. This lets the same image work with
/// plain env vars locally and Docker secrets / Kubernetes volumes in production.
/// </summary>
public static class SecretReader
{
    public static string? Read(IConfiguration configuration, string key)
    {
        var envName = key.Replace(':', '_').ToUpperInvariant() + "_FILE";

        var filePath = configuration[$"{key}_FILE"]
            ?? configuration[$"{key}:File"]
            ?? Environment.GetEnvironmentVariable(envName);

        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            return File.ReadAllText(filePath).Trim();
        }

        return configuration[key];
    }
}
