using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Infrastructure.Services;

/// <summary>Produces human-friendly, collision-checked asset tags.</summary>
public sealed class AssetTagGenerator : IAssetTagGenerator
{
    private static readonly Dictionary<AssetCategory, string> CategoryPrefixes = new()
    {
        [AssetCategory.Laptop] = "LAP",
        [AssetCategory.Monitor] = "MON",
        [AssetCategory.Phone] = "PHO",
        [AssetCategory.Furniture] = "FUR",
        [AssetCategory.Other] = "OTH"
    };

    private readonly IAssetRepository _assets;

    public AssetTagGenerator(IAssetRepository assets)
    {
        _assets = assets;
    }

    public async Task<string> GenerateAsync(AssetCategory category, CancellationToken cancellationToken = default)
    {
        var prefix = CategoryPrefixes.TryGetValue(category, out var value) ? value : "OTH";

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var candidate = $"ATBD-{prefix}-{DateTime.UtcNow.Year}-{suffix}";

            if (!await _assets.TagExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return $"ATBD-{prefix}-{Guid.NewGuid():N}";
    }
}
