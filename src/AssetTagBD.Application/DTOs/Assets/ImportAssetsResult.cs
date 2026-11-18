namespace AssetTagBD.Application.DTOs.Assets;

public sealed record ImportAssetsResult(
    int Created,
    int Skipped,
    IReadOnlyList<string> Errors,
    IReadOnlyList<AssetDto> Assets);
