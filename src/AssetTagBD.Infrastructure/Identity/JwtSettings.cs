namespace AssetTagBD.Infrastructure.Identity;

/// <summary>Bound from the "Jwt" section of appsettings / environment variables.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "AssetTagBD";

    public string Audience { get; set; } = "AssetTagBD";

    public int ExpiryMinutes { get; set; } = 480;
}
