using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;

namespace AssetTagBD.Application.DTOs.Assets;

public sealed record AssetDto(
    Guid Id,
    string AssetTag,
    string Name,
    AssetCategory Category,
    string? SerialNumber,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    AssetStatus Status,
    DateTime CreatedAt,
    string? CurrentAssigneeName,
    DateTime? AssignedDate,
    string? QrCodeBase64)
{
    public static AssetDto FromEntity(Asset asset, string? qrCodeBase64 = null)
    {
        var activeAssignment = asset.Assignments
            .FirstOrDefault(a => a.ReturnedDate is null);

        return new AssetDto(
            asset.Id,
            asset.AssetTag,
            asset.Name,
            asset.Category,
            asset.SerialNumber,
            asset.PurchaseDate,
            asset.WarrantyExpiryDate,
            asset.Status,
            asset.CreatedAt,
            activeAssignment?.AssignedToUser.FullName,
            activeAssignment?.AssignedDate,
            qrCodeBase64);
    }
}
