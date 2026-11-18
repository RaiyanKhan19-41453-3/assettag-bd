namespace AssetTagBD.API.Models.Requests;

public sealed record GetAssetsByTagsRequest(IReadOnlyList<string> Tags);
