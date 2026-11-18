using AssetTagBD.Application.Common.Interfaces;
using QRCoder;

namespace AssetTagBD.Infrastructure.Services;

/// <summary>Generates a PNG QR code whose payload is the asset's unique tag.</summary>
public sealed class QrCodeService : IQrCodeService
{
    public string GenerateQrCodeBase64(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        var pngBytes = qrCode.GetGraphic(20);

        return Convert.ToBase64String(pngBytes);
    }
}
