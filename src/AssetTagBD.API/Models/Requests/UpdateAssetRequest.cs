using AssetTagBD.Domain.Enums;

namespace AssetTagBD.API.Models.Requests;

/// <summary>
/// Request body for updates. The id comes from the route so a client cannot
/// overwrite it in the body.
/// </summary>
public sealed record UpdateAssetRequest(
    string Name,
    AssetCategory Category,
    string? SerialNumber,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    AssetStatus Status);
